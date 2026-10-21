import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { formatSchoolTime } from './schoolTime'
import type { AttendanceDay } from './types'

const card = 'rounded-md border border-border bg-surface p-[14px]'
const textButton = 'mt-2 min-h-target w-full text-label text-primary-strong'

/**
 * The attendance part of T1 Today: where today's register came from and who is absent (FR-01, D-27, D-41).
 * Never says or implies anything was sent to SATS.
 */
export function AttendanceCard({ day, onEdit }: { day: AttendanceDay; onEdit: () => void }) {
  const { t, i18n } = useTranslation()
  const time = (value: string) => formatSchoolTime(value, i18n.language, day.date)

  if (day.status === 'waiting' || day.status === 'noSchool') {
    return (
      <section aria-labelledby="attendance-title" className={card}>
        <div className="flex items-start gap-3">
          <Icon name="clock" className="mt-0.5 shrink-0 text-ink-muted" />
          <div>
            <h2 id="attendance-title" className="text-label">
              {day.status === 'noSchool' ? t('today.attendance.noSchool') : t('today.attendance.waiting.title')}
            </h2>
            {day.status === 'waiting' && day.recheckAt && (
              <p className="mt-1 text-body text-ink-muted">
                {t('today.attendance.waiting.body', { time: time(day.recheckAt) })}
              </p>
            )}
          </div>
        </div>
        <button type="button" onClick={onEdit} className={textButton}>
          {t('today.attendance.waiting.markHere')}
        </button>
      </section>
    )
  }

  const names = day.children.filter((child) => day.absentChildIds.includes(child.id))
  const source =
    day.status === 'sats'
      ? t('today.attendance.source.sats', { time: day.receivedAt ? time(day.receivedAt) : '' })
      : t('today.attendance.source.manual')

  return (
    <section aria-labelledby="attendance-title" className={card}>
      <div className="flex items-start gap-3">
        <Icon name="checkCircle" className="mt-0.5 shrink-0 text-primary-strong" />
        <div className="min-w-0">
          <h2 id="attendance-title" className="text-caption text-ink-muted">
            {source}
            {day.corrected && ` · ${t('today.attendance.corrected')}`}
          </h2>
          {names.length === 0 ? (
            <p className="mt-1 text-title">{t('today.attendance.noneAbsent')}</p>
          ) : (
            <>
              <p className="mt-1 text-title">{t('today.attendance.absentCount', { n: names.length })}</p>
              <ul className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-body">
                {names.map((child) => (
                  <li key={child.id}>{child.firstName}</li>
                ))}
              </ul>
            </>
          )}
          {day.satsDiffers && day.receivedAt && (
            <p className="mt-2 text-caption text-ink-muted">
              {t('today.attendance.satsLater', { time: time(day.receivedAt) })}
            </p>
          )}
        </div>
      </div>
      <button type="button" onClick={onEdit} className={textButton}>
        {t('today.attendance.correctAction')}
      </button>
    </section>
  )
}
