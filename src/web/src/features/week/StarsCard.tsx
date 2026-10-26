import { useTranslation } from 'react-i18next'
import { StarGlyph } from '../../components/StarGlyph'
import type { WeekStars } from './types'

/**
 * Attendance stars (FR-24, D-81) on T6: who earned this finished week's star, or is on track in the current one, with
 * each child's term total. Stars only ever add up, so there is no list, count or word for children without one.
 * Roll-number order, no ranking. Counted in code; the teacher says well done in class.
 */
export function StarsCard({ stars }: { stars: WeekStars | null | undefined }) {
  const { t } = useTranslation()
  if (!stars) {
    return null
  }

  const count = stars.children.length
  return (
    <section aria-labelledby="stars-title" className="flex flex-col gap-3 rounded-card border-[1.5px] border-edge bg-surface p-4">
      <h2 id="stars-title" className="text-title">
        {t(stars.finished ? 'week.stars.title' : 'week.stars.titleThisWeek')}
      </h2>
      <p className="text-body">
        {count === 0
          ? t('week.stars.none')
          : t(stars.finished ? 'week.stars.earned' : 'week.stars.soFar', { count })}
      </p>
      {count > 0 && (
        <ul className="flex flex-wrap gap-2">
          {stars.children.map((child) => (
            <li
              key={child.childId}
              aria-label={t('week.stars.childLabel', {
                name: child.firstName,
                roll: child.rollNo,
                total: t('week.stars.total', { count: child.total }),
                attendance: child.attendance,
                catchUps: child.catchUps,
              })}
              className="chip bg-panel text-ink"
            >
              <span aria-hidden="true">
                {child.rollNo}. {child.firstName}
              </span>
              <StarGlyph size={16} />
              <span aria-hidden="true" className="tabular-nums">
                {child.total}
              </span>
            </li>
          ))}
        </ul>
      )}
      <p className="text-label">{t('week.stars.hint')}</p>
      <p className="text-caption text-ink-muted">{t('week.stars.rule')}</p>
    </section>
  )
}
