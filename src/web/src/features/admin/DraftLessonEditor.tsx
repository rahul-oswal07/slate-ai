import { type ReactNode, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../api/client'
import {
  type AdminCurriculumLesson,
  type CurriculumView,
  type Exercises,
  type LessonEdit,
  type OtherExercise,
  type PictureNote,
  fieldErrors,
  useEditLesson,
  useRemoveLesson,
} from './useCurricula'

const PRACTICE = ['none', 'count', 'expanded', 'placeValue', 'biggest', 'smallest', 'add', 'sub'] as const
const KINDS = ['compare', 'extremes', 'add', 'sub'] as const
const POSITIONS = ['top', 'middle', 'bottom', 'full'] as const

const lines = (text: string) => text.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)

/** "48, 33" per line → [[48, 33]]; anything that isn't a number is dropped and the API checks the rest. */
const numberLines = (text: string) =>
  lines(text).map((line) => line.split(/[^0-9]+/).filter((n) => n.length > 0).map(Number))

const field = 'rounded-button border-2 border-border bg-canvas px-3 py-2 text-body'

/**
 * Edits one lesson of a draft (T-147): everything the importer found can be corrected, and "Checked" marks it ready to
 * publish. The answers to the exercises are code's (guardrail 4) and shown only here.
 */
export function DraftLessonEditor({ lesson, view }: { lesson: AdminCurriculumLesson; view: CurriculumView }) {
  const { t } = useTranslation()
  const id = useId()
  const save = useEditLesson(view.summary.id)
  const remove = useRemoveLesson(view.summary.id)
  const { lesson: l, extras, answers } = lesson
  const earlier = view.lessons.filter((other) => other.lesson.sequence < l.sequence)

  const [names, setNames] = useState({
    nameKn: l.nameKn,
    nameEn: l.nameEn,
    nameTe: l.nameTe ?? '',
    nameTa: l.nameTa ?? '',
    nameHi: l.nameHi ?? '',
  })
  const [keyWords, setKeyWords] = useState((l.keyWordsKn ?? []).join('\n'))
  const [page, setPage] = useState(l.textbookPage ? String(l.textbookPage.page) : '')
  const [prerequisites, setPrerequisites] = useState(l.prerequisites)
  const [practice, setPractice] = useState({
    kind: l.practice?.kind ?? 'none',
    digits: String(l.practice?.digits ?? 2),
    carry: l.practice?.carry ?? 'none',
    borrow: l.practice?.borrow ?? 'none',
  })
  const [exercises, setExercises] = useState<Record<(typeof KINDS)[number], string>>({
    compare: (l.exercises?.compare ?? []).map((p) => p.join(', ')).join('\n'),
    extremes: (l.exercises?.extremes ?? []).map((p) => p.join(', ')).join('\n'),
    add: (l.exercises?.add ?? []).map((p) => p.join(', ')).join('\n'),
    sub: (l.exercises?.sub ?? []).map((p) => p.join(', ')).join('\n'),
  })
  const [bookExamples, setBookExamples] = useState(l.examples.join('\n'))
  const [description, setDescription] = useState(extras?.description ?? '')
  const [examples, setExamples] = useState((extras?.examples ?? []).join('\n'))
  const [others, setOthers] = useState<OtherExercise[]>(extras?.otherExercises ?? [])
  const [pictures, setPictures] = useState<PictureNote[]>(extras?.pictures ?? [])
  const [reviewed, setReviewed] = useState(lesson.reviewStatus === 'reviewed')
  const [problem, setProblem] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [confirmRemove, setConfirmRemove] = useState(false)

  async function submit() {
    setProblem(null)
    setSaved(false)
    const edit: LessonEdit = {
      ...names,
      keyWordsKn: lines(keyWords),
      page: page.trim() === '' ? null : Number(page),
      prerequisites,
      practice:
        practice.kind === 'none'
          ? null
          : {
              kind: practice.kind,
              digits: Number(practice.digits),
              carry: practice.kind === 'add' ? practice.carry : null,
              borrow: practice.kind === 'sub' ? practice.borrow : null,
            },
      exercises: Object.fromEntries(
        KINDS.map((kind) => [kind, numberLines(exercises[kind]).length > 0 ? numberLines(exercises[kind]) : null]),
      ) as Exercises,
      bookExamples: lines(bookExamples),
      description,
      examples: lines(examples),
      otherExercises: others,
      pictures,
      reviewed,
    }
    try {
      await save.mutateAsync({ id: l.id, edit })
      setSaved(true)
    } catch (error) {
      setProblem(
        error instanceof ApiError && error.status === 400
          ? t('admin.curriculum.notSaved', { detail: fieldErrors(error.body) })
          : t('admin.curriculum.saveFailed'),
      )
    }
  }

  const answer = (kind: (typeof KINDS)[number]) =>
    kind === 'compare'
      ? answers.compare.join(' · ')
      : kind === 'extremes'
        ? answers.extremes.map((pair) => pair.join('–')).join(' · ')
        : answers[kind].join(' · ')

  return (
    <form
      className="flex flex-col gap-3"
      onSubmit={(event) => {
        event.preventDefault()
        void submit()
      }}
    >
      <Group label={t('admin.curriculum.names')}>
        {(['nameKn', 'nameEn', 'nameTe', 'nameTa', 'nameHi'] as const).map((key) => (
          <label key={key} className="flex flex-col gap-1">
            <span className="text-caption">{t(`admin.curriculum.nameIn.${key}`)}</span>
            <input value={names[key]} onChange={(e) => setNames({ ...names, [key]: e.target.value })} className={field} />
          </label>
        ))}
      </Group>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.keyWords')}</span>
        <textarea value={keyWords} onChange={(e) => setKeyWords(e.target.value)} rows={3} lang="kn" className={field} />
      </label>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.page')}</span>
        <input value={page} onChange={(e) => setPage(e.target.value)} inputMode="numeric" className={field} />
      </label>
      {earlier.length > 0 && (
        <fieldset className="flex flex-col gap-1">
          <legend className="text-label">{t('admin.curriculum.prerequisites')}</legend>
          {earlier.map((other) => (
            <label key={other.lesson.id} className="flex min-h-target items-center gap-2 text-body">
              <input
                type="checkbox"
                checked={prerequisites.includes(other.lesson.id)}
                onChange={(e) =>
                  setPrerequisites(
                    e.target.checked
                      ? [...prerequisites, other.lesson.id]
                      : prerequisites.filter((p) => p !== other.lesson.id),
                  )
                }
              />
              <span lang="en">{other.lesson.nameEn}</span>
            </label>
          ))}
        </fieldset>
      )}
      <Group label={t('admin.curriculum.practice')}>
        <select value={practice.kind} onChange={(e) => setPractice({ ...practice, kind: e.target.value })} className={field} lang="en">
          {PRACTICE.map((kind) => (
            <option key={kind} value={kind}>
              {kind}
            </option>
          ))}
        </select>
        {practice.kind !== 'none' && (
          <label className="flex flex-col gap-1">
            <span className="text-caption">{t('admin.curriculum.digits')}</span>
            <input value={practice.digits} onChange={(e) => setPractice({ ...practice, digits: e.target.value })} inputMode="numeric" className={field} />
          </label>
        )}
        {practice.kind === 'add' && (
          <select value={practice.carry} onChange={(e) => setPractice({ ...practice, carry: e.target.value })} className={field} lang="en">
            {['none', 'ones', 'any'].map((c) => (
              <option key={c} value={c}>
                carry: {c}
              </option>
            ))}
          </select>
        )}
        {practice.kind === 'sub' && (
          <select value={practice.borrow} onChange={(e) => setPractice({ ...practice, borrow: e.target.value })} className={field} lang="en">
            {['none', 'tens'].map((b) => (
              <option key={b} value={b}>
                borrow: {b}
              </option>
            ))}
          </select>
        )}
      </Group>
      <Group label={t('admin.curriculum.exercises')}>
        <p className="text-caption text-ink-muted">{t('admin.curriculum.exercisesHint')}</p>
        {KINDS.map((kind) => (
          <label key={kind} className="flex flex-col gap-1">
            <span className="text-caption">{t(`admin.curriculum.kinds.${kind}`)}</span>
            <textarea value={exercises[kind]} onChange={(e) => setExercises({ ...exercises, [kind]: e.target.value })} rows={Math.max(2, Math.min(8, exercises[kind].split('\n').length + 1))} className={field} />
            {answer(kind) && (
              <span className="text-caption text-ink-muted">
                {t('admin.curriculum.answers', { answers: answer(kind) })}
              </span>
            )}
          </label>
        ))}
      </Group>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.bookExamples')}</span>
        <textarea value={bookExamples} onChange={(e) => setBookExamples(e.target.value)} rows={2} className={field} />
      </label>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.description')}</span>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={7} lang="en" className={field} />
      </label>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.examples')}</span>
        <textarea value={examples} onChange={(e) => setExamples(e.target.value)} rows={3} lang="en" className={field} />
      </label>
      {others.length > 0 && (
        <Group label={t('admin.curriculum.otherExercises')}>
          {others.map((other, i) => (
            <div key={`${other.page}-${i}`} className="flex items-start gap-2">
              <input
                aria-label={t('admin.curriculum.otherOnPage', { page: other.printedPage ?? other.page })}
                value={other.summary}
                onChange={(e) => setOthers(others.map((o, j) => (j === i ? { ...o, summary: e.target.value } : o)))}
                lang="en"
                className={`${field} flex-1`}
              />
              <button type="button" onClick={() => setOthers(others.filter((_, j) => j !== i))} className="btn-link min-h-target">
                {t('admin.curriculum.remove')}
              </button>
            </div>
          ))}
        </Group>
      )}
      {pictures.length > 0 && (
        <Group label={t('admin.curriculum.pictures')}>
          {pictures.map((picture, i) => (
            <div key={`${picture.page}-${i}`} className="flex flex-col gap-1 rounded-button border-2 border-border p-2">
              <span className="text-caption">
                {t('admin.curriculum.pictureOnPage', { page: picture.printedPage ?? picture.page })}
              </span>
              <select
                aria-label={t('admin.curriculum.position')}
                value={picture.position}
                onChange={(e) => setPictures(pictures.map((p, j) => (j === i ? { ...p, position: e.target.value } : p)))}
                className={field}
                lang="en"
              >
                {POSITIONS.map((position) => (
                  <option key={position} value={position}>
                    {position}
                  </option>
                ))}
              </select>
              <textarea
                aria-label={t('admin.curriculum.pictureDescription')}
                value={picture.description}
                onChange={(e) => setPictures(pictures.map((p, j) => (j === i ? { ...p, description: e.target.value } : p)))}
                rows={2}
                lang="en"
                className={field}
              />
              <input
                aria-label={t('admin.curriculum.pictureTeaches')}
                value={picture.teaches}
                onChange={(e) => setPictures(pictures.map((p, j) => (j === i ? { ...p, teaches: e.target.value } : p)))}
                lang="en"
                className={field}
              />
              <button type="button" onClick={() => setPictures(pictures.filter((_, j) => j !== i))} className="btn-link min-h-target self-start">
                {t('admin.curriculum.remove')}
              </button>
            </div>
          ))}
        </Group>
      )}
      <label className="flex min-h-target items-center gap-2 text-label">
        <input type="checkbox" checked={reviewed} onChange={(e) => setReviewed(e.target.checked)} />
        {t('admin.curriculum.checked')}
      </label>
      {problem && (
        <p id={`${id}-problem`} role="alert" className="text-caption text-error" lang="en">
          {problem}
        </p>
      )}
      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={save.isPending} className="btn-secondary w-auto">
          {t('admin.curriculum.save')}
        </button>
        {saved && <output className="text-caption text-ink-muted">{t('admin.curriculum.saved')}</output>}
        {confirmRemove ? (
          <button type="button" disabled={remove.isPending} onClick={() => remove.mutate(l.id)} className="btn-link min-h-target text-error">
            {t('admin.curriculum.removeLessonConfirm')}
          </button>
        ) : (
          <button type="button" onClick={() => setConfirmRemove(true)} className="btn-link min-h-target">
            {t('admin.curriculum.removeLesson')}
          </button>
        )}
      </div>
    </form>
  )
}

function Group({ label, children }: { label: string; children: ReactNode }) {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-label">{label}</legend>
      {children}
    </fieldset>
  )
}
