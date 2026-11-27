import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Icon } from '../../components/Icon'
import type { TodaysMessage } from '../approve/todaysTypes'
import type { EveningChild, EveningDay, EveningOutcome } from '../evening/types'
import { findLesson, lessonName } from '../lessons/lessonText'
import type { Chapter } from '../lessons/types'
import { FamilyPreviewSheet } from './FamilyPreviewSheet'
import { formatSchoolTime } from './schoolTime'
import { tonightStatus, type TonightStatus } from './tonightStatus'
import type { RosterChild } from './types'
import { useNow } from './useNow'

const tagKey: Partial<Record<EveningOutcome, string>> = {
  noPhone: 'today.tonight.tag.noPhone',
  noConsent: 'today.tonight.tag.noConsent',
  stopped: 'today.tonight.tag.stopped',
  noWording: 'today.tonight.tag.noWording',
}

/**
 * Tonight's message on Today (T-86, D-126; FR-20, T-108): one card for what goes to families tonight. Before the send, a
 * checklist (wording approved once · the lesson and page · today's message), a status line that always says what happens
 * next and when (the API's `goesAt`, worked out with the 6 pm job's own hold, T-111), and one line per absent child with
 * their language, what goes and Preview of that family's real message. After the send it is a receipt: when it went, and
 * only the children left out (the answers card lists the rest, T-92). Counts come from the API (D-39).
 */
export function TonightCard({
  classId,
  day,
  roster,
  todays,
  lessonIds,
  chapters,
}: {
  classId: string
  day: EveningDay
  roster: RosterChild[]
  /** Today's message, if the lesson has one (D-91); undefined on a day without it. */
  todays: TodaysMessage | undefined
  lessonIds: string[]
  chapters: Chapter[]
}) {
  const { t } = useTranslation()
  const now = useNow()
  const [previewing, setPreviewing] = useState<RosterChild | null>(null)
  const names = new Map(roster.map((c) => [c.id, c]))

  if (!day.templateApproved) {
    return (
      <section aria-labelledby="tonight-title" className="border-t-2 border-dashed border-chalk pt-3">
        <h2 id="tonight-title" className="text-label text-primary-strong">
          {t('today.tonight.notApproved')}
        </h2>
        <Link to="/setup/evening" className="btn-secondary mt-3 flex items-center justify-center gap-2">
          {t('today.tonight.review')}
        </Link>
      </section>
    )
  }

  const status = tonightStatus(day, now)
  const sent = status.kind === 'sent'
  const title =
    day.families === 0
      ? t('today.tonight.none')
      : sent
        ? t('today.tonight.sent', { families: day.sent, absent: day.absent })
        : t('today.tonight.heading', { families: day.families, absent: day.absent })
  const leftOut = day.children.filter((c) => tagKey[c.outcome])

  return (
    <section aria-labelledby="tonight-title" className="border-t-2 border-dashed border-chalk pt-3">
      <h2 id="tonight-title" className="text-label text-primary-strong">
        {title}
      </h2>
      {day.families > 0 && <StatusLine status={status} date={day.date} />}

      {day.families > 0 && !sent && <Checklist todays={todays} lessonIds={lessonIds} chapters={chapters} />}

      {!sent && day.families > 0 ? (
        <ul className="mt-3 flex flex-col divide-y divide-divider">
          {day.children.map((c) => (
            <ChildLine
              key={c.childId}
              child={c}
              name={names.get(c.childId)?.firstName ?? ''}
              showMode={day.withTodays}
              onPreview={() => {
                const found = names.get(c.childId)
                if (found) setPreviewing(found)
              }}
            />
          ))}
        </ul>
      ) : (
        leftOut.length > 0 && (
          <>
            {sent && <p className="mt-2 text-caption text-ink-muted">{t('today.tonight.leftOut')}</p>}
            <ul className="mt-1 flex flex-col gap-1">
              {leftOut.map((c) => (
                <li key={c.childId} className="flex flex-wrap items-center gap-2 text-body">
                  <span>{names.get(c.childId)?.firstName ?? ''}</span>
                  <span className="chip bg-surface text-ink">{t(tagKey[c.outcome] ?? '')}</span>
                </li>
              ))}
            </ul>
          </>
        )
      )}

      {previewing && (
        <FamilyPreviewSheet
          classId={classId}
          date={day.date}
          child={{ id: previewing.id, firstName: previewing.firstName }}
          onClose={() => setPreviewing(null)}
        />
      )}
    </section>
  )
}

/** What happens next and when; every time comes from the API (T-86). */
function StatusLine({ status, date }: { status: TonightStatus; date: string }) {
  const { t, i18n } = useTranslation()
  const time = (value: string) => formatSchoolTime(value, i18n.language, date)
  const text = (() => {
    switch (status.kind) {
      case 'sent':
        return status.at
          ? t(status.withTodays ? 'today.tonight.status.sentTodays' : 'today.tonight.status.sentText', { at: time(status.at) })
          : null
      case 'writing':
        return t('today.tonight.status.writing')
      case 'approveBy':
        return t('today.tonight.status.approveBy', { at: time(status.at), until: time(status.until) })
      case 'waiting':
        return t('today.tonight.status.waiting', { until: time(status.until), minutes: status.minutes })
      case 'goesAt':
        return t(status.withTodays ? 'today.tonight.status.goesAtTodays' : 'today.tonight.status.goesAt', { at: time(status.at) })
      case 'goingNow':
        return t('today.tonight.status.goingNow')
      case 'none':
        return null
    }
  })()
  return text ? <p className="mt-1 text-body font-bold text-ink">{text}</p> : null
}

/** What goes tonight, in order: the wording she approved once, today's lesson and page, today's message. */
function Checklist({
  todays,
  lessonIds,
  chapters,
}: {
  todays: TodaysMessage | undefined
  lessonIds: string[]
  chapters: Chapter[]
}) {
  const { t, i18n } = useTranslation()
  const found = lessonIds.map((id) => findLesson(chapters, id))
  const lessons = lessonIds.map((id, i) => (found[i] ? lessonName(found[i].lesson, i18n.language) : id)).join(', ')
  const page = found[0]?.lesson.textbookPage?.page
  const todaysApproved = todays?.status === 'approved'

  return (
    <ul className="mt-3 flex flex-col gap-2 text-body">
      <CheckItem done>
        <span>{t('today.tonight.check.wording')}</span>
        <Link to="/setup/evening" className="btn-link">
          {t('today.tonight.check.example')}
        </Link>
      </CheckItem>
      <CheckItem done>
        <span>{page ? t('today.tonight.check.lesson', { lessons, page }) : t('today.tonight.check.lessonNoPage', { lessons })}</span>
      </CheckItem>
      {todays && (
        <CheckItem done={todaysApproved}>
          <span>
            {t(
              todaysApproved
                ? 'today.tonight.check.todaysApproved'
                : todays.status === 'generating'
                  ? 'today.tonight.check.todaysWriting'
                  : 'today.tonight.check.todaysWaiting',
            )}
          </span>
          {todaysApproved ? (
            <Link to="/approve" className="btn-link">
              {t('today.tonight.check.seeTodays')}
            </Link>
          ) : (
            // Secondary: the one primary is the next step at the top of Today (T-96).
            <Link to="/approve" className="btn-secondary mt-1 flex w-full items-center justify-center">
              {t('today.tonight.check.approve')}
            </Link>
          )}
        </CheckItem>
      )}
    </ul>
  )
}

function CheckItem({ done, children }: { done: boolean; children: ReactNode }) {
  return (
    <li className="flex items-start gap-2">
      <Icon
        name={done ? 'checkCircle' : 'clock'}
        size={20}
        className={`mt-[2px] shrink-0 ${done ? 'text-primary-strong' : 'text-ink-muted'}`}
      />
      <span className="flex min-w-0 flex-1 flex-wrap items-center gap-x-2">{children}</span>
    </li>
  )
}

/** One absent child: name · language · what goes, or why nothing does, and Preview for a family that gets it. */
function ChildLine({
  child,
  name,
  showMode,
  onPreview,
}: {
  child: EveningChild
  name: string
  /** Today's message is approved, so say for each family whether it goes with theirs. */
  showMode: boolean
  onPreview: () => void
}) {
  const { t } = useTranslation()
  const tag = tagKey[child.outcome]
  return (
    <li className="flex flex-wrap items-center gap-x-2 gap-y-1 py-2 text-body">
      <span className="font-bold">{name}</span>
      <span className="text-caption text-ink-muted">{t(`language.${child.language}`)}</span>
      {tag ? (
        <span className="chip bg-surface text-ink">{t(tag)}</span>
      ) : (
        <>
          {showMode && (
            <span className="text-caption text-ink-muted">
              · {t(child.withTodays ? 'today.tonight.child.withTodays' : 'today.tonight.child.textOnly')}
            </span>
          )}
          <button
            type="button"
            onClick={onPreview}
            aria-label={t('today.tonight.child.previewLabel', { child: name })}
            className="btn-link ml-auto min-h-target"
          >
            {t('today.tonight.child.preview')}
          </button>
        </>
      )}
    </li>
  )
}
