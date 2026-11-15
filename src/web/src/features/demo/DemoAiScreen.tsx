import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Page } from './DemoParts'
import { later } from './demoText'
import { UnderTheHood } from './UnderTheHood'
import { storedResetAt, useDemoInfo } from './useDemo'

/**
 * /demo/ai (T-126): "Under the hood" on its own, for a second screen next to the phones. The same calls as the panel
 * in /demo: everything since the last reset. Signs in as the demo teacher itself, like /demo.
 */
export function DemoAiScreen() {
  const { t } = useTranslation()
  const info = useDemoInfo()

  return (
    <Page>
      {info.isError ? (
        <SignInFailed retry={() => void info.refetch()} />
      ) : !info.data ? (
        <p className="text-body text-ink-muted">{t('demo.signingIn')}</p>
      ) : (
        <>
          <Link to="/demo" className="self-start text-body font-bold text-primary-strong underline">
            {t('demo.backToDemo')}
          </Link>
          <UnderTheHood since={later(storedResetAt(), info.data.lastResetAt)} />
        </>
      )}
    </Page>
  )
}

export function SignInFailed({ retry }: { retry: () => void }) {
  const { t } = useTranslation()
  return (
    <div role="alert" className="flex flex-col gap-3 rounded-card border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
      {t('demo.loginFailed')}
      <button type="button" onClick={retry} className="btn-secondary w-auto self-start">
        {t('common.tryAgain')}
      </button>
    </div>
  )
}
