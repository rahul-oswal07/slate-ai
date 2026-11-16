import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../api/client'
import { type AdminLessonContent, type AdminLessonLink, useSetLessonContent } from './useAdmin'

const sourceKey = {
  textbook: 'admin.lessonContent.sourceTextbook',
  written: 'admin.lessonContent.sourceWritten',
  edited: 'admin.lessonContent.sourceEdited',
} as const

/**
 * "About this lesson" under a lesson on /admin/lesson-links (T-141, D-123): the lesson's description and worked
 * examples, which today's message's spoken explanation is built on, where they came from, and an editor. An edit here
 * is kept: a deploy never overwrites it. Saved on its own, apart from the video links' Save all bar.
 */
export function LessonContentEditor({ lesson }: { lesson: AdminLessonLink }) {
  const { t } = useTranslation()
  const content = lesson.content ?? null
  const [editing, setEditing] = useState(false)
  const [saved, setSaved] = useState(false)

  return (
    <details className="px-3 pb-2">
      <summary className="inline-flex min-h-target cursor-pointer items-center gap-2 text-label text-primary-strong">
        {t('admin.lessonContent.about')}
        {content && <span className="chip bg-panel text-ink">{t(sourceKey[content.source])}</span>}
      </summary>
      {editing ? (
        <Form
          lesson={lesson}
          content={content}
          onDone={(didSave) => {
            setEditing(false)
            setSaved(didSave)
          }}
        />
      ) : (
        <div className="flex flex-col gap-2">
          {content ? <Content content={content} /> : <p className="text-body text-ink-muted">{t('admin.lessonContent.none')}</p>}
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => {
                setSaved(false)
                setEditing(true)
              }}
              className="btn-link min-h-target"
            >
              {content ? t('admin.lessonContent.edit') : t('admin.lessonContent.add')}
            </button>
            {saved && (
              <output className="text-caption text-ink-muted">
                {t('admin.lessonContent.saved')}
              </output>
            )}
          </div>
        </div>
      )}
    </details>
  )
}

function Content({ content }: { content: AdminLessonContent }) {
  const { t } = useTranslation()
  return (
    <>
      <p className="text-body" lang="en">
        {content.description}
      </p>
      <p className="text-label">{t('admin.lessonContent.examples')}</p>
      <ul className="flex list-disc flex-col gap-1 pl-5 text-body" lang="en">
        {content.examples.map((example) => (
          <li key={example}>{example}</li>
        ))}
      </ul>
    </>
  )
}

function Form({
  lesson,
  content,
  onDone,
}: {
  lesson: AdminLessonLink
  content: AdminLessonContent | null
  onDone: (saved: boolean) => void
}) {
  const { t } = useTranslation()
  const id = useId()
  const set = useSetLessonContent()
  const [description, setDescription] = useState(content?.description ?? '')
  const [examples, setExamples] = useState(content?.examples.join('\n') ?? '')
  const [problem, setProblem] = useState<'refused' | 'failed' | null>(null)

  async function save() {
    setProblem(null)
    try {
      await set.mutateAsync({ id: lesson.id, description, examples: examples.split('\n') })
      onDone(true)
    } catch (error) {
      setProblem(error instanceof ApiError && error.status === 400 ? 'refused' : 'failed')
    }
  }

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault()
        void save()
      }}
    >
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.lessonContent.descriptionLabel')}</span>
        <textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          rows={5}
          lang="en"
          aria-invalid={problem === 'refused'}
          aria-describedby={problem ? `${id}-problem` : undefined}
          className="rounded-button border-2 border-border bg-canvas px-3 py-2 text-body"
        />
      </label>
      <label className="flex flex-col gap-1">
        <span className="text-label">{t('admin.lessonContent.examplesLabel')}</span>
        <textarea
          value={examples}
          onChange={(event) => setExamples(event.target.value)}
          rows={4}
          lang="en"
          aria-invalid={problem === 'refused'}
          aria-describedby={problem ? `${id}-problem` : undefined}
          className="rounded-button border-2 border-border bg-canvas px-3 py-2 text-body"
        />
      </label>
      {problem && (
        <p id={`${id}-problem`} role="alert" className="text-caption text-error">
          {t(problem === 'refused' ? 'admin.lessonContent.refused' : 'admin.lessonContent.failed')}
        </p>
      )}
      <div className="flex items-center gap-3">
        <button type="submit" disabled={set.isPending} className="btn-secondary w-auto">
          {t('admin.lessonContent.save')}
        </button>
        <button type="button" disabled={set.isPending} onClick={() => onDone(false)} className="btn-link min-h-target">
          {t('admin.lessonContent.cancel')}
        </button>
      </div>
    </form>
  )
}
