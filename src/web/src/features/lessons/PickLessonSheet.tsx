import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { chapterTitle, findLesson, lessonName, pageText } from './lessonText'
import type { Chapter } from './types'

/**
 * Pick today's lesson from the chapter list (T2 pick sheet, FR-04), single-choice mode: one tap saves.
 * A native modal <dialog> (focus trapped, page behind inert); Esc, the backdrop or Close shut it and focus
 * goes back to the opener. "Next in the book" (from the server) is listed first; "Another chapter" is never
 * a dead end.
 */
export function PickLessonSheet({
  chapters,
  nextInBook,
  currentId,
  onPick,
  onClose,
}: {
  chapters: Chapter[]
  nextInBook: string | null
  currentId: string | null
  onPick: (id: string) => void
  onClose: () => void
}) {
  const { t, i18n } = useTranslation()
  const sheetRef = useRef<HTMLDialogElement>(null)
  const opener = useRef<Element | null>(null)
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  })
  const startId = currentId ?? nextInBook
  const [chapterNumber, setChapterNumber] = useState(
    () => (startId && findLesson(chapters, startId)?.chapter.number) ?? chapters[0]?.number ?? 0,
  )
  const [choosingChapter, setChoosingChapter] = useState(false)
  const chapter = chapters.find((c) => c.number === chapterNumber) ?? chapters[0]

  useEffect(() => {
    opener.current = document.activeElement
    const dialog = sheetRef.current
    if (dialog && !dialog.open) {
      // jsdom has no showModal; real browsers do.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal()
      } else {
        dialog.setAttribute('open', '')
      }
    }
    // Backdrop tap closes (mouse/touch only; Esc and Close cover the keyboard). It lands on the <dialog> itself.
    const onBackdrop = (event: MouseEvent) => {
      if (event.target === dialog) {
        onCloseRef.current()
      }
    }
    dialog?.addEventListener('click', onBackdrop)
    return () => {
      dialog?.removeEventListener('click', onBackdrop)
      if (opener.current instanceof HTMLElement) {
        opener.current.focus()
      }
    }
  }, [])

  // Focus the chosen lesson (or the first control) whenever the view changes.
  useEffect(() => {
    const sheet = sheetRef.current
    const target = sheet?.querySelector<HTMLElement>('input:checked') ?? sheet?.querySelector<HTMLElement>('input, button')
    target?.focus()
  }, [chapterNumber, choosingChapter])

  if (!chapter) {
    return null
  }

  const lessons = [...chapter.competencies]
  const nextIndex = lessons.findIndex((l) => l.id === nextInBook)
  if (nextIndex > 0) {
    lessons.unshift(...lessons.splice(nextIndex, 1))
  }
  const title = choosingChapter ? t('pick.anotherChapter') : chapterTitle(t, chapter, i18n.language)

  return (
    <dialog
      ref={sheetRef}
      aria-labelledby="pick-title"
      onCancel={(event) => {
        // Esc: close through React state so focus returns to the opener.
        event.preventDefault()
        onClose()
      }}
      className="fixed inset-x-0 bottom-0 top-auto m-0 mx-auto max-h-[85dvh] w-full max-w-md overflow-y-auto rounded-t-md border border-border bg-surface p-4 pb-6 text-ink backdrop:bg-ink/40"
    >
        <div className="mb-3 flex items-start justify-between gap-2">
          <h2 id="pick-title" className="text-title">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('common.close')}
            className="min-h-target min-w-target rounded-md text-title text-ink-muted"
          >
            ×
          </button>
        </div>

        {choosingChapter ? (
          <ul className="flex flex-col gap-2">
            {chapters.map((c) => (
              <li key={c.number}>
                <button
                  type="button"
                  onClick={() => {
                    setChapterNumber(c.number)
                    setChoosingChapter(false)
                  }}
                  className="min-h-target w-full rounded-md border border-border bg-surface px-3 py-2 text-left text-body"
                >
                  {chapterTitle(t, c, i18n.language)}
                </button>
              </li>
            ))}
          </ul>
        ) : (
          <>
            <fieldset className="flex flex-col gap-2">
              <legend className="sr-only">{title}</legend>
              {lessons.map((lesson) => (
                <label
                  key={lesson.id}
                  className="flex min-h-target cursor-pointer items-center gap-3 rounded-md border border-border bg-surface px-3 py-2 has-[:checked]:border-2 has-[:checked]:border-primary has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus"
                >
                  <input
                    type="radio"
                    name="pick-lesson"
                    value={lesson.id}
                    checked={lesson.id === currentId}
                    onChange={() => onPick(lesson.id)}
                    className="size-6 shrink-0 accent-primary"
                  />
                  <span className="min-w-0">
                    {lesson.id === nextInBook && (
                      <span className="block text-caption text-primary-strong">{t('pick.nextInBook')}</span>
                    )}
                    <span className="block text-body">{lessonName(lesson, i18n.language)}</span>
                    <span className="block text-caption text-ink-muted">{pageText(t, lesson.textbookPage)}</span>
                  </span>
                </label>
              ))}
            </fieldset>
            <button
              type="button"
              onClick={() => setChoosingChapter(true)}
              className="mt-3 min-h-target w-full text-label text-primary-strong"
            >
              {t('pick.anotherChapter')}
            </button>
          </>
        )}
    </dialog>
  )
}
