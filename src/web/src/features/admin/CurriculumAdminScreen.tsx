import { type FormEvent, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ApiError } from '../../api/client'
import { AdminPage, AdminQuery } from './AdminPage'
import {
  type Curricula,
  type TextbookImport,
  useCurricula,
  useDiscardImport,
  useRetryImport,
  useSetClassCurriculum,
  useTextbookImports,
  useUploadTextbook,
  fieldErrors,
} from './useCurricula'

const MEDIA = ['kn', 'en', 'te', 'ta', 'hi'] as const
const BOOKS = ['textbook', 'workbook', 'nalikali'] as const

/**
 * `/admin/curriculum` (T-146, T-147, D-125): upload a textbook for Claude to turn into a draft curriculum, follow the
 * import, open a draft to review it, and choose the curriculum each class follows.
 */
export function CurriculumAdminScreen() {
  const { t } = useTranslation()
  const imports = useTextbookImports()
  const curricula = useCurricula()

  return (
    <AdminPage title={t('admin.curriculum.title')}>
      <p className="text-body text-ink-muted">{t('admin.curriculum.intro')}</p>
      <UploadForm />
      <section aria-labelledby="imports" className="flex flex-col gap-2">
        <h2 id="imports" className="text-body-lg font-semibold">
          {t('admin.curriculum.imports')}
        </h2>
        <AdminQuery query={imports}>
          {(list) =>
            list.length === 0 ? (
              <p className="text-body text-ink-muted">{t('admin.curriculum.noImports')}</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {list.map((item) => (
                  <ImportRow key={item.id} item={item} />
                ))}
              </ul>
            )
          }
        </AdminQuery>
      </section>
      <AdminQuery query={curricula}>{(data) => <CurriculaSections data={data} />}</AdminQuery>
    </AdminPage>
  )
}

function UploadForm() {
  const { t } = useTranslation()
  const upload = useUploadTextbook()
  const [title, setTitle] = useState('')
  const [grade, setGrade] = useState('3')
  const [subject, setSubject] = useState('maths')
  const [medium, setMedium] = useState('kn')
  const [book, setBook] = useState('textbook')
  const [part, setPart] = useState('')
  const [pdf, setPdf] = useState<File | null>(null)
  const [problem, setProblem] = useState<string | null>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setProblem(null)
    if (!pdf) {
      setProblem(t('admin.curriculum.choosePdf'))
      return
    }
    try {
      await upload.mutateAsync({ title, grade, subject, medium, book, part, pdf })
      setTitle('')
      setPdf(null)
      ;(event.target as HTMLFormElement).reset()
    } catch (error) {
      setProblem(
        error instanceof ApiError && error.status === 413
          ? t('admin.curriculum.tooBig')
          : error instanceof ApiError && error.status === 415
            ? t('admin.curriculum.notPdf')
            : error instanceof ApiError && error.status === 400
              ? t('admin.curriculum.refused', { detail: fieldErrors(error.body) })
              : t('admin.curriculum.uploadFailed'),
      )
    }
  }

  const field = 'rounded-button border-2 border-border bg-canvas px-3 py-2 text-body'
  return (
    <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-3 rounded-card bg-panel p-4">
      <h2 className="text-body-lg font-semibold">{t('admin.curriculum.upload')}</h2>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.bookTitle')}</span>
        <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={200} className={field} />
      </label>
      <div className="grid grid-cols-2 gap-3">
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.curriculum.grade')}</span>
          <select value={grade} onChange={(e) => setGrade(e.target.value)} className={field}>
            {[1, 2, 3, 4, 5].map((g) => (
              <option key={g} value={g}>
                {g}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.curriculum.subject')}</span>
          <input value={subject} onChange={(e) => setSubject(e.target.value)} required lang="en" className={field} />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.curriculum.medium')}</span>
          <select value={medium} onChange={(e) => setMedium(e.target.value)} className={field}>
            {MEDIA.map((m) => (
              <option key={m} value={m}>
                {t(`language.${m}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.curriculum.book')}</span>
          <select value={book} onChange={(e) => setBook(e.target.value)} className={field}>
            {BOOKS.map((b) => (
              <option key={b} value={b}>
                {t(`admin.curriculum.books.${b}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.curriculum.part')}</span>
          <input value={part} onChange={(e) => setPart(e.target.value)} inputMode="numeric" className={field} />
        </label>
      </div>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.curriculum.pdf')}</span>
        <input type="file" accept="application/pdf" onChange={(e) => setPdf(e.target.files?.[0] ?? null)} className="text-body" />
      </label>
      {problem && (
        <p role="alert" className="text-caption text-error">
          {problem}
        </p>
      )}
      <button type="submit" disabled={upload.isPending} className="btn-primary w-auto self-start">
        {upload.isPending ? t('admin.curriculum.uploading') : t('admin.curriculum.uploadButton')}
      </button>
    </form>
  )
}

function ImportRow({ item }: { item: TextbookImport }) {
  const { t } = useTranslation()
  const retry = useRetryImport()
  const discard = useDiscardImport()
  const [confirming, setConfirming] = useState(false)

  return (
    <li className="flex flex-col gap-1 rounded-card bg-panel p-3">
      <span className="text-label">{item.title}</span>
      <span className="text-body">
        {item.status === 'ready'
          ? t('admin.curriculum.ready', { lessons: item.lessons ?? 0, flagged: item.flagged ?? 0 })
          : item.status === 'failed'
            ? t('admin.curriculum.failedImport', { error: item.error ?? '' })
            : item.status === 'queued'
              ? t('admin.curriculum.queued')
              : t('admin.curriculum.running', { done: item.stepsDone, total: Math.max(item.stepsTotal, 1) })}
      </span>
      <span className="text-caption text-ink-muted">{t('admin.curriculum.pages', { count: item.pageCount })}</span>
      <div className="flex flex-wrap items-center gap-3">
        {item.status === 'ready' && item.curriculumId && (
          <Link to={`/admin/curriculum/${item.curriculumId}`} className="btn-link min-h-target">
            {t('admin.curriculum.review')}
          </Link>
        )}
        {item.status === 'failed' && (
          <button type="button" disabled={retry.isPending} onClick={() => retry.mutate(item.id)} className="btn-link min-h-target">
            {t('common.tryAgain')}
          </button>
        )}
        {confirming ? (
          <button type="button" disabled={discard.isPending} onClick={() => discard.mutate(item.id)} className="btn-link min-h-target text-error">
            {t('admin.curriculum.discardConfirm')}
          </button>
        ) : (
          <button type="button" onClick={() => setConfirming(true)} className="btn-link min-h-target">
            {t('admin.curriculum.discard')}
          </button>
        )}
      </div>
    </li>
  )
}

function CurriculaSections({ data }: { data: Curricula }) {
  const { t } = useTranslation()
  const published = data.curricula.filter((c) => c.status === 'published')

  return (
    <>
      <section aria-labelledby="curricula" className="flex flex-col gap-2">
        <h2 id="curricula" className="text-body-lg font-semibold">
          {t('admin.curriculum.curricula')}
        </h2>
        <ul className="flex flex-col gap-2">
          <li className="rounded-card bg-panel p-3 text-body">
            {t('admin.curriculum.builtIn', { lessons: data.builtInLessons })}
          </li>
          {data.curricula.map((c) => (
            <li key={c.id} className="flex flex-col gap-1 rounded-card bg-panel p-3">
              <Link to={`/admin/curriculum/${c.id}`} className="text-label text-primary-strong underline">
                {c.title}
              </Link>
              <span className="text-body">
                {c.status === 'published'
                  ? t('admin.curriculum.published', { lessons: c.lessons, classes: c.classes })
                  : t('admin.curriculum.draft', { lessons: c.lessons, needsReview: c.needsReview })}
              </span>
            </li>
          ))}
        </ul>
      </section>
      <section aria-labelledby="classes" className="flex flex-col gap-2">
        <h2 id="classes" className="text-body-lg font-semibold">
          {t('admin.curriculum.classes')}
        </h2>
        <p className="text-body text-ink-muted">{t('admin.curriculum.classesIntro')}</p>
        <ul className="flex flex-col gap-2">
          {data.classes.map((c) => (
            <ClassRow key={c.id} classId={c.id} label={c.label} current={c.curriculumId} published={published} builtIn={data.builtInLessons} />
          ))}
        </ul>
      </section>
    </>
  )
}

function ClassRow({
  classId,
  label,
  current,
  published,
  builtIn,
}: {
  classId: string
  label: string
  current: string | null
  published: Curricula['curricula']
  builtIn: number
}) {
  const { t } = useTranslation()
  const set = useSetClassCurriculum()
  const [problem, setProblem] = useState<string | null>(null)

  async function change(value: string) {
    setProblem(null)
    try {
      await set.mutateAsync({ classId, curriculumId: value === '' ? null : value })
    } catch (error) {
      const missing = error instanceof ApiError && error.status === 409 ? (error.body as { missing?: string[] }).missing : undefined
      setProblem(missing ? t('admin.curriculum.wouldLose', { ids: missing.join(', ') }) : t('admin.curriculum.saveFailed'))
    }
  }

  return (
    <li className="flex flex-col gap-1 rounded-card bg-panel p-3">
      <label className="flex flex-col gap-1">
        <span className="text-label">{label}</span>
        <select
          value={current ?? ''}
          disabled={set.isPending}
          onChange={(e) => void change(e.target.value)}
          className="rounded-button border-2 border-border bg-canvas px-3 py-2 text-body"
        >
          <option value="">{t('admin.curriculum.builtInOption', { lessons: builtIn })}</option>
          {published.map((c) => (
            <option key={c.id} value={c.id}>
              {c.title}
            </option>
          ))}
        </select>
      </label>
      {problem && (
        <p role="alert" className="text-caption text-error">
          {problem}
        </p>
      )}
    </li>
  )
}
