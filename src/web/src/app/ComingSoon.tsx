import { useTranslation } from 'react-i18next'
import { useFocusOnMount } from '../components/useFocusOnMount'

/** Placeholder for tabs whose screen isn't built yet (Approve, Missed, Back today, Week). */
export function ComingSoon({ tab }: { tab: 'approve' | 'missed' | 'back' | 'week' }) {
  const { t } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()

  return (
    <section aria-labelledby="soon-title" className="rounded-md border border-border bg-surface p-4">
      <h1 id="soon-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t(`nav.${tab}`)}
      </h1>
      <p className="mt-2 text-body text-ink-muted">{t('comingSoon.body')}</p>
    </section>
  )
}
