import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useClassId } from '../auth/useCurrentClass'
import { findLesson, pageText } from '../lessons/lessonText'
import type { Chapter } from '../lessons/types'
import { useCurriculum } from '../lessons/useLessons'
import { LevelTag } from '../today/LevelTag'
import { useSchoolToday } from '../today/schoolDate'
import { formatSchoolTime } from '../today/schoolTime'
import { type Level, useTodaysAnswers } from '../today/useTodaysAnswers'
import { dayName, lessonNames, thisWeek } from './ledgerText'
import { ChannelTag, LearningTag } from './LedgerTags'
import { childLevel } from './levels'
import type { LearningStatus, Ledger, LedgerChild } from './types'
import { useLedger } from './useLedger'
import { SchoolWeekNav } from '../../app/SchoolDateNav'

const STATUS_ORDER: LearningStatus[] = ['missed', 'sent', 'caughtUp', 'needsBuddy']

/**
 * T4 Missed lessons (FR-05): who missed which lesson this week and where each child stands, by child or by lesson.
 * Everything is counted in code from attendance × her confirmed lessons; no AI. View and filter live in the URL so
 * Back from a child returns to the same list.
 */
export function MissedScreen() {
  const { t, i18n } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = useClassId()
  const ledger = useLedger(classId)
  const chapters = useCurriculum().data?.chapters ?? []
  const [params, setParams] = useSearchParams()
  const view = params.get('view') === 'lesson' ? 'lesson' : 'child'
  const everyone = params.get('filter') === 'all'
  const data = ledger.data
  // Today's picture question level per child (T-92), the same as on Today.
  const answers = useTodaysAnswers(classId, useSchoolToday()).data
  const levels = new Map<string, Level>((answers?.rows ?? []).map((r) => [r.childId, r.level]))

  const set = (key: 'view' | 'filter', value: string | null) =>
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (value === null) {
          next.delete(key)
        } else {
          next.set(key, value)
        }
        return next
      },
      { replace: true },
    )

  const empty = data && data.lessons.length === 0 && data.children.every((c) => c.missedLessons.length === 0)

  return (
    <section aria-labelledby="missed-title" className="flex flex-col gap-4 px-4 py-4">
      <h1 id="missed-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('missed.title')}
      </h1>
      <SchoolWeekNav />

      {data?.savedAt && (
        <output className="block border-l-4 border-l-warning bg-surface p-3 text-body text-ink">
          {t('missed.offline', {
            time: formatSchoolTime(new Date(data.savedAt).toISOString(), i18n.language),
          })}
        </output>
      )}

      {ledger.isError && !data ? (
        <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('missed.error')}
          <button
            type="button"
            onClick={() => void ledger.refetch()}
            className="min-h-target rounded-button border-2 border-border bg-surface px-4 text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      ) : empty ? (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
          <Icon name="list" size={56} className="text-ink-muted" />
          <h2 className="text-title">{t('missed.emptyTitle')}</h2>
          <p className="text-body text-ink-muted">{t('missed.emptyBody')}</p>
          <Link
            to="/"
            className="flex min-h-target w-full items-center justify-center rounded-button border-2 border-border bg-surface text-label"
          >
            {t('common.goToday')}
          </Link>
        </div>
      ) : data ? (
        <>
          <fieldset>
            <legend className="sr-only">{t('missed.view')}</legend>
            <div className="grid grid-cols-2 overflow-hidden rounded-button border-2 border-border">
              {(['child', 'lesson'] as const).map((v) => (
                <button
                  key={v}
                  type="button"
                  aria-pressed={view === v}
                  onClick={() => set('view', v === 'child' ? null : 'lesson')}
                  className={`min-h-target text-label ${view === v ? 'bg-primary text-on-primary' : 'bg-surface text-ink'}`}
                >
                  {t(v === 'child' ? 'missed.viewChild' : 'missed.viewLesson')}
                </button>
              ))}
            </div>
          </fieldset>

          {view === 'child' ? (
            <ByChild
              data={data}
              levels={levels}
              everyone={everyone}
              onFilter={(all) => set('filter', all ? 'all' : null)}
            />
          ) : (
            <ByLesson data={data} chapters={chapters} />
          )}

          <p className="text-caption text-ink-muted">{t('missed.footer')}</p>
        </>
      ) : null}
    </section>
  )
}

function ByChild({
  data,
  levels,
  everyone,
  onFilter,
}: {
  data: Ledger
  levels: Map<string, Level>
  everyone: boolean
  onFilter: (all: boolean) => void
}) {
  const { t } = useTranslation()
  const absent = data.children.filter((c) => c.daysAwayThisWeek > 0)
  const rows = everyone ? data.children : absent
  return (
    <>
      <fieldset>
        <legend className="sr-only">{t('missed.filter')}</legend>
        <div className="flex flex-wrap gap-2">
          {[false, true].map((all) => (
            <button
              key={String(all)}
              type="button"
              aria-pressed={everyone === all}
              onClick={() => onFilter(all)}
              className={`min-h-target rounded-sm border border-border px-3 text-label ${
                everyone === all ? 'bg-primary text-on-primary' : 'bg-surface text-ink'
              }`}
            >
              {all ? t('missed.filterAll', { n: data.children.length }) : t('missed.filterWeek', { n: absent.length })}
            </button>
          ))}
        </div>
      </fieldset>
      {/* T-98 (G1): one row per child, name · days away · lessons missed · level. The lessons themselves are on the
          child view, one chip each. */}
      <div className="overflow-hidden rounded-button border-2 border-border bg-surface">
        <table className="w-full table-fixed text-left">
          <caption className="sr-only">{t('missed.table.caption')}</caption>
          <thead>
            <tr className="text-caption text-ink-muted">
              <th scope="col" className="py-2 pr-1 pl-3 font-normal">
                {t('answers.child')}
              </th>
              <th scope="col" className="w-[3.5rem] px-1 py-2 text-center font-normal">
                {t('week.daysAway')}
              </th>
              <th scope="col" className="w-[3.75rem] px-1 py-2 text-center font-normal">
                {t('missed.table.lessons')}
              </th>
              <th scope="col" className="w-[38%] py-2 pr-2 pl-1 font-normal">
                {t('answers.level')}
              </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((child) => (
              <ChildRow
                key={child.childId}
                child={child}
                lessons={thisWeek(child, data.weekOf).length}
                // The catch-up's level for this week's missed lessons (D-130); else today's picture question (T-92).
                level={childLevel(child, data.weekOf) ?? levels.get(child.childId) ?? null}
              />
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function ChildRow({ child, lessons, level }: { child: LedgerChild; lessons: number; level: Level | null }) {
  const { t } = useTranslation()
  return (
    <tr className="border-t border-divider">
      <th scope="row" className="py-1 pr-1 pl-3 text-left align-middle font-normal">
        <Link
          to={`/missed/${child.childId}`}
          className="flex min-h-target items-center gap-0.5 rounded-sm text-label text-primary-strong underline underline-offset-4"
        >
          <span className="min-w-0 break-words">{child.firstName}</span>
          <Icon name="chevronRight" size={18} className="shrink-0" />
        </Link>
        {child.channel && <ChannelTag channel={child.channel} compact />}
      </th>
      <td className="px-1 py-1 text-center align-middle text-label tabular-nums">{child.daysAwayThisWeek}</td>
      <td className="px-1 py-1 text-center align-middle text-label tabular-nums">{lessons}</td>
      <td className="py-1 pr-2 pl-1 align-middle">
        {level ? (
          <LevelTag level={level} />
        ) : (
          <span className="text-label text-ink-muted">
            <span aria-hidden="true">—</span>
            <span className="sr-only">{t('missed.table.noLevel')}</span>
          </span>
        )}
      </td>
    </tr>
  )
}

/** One card per lesson this week, newest first; counts per child × lesson (B-08), not each child's latest status. */
function ByLesson({ data, chapters }: { data: Ledger; chapters: Chapter[] }) {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = useState<string | null>(null)
  const lessons = [...data.lessons].reverse()
  return (
    <ul className="flex flex-col gap-3">
      {lessons.map((lesson) => {
        const missedBy = data.children.flatMap((child) => {
          const missed = child.missedLessons.find((m) => m.lessonId === lesson.lessonId)
          return missed ? [{ child, status: missed.status }] : []
        })
        const first = lesson.competencyIds[0] ? findLesson(chapters, lesson.competencyIds[0]) : null
        const page = pageText(t, first?.lesson.textbookPage ?? null)
        const expanded = open === lesson.lessonId
        const listId = `lesson-${lesson.lessonId}`
        return (
          <li key={lesson.lessonId} className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge bg-surface p-3">
            <p className="text-caption text-ink-muted">
              {page
                ? t('missed.lessonMeta', {
                    day: dayName(lesson.date, i18n.language),
                    page,
                  })
                : dayName(lesson.date, i18n.language)}
            </p>
            <h2 className="text-body-lg font-semibold">
              {lessonNames(chapters, lesson.competencyIds, i18n.language).join(', ')}
            </h2>
            <p className="text-body">
              {t('missed.lessonCount', {
                n: missedBy.length,
                total: data.children.length,
              })}
            </p>
            {missedBy.length > 0 && (
              <>
                <p className="flex flex-wrap gap-1">
                  {STATUS_ORDER.map((status) => {
                    const count = missedBy.filter((m) => m.status === status).length
                    return count > 0 ? <LearningTag key={status} status={status} count={count} /> : null
                  })}
                </p>
                <button
                  type="button"
                  aria-expanded={expanded}
                  aria-controls={listId}
                  onClick={() => setOpen(expanded ? null : lesson.lessonId)}
                  className="flex min-h-target items-center justify-between rounded-md text-label text-primary-strong"
                >
                  {t('missed.showChildren')}
                  <Icon name="chevronRight" size={20} className={expanded ? 'rotate-90' : ''} />
                </button>
                {expanded && (
                  <ul id={listId} className="flex flex-col gap-2">
                    {missedBy.map(({ child, status }) => (
                      <li key={child.childId} className="flex flex-wrap items-center gap-2">
                        <span className="text-label">{child.firstName}</span>
                        <LearningTag status={status} />
                        {child.channel && <ChannelTag channel={child.channel} />}
                      </li>
                    ))}
                  </ul>
                )}
              </>
            )}
          </li>
        )
      })}
    </ul>
  )
}
