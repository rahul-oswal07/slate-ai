import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { duration } from './packageText'
import type { AudioSegment } from './types'

/**
 * Plays a catch-up's segments in order, as the family will hear them (D-18, D-58): her greeting (when recorded) →
 * computer voice (the AI disclosure) → the lesson. One Play/Pause button; the part playing is highlighted.
 */
export function SegmentPlayer({ segments }: { segments: AudioSegment[] }) {
  const { t } = useTranslation()
  const audio = useRef<HTMLAudioElement | null>(null)
  const [index, setIndex] = useState(0)
  const [playing, setPlaying] = useState(false)

  useEffect(() => () => audio.current?.pause(), [])

  function playFrom(i: number) {
    const segment = segments[i]
    if (!segment) {
      setPlaying(false)
      setIndex(0)
      return
    }
    audio.current?.pause()
    const element = new Audio(segment.url)
    element.onended = () => playFrom(i + 1)
    audio.current = element
    setIndex(i)
    setPlaying(true)
    void element.play().catch(() => setPlaying(false))
  }

  function toggle() {
    if (playing) {
      audio.current?.pause()
      setPlaying(false)
    } else if (audio.current && audio.current.currentTime > 0 && !audio.current.ended) {
      setPlaying(true)
      void audio.current.play().catch(() => setPlaying(false))
    } else {
      playFrom(index)
    }
  }

  return (
    <div className="flex items-center gap-3 rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
      <button
        type="button"
        onClick={toggle}
        aria-label={playing ? t('approve.player.pause') : t('approve.player.play')}
        className="flex size-[52px] shrink-0 items-center justify-center rounded-full bg-primary text-on-primary"
      >
        <Icon name={playing ? 'pause' : 'play'} size={24} />
      </button>
      <ol className="flex flex-wrap items-center gap-x-2 gap-y-1 text-caption" aria-label={t('approve.player.parts')}>
        {segments.map((segment, i) => (
          <li
            key={segment.url}
            aria-current={playing && i === index ? 'true' : undefined}
            className={playing && i === index ? 'font-bold text-primary-strong' : 'text-ink-muted'}
          >
            {i > 0 && <span aria-hidden="true">· </span>}
            {t(`approve.player.${segment.kind}`)}
            {segment.kind === 'script' ? ` · ${duration(segment.seconds)}` : ''}
          </li>
        ))}
      </ol>
    </div>
  )
}
