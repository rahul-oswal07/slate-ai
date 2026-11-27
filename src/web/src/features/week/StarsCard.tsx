import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { StarGlyph } from '../../components/StarGlyph'
import type { WeekStars } from './types'

/**
 * The weekly star (FR-24, D-118) on T6: who came to school every day of this finished week, or every day so far in the
 * current one. Each week stands alone: no totals over weeks, nothing per child, and no list or word for children
 * without one. Roll-number order, no ranking. Worked out in code; the teacher says well done in class.
 *
 * Collapsed to one line, "N earned a star · See all" (T-99, D-130): the class count is for her only; See all opens the
 * names, the hint and the rule.
 */
export function StarsCard({ stars }: { stars: WeekStars | null | undefined }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const listId = useId()
  if (!stars) {
    return null
  }

  const count = stars.children.length
  return (
    <section aria-labelledby="stars-title" className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge bg-surface p-4">
      <h2 id="stars-title" className="text-title">
        {t(stars.finished ? 'week.stars.title' : 'week.stars.titleThisWeek')}
      </h2>
      {count === 0 ? (
        <p className="text-body">{t('week.stars.none')}</p>
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-between gap-x-3">
            <p className="flex items-center gap-2 text-body">
              <StarGlyph size={18} />
              {t(stars.finished ? 'week.stars.count' : 'week.stars.countSoFar', { count })}
            </p>
            <button
              type="button"
              aria-expanded={open}
              aria-controls={listId}
              onClick={() => setOpen(!open)}
              className="flex min-h-target items-center gap-1 rounded-md text-label text-primary-strong"
            >
              {t('week.stars.seeAll')}
              <Icon name="chevronDown" size={20} className={open ? 'rotate-180' : ''} />
            </button>
          </div>
          <div id={listId} hidden={!open} className="flex flex-col gap-3">
            <p className="text-body">{t(stars.finished ? 'week.stars.earned' : 'week.stars.soFar')}</p>
            <ul className="flex flex-wrap gap-2">
              {stars.children.map((child) => (
                <li
                  key={child.childId}
                  aria-label={t('week.stars.childLabel', { name: child.firstName, roll: child.rollNo })}
                  className="chip bg-panel text-ink"
                >
                  <span aria-hidden="true">
                    {child.rollNo}. {child.firstName}
                  </span>
                  <StarGlyph size={16} />
                </li>
              ))}
            </ul>
            <p className="text-label">{t('week.stars.hint')}</p>
            <p className="text-caption text-ink-muted">{t('week.stars.rule')}</p>
          </div>
        </>
      )}
    </section>
  )
}
