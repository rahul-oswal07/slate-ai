import type { TFunction } from 'i18next'
import { useEffect, useId, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { lessonName, pageText } from '../lessons/lessonText'
import { AdminPage, AdminQuery } from './AdminPage'
import { type AdminLessonLink, type AdminLessonLinks, useAdminLessonLinks, useSetLessonLink } from './useAdmin'

type Chapter = AdminLessonLinks['chapters'][number]

/**
 * /admin/lesson-links (T-94, D-106; open without a key, D-112): every chapter and lesson of the curriculum with its video link. Whoever sets
 * a link here has checked it on a phone; code copies it into a new today's message for that lesson, never the AI.
 * Chapters start folded with "N of M linked"; search and "Only lessons without a link" narrow the list; she types in
 * as many rows as she likes and saves them together from the bar at the bottom.
 */
export function LessonLinksAdminScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.lessonLinks.title')}><LessonLinks /></AdminPage>
}

function LessonLinks() {
  const links = useAdminLessonLinks()
  return <AdminQuery query={links}>{(data) => <Editor data={data} />}</AdminQuery>
}

/** A lesson matches the search by its English or Kannada name or its ID. */
const matches = (lesson: AdminLessonLink, search: string) =>
  search === '' || [lesson.nameEn, lesson.nameKn, lesson.id].some((text) => text.toLowerCase().includes(search))

function Editor({ data }: { data: AdminLessonLinks }) {
  const { t, i18n } = useTranslation()
  const set = useSetLessonLink()
  const [search, setSearch] = useState('')
  const [onlyMissing, setOnlyMissing] = useState(false)
  const [open, setOpen] = useState<Set<number>>(() => new Set())
  // What she typed and hasn't saved yet: lesson ID → value. Kept while chapters fold or the list is filtered.
  const [drafts, setDrafts] = useState<Map<string, string>>(() => new Map())
  const [refused, setRefused] = useState<Set<string>>(() => new Set())
  const [saving, setSaving] = useState(false)
  const [result, setResult] = useState<{ saved: number; notSaved: number } | null>(null)

  const query = search.trim().toLowerCase()
  const filtering = query !== '' || onlyMissing
  const chapters = useMemo(
    () =>
      data.chapters
        .map((chapter) => ({
          chapter,
          lessons: chapter.lessons.filter((lesson) => matches(lesson, query) && (!onlyMissing || !lesson.url)),
        }))
        .filter(({ lessons }) => !filtering || lessons.length > 0),
    [data, query, onlyMissing, filtering],
  )
  const saved = new Map(data.chapters.flatMap((c) => c.lessons.map((l) => [l.id, l.url ?? ''] as const)))
  const changes = [...drafts].filter(([id, value]) => value.trim() !== (saved.get(id) ?? ''))

  // A browser warning before she leaves with links typed but not saved.
  useEffect(() => {
    if (changes.length === 0) {
      return
    }
    const warn = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [changes.length])

  function edit(id: string, value: string) {
    setResult(null)
    setRefused((current) => (current.has(id) ? new Set([...current].filter((r) => r !== id)) : current))
    setDrafts((current) => new Map(current).set(id, value))
  }

  async function saveAll() {
    setSaving(true)
    setResult(null)
    let done = 0
    let notSaved = 0
    const badLinks = new Set<string>()
    // One request per lesson, in order; each is idempotent on its own clientRequestId.
    for (const [id, value] of changes) {
      try {
        await set.mutateAsync({ id, url: value })
        done++
        setDrafts((current) => {
          const next = new Map(current)
          next.delete(id)
          return next
        })
      } catch (error) {
        // Not saved: it stays as typed. A refused link (400) says how to fix it; anything else just counts here.
        notSaved++
        if (error instanceof ApiError && error.status === 400) {
          badLinks.add(id)
        }
      }
    }
    setRefused(badLinks)
    setResult({ saved: done, notSaved })
    setSaving(false)
  }

  function toggle(number: number) {
    setOpen((current) => {
      const next = new Set(current)
      if (!next.delete(number)) {
        next.add(number)
      }
      return next
    })
  }

  return (
    <div className="flex flex-col gap-3 pb-24">
      <p className="text-body text-ink-muted">{t('admin.lessonLinks.intro')}</p>
      <div className="flex flex-col gap-2">
        <label className="flex flex-col gap-1">
          <span className="text-label">{t('admin.lessonLinks.search')}</span>
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="min-h-target rounded-button border-2 border-border bg-surface px-3 text-body"
          />
        </label>
        <label className="flex min-h-target items-center gap-2 text-body">
          <input type="checkbox" checked={onlyMissing} onChange={(event) => setOnlyMissing(event.target.checked)} className="size-5" />
          {t('admin.lessonLinks.onlyMissing')}
        </label>
      </div>

      {chapters.length === 0 && <p className="text-body text-ink-muted">{t('admin.lessonLinks.noMatches')}</p>}

      <ul className="flex flex-col gap-2">
        {chapters.map(({ chapter, lessons }) => (
          <li key={chapter.number}>
            <ChapterGroup
              chapter={chapter}
              lessons={lessons}
              title={chapterTitle(t, chapter, i18n.language)}
              open={filtering || open.has(chapter.number)}
              onToggle={() => toggle(chapter.number)}
              drafts={drafts}
              refused={refused}
              onEdit={edit}
            />
          </li>
        ))}
      </ul>

      {(changes.length > 0 || result) && (
        <div className="fixed inset-x-0 bottom-0 z-10 border-t border-edge bg-surface px-4 py-3">
          <div className="mx-auto flex max-w-xl flex-wrap items-center justify-between gap-2">
            <output className="text-body">
              {changes.length > 0
                ? t('admin.lessonLinks.unsavedCount', { count: changes.length })
                : result && result.notSaved > 0
                  ? t('admin.lessonLinks.notSavedCount', { saved: result.saved, count: result.notSaved })
                  : result && t('admin.lessonLinks.savedCount', { count: result.saved })}
            </output>
            {changes.length > 0 && (
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  disabled={saving}
                  onClick={() => {
                    setDrafts(new Map())
                    setRefused(new Set())
                  }}
                  className="btn-link min-h-target"
                >
                  {t('admin.lessonLinks.discard')}
                </button>
                <button type="button" disabled={saving} onClick={() => void saveAll()} className="btn-secondary w-auto">
                  {t('admin.lessonLinks.saveAll')}
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

function chapterTitle(t: TFunction, chapter: Chapter, language: string) {
  const name = language === 'kn' ? chapter.nameKn : chapter.nameEn
  return chapter.number === 0 ? name : t('admin.lessonLinks.chapter', { number: chapter.number, name })
}

function ChapterGroup({
  chapter,
  lessons,
  title,
  open,
  onToggle,
  drafts,
  refused,
  onEdit,
}: {
  chapter: Chapter
  lessons: AdminLessonLink[]
  title: string
  open: boolean
  onToggle: () => void
  drafts: Map<string, string>
  refused: Set<string>
  onEdit: (id: string, value: string) => void
}) {
  const { t } = useTranslation()
  const id = useId()
  const linked = chapter.lessons.filter((lesson) => lesson.url).length

  return (
    <section className="rounded-card border-[1.5px] border-edge bg-surface">
      <h2>
        <button
          type="button"
          aria-expanded={open}
          aria-controls={`${id}-lessons`}
          onClick={onToggle}
          className="flex min-h-target w-full items-center justify-between gap-2 px-3 text-left text-label"
        >
          <span>{title}</span>
          <span className="flex shrink-0 items-center gap-2 text-caption text-ink-muted">
            {t('admin.lessonLinks.linkedCount', { linked, total: chapter.lessons.length })}
            <Icon name={open ? 'chevronUp' : 'chevronDown'} size={20} />
          </span>
        </button>
      </h2>
      {open && (
        <ul id={`${id}-lessons`} className="flex flex-col divide-y divide-divider border-t border-divider">
          {lessons.map((lesson) => (
            <li key={lesson.id}>
              <LessonRow
                lesson={lesson}
                value={drafts.get(lesson.id) ?? lesson.url ?? ''}
                refused={refused.has(lesson.id)}
                onEdit={(value) => onEdit(lesson.id, value)}
              />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function LessonRow({
  lesson,
  value,
  refused,
  onEdit,
}: {
  lesson: AdminLessonLink
  value: string
  refused: boolean
  onEdit: (value: string) => void
}) {
  const { t, i18n } = useTranslation()
  const inputId = `link-${lesson.id}`
  const errorId = `${inputId}-error`
  const unsaved = value.trim() !== (lesson.url ?? '')

  return (
    <div className="flex flex-col gap-1 px-3 py-2 sm:flex-row sm:items-center sm:gap-3">
      <label htmlFor={inputId} className="flex min-w-0 flex-col sm:w-1/2">
        <span className="text-body">{lessonName(lesson, i18n.language)}</span>
        {lesson.textbookPage && <span className="text-caption text-ink-muted">{pageText(t, lesson.textbookPage)}</span>}
      </label>
      <div className="flex min-w-0 flex-1 items-center gap-2">
        <input
          id={inputId}
          type="url"
          inputMode="url"
          value={value}
          onChange={(event) => onEdit(event.target.value)}
          placeholder="https://diksha.gov.in/…"
          aria-invalid={refused}
          aria-describedby={refused ? errorId : undefined}
          className="min-h-target w-full min-w-0 rounded-button border-2 border-border bg-canvas px-3 text-body"
        />
        {unsaved && (
          <span className="shrink-0" title={t('admin.lessonLinks.unsaved')}>
            <span aria-hidden="true" className="block size-2.5 rounded-full bg-warning" />
            <span className="sr-only">{t('admin.lessonLinks.unsaved')}</span>
          </span>
        )}
      </div>
      {refused && (
        <p id={errorId} role="alert" className="text-caption text-error sm:basis-full">
          {t('admin.lessonLinks.refused')}
        </p>
      )}
    </div>
  )
}
