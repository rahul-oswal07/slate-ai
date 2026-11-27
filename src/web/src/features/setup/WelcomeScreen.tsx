import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { Icon, type IconName } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'

const promises: { icon: IconName; key: string }[] = [
  { icon: 'clock', key: 'setup.welcome.sats' },
  { icon: 'checkCircle', key: 'setup.welcome.approval' },
  { icon: 'lock', key: 'setup.welcome.privacy' },
]

/**
 * T0 Set up, Welcome (state `0`, docs/design/T0-setup.md). Shown once, until Teacher.ConsentAt is set. Start opens
 * the greeting step (T0 step 1), where the voice consent sets it; then the evening-message step (T-44).
 */
export function WelcomeScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()

  return (
    <section aria-labelledby="welcome-title" className="mx-auto max-w-md">
      <div className="mb-2 mt-4 flex justify-center text-primary-strong">
        <Icon name="book" size={44} strokeWidth={1.6} />
      </div>
      <h1 id="welcome-title" ref={headingRef} tabIndex={-1} className="text-center text-title">
        {t('setup.welcome.heading', { teacher: me.data?.displayName ?? '' })}
      </h1>
      <p className="mb-5 mt-1 text-center text-body text-ink-muted">{t('setup.welcome.lead')}</p>

      <ul className="flex flex-col gap-3">
        {promises.map(({ icon, key }) => (
          <li key={key} className="flex items-start gap-3 rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
            <Icon name={icon} className="mt-0.5 shrink-0 text-primary-strong" />
            <span className="text-body">{t(key)}</span>
          </li>
        ))}
      </ul>

      {/* AI explained (T-102, PAIR): what the AI does and doesn't, before she starts. */}
      <section aria-labelledby="welcome-ai-title" className="mt-3 rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
        <h2 id="welcome-ai-title" className="flex items-center gap-2 text-label">
          <Icon name="bot" className="shrink-0 text-primary-strong" />
          {t('setup.welcome.ai.title')}
        </h2>
        <p className="mt-2 text-body">{t('setup.welcome.ai.does')}</p>
        <p className="mt-2 text-body">{t('setup.welcome.ai.doesNot')}</p>
        <p className="mt-2 flex items-start gap-2 text-body font-bold">
          <Icon name="checkCircle" className="mt-0.5 shrink-0 text-primary-strong" />
          {t('setup.welcome.ai.approve')}
        </p>
      </section>

      <div className="sticky bottom-0 mt-4 border-t border-divider bg-canvas pb-3 pt-3">
        <button
          type="button"
          onClick={() => void navigate('/welcome/greeting')}
          className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
        >
          {t('setup.welcome.start')}
        </button>
      </div>
    </section>
  )
}
