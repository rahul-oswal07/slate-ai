import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { classLabel } from '../auth/classLine'
import { classesOf, selectClass } from '../auth/currentClass'
import { useCurrentClass } from '../auth/useCurrentClass'
import { useMe } from '../auth/useMe'

/**
 * The class dropdown above the date on Today (T-115): "3A · Grade 3 · Kannada medium" for each class she belongs to.
 * Picking one switches the whole app (every screen, the header and the bottom nav's badges) and is remembered in this
 * browser. Always shown, also with one class, so she always sees which class she is in. A native select: the phone's
 * own picker, keyboard and screen reader work as everywhere.
 */
export function ClassPicker() {
  const { t } = useTranslation()
  const me = useMe().data
  const current = useCurrentClass()
  if (!me || !current) {
    return null
  }

  return (
    <label className="flex flex-col gap-1">
      <span className="text-caption text-ink-muted">{t('profile.class')}</span>
      <span className="relative flex items-center">
        <select
          value={current.id}
          onChange={(event) => selectClass(me.teacherId, event.target.value)}
          className="min-h-target w-full appearance-none rounded-button border-2 border-border bg-surface pl-3 pr-10 text-label font-bold text-ink"
        >
          {classesOf(me).map((group) => (
            <option key={group.id} value={group.id}>
              {classLabel(group, t) ?? group.name}
            </option>
          ))}
        </select>
        <Icon name="chevronDown" size={18} className="pointer-events-none absolute right-3 text-ink" />
      </span>
    </label>
  )
}
