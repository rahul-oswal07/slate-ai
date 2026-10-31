import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { api, NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { saveCachedMe, type Me } from '../auth/session'
import { meQueryKey, useMe } from '../auth/useMe'
import { useRecorder } from '../lessons/useRecorder'
import { recordingToWav } from './wav'

/** A greeting is about ten seconds; the recorder stops itself there (T0 card, FR-09). */
export const GREETING_MAX_MS = 10_000

/**
 * T0 step 1 (docs/design/T0-setup.md, states `1`, `1rec`, `1done`, `1err`, `1deny`): she records one Kannada greeting,
 * plays it back, can record again, and ticks the voice consent. Saving sets `Teacher.ConsentAt`. With the microphone
 * blocked she continues without a greeting (D-34) and records it later from Today.
 *
 * `welcome` is the first run (Welcome → here → evening-message step); `settings` re-records it from Settings and goes
 * back there (T-71); `later` is reached from Today by a teacher
 * who is already set up. Online only for now: the offline state is roadmap (D-70).
 */
export function GreetingScreen({ mode }: { mode: 'welcome' | 'later' | 'settings' }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const announce = useAnnounce()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const next = mode === 'welcome' ? '/setup/evening' : mode === 'settings' ? '/settings' : '/'

  const [recording, setRecording] = useState<Blob | null>(null)
  const [consent, setConsent] = useState(false)
  const [missing, setMissing] = useState<'noGreeting' | 'noConsent' | null>(null)
  const [playing, setPlaying] = useState(false)
  // One ID per recording, so a retried Save is the same request and a new recording is a new one.
  const requestId = useRef(crypto.randomUUID())
  const player = useRef<HTMLAudioElement | null>(null)

  const recorder = useRecorder(
    (note) => {
      requestId.current = crypto.randomUUID()
      setRecording(note)
      setMissing(null)
    },
    { maxMs: GREETING_MAX_MS },
  )

  useEffect(() => {
    if (recorder.status === 'recording') {
      announce(t('announce.recording'))
    }
  }, [recorder.status, announce, t])

  useEffect(() => () => player.current?.pause(), [])

  function finish(updated: Me) {
    saveCachedMe(updated)
    // Navigate first: once `me` says setup is complete, the welcome route's gate must not win over the next step.
    void navigate(next, { replace: true })
    queryClient.setQueryData(meQueryKey, updated)
  }

  const save = useMutation({
    mutationFn: async (note: Blob) => {
      const form = new FormData()
      form.append('file', await recordingToWav(note), 'greeting.wav')
      form.append('clientRequestId', requestId.current)
      form.append('consent', 'true')
      return api<Me>('/api/me/greeting', { method: 'POST', form })
    },
    onSuccess: (updated) => {
      announce(t('announce.greetingSaved'))
      finish(updated)
    },
  })

  const withoutGreeting = useMutation({
    mutationFn: () => api<Me>('/api/me/welcome', { method: 'POST', body: { clientRequestId: requestId.current } }),
    onSuccess: finish,
  })

  function onSave() {
    if (!recording) {
      setMissing('noGreeting')
    } else if (!consent) {
      setMissing('noConsent')
    } else {
      setMissing(null)
      save.mutate(recording)
    }
  }

  function togglePlay() {
    if (!recording) {
      return
    }
    if (playing) {
      player.current?.pause()
      setPlaying(false)
      return
    }
    player.current?.pause()
    const audio = new Audio(URL.createObjectURL(recording))
    audio.onended = () => setPlaying(false)
    player.current = audio
    setPlaying(true)
    void audio.play().catch(() => setPlaying(false))
  }

  function again() {
    player.current?.pause()
    setPlaying(false)
    setRecording(null)
    recorder.reset()
  }

  const isRecording = recorder.status === 'recording'
  const blocked = recorder.status === 'blocked' && !recording
  const seconds = Math.min(Math.floor(recorder.elapsedMs / 1000), GREETING_MAX_MS / 1000)
  const busy = save.isPending || withoutGreeting.isPending
  const failure = save.error ?? withoutGreeting.error

  return (
    <section aria-labelledby="greeting-title" className="mx-auto flex max-w-md flex-col gap-3">
      {mode === 'welcome' && <p className="mt-4 text-caption text-ink-muted">{t('setup.step1.progress')}</p>}
      <h1 id="greeting-title" ref={headingRef} tabIndex={-1} className={`text-title ${mode === 'welcome' ? '' : 'mt-4'}`}>
        {t('setup.greeting.heading')}
      </h1>
      <p className="text-body text-ink-muted">{t('setup.greeting.lead')}</p>

      <div className="card p-[14px]">
        <p className="text-caption text-ink-muted">{t('setup.greeting.suggestLabel')}</p>
        <p className="mt-1 text-body">
          {t('setup.greeting.suggestion', { teacher: me.data?.displayName ?? '' })}
        </p>
      </div>

      {blocked ? (
        <div className="card p-[14px]">
          <p className="flex items-start gap-2 text-body">
            <Icon name="alert" className="mt-0.5 shrink-0 text-warning" />
            {t('setup.greeting.micDenied')}
          </p>
          <button type="button" onClick={() => void recorder.start()} className="btn-link mt-1">
            {t('mic.openSettings')}
          </button>
        </div>
      ) : recording ? (
        <div className="card p-[14px]">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={togglePlay}
              aria-pressed={playing}
              aria-label={t('setup.greeting.play')}
              className="flex size-12 shrink-0 items-center justify-center rounded-full bg-primary text-on-primary"
            >
              <Icon name={playing ? 'pause' : 'play'} size={22} />
            </button>
            <p className="text-body">{t('setup.greeting.recorded')}</p>
          </div>
          <button type="button" onClick={again} className="btn-link mt-2">
            {t('setup.greeting.again')}
          </button>
        </div>
      ) : (
        <div className="text-center">
          <div className="relative mx-auto flex size-[132px] items-center justify-center rounded-full bg-panel">
            <span aria-hidden="true" className="pointer-events-none absolute inset-2 rounded-full border-[2.5px] border-dashed border-chalk" />
            <button
              type="button"
              aria-pressed={isRecording}
              aria-label={isRecording ? t('mic.stop') : t('mic.record')}
              {...recorder.buttonHandlers}
              className={`relative flex size-[96px] touch-none select-none items-center justify-center rounded-full ${
                isRecording
                  ? 'breathing border-[3px] border-transparent bg-error text-on-error'
                  : 'btn-hero shadow-[0_8px_0_-2px_rgb(0_0_0/0.14)]'
              }`}
            >
              {isRecording ? <span aria-hidden="true" className="size-7 rounded-sm bg-on-error" /> : <Icon name="mic" size={40} />}
            </button>
          </div>
          {isRecording ? (
            <>
              <p className="mt-2 text-title" aria-hidden="true">
                {t('mic.timerGreeting', { ss: String(seconds).padStart(2, '0') })}
              </p>
              <p className="text-caption text-ink-muted">{t('mic.recHint')}</p>
            </>
          ) : (
            <p className="mt-2 text-caption text-ink-muted">{t('setup.greeting.idleHint')}</p>
          )}
          {recorder.status === 'tooShort' && (
            <p role="alert" className="mt-2 text-caption text-error">
              {t('today.lesson.tooShort')}
            </p>
          )}
          {recorder.status === 'cancelled' && (
            <output className="mt-2 block text-caption text-ink-muted">{t('announce.recCancelled')}</output>
          )}
        </div>
      )}

      <div className="mt-2 border-t border-divider pb-3 pt-3">
        {!blocked && (
          <label className="mb-3 flex items-start gap-3 text-caption">
            <input
              type="checkbox"
              checked={consent}
              onChange={(event) => {
                setConsent(event.target.checked)
                setMissing(null)
              }}
              className="mt-0.5 size-6 shrink-0 accent-primary"
            />
            <span>{t('setup.consent.voice')}</span>
          </label>
        )}
        {(missing || failure) && (
          <p role="alert" className="mb-2 flex items-start gap-2 text-caption text-error">
            <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
            {missing
              ? t(`setup.error.${missing}`)
              : failure instanceof NetworkError
                ? t('setup.greeting.needsNetwork')
                : t('setup.greeting.error')}
          </p>
        )}
        {blocked ? (
          <button
            type="button"
            onClick={() => (mode === 'welcome' ? withoutGreeting.mutate() : void navigate(next))}
            disabled={busy}
            className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
          >
            {t('setup.greeting.continueWithout')}
          </button>
        ) : (
          <button
            type="button"
            onClick={onSave}
            disabled={busy}
            className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
          >
            {t('setup.greeting.save')}
          </button>
        )}
        {mode !== 'welcome' && (
          <Link to={next} className="btn-link mt-1 block text-center">
            {t('setup.greeting.notNow')}
          </Link>
        )}
      </div>
    </section>
  )
}
