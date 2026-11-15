import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { SignInFailed } from './DemoAiScreen'
import { DemoHeader, Page } from './DemoParts'
import { familyTitle } from './demoText'
import { FamilyChat } from './FamilyPhone'
import { useDemoChild, useDemoInfo } from './useDemo'

/**
 * /demo/family/:childId (T-126): one demo child's family phone on its own. On a phone the chat fills the screen, as a
 * messaging app does; from `lg` it sits in the 360 px frame under the demo header. Signs in as the demo teacher itself.
 */
export function DemoFamilyScreen() {
  const { t } = useTranslation()
  const { childId = '' } = useParams()
  const info = useDemoInfo()
  const child = useDemoChild(childId, info.isSuccess)

  if (info.isError || child.isError) {
    return (
      <Page>
        {info.isError ? (
          <SignInFailed retry={() => void info.refetch()} />
        ) : (
          <div role="alert" className="flex flex-col gap-3 rounded-card border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
            {t('demo.childNotFound')}
            <Link to="/demo" className="self-start font-bold text-primary-strong underline">
              {t('demo.backToDemo')}
            </Link>
          </div>
        )}
      </Page>
    )
  }

  if (!child.data) {
    return (
      <Page>
        <p className="text-body text-ink-muted">{t('demo.signingIn')}</p>
      </Page>
    )
  }

  const { firstName, language } = child.data
  const title = familyTitle(t, firstName, language)

  return (
    <div className="flex h-dvh flex-col font-sans text-ink lg:h-auto lg:min-h-dvh">
      {/* On a phone only the chat shows, as a messaging app would; from lg the demo header and the frame come back. */}
      <div className="hidden lg:block">
        <DemoHeader titleAs="p" />
      </div>
      <main id="main" aria-labelledby="family-title" className="flex min-h-0 flex-1 flex-col lg:mx-auto lg:w-[360px] lg:flex-none lg:gap-2 lg:py-4">
        <h1 id="family-title" className="sr-only lg:not-sr-only lg:text-label lg:font-extrabold">
          {title}
        </h1>
        <p className="hidden text-caption text-ink-muted lg:block">{t('demo.parentCaption')}</p>
        <div className="min-h-0 flex-1 bg-surface lg:h-[740px] lg:flex-none lg:overflow-hidden lg:rounded-sheet lg:border-[3px] lg:border-ink">
          <FamilyChat childId={childId} name={firstName} />
        </div>
      </main>
    </div>
  )
}
