import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { clock } from './clock'
import { listenLabelKey, type ListenState } from './listenLabel'
import { PrimaryButton } from './ParentParts'
import type { ParentCatchUp } from './types'

/** 34 bars, 8–40 px: a drawing of "sound", not a measurement. The same every time. */
const BARS = Array.from({ length: 34 }, (_, i) => 8 + Math.round(32 * Math.abs(Math.sin(i * 1.7) * Math.cos(i * 0.6))))

/**
 * P1 Listen: one big button plays the segments in order (greeting → computer voice → lesson, FR-09), with elapsed /
 * total time and the part playing highlighted. Next works before, during and after playback (C-05, D-08).
 */
export function ListenStep({ page, onNext }: { page: ParentCatchUp; onNext: () => void }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const segments = page.audio?.segments ?? []
  const total = page.audio?.totalSeconds ?? 0
  const offsets = segments.map((_, i) => segments.slice(0, i).reduce((sum, s) => sum + s.seconds, 0))
  const audio = useRef<HTMLAudioElement | null>(null)
  const [state, setState] = useState<ListenState>('idle')
  const [index, setIndex] = useState(0)
  const [elapsed, setElapsed] = useState(0)
  const [failed, setFailed] = useState(false)

  useEffect(() => () => audio.current?.pause(), [])

  function stopWithError() {
    setFailed(true)
    setState((current) => (current === 'idle' ? 'idle' : 'paused'))
  }

  function playFrom(i: number) {
    const segment = segments[i]
    audio.current?.pause()
    if (!segment) {
      audio.current = null
      setState('ended')
      setIndex(0)
      setElapsed(total)
      return
    }
    const element = new Audio(segment.url)
    element.onplaying = () => setState('playing')
    element.ontimeupdate = () => setElapsed((offsets[i] ?? 0) + element.currentTime)
    element.onended = () => playFrom(i + 1)
    element.onerror = stopWithError
    audio.current = element
    setIndex(i)
    setFailed(false)
    setState((current) => (current === 'playing' ? 'playing' : 'loading'))
    void element.play().catch(stopWithError)
  }

  function toggle() {
    const element = audio.current
    if (state === 'playing' || state === 'loading') {
      element?.pause()
      setState('paused')
      announce(t('parent.announce.paused'))
    } else if (state === 'paused' && element && !failed) {
      setState('playing')
      announce(t('parent.announce.playing'))
      void element.play().catch(stopWithError)
    } else {
      // Start, Listen again, or Resume after a failed load: (re)start the current part.
      announce(t('parent.announce.playing'))
      playFrom(state === 'ended' ? 0 : index)
    }
  }

  const hasGreeting = segments.some((s) => s.kind === 'greeting')
  const progress = total > 0 ? Math.min(100, (elapsed / total) * 100) : 0
  const label = t(listenLabelKey(state))

  return (
    <div className="flex flex-col gap-5">
      <p className="flex items-center justify-center gap-3 text-body-lg font-bold">
        <span
          aria-hidden="true"
          className="flex size-target items-center justify-center rounded-full border-2 border-ink bg-accent text-label font-extrabold text-on-accent"
        >
          {page.teacherName.slice(0, 1)}
        </span>
        {t('parent.from', { teacher: page.teacherName })}
      </p>

      {segments.length > 0 && (
        <div className="flex flex-col items-center gap-3">
          <div className="relative flex size-[184px] items-center justify-center rounded-full bg-panel">
            <button
              type="button"
              onClick={toggle}
              aria-pressed={state === 'playing'}
              className={`btn-hero size-[156px] flex-col gap-1 text-label font-extrabold shadow-[0_8px_0_-2px_rgb(0_0_0/0.14)] ${
                state === 'playing' ? 'breathing' : ''
              }`}
            >
              <Icon name={state === 'loading' ? 'clock' : state === 'playing' ? 'pause' : 'play'} size={56} />
              {label}
            </button>
          </div>
          {/* Decorative: a drawing of sound. Played bars are ink, the rest chalk; it moves only while playing. */}
          <div aria-hidden="true" className="flex h-10 items-center justify-center gap-[3px]">
            {BARS.map((height, i) => (
              <span
                key={i}
                style={{ height, animationDelay: `${(i % 6) * 50}ms` }}
                className={`w-[5px] rounded-pill ${i < Math.round((BARS.length * progress) / 100) ? 'bg-ink' : 'bg-chalk'} ${
                  state === 'playing' ? 'wave-bar' : ''
                }`}
              />
            ))}
          </div>
          <div className="w-full">
            <div aria-hidden="true" className="h-2 w-full overflow-hidden rounded-pill border border-border bg-divider">
              <div className="h-full bg-primary" style={{ width: `${progress}%` }} />
            </div>
            <p className="mt-1 flex justify-between text-caption text-ink-muted">
              <span>{clock(elapsed)}</span>
              <span>{clock(total)}</span>
            </p>
          </div>
          <ol aria-label={t('parent.listen.parts')} className="flex flex-wrap justify-center gap-x-2 text-caption">
            {segments.map((segment, i) => (
              <li
                key={segment.url}
                aria-current={state !== 'idle' && state !== 'ended' && i === index ? 'step' : undefined}
                className={
                  state !== 'idle' && state !== 'ended' && i === index ? 'font-semibold text-ink' : 'text-ink-muted'
                }
              >
                {i > 0 && <span aria-hidden="true">· </span>}
                {t(`parent.listen.seg.${segment.kind}`)}
              </li>
            ))}
          </ol>
          {failed && (
            <p role="alert" className="w-full rounded-md border-l-4 border-l-warning bg-surface p-3 text-body text-ink">
              {t('parent.listen.error')}
            </p>
          )}
        </div>
      )}

      {page.transcript && (
        <details className="rounded-button border-2 border-primary bg-panel">
          <summary className="flex min-h-target cursor-pointer items-center justify-center px-4 text-label font-bold text-primary-strong">
            {t('parent.listen.readWords')}
          </summary>
          <div className="flex flex-col gap-3 px-4 pb-4 text-body">
            {hasGreeting && <p>{t('parent.listen.greetingLine', { teacher: page.teacherName })}</p>}
            {page.transcript.disclosureText && <p lang={page.language}>{page.transcript.disclosureText}</p>}
            <p lang={page.language} className="whitespace-pre-line">
              {page.transcript.script}
            </p>
            {page.transcript.keyWordsKn.length > 0 && (
              <p>
                <span className="text-caption text-ink-muted">{t('parent.listen.keyWords')}: </span>
                <span lang="kn">{page.transcript.keyWordsKn.join(' · ')}</span>
              </p>
            )}
          </div>
        </details>
      )}

      <PrimaryButton onClick={onNext}>{t('parent.next')}</PrimaryButton>
    </div>
  )
}
