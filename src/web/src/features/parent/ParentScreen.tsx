import { useEffect, useMemo, useRef, type ReactNode } from 'react'
import { I18nextProvider, useTranslation } from 'react-i18next'
import { useParams, useSearchParams } from 'react-router'
import { ApiError } from '../../api/client'
import { AnnouncerProvider } from '../../components/Announcer'
import { Icon } from '../../components/Icon'
import { ActivityStep } from './ActivityStep'
import { AnswerStep } from './AnswerStep'
import { DoneStep } from './DoneStep'
import { EveningView } from './EveningView'
import { ListenStep } from './ListenStep'
import { AiLabel, HomeworkCard, ParentHeader, PrimaryButton, StepDots, StopControl, StoppedView } from './ParentParts'
import type { ParentCatchUp, ParentExpired } from './types'
import { useParentPage } from './useParentPage'

/**
 * The family's page in their home language, without changing the language saved on this phone: a shared phone or
 * the demo's two screens may also have the teacher's app open. A cloned i18n instance has no "languageChanged"
 * listener, so nothing is written to localStorage.
 */
function ParentFrame({ language, children }: { language: string; children: ReactNode }) {
  const { i18n } = useTranslation()
  const scoped = useMemo(() => i18n.cloneInstance({ lng: language }), [i18n, language])
  useEffect(() => {
    const before = document.documentElement.lang
    document.documentElement.lang = language
    return () => {
      document.documentElement.lang = before
    }
  }, [language])
  return (
    <I18nextProvider i18n={scoped}>
      <AnnouncerProvider>
        <div lang={language} className="min-h-dvh bg-canvas font-sans text-ink">
          <main id="main" className="mx-auto max-w-md">
            {children}
          </main>
        </div>
      </AnnouncerProvider>
    </I18nextProvider>
  )
}

/** P1–P4 at /p/:token (FR-10). No sign-in: the token is the key. */
export function ParentScreen() {
  const { token = '' } = useParams()
  const { i18n } = useTranslation()
  const page = useParentPage(token)
  const error = page.error instanceof ApiError ? page.error : null
  const expired = error?.status === 410 && isExpired(error.body) ? error.body : null
  const language = page.data?.language ?? expired?.language ?? i18n.language

  return (
    <ParentFrame language={language}>
      {page.data?.kind === 'catchUp' ? (
        page.data.stopped ? (
          <StoppedView token={token} childName={page.data.childName} />
        ) : (
          <CatchUpPages token={token} page={page.data} />
        )
      ) : page.data?.kind === 'evening' ? (
        <EveningView token={token} message={page.data} />
      ) : expired ? (
        <ExpiredView token={token} expired={expired} />
      ) : page.isPending ? (
        <Message status text="parent.loading" />
      ) : error?.status === 404 ? (
        <Message text="parent.notFound" />
      ) : (
        <Message text="parent.offline" onRetry={() => void page.refetch()} />
      )}
    </ParentFrame>
  )
}

function isExpired(body: unknown): body is ParentExpired {
  return typeof body === 'object' && body !== null && 'teacherName' in body && 'language' in body
}

function CatchUpPages({ token, page }: { token: string; page: ParentCatchUp }) {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const headingRef = useRef<HTMLHeadingElement>(null)
  const requested = Number(params.get('step'))
  let step = [1, 2, 3, 4].includes(requested) ? requested : page.answered ? 4 : 1
  // Reopened after answering: the first answer counts, so the question goes straight to Done (D-62).
  if (page.answered && step === 3) {
    step = 4
  }

  useEffect(() => {
    headingRef.current?.focus()
  }, [step])

  const go = (next: number, replace = false) => setParams({ step: String(next) }, { replace })
  const back = step === 1 ? undefined : () => go(page.answered && step === 4 ? 2 : step - 1)
  const titles: Record<number, string> = {
    1: t('parent.title', { child: page.childName }),
    2: t('parent.activity.title'),
    3: t('parent.answer.title'),
    4: t('parent.done.title', { teacher: page.teacherName }),
  }

  return (
    <>
      <ParentHeader title={titles[step] ?? ''} onBack={back} headingRef={headingRef} />
      <div className="flex flex-col gap-4 px-4 py-4">
        <StepDots step={step} />
        {step < 4 && <AiLabel teacher={page.teacherName} />}
        {step === 1 && <ListenStep page={page} onNext={() => go(2)} />}
        {step === 2 && <ActivityStep page={page} onNext={() => go(3)} />}
        {step === 3 && <AnswerStep token={token} page={page} onSent={() => go(4, true)} />}
        {step === 4 && <DoneStep page={page} />}
        <StopControl token={token} childName={page.childName} />
      </div>
    </>
  )
}

/** "This link is old" (P1): no dead end; the homework still shows and the stop link still works. */
function ExpiredView({ token, expired }: { token: string; expired: ParentExpired }) {
  const { t } = useTranslation()
  const headingRef = useRef<HTMLHeadingElement>(null)
  useEffect(() => headingRef.current?.focus(), [])
  return (
    <div className="flex flex-col gap-4 px-4 py-8">
      <section className="flex flex-col items-center gap-3 text-center">
        <Icon name="clock" size={56} className="text-ink-muted" />
        <h1 ref={headingRef} tabIndex={-1} className="text-display">
          {t('parent.expired.title')}
        </h1>
        <p className="text-body-lg">{t('parent.expired.body', { teacher: expired.teacherName })}</p>
      </section>
      {expired.homework && <HomeworkCard homework={expired.homework} teacher={expired.teacherName} />}
      <StopControl token={token} childName={expired.childName} />
    </div>
  )
}

function Message({ text, status, onRetry }: { text: string; status?: boolean; onRetry?: () => void }) {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col gap-4 px-4 py-10 text-center">
      {status ? <output className="block text-body-lg">{t(text)}</output> : <p className="text-body-lg">{t(text)}</p>}
      {onRetry && <PrimaryButton onClick={onRetry}>{t('parent.retry')}</PrimaryButton>}
    </div>
  )
}
