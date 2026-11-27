import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon, type IconName } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useClassId } from '../auth/useCurrentClass'
import type { Chapter } from '../lessons/types'
import { useCurriculum } from '../lessons/useLessons'
import { type WeekSummary, weekSummary } from '../missed/levels'
import { useLedger } from '../missed/useLedger'
import { LEVEL_LOOK } from '../today/levelLook'
import { useSchoolToday } from '../today/schoolDate'
import { formatSchoolTime } from '../today/schoolTime'
import type { ReturnSpeed, Week, WeekReasons } from './types'
import { StarsCard } from './StarsCard'
import { nudgeText } from './weekText'
import { useDecide, useWeek } from './useWeek'
import { BackToToday, SchoolDateNav } from '../../app/SchoolDateNav'

/** "19–24 October", in the UI language (English in the Indian style, day before month). */
function weekRange(from: string, to: string, language: string) {
  const format = new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    day: 'numeric',
    month: 'long',
    timeZone: 'UTC',
  })
  return format.formatRange(new Date(`${from}T00:00:00Z`), new Date(`${to}T00:00:00Z`))
}

/**
 * T6 Week (FR-22, FR-21, FR-13): how quickly absent children came back this week and last, the families' reasons,
 * and the pacing nudge. Every number is counted in code from attendance; no AI on this screen. Teacher only.
 * The head teacher's view (T-99, G2, G5): one headline, then the chart; rows that would say 0 are hidden; stars fold.
 */
export function WeekScreen() {
  const { t, i18n } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = useClassId()
  const today = useSchoolToday()
  const query = useWeek(classId, today)
  const chapters = useCurriculum().data?.chapters ?? []
  const week = query.data
  // The headline's counts come from the ledger for the same school day (T-99, D-130); without it, no headline.
  const ledger = useLedger(classId).data
  const summary = ledger ? weekSummary(ledger) : null

  return (
    <section aria-labelledby="week-title" className="flex flex-col gap-4 px-4 py-4">
      <div>
        <h1 id="week-title" ref={headingRef} tabIndex={-1} className="text-title">
          {t('week.title')}
        </h1>
        {week && (
          <p className="text-caption text-ink-muted">
            {/* The demo can step a week back or pick a day here (T-134); otherwise just the range. */}
            <SchoolDateNav mode="week">{weekRange(week.from, week.to, i18n.language)}</SchoolDateNav>
          </p>
        )}
        <BackToToday />
      </div>

      {week?.savedAt && (
        <output className="block border-l-4 border-l-warning bg-surface p-3 text-body text-ink">
          {t('week.offline', { time: formatSchoolTime(new Date(week.savedAt).toISOString(), i18n.language) })}
        </output>
      )}

      {query.isError && !week ? (
        <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('week.loadFailed')}
          <button
            type="button"
            onClick={() => void query.refetch()}
            className="min-h-target rounded-button border-2 border-border bg-surface px-4 text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      ) : week ? (
        <>
          {week.empty ? (
            <p className="rounded-card border-[1.5px] border-edge bg-surface p-4 text-body-lg">{t('week.empty')}</p>
          ) : (
            <>
              {summary && summary.away > 0 && <Headline summary={summary} />}
              <ReturnSpeedCard week={week} />
              <ReasonsCard reasons={week.reasons} />
            </>
          )}
          <StarsCard stars={week.stars} />
          <Pacing classId={classId} date={today} week={week} chapters={chapters} />
          <p className="text-caption text-ink-muted">{t('week.footer')}</p>
        </>
      ) : null}
    </section>
  )
}

/**
 * The head teacher's view in one line (T-99, G5): "6 away · 4 got it · 2 need help". Children away this week, and how
 * many of them got their catch-up's picture question or need help (D-130). Counted in code from the ledger; the level
 * icons and colours are the same as on every other screen (T-97).
 */
function Headline({ summary }: { summary: WeekSummary }) {
  const { t } = useTranslation()
  const parts = [
    { key: 'away', text: t('week.summary.away', { count: summary.away }), icon: 'users' as IconName, className: 'text-ink' },
    {
      key: 'gotIt',
      text: t('week.summary.gotIt', { count: summary.gotIt }),
      icon: LEVEL_LOOK.gotIt.icon,
      className: LEVEL_LOOK.gotIt.text,
    },
    {
      key: 'needsHelp',
      text: t('week.summary.needHelp', { count: summary.needsHelp }),
      icon: LEVEL_LOOK.needsHelp.icon,
      className: LEVEL_LOOK.needsHelp.text,
    },
  ]
  return (
    <p className="flex flex-wrap items-center gap-x-2 gap-y-1 text-title font-bold">
      {parts.map((part, i) => (
        <span key={part.key} className="inline-flex items-center gap-2">
          {i > 0 && (
            <span aria-hidden="true" className="text-ink-muted">
              ·
            </span>
          )}
          <span className={`inline-flex items-center gap-1 ${part.className}`}>
            <Icon name={part.icon} size={22} className="shrink-0" />
            {part.text}
          </span>
        </span>
      ))}
    </p>
  )
}

const BUCKETS = [
  ['bucket1', 'oneDay'],
  ['bucket2', 'twoDays'],
  ['bucket3', 'threeDays'],
  ['bucket4', 'fourPlusDays'],
] as const satisfies readonly (readonly [string, keyof ReturnSpeed])[]

/** Headline, sentence, side-by-side bars and a screen-reader table, all from the same counts (B-06, B-09). */
function ReturnSpeedCard({ week }: { week: Week }) {
  const { t } = useTranslation()
  const { thisWeek, lastWeek } = week
  const max = Math.max(1, ...BUCKETS.flatMap(([, key]) => [thisWeek[key] as number, lastWeek[key] as number]))
  // T-99: a length nobody was away for, this week or last, is not drawn (the headline and sentence still add up).
  const buckets = BUCKETS.filter(([, key]) => (thisWeek[key] as number) > 0 || (lastWeek[key] as number) > 0)
  return (
    <section aria-labelledby="return-title" className="flex flex-col gap-3 rounded-card border-[1.5px] border-edge bg-surface p-4">
      <h2 id="return-title" className="text-title">
        {t('week.returnTitle')}
      </h2>
      <p>
        <span className="block text-title font-bold">
          {t('week.headline', { within: thisWeek.backWithinTwoDays, total: thisWeek.total })}
        </span>
        <span className="text-caption text-ink-muted">
          {lastWeek.total > 0
            ? t('week.sentence', { lastWithin: lastWeek.backWithinTwoDays, lastTotal: lastWeek.total })
            : t('week.sentenceLastNone')}
        </span>
      </p>

      <div aria-hidden="true" className="flex flex-col gap-2">
        <p className="flex gap-4 text-caption text-ink-muted">
          <span className="flex items-center gap-1">
            <span className="inline-block size-3 rounded-sm bg-primary" /> {t('week.thisWeek')}
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block size-3 rounded-sm border border-border bg-panel" /> {t('week.lastWeek')}
          </span>
        </p>
        {buckets.map(([label, key]) => (
          <div key={key} className="grid grid-cols-[4.5rem_1fr] items-center gap-2">
            <span className="text-caption">{t(`week.${label}`)}</span>
            <span className="flex flex-col gap-1">
              <Bar value={thisWeek[key] as number} max={max} className="bg-primary" />
              <Bar value={lastWeek[key] as number} max={max} className="border border-border bg-panel" />
            </span>
          </div>
        ))}
      </div>

      <table className="sr-only">
        <caption>{t('week.tableCaption')}</caption>
        <thead>
          <tr>
            <th scope="col">{t('week.daysAway')}</th>
            <th scope="col">{t('week.thisWeek')}</th>
            <th scope="col">{t('week.lastWeek')}</th>
          </tr>
        </thead>
        <tbody>
          {buckets.map(([label, key]) => (
            <tr key={key}>
              <th scope="row">{t(`week.${label}`)}</th>
              <td>{thisWeek[key]}</td>
              <td>{lastWeek[key]}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function Bar({ value, max, className }: { value: number; max: number; className: string }) {
  return (
    <span className="flex items-center gap-2">
      {/* No bar for 0 (a bordered empty bar would still show as a line); the number says it. */}
      {value > 0 && <span className={`h-3 rounded-sm ${className}`} style={{ width: `${Math.max(4, (value / max) * 80)}%` }} />}
      <span className="text-caption tabular-nums">{value}</span>
    </span>
  )
}

const REASONS: { key: keyof WeekReasons; icon: IconName }[] = [
  { key: 'unwell', icon: 'thermometer' },
  { key: 'travelOrWork', icon: 'bus' },
  { key: 'festival', icon: 'flame' },
  { key: 'neededAtHome', icon: 'house' },
  { key: 'other', icon: 'messageCircle' },
  { key: 'notGiven', icon: 'helpCircle' },
]

/**
 * One per absent child (latest reason, else "Not given"): categories only, never names (FR-21, D-23). A category
 * nobody gave is not listed (T-99).
 */
function ReasonsCard({ reasons }: { reasons: WeekReasons }) {
  const { t } = useTranslation()
  const rows = REASONS.filter(({ key }) => reasons[key] > 0)
  if (rows.length === 0) {
    return null
  }
  return (
    <section aria-labelledby="reasons-title" className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge bg-surface p-4">
      <h2 id="reasons-title" className="text-title">
        {t('week.reasonsTitle')}
      </h2>
      <ul className="flex flex-col">
        {rows.map(({ key, icon }) => (
          <li key={key} className="flex min-h-target items-center gap-3 border-b border-divider last:border-b-0">
            <Icon name={icon} size={20} className="shrink-0 text-primary-strong" />
            <span className="flex-1 text-body">
              {key === 'notGiven' ? t('week.notGiven') : t(`parent.reason.category.${key}`)}
            </span>
            <span className="text-label tabular-nums">{reasons[key]}</span>
          </li>
        ))}
      </ul>
      <p className="text-caption text-ink-muted">{t('week.private')}</p>
    </section>
  )
}

/** The nudge with Plan recap / Not this week, or the decision with Change; the on-track card when no nudge. */
function Pacing({ classId, date, week, chapters }: { classId: string; date: string; week: Week; chapters: Chapter[] }) {
  const { t, i18n } = useTranslation()
  const announce = useAnnounce()
  const decide = useDecide(classId, date)
  // Change reopens the two choices (T6 §7) rather than flipping the decision.
  const [reopened, setReopened] = useState(false)
  const nudge = week.nudge
  if (!nudge) {
    return week.empty ? null : (
      <p className="flex items-center gap-2 rounded-card border-[1.5px] border-edge bg-surface p-4 text-body">
        <Icon name="checkCircle" size={20} className="shrink-0 text-primary-strong" />
        {t('week.onTrack')}
      </p>
    )
  }

  const choose = (decision: 'recap' | 'skip') =>
    decide.mutate(
      { decision, lessonId: nudge.lessonId },
      {
        onSuccess: () => {
          setReopened(false)
          announce(t(decision === 'recap' ? 'week.planned' : 'week.skipped'))
        },
      },
    )

  return (
    <section aria-labelledby="nudge-title" className="flex flex-col gap-3 rounded-card border-2 border-primary bg-panel p-4">
      <h2 id="nudge-title" className="text-title text-primary-strong">
        {t('week.nudgeTitle')}
      </h2>
      <p className="text-body">{nudgeText(t, nudge, chapters, i18n.language)}</p>
      {decide.isError && (
        <p role="alert" className="border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('week.saveFailed')}
        </p>
      )}
      {week.decision && !reopened ? (
        <div className="flex items-center justify-between gap-3 border-l-4 border-l-primary bg-surface p-3">
          <span className="flex items-center gap-2 text-body">
            <Icon name="checkCircle" size={20} className="shrink-0 text-primary-strong" />
            {t(week.decision === 'recap' ? 'week.planned' : 'week.skipped')}
          </span>
          <button
            type="button"
            onClick={() => setReopened(true)}
            className="min-h-target shrink-0 rounded-md px-3 text-label text-primary-strong underline"
          >
            {t('week.change')}
          </button>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          <button
            type="button"
            disabled={decide.isPending}
            onClick={() => choose('recap')}
            className="min-h-target w-full rounded-button bg-primary font-extrabold px-4 text-label text-on-primary disabled:opacity-60"
          >
            {t('week.plan')}
          </button>
          <button
            type="button"
            disabled={decide.isPending}
            onClick={() => choose('skip')}
            className="min-h-target w-full rounded-button border-2 border-border bg-surface px-4 text-label text-ink"
          >
            {t('week.skip')}
          </button>
        </div>
      )}
    </section>
  )
}
