import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { api } from '../../api/client'
import { Icon } from '../../components/Icon'
import { db } from '../../offline/db'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import { useMessageTemplate } from '../evening/useEvening'
import { useWeeklyStarWording } from '../star/useWeeklyStar'

/**
 * T10 Settings, MVP part (T-71, D-86): opened from the gear on Today. Her greeting (play it, record it again) and the
 * evening message (approved or not, review it). Families, Help, app language and withdrawing voice consent are roadmap.
 * In the demo class only, "Reset today" clears the day so the Today flow can be run again (T-143).
 */
export function SettingsScreen() {
  const { t } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  // The demo class only (T-143): a real class never sees Reset today.
  const demo = useMe().data?.demo != null

  return (
    <section aria-labelledby="settings-title" className="flex flex-col gap-3">
      <Link to="/" className="btn-link self-start">
        {t('settings.backToToday')}
      </Link>
      <h1 id="settings-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('settings.title')}
      </h1>
      <GreetingRow />
      <EveningRow />
      <StarRow />
      {demo && <ResetTodayRow />}
    </section>
  )
}

/**
 * "Reset today" (T-143), demo class only: after "Yes, reset today" the API deletes the school day's lesson, today's
 * message, lesson audio, tonight's messages with their answers and her register corrections, and puts the register back
 * as the simulated SATS sent it; earlier days, children and catch-ups stay. Every screen then reloads from the server.
 */
function ResetTodayRow() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [step, setStep] = useState<'idle' | 'confirm' | 'resetting' | 'done' | 'failed'>('idle')

  async function reset() {
    setStep('resetting')
    try {
      const { date } = await api<{ date: string }>('/api/demo/reset-today', {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      })
      // Her unsent note and photo for the day belonged to the lesson that is gone.
      await Promise.all([db.notes.delete(date), db.photos.delete(date)])
      await queryClient.invalidateQueries()
      setStep('done')
    } catch {
      setStep('failed')
    }
  }

  return (
    <section aria-labelledby="settings-reset" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-start gap-3">
        <Icon name="calendarX" className="mt-0.5 shrink-0 text-primary-strong" />
        <div className="min-w-0 flex-1">
          <h2 id="settings-reset" className="text-label">
            {t('settings.reset.title')}
          </h2>
          <p className="text-caption text-ink-muted">{t('settings.reset.hint')}</p>
        </div>
      </div>
      {step === 'confirm' || step === 'resetting' ? (
        <div className="flex flex-col gap-2">
          <p className="text-body">{t('settings.reset.confirm')}</p>
          <div className="flex items-center gap-3">
            <button type="button" disabled={step === 'resetting'} onClick={() => void reset()} className="btn-secondary w-auto">
              {t('settings.reset.yes')}
            </button>
            <button type="button" disabled={step === 'resetting'} onClick={() => setStep('idle')} className="btn-link min-h-target">
              {t('settings.reset.cancel')}
            </button>
          </div>
        </div>
      ) : (
        <button type="button" onClick={() => setStep('confirm')} className="btn-secondary flex items-center justify-center">
          {t('settings.reset.button')}
        </button>
      )}
      {step === 'done' && (
        <output className="text-caption text-ink-muted">
          {t('settings.reset.done')}{' '}
          <Link to="/" className="btn-link">
            {t('settings.backToToday')}
          </Link>
        </output>
      )}
      {step === 'failed' && (
        <p role="alert" className="text-caption text-error">
          {t('settings.reset.failed')}
        </p>
      )}
    </section>
  )
}

function GreetingRow() {
  const { t } = useTranslation()
  const greeting = useMe().data?.greeting ?? null
  const [playing, setPlaying] = useState(false)
  const player = useRef<HTMLAudioElement | null>(null)
  useEffect(() => () => player.current?.pause(), [])

  function togglePlay() {
    if (!greeting) {
      return
    }
    if (playing) {
      player.current?.pause()
      setPlaying(false)
      return
    }
    const audio = new Audio(greeting.url)
    audio.onended = () => setPlaying(false)
    player.current = audio
    setPlaying(true)
    void audio.play().catch(() => setPlaying(false))
  }

  return (
    <section aria-labelledby="settings-greeting" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-start gap-3">
        <Icon name="mic" className="mt-0.5 shrink-0 text-primary-strong" />
        <div className="min-w-0 flex-1">
          <h2 id="settings-greeting" className="text-label">
            {t('settings.greeting.title')}
          </h2>
          <p className="text-caption text-ink-muted">
            {greeting
              ? t('settings.greeting.recorded', { seconds: Math.round(greeting.seconds) })
              : t('settings.greeting.none')}
          </p>
        </div>
        {greeting && (
          <button
            type="button"
            onClick={togglePlay}
            aria-pressed={playing}
            aria-label={t('settings.greeting.play')}
            className="flex size-target shrink-0 items-center justify-center rounded-full bg-primary text-on-primary"
          >
            <Icon name={playing ? 'pause' : 'play'} size={22} />
          </button>
        )}
      </div>
      <Link to="/settings/greeting" className="btn-secondary flex items-center justify-center">
        {t(greeting ? 'settings.greeting.again' : 'settings.greeting.first')}
      </Link>
    </section>
  )
}

function EveningRow() {
  const { t, i18n } = useTranslation()
  const template = useMessageTemplate(useMe().data?.classId ?? '')
  const approved = template.data?.status === 'approved'
  const approvedAt = approved ? template.data?.approvedAt : null

  return (
    <section aria-labelledby="settings-evening" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-start gap-3">
        {/* Not approved: no evening message goes (guardrail 3), so the icon warns; the words say why. */}
        <Icon
          name={template.data && !approved ? 'alert' : 'messageCircle'}
          className={`mt-0.5 shrink-0 ${template.data && !approved ? 'text-warning' : 'text-primary-strong'}`}
        />
        <div className="min-w-0 flex-1">
          <h2 id="settings-evening" className="text-label">
            {t('settings.evening.title')}
          </h2>
          {template.data && (
            <p className={`text-caption ${approved ? 'text-ink-muted' : 'text-ink'}`}>
              {approved
                ? t('settings.evening.approved', {
                    date: approvedAt
                      ? new Intl.DateTimeFormat(i18n.language, { timeZone: 'Asia/Kolkata', day: 'numeric', month: 'short' })
                          .format(new Date(approvedAt))
                      : '',
                  })
                : t('settings.evening.needsApproval')}
            </p>
          )}
        </div>
      </div>
      <Link to="/settings/evening" className="btn-secondary flex items-center justify-center">
        {t('settings.evening.review')}
      </Link>
    </section>
  )
}

/** The weekly star families get (T-125, D-118): approved or not, and the way to review it. */
function StarRow() {
  const { t } = useTranslation()
  const wording = useWeeklyStarWording(useMe().data?.classId ?? '')
  const approved = wording.data?.approved === true

  return (
    <section aria-labelledby="settings-star" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-start gap-3">
        {/* Not approved: no star message goes (guardrail 3), so the icon warns; the words say why. */}
        <Icon
          name={wording.data && !approved ? 'alert' : 'star'}
          className={`mt-0.5 shrink-0 ${wording.data && !approved ? 'text-warning' : 'text-primary-strong'}`}
        />
        <div className="min-w-0 flex-1">
          <h2 id="settings-star" className="text-label">
            {t('settings.star.title')}
          </h2>
          {wording.data && (
            <p className={`text-caption ${approved ? 'text-ink-muted' : 'text-ink'}`}>
              {t(approved ? 'settings.star.approved' : 'settings.star.needsApproval')}
            </p>
          )}
        </div>
      </div>
      <Link to="/settings/star" className="btn-secondary flex items-center justify-center">
        {t('settings.star.review')}
      </Link>
    </section>
  )
}
