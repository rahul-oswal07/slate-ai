import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { isLanguage } from '../../i18n'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'

/**
 * Profile (T-109, D-101), opened from the header's profile menu: her name, her phone with only the last 4 digits and
 * her class. Read only: nothing here can be changed in the app. Everything comes with `me`, so it also shows offline.
 * A row the server didn't send (a `me` cached before this screen existed, a teacher without a class) is left out.
 */
export function ProfileScreen() {
  const { t } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const me = useMe().data

  // A mask and digits, the same in every language: not a string to translate.
  const phone = me?.phoneLast4 ? `•••• ${me.phoneLast4}` : null
  const classLine =
    me?.grade != null && me.medium
      ? t('profile.classValue', { grade: me.grade, medium: isLanguage(me.medium) ? t(`language.${me.medium}`) : me.medium })
      : null
  const rows: [label: string, value: string | null | undefined][] = [
    [t('profile.name'), me?.displayName],
    [t('profile.phone'), phone],
    [t('profile.class'), classLine],
  ]

  return (
    <section aria-labelledby="profile-title" className="flex flex-col gap-3">
      <Link to="/" className="btn-link self-start">
        {t('settings.backToToday')}
      </Link>
      <h1 id="profile-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('profile.title')}
      </h1>
      <dl className="card flex flex-col gap-3 p-[14px]">
        {rows.map(
          ([label, value]) =>
            value && (
              <div key={label}>
                <dt className="text-caption text-ink-muted">{label}</dt>
                <dd className="text-label">{value}</dd>
              </div>
            ),
        )}
      </dl>
    </section>
  )
}
