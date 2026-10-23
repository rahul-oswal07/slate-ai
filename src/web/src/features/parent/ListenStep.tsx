import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { listenLabelKey, type ListenState } from './listenLabel'
import { PrimaryButton } from './ParentParts'
import type { ParentCatchUp } from './types'

function clock(seconds: number) {
  const s = Math.max(0, Math.round(seconds))
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
}

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
      <p className="flex items-center justify-center gap-2 text-body">
        <span
          aria-hidden="true"
          className="flex size-target items-center justify-center rounded-full bg-panel text-label text-primary-strong"
        >
          {page.teacherName.slice(0, 1)}
        </span>
        {t('parent.from', { teacher: page.teacherName })}
      </p>

      {segments.length > 0 && (
        <div className="flex flex-col items-center gap-3">
          <button
            type="button"
            onClick={toggle}
            aria-pressed={state === 'playing'}
            className="flex size-[156px] flex-col items-center justify-center gap-1 rounded-full bg-primary text-label text-on-primary"
          >
            <Icon
              name={state === 'loading' ? 'clock' : state === 'playing' ? 'pause' : 'play'}
              size={52}
            />
            {label}
          </button>
          <div className="w-full">
            <div aria-hidden="true" className="h-2 w-full overflow-hidden rounded-full border border-border bg-divider">
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
            <p role="alert" className="w-full border-l-4 border-l-warning bg-surface p-3 text-body text-ink">
              {t('parent.listen.error')}
            </p>
          )}
        </div>
      )}

      {page.transcript && (
        <details className="rounded-md border border-border bg-surface">
          <summary className="flex min-h-target cursor-pointer items-center px-4 text-label">
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
