import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '../../components/LanguageSwitcher'

/** The demo pages' header: title, subtitle and language. `titleAs="p"` when the page's own heading is its h1. */
export function DemoHeader({ titleAs: Title = 'h1' }: { titleAs?: 'h1' | 'p' }) {
  const { t } = useTranslation()
  return (
    // Labelled: the teacher app in its frame has its own header and main.
    <header aria-label={t('demo.title')} className="border-b border-edge px-4 py-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <Title className="text-title font-extrabold">{t('demo.title')}</Title>
          <p className="text-caption text-ink-muted">{t('demo.subtitle')}</p>
        </div>
        <LanguageSwitcher />
      </div>
    </header>
  )
}

/** The demo pages' header and main. */
export function Page({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  return (
    <div className="min-h-dvh font-sans text-ink">
      <DemoHeader />
      <main id="main" aria-label={t('demo.panels')} className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-4">
        {children}
      </main>
    </div>
  )
}

/** A 360 px phone frame with a heading and caption. */
export function Phone({ title, caption, children }: { title: string; caption: string; children: ReactNode }) {
  return (
    <section aria-label={title} className="flex flex-col gap-2">
      <h2 className="text-label font-extrabold">{title}</h2>
      <p className="text-caption text-ink-muted">{caption}</p>
      <div className="h-[740px] w-full max-w-[360px] overflow-hidden rounded-sheet border-[3px] border-ink bg-surface">
        {children}
      </div>
    </section>
  )
}
