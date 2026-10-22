import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useNavigate } from 'react-router'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useOnline } from '../../components/useOnline'
import { useMe } from '../auth/useMe'
import { istToday } from '../today/schoolTime'
import { findLesson, lessonName, pageText } from './lessonText'
import { PickLessonSheet } from './PickLessonSheet'
import type { Chapter } from './types'
import { useCurriculum, useLessonDay, useNote, useSaveLesson } from './useLessons'

const SLOW_MS = 20_000
const primary = 'min-h-target-primary w-full rounded-md bg-primary text-label text-on-primary disabled:opacity-60'
const textButton = 'min-h-target w-full text-label text-primary-strong'

/**
 * T2 Lesson confirm (docs/design/T2-lesson-confirm.md): her voice note matched to 1–3 lessons by A1, shown
 * ticked for one-tap confirm (D-33). Low confidence asks her to choose; an unclear note sends her to the list.
 * Confidence is never shown as a number. Nothing reaches missed lessons until she confirms.
 */
export function ConfirmLessonScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const announce = useAnnounce()
  const online = useOnline()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const date = istToday()
  const classId = me.data?.classId ?? ''
  const day = useLessonDay(classId, date)
  const curriculum = useCurriculum()
  const save = useSaveLesson(classId, date)
  const chapters = curriculum.data?.chapters ?? []
  const lesson = day.data?.lesson
  const status = lesson?.status
  const [openedAt] = useState(() => Date.now())
  const [now, setNow] = useState(() => Date.now())
  const [ticked, setTicked] = useState<string[] | null>(null)
  const [sheet, setSheet] = useState<'single' | 'add' | null>(null)

  useEffect(() => {
    if (status !== 'processing') {
      return
    }
    const id = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(id)
  }, [status])

  useEffect(() => {
    if (status === 'mapped' || status === 'low') {
      announce(t('announce.lessonReady'))
    }
  }, [status, announce, t])

  if (day.data && (!lesson || status === 'confirmed')) {
    return <Navigate to="/" replace />
  }

  const chosen = ticked ?? lesson?.competencyIds ?? []

  function confirm(ids: string[], source: 'voice' | 'picked') {
    announce(t(source === 'voice' ? 'announce.lessonConfirmed' : 'announce.lessonSaved'))
    void save(ids, source)
    void navigate('/')
  }

  return (
    <section aria-labelledby="confirm-title" className="flex flex-col gap-3">
      <Link to="/" className="flex min-h-target w-fit items-center gap-1 text-label text-primary-strong">
        <span aria-hidden="true">‹</span> {t('common.back')}
      </Link>
      <h1 id="confirm-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('confirm.title')}
      </h1>

      {status === 'processing' && !online && (
        <Banner icon="cloudOff" tone="warning" text={t('confirm.offline')}>
          <button type="button" onClick={() => setSheet('single')} className={primary}>
            {t('confirm.pickFromList')}
          </button>
          <Link to="/" className={`${textButton} flex items-center justify-center`}>
            {t('common.goToday')}
          </Link>
        </Banner>
      )}

      {status === 'processing' && online && now - openedAt > SLOW_MS && (
        <Banner icon="clock" tone="warning" text={t('confirm.slow')}>
          <button type="button" onClick={() => setSheet('single')} className={primary}>
            {t('confirm.pickFromList')}
          </button>
          <Link to="/" className={`${textButton} flex items-center justify-center`}>
            {t('confirm.wait')}
          </Link>
        </Banner>
      )}

      {status === 'processing' && online && now - openedAt <= SLOW_MS && (
        <output className="flex items-start gap-3 rounded-md border border-border bg-surface p-[14px]">
          <span
            aria-hidden="true"
            className="mt-1 size-6 shrink-0 animate-spin rounded-full border-4 border-panel border-t-primary motion-reduce:animate-none"
          />
          <span>
            <span className="block text-body">{t('confirm.processing')}</span>
            <span className="block text-caption text-ink-muted">{t('confirm.processingHint')}</span>
          </span>
        </output>
      )}

      {status === 'unclear' && (
        <div role="alert" className="rounded-md border border-border border-l-4 border-l-error bg-error-bg p-[14px]">
          <p className="flex items-start gap-2 text-body">
            <Icon name="alert" className="mt-0.5 shrink-0 text-error" />
            {t('confirm.unclear')}
          </p>
          <button type="button" onClick={() => setSheet('single')} className={`${primary} mt-3`}>
            {t('confirm.pickFromList')}
          </button>
          <Link to="/" className={`${textButton} flex items-center justify-center`}>
            {t('confirm.sayAgain')}
          </Link>
        </div>
      )}

      {status === 'mapped' && (
        <section aria-labelledby="matched-title" className="rounded-md border border-border bg-surface p-[14px]">
          <AiLabel />
          <h2 id="matched-title" className="mt-2 text-label">
            {chosen.length > 1 || (lesson?.competencyIds.length ?? 0) > 1 ? t('confirm.lessonsAre') : t('confirm.lessonIs')}
          </h2>
          <fieldset className="mt-2 flex flex-col gap-2 rounded-md border-2 border-primary p-2">
            <legend className="sr-only">{t('confirm.lessons')}</legend>
            {[...new Set([...(lesson?.competencyIds ?? []), ...chosen])].map((id) => (
              <LessonOption
                key={id}
                id={id}
                chapters={chapters}
                type="checkbox"
                checked={chosen.includes(id)}
                onChange={() => setTicked(chosen.includes(id) ? chosen.filter((x) => x !== id) : [...chosen, id])}
              />
            ))}
          </fieldset>
          <button
            type="button"
            onClick={() => confirm(chosen, 'voice')}
            disabled={chosen.length === 0}
            aria-describedby={chosen.length === 0 ? 'keep-one' : undefined}
            className={`${primary} mt-3`}
          >
            {chosen.length > 1 ? t('confirm.confirmN', { n: chosen.length }) : t('confirm.confirm')}
          </button>
          {chosen.length === 0 && (
            <p id="keep-one" className="mt-1 text-caption text-error">
              {t('confirm.keepOne')}
            </p>
          )}
          <button type="button" onClick={() => setSheet('add')} disabled={chapters.length === 0} className={`${textButton} mt-1`}>
            {t('confirm.addLesson')}
          </button>
        </section>
      )}

      {status === 'low' && (
        <section
          aria-labelledby="low-title"
          className="rounded-md border-2 border-warning bg-surface p-[14px]"
        >
          <AiLabel />
          <h2 id="low-title" className="mt-2 flex items-start gap-2 text-label">
            <Icon name="alert" className="mt-0.5 shrink-0 text-warning" />
            {t('confirm.lowConf')}
          </h2>
          <fieldset className="mt-2 flex flex-col gap-2">
            <legend className="sr-only">{t('confirm.choices')}</legend>
            {(lesson?.competencyIds ?? []).slice(0, 2).map((id) => (
              <LessonOption
                key={id}
                id={id}
                chapters={chapters}
                type="radio"
                checked={false}
                onChange={() => confirm([id], 'voice')}
              />
            ))}
          </fieldset>
          <button type="button" onClick={() => setSheet('single')} disabled={chapters.length === 0} className={`${textButton} mt-1`}>
            {t('confirm.anotherLesson')}
          </button>
        </section>
      )}

      {(status === 'mapped' || status === 'low') && lesson && <NoteCard date={date} lesson={lesson} />}

      {sheet && (
        <PickLessonSheet
          chapters={chapters}
          nextInBook={day.data?.nextInBook ?? null}
          currentId={null}
          onClose={() => setSheet(null)}
          onPick={(id) => {
            setSheet(null)
            confirm([id], 'picked')
          }}
          add={
            sheet === 'add'
              ? {
                  selected: chosen,
                  onDone: (ids) => {
                    setSheet(null)
                    if (ids.length > chosen.length) {
                      announce(t('announce.lessonAdded'))
                    }
                    setTicked(ids)
                  },
                }
              : undefined
          }
        />
      )}
    </section>
  )
}

function AiLabel() {
  const { t } = useTranslation()
  return (
    <span className="inline-block rounded-sm bg-panel px-2 py-0.5 text-caption text-primary-strong">{t('ai.label')}</span>
  )
}

function Banner({
  icon,
  tone,
  text,
  children,
}: {
  icon: 'cloudOff' | 'clock'
  tone: 'warning'
  text: string
  children: ReactNode
}) {
  return (
    <div className={`rounded-md border border-border border-l-4 bg-surface p-[14px] ${tone === 'warning' ? 'border-l-warning' : ''}`}>
      <p className="flex items-start gap-2 text-body text-ink">
        <Icon name={icon} size={20} className="mt-0.5 shrink-0 text-warning" />
        {text}
      </p>
      <div className="mt-3 flex flex-col gap-1">{children}</div>
    </div>
  )
}

function LessonOption({
  id,
  chapters,
  type,
  checked,
  onChange,
}: {
  id: string
  chapters: Chapter[]
  type: 'checkbox' | 'radio'
  checked: boolean
  onChange: () => void
}) {
  const { t, i18n } = useTranslation()
  const found = findLesson(chapters, id)
  return (
    <label className="flex min-h-target cursor-pointer items-center gap-3 rounded-md bg-surface px-2 py-2 has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-focus">
      <input
        type={type}
        name={type === 'radio' ? 'low-choice' : undefined}
        checked={checked}
        onChange={onChange}
        className="size-6 shrink-0 accent-primary"
      />
      <span>
        <span className="block text-body">{found ? lessonName(found.lesson, i18n.language) : id}</span>
        {found && <span className="block text-caption text-ink-muted">{pageText(t, found.lesson.textbookPage)}</span>}
      </span>
    </label>
  )
}

/** Her own recording (from the phone) and what A1 understood (FR-03, D-56). */
function NoteCard({
  date,
  lesson,
}: {
  date: string
  lesson: { summaryKn: string | null; summaryEn: string | null; examplesUsed: string[] }
}) {
  const { t, i18n } = useTranslation()
  const note = useNote(date)
  const src = useMemo(() => (note ? URL.createObjectURL(new Blob([note.data], { type: note.type })) : null), [note])
  useEffect(() => () => {
    if (src) {
      URL.revokeObjectURL(src)
    }
  }, [src])
  const summary = i18n.language === 'kn' ? (lesson.summaryKn ?? lesson.summaryEn) : (lesson.summaryEn ?? lesson.summaryKn)

  return (
    <section aria-labelledby="note-title" className="rounded-md border border-border bg-surface p-[14px]">
      <h2 id="note-title" className="text-label">
        {t('confirm.note.label')}
      </h2>
      {src && (
        <audio controls src={src} aria-label={t('confirm.note.play')} className="mt-2 w-full">
          <track kind="captions" />
        </audio>
      )}
      {summary && (
        <p className="mt-2 text-body" lang={i18n.language === 'kn' && lesson.summaryKn ? 'kn' : 'en'}>
          {summary}
        </p>
      )}
      {lesson.examplesUsed.length > 0 && (
        <p className="mt-2 text-caption text-ink-muted">
          {t('confirm.note.examples')}: {lesson.examplesUsed.join(', ')}
        </p>
      )}
    </section>
  )
}
