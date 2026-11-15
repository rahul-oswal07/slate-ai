import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../api/client'
import { lessonName, pageText } from '../lessons/lessonText'
import { AdminPage, AdminQuery } from './AdminPage'
import { type AdminLessonLink, useAdminLessonLinks, useSetLessonLink } from './useAdmin'

/**
 * /admin/lesson-links#key=… (T-94, D-106): every chapter and lesson of the curriculum with its video link. Whoever sets
 * a link here has checked it on a phone; code copies it into a new today's message for that lesson, never the AI.
 */
export function LessonLinksAdminScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.lessonLinks.title')}>{(key) => <LessonLinks adminKey={key} />}</AdminPage>
}

function LessonLinks({ adminKey: key }: { adminKey: string | null }) {
  const { t, i18n } = useTranslation()
  const links = useAdminLessonLinks(key)

  return (
    <AdminQuery query={links}>
      {({ chapters }) => (
        <>
          <p className="text-body text-ink-muted">{t('admin.lessonLinks.intro')}</p>
          {chapters.map((chapter) => (
            <section key={chapter.number} aria-labelledby={`chapter-${chapter.number}`} className="flex flex-col gap-3">
              <h2 id={`chapter-${chapter.number}`} className="text-label">
                {chapter.number === 0
                  ? i18n.language === 'kn' ? chapter.nameKn : chapter.nameEn
                  : t('admin.lessonLinks.chapter', {
                      number: chapter.number,
                      name: i18n.language === 'kn' ? chapter.nameKn : chapter.nameEn,
                    })}
              </h2>
              <ul className="flex flex-col gap-3">
                {chapter.lessons.map((lesson) => (
                  <li key={lesson.id}>
                    <LessonLinkForm adminKey={key} lesson={lesson} />
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </>
      )}
    </AdminQuery>
  )
}

function LessonLinkForm({ adminKey: key, lesson }: { adminKey: string | null; lesson: AdminLessonLink }) {
  const { t, i18n } = useTranslation()
  const [value, setValue] = useState(lesson.url ?? '')
  const [saved, setSaved] = useState<'saved' | 'removed' | null>(null)
  const set = useSetLessonLink(key)
  const name = lessonName(lesson, i18n.language)
  const inputId = `link-${lesson.id}`
  const errorId = `${inputId}-error`
  const refused = set.error instanceof ApiError && set.error.status === 400
  const failed = set.isError && !refused

  function save(url: string) {
    setSaved(null)
    set.mutate(
      { id: lesson.id, url },
      {
        onSuccess: (result) => {
          setValue(result.url ?? '')
          setSaved(result.url ? 'saved' : 'removed')
        },
      },
    )
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    save(value)
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge bg-surface p-3">
      <label htmlFor={inputId} className="flex flex-col">
        <span className="text-body">{name}</span>
        {lesson.textbookPage && <span className="text-caption text-ink-muted">{pageText(t, lesson.textbookPage)}</span>}
      </label>
      <input
        id={inputId}
        type="url"
        inputMode="url"
        value={value}
        onChange={(event) => {
          setValue(event.target.value)
          setSaved(null)
        }}
        placeholder="https://diksha.gov.in/…"
        aria-invalid={refused}
        aria-describedby={refused ? errorId : undefined}
        className="min-h-target rounded-button border-2 border-border bg-canvas px-3 text-body"
      />
      {refused && (
        <p id={errorId} role="alert" className="text-caption text-error">
          {t('admin.lessonLinks.refused')}
        </p>
      )}
      {failed && (
        <p role="alert" className="text-caption text-error">
          {t('admin.lessonLinks.failed')}
        </p>
      )}
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={set.isPending || value.trim() === (lesson.url ?? '')}
          className="btn-secondary w-auto"
          aria-label={t('admin.lessonLinks.saveFor', { name })}
        >
          {t('admin.lessonLinks.save')}
        </button>
        {lesson.url && (
          <button
            type="button"
            disabled={set.isPending}
            onClick={() => save('')}
            className="btn-link min-h-target"
            aria-label={t('admin.lessonLinks.removeFor', { name })}
          >
            {t('admin.lessonLinks.remove')}
          </button>
        )}
        <output className="text-caption text-ink-muted">
          {saved === 'saved' ? t('admin.lessonLinks.saved') : saved === 'removed' ? t('admin.lessonLinks.removed') : ''}
        </output>
      </div>
    </form>
  )
}
