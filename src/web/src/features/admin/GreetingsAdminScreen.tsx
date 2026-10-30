import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { ApiError } from '../../api/client'
import { type AdminGreeting, adminKey, fetchGreetingAudio, useAdminGreetings } from './useAdminGreetings'

/** "28 Oct 2026, 2:30 pm", in the UI language and school time. */
function recorded(at: string, language: string) {
  return new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Kolkata',
  }).format(new Date(at))
}

/**
 * /admin/greetings#key=… (T-66, D-83): every teacher's recorded greeting, to listen to. Linked from nowhere, no
 * teacher sign-in; the key in the link is the only way in.
 */
export function GreetingsAdminScreen() {
  const { t } = useTranslation()
  const key = adminKey(useLocation().hash)
  const greetings = useAdminGreetings(key)

  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col gap-4 bg-canvas px-4 py-6 text-ink">
      <h1 className="text-title">{t('admin.greetings.title')}</h1>
      {key === null || (greetings.error instanceof ApiError && greetings.error.status === 404) ? (
        <p role="alert" className="border-l-4 border-l-warning bg-surface p-3 text-body">
          {t('admin.greetings.needsLink')}
        </p>
      ) : greetings.isError ? (
        <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body">
          {t('admin.greetings.failed')}
          <button type="button" onClick={() => void greetings.refetch()} className="btn-secondary w-auto self-start">
            {t('common.tryAgain')}
          </button>
        </div>
      ) : !greetings.data ? (
        <output className="block text-body text-ink-muted">{t('admin.greetings.loading')}</output>
      ) : greetings.data.length === 0 ? (
        <p className="text-body text-ink-muted">{t('admin.greetings.empty')}</p>
      ) : (
        <>
          <p className="text-body text-ink-muted">{t('admin.greetings.count', { count: greetings.data.length })}</p>
          <ul className="flex flex-col gap-3">
            {greetings.data.map((greeting) => (
              <GreetingRow key={greeting.clipId} adminKey={key} greeting={greeting} />
            ))}
          </ul>
        </>
      )}
    </main>
  )
}

function GreetingRow({ adminKey: key, greeting }: { adminKey: string; greeting: AdminGreeting }) {
  const { t, i18n } = useTranslation()
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
      setSrc(URL.createObjectURL(await fetchGreetingAudio(key, greeting.clipId)))
      setState('idle')
    } catch {
      setState('failed')
    }
  }

  return (
    <li className="rounded-card border-[1.5px] border-edge bg-surface p-3">
      <p className="text-label">{greeting.teacherName}</p>
      <p className="text-caption text-ink-muted">
        {t('admin.greetings.meta', { seconds: greeting.seconds, date: recorded(greeting.recordedAt, i18n.language) })}
      </p>
      {src ? (
        <audio
          src={src}
          controls
          autoPlay
          className="mt-2 w-full"
          aria-label={t('admin.greetings.player', { name: greeting.teacherName })}
        >
          <track kind="captions" />
        </audio>
      ) : (
        <button
          type="button"
          onClick={() => void play()}
          disabled={state === 'loading'}
          aria-label={t('admin.greetings.play', { name: greeting.teacherName })}
          className="mt-2 min-h-target w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
        >
          {t(state === 'loading' ? 'admin.greetings.loadingAudio' : 'admin.greetings.playShort')}
        </button>
      )}
      {state === 'failed' && (
        <p role="alert" className="mt-2 text-caption text-error">
          {t('admin.greetings.audioFailed')}
        </p>
      )}
    </li>
  )
}
