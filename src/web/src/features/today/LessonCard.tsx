import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { findLesson, lessonName, pageText } from '../lessons/lessonText'
import type { Chapter, LessonDay } from '../lessons/types'
import { useNoteQueued, useSendNote } from '../lessons/useLessons'
import { MAX_MS, useRecorder } from '../lessons/useRecorder'

const card = 'rounded-card border-[1.5px] border-edge bg-surface p-[14px]'
const textButton = 'mt-1 min-h-target w-full text-label text-primary-strong'

/**
 * Today's lesson (T1). Not recorded: the mic (FR-02, D-31) and "Or pick the lesson from the list". After she
 * records: sending (3-second rule, D-34), queued offline, or "Note saved · Open" while T2 matches it. Once
 * confirmed: every lesson with its page and one "Change" (D-33); no second note (D-55).
 */
export function LessonCard({
  classId,
  date,
  day,
  chapters,
  onPick,
}: {
  classId: string
  date: string
  day: LessonDay | undefined
  chapters: Chapter[]
  onPick: () => void
}) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const announce = useAnnounce()
  const queued = useNoteQueued(classId, date)
  const send = useSendNote(classId, date)
  const [sending, setSending] = useState(false)
  const recorder = useRecorder((note) => {
    let slow = false
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
  })

  useEffect(() => {
    if (recorder.status === 'recording') {
      announce(t('announce.recording'))
    }
  }, [recorder.status, announce, t])
  const lesson = day?.lesson
  const canPick = !!day && chapters.length > 0

  if (lesson?.status === 'confirmed') {
    const ids = lesson.competencyIds
    return (
      <section aria-labelledby="lesson-title" className={card}>
        <h2 id="lesson-title" className="text-label">
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
                </span>
              </li>
            )
          })}
        </ul>
        <button type="button" onClick={onPick} className={textButton}>
          {t('today.lesson.change')}
        </button>
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

  // A note that couldn't be heard falls through to the mic, so "Say it again" works (T2).
  if (lesson && lesson.status !== 'unclear') {
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
          className="mt-3 min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
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
  const seconds = Math.min(Math.floor(recorder.elapsedMs / 1000), MAX_MS / 1000)
  return (
    <section aria-labelledby="lesson-title" className={`${card} text-center`}>
      <h2 id="lesson-title" className="text-left text-label">
        {t('today.lesson.title')}
      </h2>
      <p className="mt-1 text-body">{t('today.lesson.prompt')}</p>
      {/* The one amber hero on this screen (D-77): a 112 px disc in a panel halo with a dashed chalk ring. */}
      <div className="relative mx-auto mt-3 flex size-[168px] items-center justify-center rounded-full bg-panel">
        <span aria-hidden="true" className="pointer-events-none absolute inset-2 rounded-full border-[2.5px] border-dashed border-chalk" />
        <button
          type="button"
          aria-pressed={recording}
          aria-label={recording ? t('mic.stop') : t('mic.record')}
          {...recorder.buttonHandlers}
          className={`relative flex size-[112px] touch-none select-none items-center justify-center rounded-full ${
            recording
              ? 'breathing border-[3px] border-transparent bg-error text-on-error'
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
        <button type="button" onClick={onPick} disabled={!canPick} className={textButton}>
          {t('today.lesson.pickInstead')}
        </button>
      )}
    </section>
  )
}
