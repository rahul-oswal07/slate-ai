import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { EveningDay, EveningOutcome } from '../evening/types'
import type { RosterChild } from './types'

const tagKey: Partial<Record<EveningOutcome, string>> = {
  noPhone: 'today.tonight.tag.noPhone',
  noConsent: 'today.tonight.tag.noConsent',
  stopped: 'today.tonight.tag.stopped',
  noWording: 'today.tonight.tag.noWording',
}

/**
 * Tonight's card (T1 `done`, FR-20): "After 6 pm tonight: message to 3 of 4 families". Counts come from the API
 * (absent ∩ consent Yes ∩ not stopped, D-39); every absent child left out is named with a tag (A-26). Before the
 * template is approved it links to the approval step instead.
 */
export function TonightCard({ day, roster }: { day: EveningDay; roster: RosterChild[] }) {
  const { t } = useTranslation()
  const names = new Map(roster.map((c) => [c.id, c.firstName]))
  const leftOut = day.children.filter((c) => tagKey[c.outcome])

  if (!day.templateApproved) {
    return (
      <section aria-labelledby="tonight-title" className="rounded-card border-2 border-primary bg-panel p-[14px]">
        <h2 id="tonight-title" className="text-label text-primary-strong">
          {t('today.tonight.notApproved')}
        </h2>
        <Link
          to="/setup/evening"
          className="mt-2 flex min-h-target w-full items-center justify-center text-label text-primary-strong"
        >
          {t('today.tonight.review')}
        </Link>
      </section>
    )
  }

  const title =
    day.families === 0
      ? t('today.tonight.none')
      : day.sent > 0
        ? t('today.tonight.sent', { families: day.sent, absent: day.absent })
        : t('today.tonight.title', { families: day.families, absent: day.absent })

  return (
    <section aria-labelledby="tonight-title" className="rounded-card border-2 border-primary bg-panel p-[14px]">
      <h2 id="tonight-title" className="text-label text-primary-strong">
        {title}
      </h2>
      {day.families > 0 && <p className="mt-1 text-body text-ink">{t('today.tonight.body')}</p>}
      {leftOut.length > 0 && (
        <ul className="mt-2 flex flex-col gap-1">
          {leftOut.map((c) => (
            <li key={c.childId} className="flex flex-wrap items-center gap-2 text-body">
              <span>{names.get(c.childId) ?? ''}</span>
              <span className="rounded-full border border-border bg-surface px-2 text-caption text-ink-muted">
                {t(tagKey[c.outcome] ?? '')}
              </span>
            </li>
          ))}
        </ul>
      )}
      <Link
        to="/setup/evening"
        className="mt-2 flex min-h-target w-full items-center justify-center text-label text-primary-strong"
      >
        {t('today.tonight.see')}
      </Link>
    </section>
  )
}
