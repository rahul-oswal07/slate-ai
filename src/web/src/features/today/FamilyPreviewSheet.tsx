import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useEveningPreview } from '../evening/useEvening'
import { formatSchoolTime } from './schoolTime'

/**
 * One family's real evening message, from Tonight's message on Today (T-86, D-126): the text with the child's name and
 * the lesson filled by code, in their language, and whether today's message goes with it. Read only; the family's step
 * pages aren't replayed here. A native modal <dialog> like the pick sheet: Esc, the backdrop or Close shut it and focus
 * goes back to the Preview button.
 */
export function FamilyPreviewSheet({
  classId,
  date,
  child,
  onClose,
}: {
  classId: string
  date: string
  child: { id: string; firstName: string }
  onClose: () => void
}) {
  const { t, i18n } = useTranslation()
  const preview = useEveningPreview(classId, date, child.id)
  const sheetRef = useRef<HTMLDialogElement>(null)
  const onCloseRef = useRef(onClose)
  // The Preview button, read before the modal takes focus: an effect can run twice (StrictMode), and by the second run
  // focus is already inside the dialog.
  const [opener] = useState(() => document.activeElement)
  useEffect(() => {
    onCloseRef.current = onClose
  })

  useEffect(() => {
    const dialog = sheetRef.current
    if (dialog && !dialog.open) {
      // jsdom has no showModal; real browsers do.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal()
      } else {
        dialog.setAttribute('open', '')
      }
    }
    const onBackdrop = (event: MouseEvent) => {
      if (event.target === dialog) {
        onCloseRef.current()
      }
    }
    dialog?.addEventListener('click', onBackdrop)
    return () => {
      dialog?.removeEventListener('click', onBackdrop)
      if (opener instanceof HTMLElement) {
        opener.focus()
      }
    }
  }, [opener])

  const data = preview.data
  return (
    <dialog
      ref={sheetRef}
      aria-labelledby="family-preview-title"
      onCancel={(event) => {
        // Esc: close through React state so focus returns to the opener.
        event.preventDefault()
        onClose()
      }}
      className="fixed inset-x-0 bottom-0 top-auto m-0 mx-auto max-h-[85dvh] w-full max-w-md overflow-y-auto rounded-t-md border border-border bg-surface p-4 pb-6 text-ink backdrop:bg-ink/40"
    >
      <div className="mb-3 flex items-start justify-between gap-2">
        <h2 id="family-preview-title" className="text-title">
          {t('today.tonight.preview.title', {
            child: child.firstName,
            language: data ? t(`language.${data.language}`) : '',
          })}
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

      {preview.isPending && <div aria-hidden="true" className="h-24 rounded-button border-2 border-border bg-surface" />}
      {preview.isError && (
        <p role="alert" className="text-body">
          {t('app.loadError')}
        </p>
      )}
      {data && (
        <div className="flex flex-col gap-3">
          <p lang={data.language} className="card whitespace-pre-line p-[14px] text-body">
            {data.text}
          </p>
          <p className="text-body text-ink">
            {t(data.withTodays ? 'today.tonight.preview.withTodays' : 'today.tonight.preview.textOnly')}
          </p>
          {data.sent && data.sentAt && (
            <p className="text-caption text-ink-muted">
              {t('today.tonight.preview.sentAt', { at: formatSchoolTime(data.sentAt, i18n.language) })}
            </p>
          )}
        </div>
      )}
    </dialog>
  )
}
