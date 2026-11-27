import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import type { Chapter } from '../lessons/types'
import { useCurriculum } from '../lessons/useLessons'
import { LevelTag } from '../today/LevelTag'
import { dayName, lessonNames, thisWeek } from './ledgerText'
import { ChannelTag, MissedLessonChip } from './LedgerTags'
import { childLevel, lessonLevel } from './levels'
import type { LedgerChild, LedgerEvent } from './types'
import { useDeleteChild, useLedger } from './useLedger'

/**
 * T4 child history: this week's events, each with its day (B-27), and the "Child's data" delete (FR-15). No
 * evening-message event (D-36); reasons show as a category "from the parent", teacher only (FR-21).
 */
export function ChildScreen() {
  const { t } = useTranslation()
  const { childId = '' } = useParams()
  const classId = useMe().data?.classId ?? ''
  const ledger = useLedger(classId)
  const chapters = useCurriculum().data?.chapters ?? []
  const child = ledger.data?.children.find((c) => c.childId === childId)

  return (
    <section className="flex flex-col">
      <header className="flex items-center gap-2 border-b border-border bg-surface px-2 py-2">
        <Link
          to="/missed"
          aria-label={t('common.back')}
          className="flex min-h-target min-w-target items-center justify-center rounded-md text-ink"
        >
          <Icon name="arrowLeft" />
        </Link>
        {child ? <ChildTitle name={child.firstName} /> : null}
      </header>
      {child && ledger.data ? (
        <ChildDetails classId={classId} child={child} weekOf={ledger.data.weekOf} chapters={chapters} />
      ) : ledger.data ? (
        <p className="px-4 py-6 text-body">{t('missed.child.missing')}</p>
      ) : null}
    </section>
  )
}

function ChildTitle({ name }: { name: string }) {
  const ref = useFocusOnMount<HTMLHeadingElement>()
  return (
    <h1 ref={ref} tabIndex={-1} className="text-title">
      {name}
    </h1>
  )
}

function ChildDetails({
  classId,
  child,
  weekOf,
  chapters,
}: {
  classId: string
  child: LedgerChild
  weekOf: string
  chapters: Chapter[]
}) {
  const { t, i18n } = useTranslation()
  const [deleting, setDeleting] = useState(false)
  const level = childLevel(child, weekOf)
  const lessons = thisWeek(child, weekOf)
  // T-98 (G2): the away days are one line, the lessons one chip each; the rest of the week stays a dated list.
  const awayDays = [...new Set(child.history.filter((e) => e.kind === 'away').map((e) => e.date))]
  const events = child.history.filter((e) => e.kind !== 'away')
  const daysAway = Math.max(child.daysAwayThisWeek, awayDays.length)
  const away = daysAway > 0 || lessons.length > 0
  return (
    <div className="flex flex-col gap-5 px-4 py-4">
      <div className="flex flex-col gap-2">
        <p className="text-caption text-ink-muted">
          {t('missed.child.meta', {
            roll: child.rollNo,
            language: t(`missed.language.${child.homeLanguage}`),
          })}
        </p>
        {(level || child.channel) && (
          <p className="flex flex-wrap gap-1">
            {level && <LevelTag level={level} />}
            {child.channel && <ChannelTag channel={child.channel} />}
          </p>
        )}
      </div>

      <section aria-labelledby="this-week" className="flex flex-col gap-3">
        <h2 id="this-week" className="text-caption font-bold text-ink-muted">
          {t('missed.child.thisWeek')}
        </h2>
        {!away && events.length === 0 && <p className="text-body">{t('missed.child.none')}</p>}
        {away && (
          <div className="card flex flex-col gap-2 p-3">
            {daysAway > 0 && (
              <p className="text-label">
                {awayDays.length > 0
                  ? t('missed.child.awayDays', {
                      count: daysAway,
                      days: awayDays.map((d) => dayName(d, i18n.language)).join(', '),
                    })
                  : t('missed.child.awayCount', { count: daysAway })}
              </p>
            )}
            {lessons.length > 0 && (
              <ul aria-label={t('missed.table.lessons')} className="flex flex-wrap gap-2">
                {lessons.map((lesson) => {
                  const name = lessonNames(chapters, lesson.competencyIds, i18n.language).join(', ')
                  const lessonLevelNow = lessonLevel(lesson.status)
                  return (
                    <li key={lesson.lessonId}>
                      {lessonLevelNow ? (
                        <LevelTag level={lessonLevelNow} label={name} />
                      ) : (
                        <MissedLessonChip lesson={name} />
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
          </div>
        )}
        {events.length > 0 && (
          <ol aria-labelledby="this-week" className="flex flex-col gap-3 border-l-2 border-divider pl-4">
            {events.map((event, i) => (
              <li key={`${event.date}-${event.kind}-${i}`} className="relative text-body">
                <span
                  aria-hidden="true"
                  className="absolute top-2 -left-[23px] size-[10px] rounded-full border border-border bg-surface"
                />
                <span className="font-bold">{dayName(event.date, i18n.language)}</span> · {eventText(t, event)}
              </li>
            ))}
          </ol>
        )}
      </section>

      {/* T-98: deleting is rare and final, so it sits under More; it still asks first (FR-15). */}
      <details className="mt-2 border-t border-divider pt-2">
        <summary className="flex min-h-target cursor-pointer items-center gap-1 text-label text-primary-strong">
          {t('missed.child.more')}
        </summary>
        <section aria-labelledby="child-data" className="flex flex-col gap-2 pt-2">
          <h2 id="child-data" className="text-caption font-bold text-ink-muted">
            {t('missed.child.dataHeading')}
          </h2>
          <button
            type="button"
            onClick={() => setDeleting(true)}
            className="min-h-target w-full rounded-button border-2 border-error bg-surface px-4 text-label text-error"
          >
            {t('missed.child.delete', { name: child.firstName })}
          </button>
        </section>
      </details>

      {deleting && <DeleteDialog classId={classId} child={child} onClose={() => setDeleting(false)} />}
    </div>
  )
}

/** Every event but "away", which is grouped above the list (T-98). */
function eventText(t: ReturnType<typeof useTranslation>['t'], event: LedgerEvent) {
  switch (event.kind) {
    case 'away':
      return null
    case 'reason':
      return t('missed.child.reason', {
        reason: t(`parent.reason.category.${event.category ?? 'other'}`),
      })
    case 'catchUpSent':
      return t('missed.child.catchUpSent', {
        language: t(`missed.language.${event.language ?? 'kn'}`),
      })
    case 'answered':
      return t(event.outcome === 'caughtUp' ? 'missed.child.checkRight' : 'missed.child.checkPractice')
    case 'stopped':
      return t('missed.child.stopped')
  }
}

/**
 * Delete confirmation (B-07, B-24): what is removed, Keep focused and primary, Delete outlined below. Esc and the
 * backdrop close it. Online only: a failure says nothing changed.
 */
function DeleteDialog({ classId, child, onClose }: { classId: string; child: LedgerChild; onClose: () => void }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const navigate = useNavigate()
  const remove = useDeleteChild(classId, child.childId)
  const dialogRef = useRef<HTMLDialogElement>(null)
  const keepRef = useRef<HTMLButtonElement>(null)
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  })

  useEffect(() => {
    const opener = document.activeElement
    const dialog = dialogRef.current
    if (dialog && !dialog.open) {
      // jsdom has no showModal; real browsers do.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal()
      } else {
        dialog.setAttribute('open', '')
      }
    }
    keepRef.current?.focus()
    const onBackdrop = (event: MouseEvent) => {
      if (event.target === dialog) {
        onCloseRef.current()
      }
    }
    dialog?.addEventListener('click', onBackdrop)
    return () => {
      dialog?.removeEventListener('click', onBackdrop)
      if (opener instanceof HTMLElement && opener.isConnected) {
        opener.focus()
      }
    }
  }, [])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="delete-title"
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      className="fixed inset-x-0 bottom-0 top-auto m-0 mx-auto w-full max-w-md rounded-t-md border border-border bg-surface p-4 pb-6 text-ink backdrop:bg-ink/40"
    >
      <h2 id="delete-title" className="text-title">
        {t('missed.delete.title', { name: child.firstName })}
      </h2>
      <ul className="mt-3 flex list-disc flex-col gap-1 pl-5 text-body">
        <li>{t('missed.delete.removed')}</li>
        <li>{t('missed.delete.cancelled')}</li>
        <li>{t('missed.delete.counts')}</li>
        <li>{t('missed.delete.final')}</li>
      </ul>
      {remove.isError && (
        <p role="alert" className="mt-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('missed.delete.failed')}
        </p>
      )}
      <div className="mt-4 flex flex-col gap-2">
        <button
          ref={keepRef}
          type="button"
          onClick={onClose}
          className="min-h-target-primary w-full rounded-button bg-primary font-extrabold px-4 text-label text-on-primary"
        >
          {t('missed.delete.keep')}
        </button>
        <button
          type="button"
          disabled={remove.isPending}
          onClick={() =>
            remove.mutate(undefined, {
              onSuccess: () => {
                announce(t('announce.deleted'))
                void navigate('/missed', { replace: true })
              },
            })
          }
          className="min-h-target w-full rounded-button border-2 border-error bg-surface px-4 text-label text-error disabled:opacity-60"
        >
          {remove.isPending ? t('missed.delete.deleting') : t('missed.delete.confirm')}
        </button>
      </div>
    </dialog>
  )
}
