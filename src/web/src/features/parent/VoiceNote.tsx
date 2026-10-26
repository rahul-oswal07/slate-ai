import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'

/**
 * The teacher's own greeting as a voice-note row (P0 evening message, the demo chat): a play button and
 * with a ready-made label ("{teacher} teacher's voice · 0:09"). `url` is the family's token URL, never a
 * teacher-session one.
 */
export function VoiceNote({ url, label }: { url: string; label: string }) {
  const { t } = useTranslation()
  const [playing, setPlaying] = useState(false)
  const audio = useRef<HTMLAudioElement | null>(null)

  useEffect(() => () => audio.current?.pause(), [])

  function toggle() {
    if (playing) {
      audio.current?.pause()
      setPlaying(false)
      return
    }
    audio.current ??= new Audio(url)
    audio.current.onended = () => setPlaying(false)
    setPlaying(true)
    void audio.current.play().catch(() => setPlaying(false))
  }

  return (
    <div className="card flex items-center gap-3 p-[14px]">
      <button
        type="button"
        onClick={toggle}
        aria-pressed={playing}
        aria-label={t('parent.evening.play')}
        className="btn-hero flex size-12 shrink-0 items-center justify-center rounded-full"
      >
        <Icon name={playing ? 'pause' : 'play'} size={22} />
      </button>
      <p className="text-body font-bold">{label}</p>
    </div>
  )
}
