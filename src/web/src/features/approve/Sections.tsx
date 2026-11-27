import { type ReactNode, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import type { SectionKind } from './sectionKinds'

/**
 * One section: a header that opens it (one open at a time, the caller decides), "Removed: not sent" when she removed it,
 * and its body.
 */
export function SectionCard({
  kind,
  removed,
  open,
  onToggle,
  children,
}: {
  kind: SectionKind
  removed: boolean
  open: boolean
  onToggle: () => void
  children: ReactNode
}) {
  const { t } = useTranslation()
  const id = useId()
  return (
    <div className={`rounded-card border-[1.5px] border-edge bg-surface ${removed ? 'opacity-70' : ''}`}>
      <h3>
        <button
          type="button"
          aria-expanded={open}
          aria-controls={`${id}-body`}
          onClick={onToggle}
          className="flex min-h-target w-full items-center justify-between gap-2 px-[14px] text-left text-label"
        >
          <span>
            {t(`todays.section.${kind}`)}
            {removed && <span className="ml-2 text-caption text-ink-muted">{t('todays.removedTag')}</span>}
          </span>
          <Icon name={open ? 'chevronUp' : 'chevronDown'} size={20} className="shrink-0 text-ink-muted" />
        </button>
      </h3>
      {open && (
        <div id={`${id}-body`} className="flex flex-col gap-3 border-t border-divider p-[14px]">
          {children}
        </div>
      )}
    </div>
  )
}

/** Edit (or Add my note) and Remove / Put back, under a section she can still change. */
export function SectionActions({
  editLabel,
  onEdit,
  remove,
}: {
  /** Null: no Edit (the picture, the video, a removed section). */
  editLabel: string | null
  onEdit: () => void
  /** Null: this section can't be removed. */
  remove: { removed: boolean; pending: boolean; onToggle: () => void } | null
}) {
  const { t } = useTranslation()
  if (editLabel === null && remove === null) {
    return null
  }
  return (
    <div className="flex flex-wrap gap-2">
      {editLabel !== null && (
        <button type="button" onClick={onEdit} className="btn-secondary w-auto">
          {editLabel}
        </button>
      )}
      {remove && (
        <button type="button" disabled={remove.pending} onClick={remove.onToggle} className="btn-link">
          {remove.removed ? t('todays.restore') : t('todays.remove')}
        </button>
      )}
    </div>
  )
}

export interface EditField {
  label: string
  value: string
  placeholder?: string
  rows?: number
  /** The language she writes in, when it is fixed (the family's for a catch-up). */
  lang?: string
}

/**
 * Her words for one section (D-92): one box per field, Save and Cancel. The API checks them; a refusal comes back as
 * `problem` (already in words) and nothing is saved. `more`: she may add boxes (the activity's steps), up to `max`.
 */
export function SectionEditForm({
  fields,
  more,
  hint,
  problem,
  conflict,
  pending,
  onSave,
  onCancel,
}: {
  fields: EditField[]
  more?: { max: number; label: (n: number) => string; placeholder: string; add: string }
  hint: string
  problem: string | null
  conflict: boolean
  pending: boolean
  onSave: (values: string[]) => void
  onCancel: () => void
}) {
  const { t } = useTranslation()
  const [values, setValues] = useState<string[]>(fields.length > 0 ? fields.map((f) => f.value) : [''])
  const errorId = useId()
  const fieldFor = (i: number): EditField =>
    fields[i] ?? { label: more?.label(i + 1) ?? '', value: '', placeholder: more?.placeholder, rows: 2 }

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(e) => {
        e.preventDefault()
        onSave(values)
      }}
    >
      {values.map((value, i) => {
        const field = fieldFor(i)
        return (
          <label key={i} className="flex flex-col gap-1 text-caption">
            {field.label}
            <textarea
              value={value}
              rows={field.rows ?? 4}
              lang={field.lang}
              placeholder={field.placeholder}
              aria-describedby={problem ? errorId : undefined}
              onChange={(e) => setValues(values.map((v, j) => (j === i ? e.target.value : v)))}
              className="rounded-button border-2 border-border bg-surface p-2 text-body text-ink"
            />
          </label>
        )
      })}
      {more && values.length < more.max && (
        <button type="button" onClick={() => setValues([...values, ''])} className="btn-link self-start">
          {more.add}
        </button>
      )}
      {problem && (
        <p id={errorId} role="alert" className="text-caption text-error">
          {problem}
        </p>
      )}
      {conflict && !problem && (
        <p role="alert" className="text-caption text-error">
          {t('todays.changed')}
        </p>
      )}
      <p className="text-caption text-ink-muted">{hint}</p>
      <div className="flex gap-2">
        <button type="submit" disabled={pending} className="btn-primary w-auto">
          {t('todays.save')}
        </button>
        <button type="button" onClick={onCancel} className="btn-secondary w-auto">
          {t('common.cancel')}
        </button>
      </div>
    </form>
  )
}
