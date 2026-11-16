import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import { useMessageTemplate } from '../evening/useEvening'
import { useWeeklyStarWording } from '../star/useWeeklyStar'

/**
 * T10 Settings, MVP part (T-71, D-86): opened from the gear on Today. Her greeting (play it, record it again) and the
 * evening message (approved or not, review it). Families, Help, app language and withdrawing voice consent are roadmap.
 */
export function SettingsScreen() {
  const { t } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()

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
