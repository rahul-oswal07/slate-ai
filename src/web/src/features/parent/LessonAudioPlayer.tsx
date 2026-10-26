import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { clock } from './clock'
import type { ParentEvening } from './types'

/**
 * The evening message's optional lesson audio (T-62): one Play button plays the spoken AI disclosure and then the
 * lesson, each from this link's token. Carries the AI label (NFR-08) and the written words (FR-09).
 */
export function LessonAudioPlayer({ audio, teacherName }: { audio: NonNullable<ParentEvening['lessonAudio']>; teacherName: string }) {
  const { t } = useTranslation()
  const element = useRef<HTMLAudioElement | null>(null)
  const [index, setIndex] = useState(0)
  const [playing, setPlaying] = useState(false)
  const [failed, setFailed] = useState(false)

  useEffect(() => () => element.current?.pause(), [])

  function playFrom(i: number) {
    const segment = audio.segments[i]
    element.current?.pause()
    if (!segment) {
      element.current = null
      setPlaying(false)
      setIndex(0)
      return
    }
    const next = new Audio(segment.url)
    next.onended = () => playFrom(i + 1)
    next.onerror = () => {
      setFailed(true)
      setPlaying(false)
    }
    element.current = next
    setIndex(i)
    setFailed(false)
    setPlaying(true)
    void next.play().catch(() => {
      setFailed(true)
      setPlaying(false)
    })
  }

  function toggle() {
    if (playing) {
      element.current?.pause()
      setPlaying(false)
    } else if (element.current && !failed && !element.current.ended) {
      setPlaying(true)
      void element.current.play().catch(() => setPlaying(false))
    } else {
      playFrom(failed ? index : 0)
    }
  }

  return (
    <section aria-labelledby="lesson-audio-title" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={toggle}
          aria-pressed={playing}
          aria-label={playing ? t('parent.listen.pause') : t('parent.evening.lesson.play')}
          className="btn-hero flex size-14 shrink-0 items-center justify-center rounded-full"
        >
          <Icon name={playing ? 'pause' : 'play'} size={26} />
        </button>
        <div className="min-w-0">
          <h2 id="lesson-audio-title" className="text-body font-bold">
            {t('parent.evening.lesson.title')} · {clock(audio.totalSeconds)}
          </h2>
          <p className="text-caption text-ink-muted">{t('parent.aiLabel', { teacher: teacherName })}</p>
        </div>
      </div>
      <ol className="flex flex-wrap gap-x-2 text-caption text-ink-muted" aria-label={t('parent.listen.parts')}>
        {audio.segments.map((segment, i) => (
          <li key={segment.url} aria-current={playing && i === index ? 'true' : undefined} className={playing && i === index ? 'font-bold text-primary-strong' : ''}>
            {i > 0 && <span aria-hidden="true">· </span>}
            {t(`parent.listen.seg.${segment.kind}`)}
          </li>
        ))}
      </ol>
      {failed && (
        <p role="alert" className="text-caption text-error">
          {t('parent.listen.error')}
        </p>
      )}
      <details>
        <summary className="cursor-pointer text-caption font-bold text-primary-strong">{t('parent.listen.readWords')}</summary>
        <p className="mt-1 text-body">{audio.disclosureText}</p>
        {audio.transcript && <p className="mt-2 text-body">{audio.transcript}</p>}
      </details>
    </section>
  )
}
