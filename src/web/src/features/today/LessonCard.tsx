import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { AccentTitle } from '../../components/AccentWord'
import { useCurrentClass } from '../auth/useCurrentClass'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { ExercisesTag } from '../lessons/ExercisesTag'
import { findLesson, lessonName, pageText } from '../lessons/lessonText'
import { LessonPhoto } from '../lessons/LessonPhoto'
import type { Chapter, LessonDay } from '../lessons/types'
import { useNoteQueued, useSendNote } from '../lessons/useLessons'
import { MAX_MS, useRecorder } from '../lessons/useRecorder'

const card = 'card p-[14px]'
const textButton = 'btn-link mt-1'

/**
 * Today's lesson (T1). Not recorded: the mic (FR-02, D-31), then "Add a photo of the page" (T-64) and "Pick from the
 * list" side by side, with the photo hint under both. After she
 * records: sending (3-second rule, D-34), queued offline, or "Note saved · Open" while T2 matches it. Once
 * confirmed: while she can still replace it (`recordOpen`: today's message not approved, or tonight's message not
 * gone, D-104) the mic stays with the lesson as one line and "Change"; after that every lesson with its page and one
 * "Change" (D-33). `rerecord` (Re-record on T2, T-114) brings the mic back while a match waits, with focus on it.
 * Once tonight's message has gone to families (`day.sentToFamilies`, T-143, D-124) there is no "Change": a line says
 * why, because families already have the message about this lesson.
 */
export function LessonCard({
  classId,
  date,
  day,
  chapters,
  onPick,
  recordOpen = false,
  rerecord = false,
}: {
  classId: string
  date: string
  day: LessonDay | undefined
  chapters: Chapter[]
  onPick: () => void
  recordOpen?: boolean
  rerecord?: boolean
}) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const announce = useAnnounce()
  const queued = useNoteQueued(classId, date)
  const send = useSendNote(classId, date)
  const sampleVoiceNote = useCurrentClass()?.demo?.sampleVoiceNote === true
  const [sending, setSending] = useState(false)
  const [sampleFailed, setSampleFailed] = useState(false)
  // Re-record holds only until she sends the new note; then the card follows the server again.
  const [sentSinceRerecord, setSentSinceRerecord] = useState(false)
  const micRef = useRef<HTMLButtonElement>(null)
  // A recording, or null for her photo alone; either way the day's photo goes with it.
  const sendNote = (note: Blob | null) => {
    let slow = false
    setSentSinceRerecord(true)
    setSending(true)
    announce(t('announce.sending'))
    void send(note, () => {
      // Still uploading after 3 s: it stays on the phone and sends later (D-34); she carries on.
      slow = true
      setSending(false)
    }).then((result) => {
      setSending(false)
      if (result === 'sent' && !slow) {
        void navigate('/lesson')
      }
    })
  }
  const recorder = useRecorder(sendNote)

  // Demo only (T-93): the bundled Kannada note about Nalikali p 4 goes the same way as a recording (STT, scrubber, A1).
  const sendSample = async () => {
    setSampleFailed(false)
    try {
      const response = await fetch('/api/demo/voice-note', { credentials: 'same-origin' })
      if (!response.ok) {
        throw new Error(String(response.status))
      }
      sendNote(await response.blob())
    } catch {
      setSampleFailed(true)
    }
  }

  useEffect(() => {
    if (recorder.status === 'recording') {
      announce(t('announce.recording'))
    }
  }, [recorder.status, announce, t])
  const lesson = day?.lesson
  const canPick = !!day && chapters.length > 0
  const reRecording = rerecord && !sentSinceRerecord
  const confirmedOpen = lesson?.status === 'confirmed' && recordOpen

  useEffect(() => {
    if (rerecord) {
      micRef.current?.focus()
    }
  }, [rerecord])

  if (lesson?.status === 'confirmed' && !recordOpen) {
    const ids = lesson.competencyIds
    return (
      <section aria-labelledby="lesson-title" className="card-hero">
        <output className="flex items-center gap-3 border-b-2 border-dashed border-chalk pb-3 text-title">
          <span aria-hidden="true" className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary text-on-primary">
            <Icon name="check" size={22} strokeWidth={3} />
          </span>
          {t('today.done')}
        </output>
        <h2 id="lesson-title" className="mt-3 text-label">
          {ids.length > 1 ? t('today.lesson.titlePlural') : t('today.lesson.title')}
        </h2>
        <ul className="mt-2 flex flex-col gap-2">
          {ids.map((id) => {
            const found = findLesson(chapters, id)
            return (
              <li key={id} className="flex items-start gap-2">
                <Icon name="checkCircle" className="mt-0.5 shrink-0 text-primary-strong" />
                <span>
                  <span className="block text-body">{found ? lessonName(found.lesson, i18n.language) : id}</span>
                  {found && <span className="block text-caption text-ink-muted">{pageText(t, found.lesson.textbookPage)}</span>}
                  {found && <ExercisesTag lesson={found.lesson} />}
                </span>
              </li>
            )
          })}
        </ul>
        {day?.sentToFamilies ? (
          <p className="mt-2 text-caption text-ink-muted">{t('today.lesson.locked')}</p>
        ) : (
          <button type="button" onClick={onPick} className={textButton}>
            {t('today.lesson.change')}
          </button>
        )}
      </section>
    )
  }

  if (sending) {
    return (
      <section aria-labelledby="lesson-title" className={card}>
        <h2 id="lesson-title" className="text-label">
          {t('today.lesson.title')}
        </h2>
        <output className="mt-2 flex items-start gap-3">
          <span
            aria-hidden="true"
            className="mt-1 size-6 shrink-0 animate-spin rounded-full border-4 border-panel border-t-primary motion-reduce:animate-none"
          />
          <span>
            <span className="block text-body">{t('today.lesson.sending')}</span>
            <span className="block text-caption text-ink-muted">{t('today.lesson.sendingRule')}</span>
          </span>
        </output>
      </section>
    )
  }

  if (queued) {
    return (
      <section aria-labelledby="lesson-title" className={card}>
        <h2 id="lesson-title" className="text-label">
          {t('today.lesson.title')}
        </h2>
        <p className="mt-2 flex items-start gap-2 text-body">
          <Icon name="cloudOff" size={20} className="mt-0.5 shrink-0 text-status-queued" />
          {t('today.lesson.queued')}
        </p>
        <button type="button" onClick={onPick} disabled={!canPick} className={textButton}>
          {t('today.lesson.pickInstead')}
        </button>
      </section>
    )
  }

  // A note that couldn't be heard falls through to the mic, and so does Re-record on T2 (T-114).
  if (lesson && lesson.status !== 'unclear' && lesson.status !== 'confirmed' && !reRecording) {
    return (
      <section aria-labelledby="lesson-title" className={card}>
        <h2 id="lesson-title" className="text-label">
          {t('today.lesson.title')}
        </h2>
        <div className="mt-2 flex items-center justify-between gap-3">
          <p className="flex items-center gap-2 text-body">
            <Icon name="checkCircle" className="shrink-0 text-primary-strong" />
            {t('today.lesson.processing')}
          </p>
          <Link to="/lesson" className="flex min-h-target items-center px-3 text-label text-primary-strong">
            {t('today.lesson.open')}
          </Link>
        </div>
      </section>
    )
  }

  if (recorder.status === 'blocked') {
    return (
      <section aria-labelledby="lesson-title" className={card}>
        <h2 id="lesson-title" className="text-label">
          {t('today.lesson.title')}
        </h2>
        <p className="mt-2 flex items-start gap-2 text-body text-ink">
          <Icon name="alert" className="mt-0.5 shrink-0 text-warning" />
          {t('today.lesson.micBlocked')}
        </p>
        <button
          type="button"
          onClick={onPick}
          disabled={!canPick}
          data-next-step="record"
          className="btn-secondary mt-3"
        >
          {t('confirm.pickFromList')}
        </button>
        <button type="button" onClick={() => void recorder.start()} className={textButton}>
          {t('mic.openSettings')}
        </button>
      </section>
    )
  }

  const recording = recorder.status === 'recording'
  // Once the lesson is confirmed, recording again is not the next step (T-96): the mic is outlined, not the amber hero,
  // unless she came back with Re-record.
  const quietMic = confirmedOpen && !reRecording
  const seconds = Math.min(Math.floor(recorder.elapsedMs / 1000), MAX_MS / 1000)
  return (
    <section aria-labelledby="lesson-title" className="card-hero text-center">
      <h2 id="lesson-title" className="sr-only">
        {t('today.lesson.title')}
      </h2>
      {confirmedOpen && <CurrentLesson ids={lesson.competencyIds} chapters={chapters} onChange={onPick} />}
      <p className="text-title">
        <AccentTitle title={t('today.lesson.prompt')} accent={t('nav.today').toLowerCase()} />
      </p>
      {/* The one amber hero on this screen (D-77): a 112 px disc in a panel halo with a dashed chalk ring; outlined once the
          lesson is confirmed (T-96). */}
      <div className="relative mx-auto mt-3 flex size-[168px] items-center justify-center rounded-full bg-panel">
        <span aria-hidden="true" className="pointer-events-none absolute inset-2 rounded-full border-[2.5px] border-dashed border-chalk" />
        <button
          ref={micRef}
          type="button"
          data-next-step="record"
          aria-pressed={recording}
          aria-label={recording ? t('mic.stop') : t('mic.record')}
          {...recorder.buttonHandlers}
          className={`relative flex size-[112px] touch-none select-none items-center justify-center rounded-full ${
            recording
              ? 'breathing border-[3px] border-transparent bg-error text-on-error'
              : quietMic
                ? 'border-[3px] border-primary bg-panel text-primary-strong'
                : 'btn-hero shadow-[0_8px_0_-2px_rgb(0_0_0/0.14)]'
          }`}
        >
          {recording ? <span aria-hidden="true" className="size-8 rounded-sm bg-on-error" /> : <Icon name="mic" size={44} />}
        </button>
      </div>
      {recording ? (
        <>
          <p className="mt-2 text-title" aria-hidden="true">
            {t('mic.timer', { ss: String(seconds).padStart(2, '0') })}
          </p>
          <p className="text-caption text-ink-muted">{t('mic.recHint')}</p>
        </>
      ) : (
        <p className="mt-2 text-caption text-ink-muted">{t('today.lesson.hint')}</p>
      )}
      {recorder.status === 'tooShort' && (
        <p role="alert" className="mt-2 text-caption text-error">
          {t('today.lesson.tooShort')}
        </p>
      )}
      {recorder.status === 'cancelled' && (
        <output className="mt-2 block text-caption text-ink-muted">{t('announce.recCancelled')}</output>
      )}
      {!recording && (
        <>
          <LessonPhoto
            date={date}
            onMatch={() => sendNote(null)}
            beside={
              <button type="button" onClick={onPick} disabled={!canPick} className="btn-tile">
                <Icon name="list" size={20} />
                {t('confirm.pickFromList')}
              </button>
            }
          />
          {sampleVoiceNote && (
            <button type="button" onClick={() => void sendSample()} className="btn-secondary mt-3 inline-flex items-center justify-center gap-2">
              <Icon name="mic" size={20} />
              {t('today.lesson.sample')}
            </button>
          )}
          {sampleFailed && (
            <p role="alert" className="mt-2 text-caption text-error">
              {t('today.lesson.sampleFailed')}
            </p>
          )}
        </>
      )}
    </section>
  )
}

/** "Today: <lesson> · p <page>" and Change above the mic, once confirmed but still replaceable (T-114). */
function CurrentLesson({ ids, chapters, onChange }: { ids: string[]; chapters: Chapter[]; onChange: () => void }) {
  const { t, i18n } = useTranslation()
  const found = ids.map((id) => findLesson(chapters, id))
  const lessons = ids.map((id, i) => (found[i] ? lessonName(found[i].lesson, i18n.language) : id)).join(', ')
  const page = found[0]?.lesson.textbookPage?.page
  return (
    <p className="mb-3 flex flex-wrap items-center justify-center gap-x-2 border-b-2 border-dashed border-chalk pb-3 text-body">
      <Icon name="checkCircle" className="shrink-0 text-primary-strong" />
      <span>{page ? t('today.lesson.current', { lessons, page }) : t('today.lesson.currentNoPage', { lessons })}</span>
      <button type="button" onClick={onChange} className="btn-link min-h-target">
        {t('today.lesson.change')}
      </button>
    </p>
  )
}
