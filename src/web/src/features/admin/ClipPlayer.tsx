import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { fetchAdminClip } from './useAdmin'

/** Play → fetch the clip with the admin key → an `<audio>` player. The object URL is freed when it goes. */
export function ClipPlayer({
  adminKey: key,
  url,
  playLabel,
  playerLabel,
}: {
  adminKey: string
  url: string
  playLabel: string
  playerLabel: string
}) {
  const { t } = useTranslation()
  const [src, setSrc] = useState<string | null>(null)
  const [state, setState] = useState<'idle' | 'loading' | 'failed'>('idle')
  useEffect(() => () => {
    if (src) {
      URL.revokeObjectURL(src)
    }
  }, [src])

  async function play() {
    setState('loading')
    try {
      setSrc(URL.createObjectURL(await fetchAdminClip(key, url)))
      setState('idle')
    } catch {
      setState('failed')
    }
  }

  return (
    <>
      {src ? (
        <audio src={src} controls autoPlay className="mt-2 w-full" aria-label={playerLabel}>
          <track kind="captions" />
        </audio>
      ) : (
        <button
          type="button"
          onClick={() => void play()}
          disabled={state === 'loading'}
          aria-label={playLabel}
          className="mt-2 min-h-target w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
        >
          {t(state === 'loading' ? 'admin.loadingAudio' : 'admin.play')}
        </button>
      )}
      {state === 'failed' && (
        <p role="alert" className="mt-2 text-caption text-error">
          {t('admin.audioFailed')}
        </p>
      )}
    </>
  )
}
