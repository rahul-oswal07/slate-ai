import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import type { RosterChild } from './types'

/**
 * Correct the SATS register, or mark it here when SATS hasn't come in (T1 `correct` state, D-03).
 * Edits a draft: Save commits, Cancel discards (A-14). Tiles are native checkboxes, checked = absent (A-15).
 */
export function AttendanceGrid({
  mode,
  roster,
  initialAbsent,
  onCancel,
  onSave,
}: {
  mode: 'correct' | 'manual'
  roster: RosterChild[]
  initialAbsent: string[]
  onCancel: () => void
  onSave: (absentChildIds: string[]) => void
}) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const [absent, setAbsent] = useState(() => new Set(initialAbsent))

  function toggle(id: string) {
    setAbsent((current) => {
      const next = new Set(current)
      if (!next.delete(id)) {
        next.add(id)
      }
      return next
    })
  }

  function cancel() {
    announce(t('announce.attDiscarded'))
    onCancel()
  }

  function save() {
    announce(t('announce.attSaved'))
    onSave(roster.filter((child) => absent.has(child.id)).map((child) => child.id))
  }

  return (
    <section aria-labelledby="grid-title" className="scroll-pb-28">
      <h2 id="grid-title" ref={headingRef} tabIndex={-1} className="text-title">
        {mode === 'correct' ? t('today.attendance.edit.titleCorrect') : t('today.attendance.edit.titleManual')}
      </h2>
      <p className="mb-3 mt-1 text-body text-ink-muted">
        {mode === 'correct' ? t('today.attendance.edit.hintCorrect') : t('today.attendance.edit.hintManual')}
      </p>

      <fieldset className="grid grid-cols-4 gap-2">
        <legend className="sr-only">{t('today.attendance.grid')}</legend>
        {roster.map((child) => {
          const isAbsent = absent.has(child.id)
          const state = isAbsent ? t('today.attendance.absent') : t('today.attendance.present')
          return (
            <label
              key={child.id}
              className={`flex min-h-[64px] cursor-pointer flex-col items-center justify-center rounded-button p-1 text-center has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus ${
                isAbsent ? 'border-2 border-error bg-error-bg text-error' : 'border border-border bg-surface text-ink'
              }`}
            >
              <input
                type="checkbox"
                checked={isAbsent}
                onChange={() => toggle(child.id)}
                aria-label={t('today.attendance.tile', { roll: child.rollNo, name: child.firstName, state })}
                className="sr-only"
              />
              <span className="text-caption" aria-hidden="true">
                {child.rollNo}
              </span>
              <span className="text-label leading-tight [overflow-wrap:anywhere]" aria-hidden="true">
                {child.firstName}
              </span>
              {isAbsent && (
                <span className="text-caption" aria-hidden="true">
                  {t('today.attendance.absent')}
                </span>
              )}
            </label>
          )
        })}
      </fieldset>

      <div className="sticky bottom-[72px] mt-3 border-t border-divider bg-canvas py-3">
        <p className="mb-2 text-center text-label" aria-live="polite">
          {t('today.attendance.edit.count', { absent: absent.size, present: roster.length - absent.size })}
        </p>
        <div className="grid grid-cols-2 gap-2">
          <button
            type="button"
            onClick={cancel}
            className="min-h-target rounded-button border-2 border-border bg-surface text-label"
          >
            {t('common.cancel')}
          </button>
          <button
            type="button"
            onClick={save}
            className="min-h-target rounded-button bg-primary font-extrabold text-label text-on-primary"
          >
            {t('common.save')}
          </button>
        </div>
      </div>
    </section>
  )
}
