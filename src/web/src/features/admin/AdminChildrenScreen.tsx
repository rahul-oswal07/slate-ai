import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../api/client'
import { AdminPage, AdminQuery } from './AdminPage'
import { type AdminChild, type AdminChildren, useAdminChildren, useSetParentPhone } from './useAdmin'

/**
 * /admin/children (T-118, D-110; open without a key, D-112): every class's children with the number their family's WhatsApp goes to. A
 * number saved here is dialled first; without one, the `WhatsApp__Recipients__{childId}` server setting is used; with
 * neither, WhatsApp skips the family. Seeded numbers are fictional and never shown. Saved numbers survive the demo Reset.
 */
export function AdminChildrenScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.children.title')}><Children /></AdminPage>
}

function Children() {
  const children = useAdminChildren()
  return <AdminQuery query={children}>{(data) => <Classes data={data} />}</AdminQuery>
}

function Classes({ data }: { data: AdminChildren }) {
  const { t } = useTranslation()
  return (
    <>
      <p className="text-body text-ink-muted">{t('admin.children.intro')}</p>
      {data.classes.map((cls) => (
        <section key={cls.id} aria-labelledby={`class-${cls.id}`} className="flex flex-col gap-2">
          <h2 id={`class-${cls.id}`} className="text-label">
            {t('admin.children.class', { label: cls.label, count: cls.children.length })}
          </h2>
          <ul className="flex flex-col divide-y divide-divider rounded-card border-[1.5px] border-edge bg-surface">
            {cls.children.map((child) => (
              <ChildRow key={child.id} child={child} />
            ))}
          </ul>
        </section>
      ))}
    </>
  )
}

function ChildRow({ child }: { child: AdminChild }) {
  const { t } = useTranslation()
  const set = useSetParentPhone()
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState('')
  const [saved, setSaved] = useState<'saved' | 'cleared' | null>(null)
  const inputId = useId()
  const errorId = `${inputId}-error`
  const refused = set.error instanceof ApiError && set.error.status === 400
  const name = t('admin.children.child', {
    rollNo: child.rollNo,
    name: child.firstName,
    language: t(`missed.language.${child.homeLanguage}`),
  })

  const save = (phone: string, outcome: 'saved' | 'cleared') =>
    set.mutate(
      { id: child.id, phone },
      {
        onSuccess: () => {
          setEditing(false)
          setSaved(outcome)
        },
      },
    )

  return (
    <li className="flex flex-col gap-2 p-3">
      <div className="flex flex-wrap items-baseline justify-between gap-x-3">
        <span className="text-body font-bold">{name}</span>
        {(child.consent !== 'yes' || child.stopped) && (
          <span className="text-caption text-status-stopped">
            {t(child.stopped ? 'admin.children.stopped' : 'admin.children.noConsent')}
          </span>
        )}
      </div>

      {editing ? (
        <form
          className="flex flex-col gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            save(value, value.trim() === '' ? 'cleared' : 'saved')
          }}
        >
          <label htmlFor={inputId} className="text-caption text-ink-muted">
            {t('admin.children.phoneLabel', { name: child.firstName })}
          </label>
          <input
            id={inputId}
            type="tel"
            inputMode="tel"
            autoComplete="off"
            value={value}
            onChange={(event) => setValue(event.target.value)}
            placeholder="98765 43210"
            aria-invalid={refused}
            aria-describedby={refused ? errorId : undefined}
            className="min-h-target w-full rounded-button border-2 border-border bg-canvas px-3 text-body"
          />
          {refused && (
            <p id={errorId} role="alert" className="text-caption text-error">
              {t('admin.children.refused')}
            </p>
          )}
          {set.isError && !refused && (
            <p role="alert" className="text-caption text-error">
              {t('admin.children.failed')}
            </p>
          )}
          <div className="flex gap-2">
            <button type="submit" disabled={set.isPending} className="btn-primary w-auto flex-1">
              {t('admin.children.save')}
            </button>
            <button
              type="button"
              onClick={() => {
                setEditing(false)
                set.reset()
              }}
              className="btn-secondary w-auto flex-1"
            >
              {t('admin.children.cancel')}
            </button>
          </div>
        </form>
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex min-w-0 flex-col">
            {child.phone ? (
              <>
                <span className="text-body tabular-nums">{child.phone}</span>
                <span className="text-caption text-ink-muted">
                  {t(child.phoneFrom === 'saved' ? 'admin.children.fromSaved' : 'admin.children.fromSetting')}
                </span>
              </>
            ) : (
              <span className="text-body text-ink-muted">{t('admin.children.noNumber')}</span>
            )}
            {saved && (
              <output className="text-caption text-primary-strong">
                {t(saved === 'saved' ? 'admin.children.saved' : 'admin.children.cleared')}
              </output>
            )}
          </div>
          <div className="flex gap-2">
            {child.phoneFrom === 'saved' && (
              <button
                type="button"
                disabled={set.isPending}
                onClick={() => save('', 'cleared')}
                className="btn-secondary w-auto"
                aria-label={t('admin.children.clearFor', { name: child.firstName })}
              >
                {t('admin.children.clear')}
              </button>
            )}
            <button
              type="button"
              onClick={() => {
                setValue(child.phoneFrom === 'saved' ? (child.phone ?? '') : '')
                setSaved(null)
                set.reset()
                setEditing(true)
              }}
              className="btn-secondary w-auto"
              aria-label={t('admin.children.editFor', { name: child.firstName })}
            >
              {t('admin.children.edit')}
            </button>
          </div>
        </div>
      )}
    </li>
  )
}
