import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { AccentTitle } from '../../components/AccentWord'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useAnnounce } from '../../components/useAnnounce'
import { useOnline } from '../../components/useOnline'
import { useTodaysMessage, useTodaysWaiting } from '../approve/useTodaysMessage'
import { useMe } from '../auth/useMe'
import { useEveningDay } from '../evening/useEvening'
import { PickLessonSheet } from '../lessons/PickLessonSheet'
import { useCurriculum, useLessonDay, useSaveLesson } from '../lessons/useLessons'
import { AddedCard } from './AddedCard'
import { AttendanceCard } from './AttendanceCard'
import { AttendanceGrid } from './AttendanceGrid'
import { DayStrip } from './DayStrip'
import { GreetingCard } from './GreetingCard'
import { AnswersCard } from './AnswersCard'
import { LessonCard } from './LessonCard'
import { NextStepButton } from './NextStepButton'
import { useSchoolToday } from './schoolDate'
import { formatSchoolDate, formatSchoolTime } from './schoolTime'
import { SyncBadge } from './SyncBadge'
import { TodaysMessageCard } from './TodaysMessageCard'
import { TonightCard } from './TonightCard'
import { RecapCard } from '../week/RecapCard'
import { useAttendanceDay, useSaveAttendance } from './useAttendanceDay'
import { BackToToday, SchoolDateNav } from '../../app/SchoolDateNav'

/**
 * T1 Today (docs/design/T1-today.md): register from the simulated SATS feed with corrections and manual
 * fallback; today's lesson picked from the list (FR-04) with Change; "Added to missed lessons"; offline outbox
 * and sync badge; today's message waiting or approved (T-108); tonight's evening-message card (T-44).
 */
export function TodayScreen() {
  const { t, i18n } = useTranslation()
  const me = useMe()
  const online = useOnline()
  // After Re-record on Lesson confirm (T-114) focus goes to the mic, not the heading.
  const rerecord = (useLocation().state as { rerecord?: boolean } | null)?.rerecord === true
  const headingRef = useFocusOnMount<HTMLHeadingElement>(!rerecord)
  // The demo teacher may act as a past day (T-134, D-120); otherwise today in IST.
  const date = useSchoolToday()
  const classId = me.data?.classId ?? ''
  const day = useAttendanceDay(classId, date)
  const save = useSaveAttendance(classId, date)
  const [editing, setEditing] = useState(false)
  const [picking, setPicking] = useState(false)
  const announce = useAnnounce()
  const navigate = useNavigate()
  const curriculum = useCurriculum()
  const lesson = useLessonDay(classId, date)
  const saveLesson = useSaveLesson(classId, date)
  const chapters = curriculum.data?.chapters ?? []
  const confirmed = lesson.data?.lesson?.status === 'confirmed' ? lesson.data.lesson.competencyIds : []
  const absent = day.data && (day.data.status === 'sats' || day.data.status === 'manual') ? day.data.absentChildIds.length : 0
  const done = confirmed.length > 0 && absent > 0
  const attendanceDone = day.data?.status === 'sats' || day.data?.status === 'manual'
  const satsTime =
    day.data?.status === 'sats' && day.data.receivedAt
      ? formatSchoolTime(day.data.receivedAt, i18n.language, date)
      : null
  const evening = useEveningDay(classId, date, done)
  const todaysQuery = useTodaysMessage(classId, date, done)
  const todays = todaysQuery.data
  const todaysWaiting = useTodaysWaiting(classId, date)
  // What goes tonight carries today's message only once she has approved it (guardrail 3).
  const todaysApproved = todays != null && (todays.sent || todays.status === 'approved')
  // D-104: once confirmed, a new note can still replace the lesson until today's message is approved, or, on a day
  // without one, until tonight's message has gone; until then the record card stays (T-114). Nobody away: nothing
  // goes tonight, so it stays all day. While that is still loading, the confirmed card shows, so nothing jumps.
  // Once tonight's message has gone (any message, T-143) families have it: no new note, no Change.
  const recordOpen =
    confirmed.length > 0 &&
    lesson.data?.sentToFamilies !== true &&
    (!done ||
      (todaysQuery.isSuccess &&
        (todays ? !todaysApproved : evening.isSuccess && evening.data.sent === 0)))
  // D-68: when the copy on screen was saved (snapshot) or last fetched.
  const savedAt = day.data ? (day.data.savedAt ?? day.dataUpdatedAt) : null

  function finishEdit() {
    setEditing(false)
    headingRef.current?.focus()
  }

  return (
    <section aria-labelledby="today-title" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 id="today-title" ref={headingRef} tabIndex={-1} className="text-title">
          {/* The demo can step back a day or pick one from the calendar here (T-134); otherwise just the title. */}
          <SchoolDateNav mode="day">
            <AccentTitle title={t('today.title', { date: formatSchoolDate(date, i18n.language) })} accent={t('nav.today')} />
          </SchoolDateNav>
        </h1>
        {/* Settings moved to the header's profile menu (T-109); the sync badge stays beside the title. */}
        <SyncBadge />
      </div>
      <BackToToday />

      <DayStrip
        attendanceDone={attendanceDone}
        attendanceTime={satsTime}
        lessonDone={confirmed.length > 0}
        tonightDone={(evening.data?.sent ?? 0) > 0}
      />

      {/* The one next step (T-96): the only primary button on Today. Waits for the lesson day, so Record never flashes. */}
      {!editing && lesson.data && (
        <NextStepButton classId={classId} date={date} lessonStatus={lesson.data.lesson?.status ?? null} todays={todays} />
      )}

      <RecapCard classId={classId} date={date} chapters={chapters} />

      <GreetingCard />

      {!online && (
        <output className="flex items-start gap-2 rounded-card border-[1.5px] border-edge border-l-4 border-l-warning bg-surface p-3">
          <Icon name="cloudOff" size={18} className="mt-1 shrink-0 text-warning" />
          <span className="text-body text-ink">
            {savedAt
              ? t('offline.showingSaved', { time: formatSchoolTime(new Date(savedAt).toISOString(), i18n.language) })
              : t('offline.noNetwork')}
          </span>
        </output>
      )}

      {day.isError && !day.data && (
        <div role="alert" className="rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
          <p className="text-body">{t('app.loadError')}</p>
          <button
            type="button"
            onClick={() => void day.refetch()}
            className="mt-3 min-h-target w-full rounded-button border-2 border-border bg-surface text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      )}

      {day.data &&
        (editing ? (
          <AttendanceGrid
            mode={day.data.status === 'sats' ? 'correct' : 'manual'}
            roster={day.data.children}
            initialAbsent={day.data.absentChildIds}
            onCancel={finishEdit}
            onSave={(absent) => {
              void save(absent)
              finishEdit()
            }}
          />
        ) : (
          <AttendanceCard day={day.data} onEdit={() => setEditing(true)} />
        ))}

      {!editing && (
        <LessonCard
          classId={classId}
          date={date}
          day={lesson.data}
          chapters={chapters}
          onPick={() => setPicking(true)}
          recordOpen={recordOpen}
          rerecord={rerecord}
        />
      )}

      {/* "Added" and "Tonight" are one payoff card (T-49): what the lesson just did, and what goes out at 6 pm. */}
      {!editing && done && (
        <div className="card-payoff flex flex-col gap-3">
          <AddedCard absent={absent} lessonIds={confirmed} chapters={chapters} />
          {todays && <TodaysMessageCard message={todays} />}
          {evening.data && day.data && (
            <TonightCard day={evening.data} roster={day.data.children} todaysApproved={todaysApproved} />
          )}
          <AnswersCard classId={classId} date={date} />
        </div>
      )}

      {picking && (
        <PickLessonSheet
          chapters={chapters}
          nextInBook={lesson.data?.nextInBook ?? null}
          currentId={confirmed[0] ?? null}
          onClose={() => setPicking(false)}
          onPick={(id) => {
            setPicking(false)
            announce(t('announce.lessonSaved'))
            const saved = saveLesson([id])
            if (online) {
              // The same next step as on the confirm screen (T-108): to today's message when one is waiting for her.
              void todaysWaiting(saved).then((waiting) => waiting && navigate('/approve'))
            }
          }}
        />
      )}
    </section>
  )
}
