import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { AdminPage, AdminQuery } from './AdminPage'
import { DraftLessonEditor } from './DraftLessonEditor'
import { type AdminCurriculumLesson, type CurriculumView, useCurriculum, usePublishCurriculum } from './useCurricula'

/**
 * `/admin/curriculum/:id` (T-147, D-125): a curriculum's chapters and lessons. A draft's lessons are checked and edited
 * one by one; once every lesson is reviewed and the curriculum passes the curriculum rules, Publish fixes it for good and
 * classes can follow it.
 */
export function CurriculumReviewScreen() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const curriculum = useCurriculum(id)

  return (
    <AdminPage title={curriculum.data?.summary.title ?? t('admin.curriculum.title')}>
      <Link to="/admin/curriculum" className="btn-link min-h-target self-start">
        {t('admin.curriculum.back')}
      </Link>
      <AdminQuery query={curriculum}>{(view) => <Review view={view} />}</AdminQuery>
    </AdminPage>
  )
}

function Review({ view }: { view: CurriculumView }) {
  const { t } = useTranslation()
  const draft = view.summary.status === 'draft'

  return (
    <>
      <p className="text-body">
        {draft
          ? t('admin.curriculum.draft', { lessons: view.summary.lessons, needsReview: view.summary.needsReview })
          : t('admin.curriculum.published', { lessons: view.summary.lessons, classes: view.summary.classes })}
      </p>
      {view.notes.length > 0 && (
        <ul className="flex list-disc flex-col gap-1 pl-5 text-body" lang="en">
          {view.notes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      )}
      {draft && <Publish view={view} />}
      {view.chapters.map((chapter) => (
        <section key={chapter.number} aria-labelledby={`chapter-${chapter.number}`} className="flex flex-col gap-2">
          <h2 id={`chapter-${chapter.number}`} className="text-body-lg font-semibold">
            {t('admin.curriculum.chapter', { number: chapter.number, name: chapter.nameEn })}{' '}
            <span lang="kn">{chapter.nameKn}</span>
          </h2>
          <ul className="flex flex-col gap-2">
            {view.lessons
              .filter((l) => l.lesson.chapter === chapter.number)
              .map((lesson) => (
                <LessonRow key={lesson.lesson.id} lesson={lesson} view={view} draft={draft} />
              ))}
          </ul>
        </section>
      ))}
    </>
  )
}

function Publish({ view }: { view: CurriculumView }) {
  const { t } = useTranslation()
  const publish = usePublishCurriculum(view.summary.id)
  const [problems, setProblems] = useState<string[]>([])
  const shown = problems.length > 0 ? problems : view.publishProblems

  async function run() {
    setProblems([])
    try {
      await publish.mutateAsync()
    } catch (error) {
      const list = error instanceof ApiError && error.status === 409 ? (error.body as { problems?: string[] }).problems : undefined
      setProblems(list ?? [t('admin.curriculum.saveFailed')])
    }
  }

  return (
    <div className="flex flex-col gap-2 rounded-card bg-panel p-3">
      <p className="text-body">{t('admin.curriculum.publishIntro')}</p>
      {shown.length > 0 && (
        <>
          <p className="text-label">{t('admin.curriculum.beforePublishing')}</p>
          <ul className="flex list-disc flex-col gap-1 pl-5 text-caption" lang="en">
            {shown.slice(0, 10).map((p) => (
              <li key={p}>{p}</li>
            ))}
            {shown.length > 10 && <li>{t('admin.curriculum.more', { count: shown.length - 10 })}</li>}
          </ul>
        </>
      )}
      <button type="button" disabled={shown.length > 0 || publish.isPending} onClick={() => void run()} className="btn-primary w-auto self-start">
        {t('admin.curriculum.publish')}
      </button>
    </div>
  )
}

function LessonRow({ lesson, view, draft }: { lesson: AdminCurriculumLesson; view: CurriculumView; draft: boolean }) {
  const { t } = useTranslation()
  const { lesson: l } = lesson

  return (
    <li className="rounded-card bg-panel">
      <details>
        <summary className="flex min-h-target cursor-pointer flex-wrap items-center gap-2 p-3">
          <span className="text-label" lang="en">
            {l.nameEn}
          </span>
          <span className="text-caption text-ink-muted" lang="en">
            {l.id}
            {l.textbookPage ? ` · p ${l.textbookPage.page}` : ''}
          </span>
          {lesson.reviewStatus === 'reviewed' ? (
            <span className="chip bg-canvas text-ink">{t('admin.curriculum.reviewed')}</span>
          ) : (
            <span className="chip bg-canvas text-ink">{t('admin.curriculum.needsReview')}</span>
          )}
          {lesson.flags.length > 0 && (
            <span className="chip bg-canvas text-ink">{t('admin.curriculum.flags', { count: lesson.flags.length })}</span>
          )}
        </summary>
        <div className="flex flex-col gap-3 px-3 pb-3">
          {lesson.flags.length > 0 && (
            <ul className="flex list-disc flex-col gap-1 pl-5 text-caption" lang="en">
              {lesson.flags.map((flag) => (
                <li key={flag}>{flag}</li>
              ))}
            </ul>
          )}
          {draft ? <DraftLessonEditor lesson={lesson} view={view} /> : <ReadOnly lesson={lesson} />}
        </div>
      </details>
    </li>
  )
}

function ReadOnly({ lesson }: { lesson: AdminCurriculumLesson }) {
  const { t } = useTranslation()
  const { lesson: l, extras } = lesson
  return (
    <dl className="grid grid-cols-1 gap-1 text-body">
      <dt className="text-label">{t('admin.curriculum.names')}</dt>
      <dd>
        <span lang="kn">{l.nameKn}</span> · <span lang="en">{l.nameEn}</span>
      </dd>
      {l.prerequisites.length > 0 && (
        <>
          <dt className="text-label">{t('admin.curriculum.prerequisites')}</dt>
          <dd lang="en">{l.prerequisites.join(', ')}</dd>
        </>
      )}
      {extras?.description && (
        <>
          <dt className="text-label">{t('admin.curriculum.description')}</dt>
          <dd lang="en">{extras.description}</dd>
        </>
      )}
    </dl>
  )
}
