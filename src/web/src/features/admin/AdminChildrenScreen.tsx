import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ApiError } from '../../api/client'
import { type Language, SUPPORTED_LANGUAGES } from '../../i18n'
import { AdminPage, AdminQuery } from './AdminPage'
import {
  type AdminChild,
  type AdminChildren,
  type PhoneSlot,
  useAdminChildren,
  useSetHomeLanguage,
  useSetParentPhone,
} from './useAdmin'

/**
 * /admin/children (T-118; open without a key, D-112): every class's children with the numbers their family's WhatsApp
 * goes to. WhatsApp uses only numbers saved here (T-123, D-114): each family has an optional Number 1 and Number 2, and
 * every message goes to each one saved (T-140, D-122); with neither it skips the family. Seeded numbers are fictional
 * and never shown. Saved numbers survive the demo Reset.
 * Each child's home language can be changed here too (T-122, D-113): it is used for everything made after the change
 * and also survives the Reset.
 * In the demo class each child links to their simulated family phone, /demo/family/:childId (T-130).
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
              <ChildRow key={child.id} child={child} demo={cls.demo} />
            ))}
          </ul>
        </section>
      ))}
    </>
  )
}

function ChildRow({ child, demo }: { child: AdminChild; demo: boolean }) {
  const { t } = useTranslation()
  const name = t('admin.children.child', {
    rollNo: child.rollNo,
    name: child.firstName,
    language: t(`missed.language.${child.homeLanguage}`),
  })

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

      {demo && (
        <Link
          to={`/demo/family/${child.id}`}
          aria-label={t('admin.children.familyPhoneFor', { name: child.firstName })}
          className="self-start text-body font-bold text-primary-strong underline"
        >
          {t('admin.children.familyPhone')}
        </Link>
      )}

      <LanguagePicker child={child} />

      {!child.phone && !child.phone2 && (
        <span className="text-body text-ink-muted">{t('admin.children.noNumber')}</span>
      )}
      <PhoneNumber child={child} slot={1} />
      <PhoneNumber child={child} slot={2} />
    </li>
  )
}

/** One of the family's two numbers (T-140, D-122), with its own Edit, Save and Clear; the other is left as it is. */
function PhoneNumber({ child, slot }: { child: AdminChild; slot: PhoneSlot }) {
  const { t } = useTranslation()
  const set = useSetParentPhone()
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState('')
  const [saved, setSaved] = useState<'saved' | 'cleared' | null>(null)
  const inputId = useId()
  const errorId = `${inputId}-error`
  const refused = set.error instanceof ApiError && set.error.status === 400
  const phone = (slot === 1 ? child.phone : child.phone2) ?? null
  const label = { name: child.firstName, slot }

  const save = (next: string, outcome: 'saved' | 'cleared') =>
    set.mutate(
      { id: child.id, phone: next, slot },
      {
        onSuccess: () => {
          setEditing(false)
          setSaved(outcome)
        },
      },
    )

  if (editing) {
    return (
      <form
        className="flex flex-col gap-2"
        onSubmit={(event) => {
          event.preventDefault()
          save(value, value.trim() === '' ? 'cleared' : 'saved')
        }}
      >
        <label htmlFor={inputId} className="text-caption text-ink-muted">
          {t('admin.children.phoneLabel', label)}
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
    )
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex min-w-0 flex-col">
        <span className="text-caption text-ink-muted">{t('admin.children.number', { slot })}</span>
        {phone ? (
          <span className="text-body tabular-nums">{phone}</span>
        ) : (
          <span className="text-body text-ink-muted">{t('admin.children.notSet')}</span>
        )}
        {saved && (
          <output className="text-caption text-primary-strong">
            {t(saved === 'saved' ? 'admin.children.saved' : 'admin.children.cleared')}
          </output>
        )}
      </div>
      <div className="flex gap-2">
        {phone && (
          <button
            type="button"
            disabled={set.isPending}
            onClick={() => save('', 'cleared')}
            className="btn-secondary w-auto"
            aria-label={t('admin.children.clearFor', label)}
          >
            {t('admin.children.clear')}
          </button>
        )}
        <button
          type="button"
          onClick={() => {
            setValue(phone ?? '')
            setSaved(null)
            set.reset()
            setEditing(true)
          }}
          className="btn-secondary w-auto"
          aria-label={t('admin.children.editFor', label)}
        >
          {t('admin.children.edit')}
        </button>
      </div>
    </div>
  )
}

/** The child's home language (T-122, D-113): saved as soon as another one is chosen. */
function LanguagePicker({ child }: { child: AdminChild }) {
  const { t } = useTranslation()
  const set = useSetHomeLanguage()
  const selectId = useId()

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={selectId} className="text-caption text-ink-muted">
        {t('admin.children.languageLabel', { name: child.firstName })}
      </label>
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <select
          id={selectId}
          value={child.homeLanguage}
          disabled={set.isPending}
          onChange={(event) => set.mutate({ id: child.id, language: event.target.value as Language })}
          className="min-h-target min-w-0 flex-1 rounded-button border-2 border-border bg-canvas px-3 text-body"
        >
          {SUPPORTED_LANGUAGES.map((code) => (
            <option key={code} value={code}>
              {t(`missed.language.${code}`)}
            </option>
          ))}
        </select>
        {child.languageChanged && (
          <span className="text-caption text-ink-muted">{t('admin.children.languageChanged')}</span>
        )}
      </div>
      {set.isSuccess && (
        <output className="text-caption text-primary-strong">{t('admin.children.languageSaved')}</output>
      )}
      {set.isError && (
        <p role="alert" className="text-caption text-error">
          {t('admin.children.languageFailed')}
        </p>
      )}
    </div>
  )
}
