import { type ReactNode, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '../../components/LanguageSwitcher'
import { formatSchoolTime } from '../today/schoolTime'
import type { AiCall, FamilyMessage } from './types'
import { storedResetAt, useAiCalls, useBringAttendance, useDemoInfo, useFamily, useResetDemo } from './useDemo'

const PANELS = ['teacher', 'parent', 'ai'] as const
type Panel = (typeof PANELS)[number]

const STEPS = ['today', 'lesson', 'evening', 'approve', 'parent', 'after'] as const

/** The later of two ISO instants: this browser's own reset or anyone's, so "Under the hood" starts at the last one. */
function later(a: string | null, b: string | null) {
  if (!a || !b) {
    return a ?? b
  }
  return new Date(a) >= new Date(b) ? a : b
}

/** "22 Oct", in the UI language. */
function shortDate(date: string, language: string) {
  return new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    day: 'numeric',
    month: 'short',
    timeZone: 'UTC',
  }).format(new Date(`${date}T00:00:00Z`))
}

/**
 * /demo (FR-14, T-18): the teacher's phone and Rohan's family's phone (English) side by side, a step guide, Reset and the
 * "Under the hood" list of AI calls, so a judge can run the loop alone. Signs in as the fictional demo teacher with
 * one tap. Both phones are the real app in frames; nothing here is a mock-up.
 */
export function DemoScreen() {
  const { t } = useTranslation()
  const info = useDemoInfo()
  const reset = useResetDemo()
  const bring = useBringAttendance(info.data?.classId ?? '')
  const [panel, setPanel] = useState<Panel>('teacher')
  const [frame, setFrame] = useState(0)
  const [resetAt, setResetAt] = useState<string | null>(storedResetAt)
  const [confirming, setConfirming] = useState(false)
  const [status, setStatus] = useState('')

  const reload = () => setFrame((n) => n + 1)

  if (info.isError) {
    return (
      <Page>
        <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('demo.loginFailed')}
          <button
            type="button"
            onClick={() => void info.refetch()}
            className="min-h-target self-start rounded-md border border-border bg-surface px-4 text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      </Page>
    )
  }

  if (!info.data) {
    return (
      <Page>
        <p className="text-body text-ink-muted">{t('demo.signingIn')}</p>
      </Page>
    )
  }

  const { parentChildId, lastResetAt } = info.data

  return (
    <Page>
      <section aria-labelledby="demo-guide" className="flex flex-col gap-3 rounded-md border border-border bg-surface p-4">
        <h2 id="demo-guide" className="text-title">
          {t('demo.guide.title')}
        </h2>
        <ol className="flex list-decimal flex-col gap-1 pl-5 text-body">
          {STEPS.map((step) => (
            <li key={step}>{t(`demo.guide.${step}`)}</li>
          ))}
        </ol>
        <p className="text-caption text-ink-muted">{t('demo.guide.calendar')}</p>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setConfirming(true)}
            className="min-h-target rounded-md bg-primary px-4 text-label text-on-primary"
          >
            {t('demo.reset.button')}
          </button>
          <button
            type="button"
            disabled={bring.isPending}
            onClick={() =>
              bring.mutate(undefined, {
                onSuccess: () => {
                  setStatus(t('demo.attendanceIn'))
                  reload()
                },
                onError: () => setStatus(t('demo.attendanceFailed')),
              })
            }
            className="min-h-target rounded-md border border-border bg-surface px-4 text-label disabled:opacity-60"
          >
            {t('demo.bringAttendance')}
          </button>
        </div>
        <output aria-live="polite" className="block text-body">
          {status}
        </output>
      </section>

      <div role="tablist" aria-label={t('demo.panels')} className="flex gap-2 lg:hidden">
        {PANELS.map((name) => (
          <button
            key={name}
            type="button"
            role="tab"
            id={`tab-${name}`}
            aria-selected={panel === name}
            aria-controls={`panel-${name}`}
            onClick={() => setPanel(name)}
            className={`min-h-target flex-1 rounded-md border px-2 text-label ${
              panel === name ? 'border-primary bg-primary text-on-primary' : 'border-border bg-surface text-ink'
            }`}
          >
            {t(`demo.tab.${name}`)}
          </button>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-[360px_360px_1fr]">
        <PanelBox name="teacher" active={panel}>
          <Phone title={t('demo.teacherPhone')} caption={t('demo.teacherCaption')}>
            <iframe key={`teacher-${frame}`} title={t('demo.teacherPhone')} src="/" className="size-full" />
          </Phone>
        </PanelBox>
        <PanelBox name="parent" active={panel}>
          <ParentPhone key={`parent-${frame}`} childId={parentChildId} />
        </PanelBox>
        <PanelBox name="ai" active={panel}>
          <UnderTheHood since={later(resetAt, lastResetAt)} />
        </PanelBox>
      </div>

      {confirming && (
        <ResetDialog
          pending={reset.isPending}
          failed={reset.isError}
          onClose={() => setConfirming(false)}
          onConfirm={() =>
            reset.mutate(undefined, {
              onSuccess: (done) => {
                setResetAt(done.resetAt)
                setStatus(t(done.preMade ? 'demo.reset.donePreMade' : 'demo.reset.done'))
                setConfirming(false)
                reload()
              },
            })
          }
        />
      )}
    </Page>
  )
}

function Page({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  return (
    <div className="min-h-dvh bg-canvas font-sans text-ink">
      {/* Labelled: the teacher app in its frame has its own header and main. */}
      <header aria-label={t('demo.title')} className="border-b border-border px-4 py-3">
        <h1 className="text-title">{t('demo.title')}</h1>
        <p className="text-caption text-ink-muted">{t('demo.subtitle')}</p>
        <LanguageSwitcher />
      </header>
      <main id="main" aria-label={t('demo.panels')} className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-4">
        {children}
      </main>
    </div>
  )
}

/** On a phone one panel shows at a time (tabs); from `lg` all three sit side by side. */
function PanelBox({ name, active, children }: { name: Panel; active: Panel; children: ReactNode }) {
  return (
    <div
      id={`panel-${name}`}
      role="tabpanel"
      aria-labelledby={`tab-${name}`}
      className={`${active === name ? 'block' : 'hidden'} min-w-0 lg:block`}
    >
      {children}
    </div>
  )
}

function Phone({ title, caption, children }: { title: string; caption: string; children: ReactNode }) {
  return (
    <section aria-label={title} className="flex flex-col gap-2">
      <h2 className="text-label">{title}</h2>
      <p className="text-caption text-ink-muted">{caption}</p>
      <div className="h-[740px] w-full max-w-[360px] overflow-hidden rounded-md border-2 border-ink bg-surface">
        {children}
      </div>
    </section>
  )
}

/** Rohan's family's phone: the links they were sent, each opened as a real parent would at /p/{token}. */
function ParentPhone({ childId }: { childId: string }) {
  const { t, i18n } = useTranslation()
  const family = useFamily(childId)
  const [open, setOpen] = useState<FamilyMessage | null>(null)
  const messages = family.data ?? []

  return (
    <Phone title={t('demo.parentPhone')} caption={t('demo.parentCaption')}>
      {open ? (
        <div className="flex size-full flex-col">
          <button
            type="button"
            onClick={() => setOpen(null)}
            className="min-h-target border-b border-border bg-panel px-4 text-left text-label"
          >
            {t('demo.backToMessages')}
          </button>
          <iframe title={t('demo.parentPage')} src={`/p/${open.token}`} className="w-full flex-1" />
        </div>
      ) : (
        <div className="flex size-full flex-col gap-3 overflow-y-auto bg-panel p-3">
          {family.isError && <p role="alert" className="text-body">{t('demo.familyFailed')}</p>}
          {messages.length === 0 && !family.isError && <p className="text-body">{t('demo.noMessages')}</p>}
          {messages.map((message) => (
            <button
              key={message.token}
              type="button"
              onClick={() => setOpen(message)}
              className="min-h-target flex flex-col gap-1 self-start rounded-md border border-border bg-surface p-3 text-left"
            >
              <span className="text-label">
                {t(message.kind === 'evening' ? 'demo.message.evening' : 'demo.message.catchUp', {
                  date: shortDate(message.date, i18n.language),
                })}
              </span>
              <span className="text-caption text-ink-muted">
                {t('demo.message.sentAt', { time: formatSchoolTime(message.sentAt, i18n.language) })}
              </span>
              <span className="text-body text-primary underline">{t('demo.message.open')}</span>
            </button>
          ))}
        </div>
      )}
    </Phone>
  )
}

/** Each Claude and Sarvam call since the reset: what for, which model and prompt version, and the verdict. */
function UnderTheHood({ since }: { since: string | null }) {
  const { t, i18n } = useTranslation()
  const calls = useAiCalls(since)
  const rows = calls.data ?? []

  return (
    <section className="flex flex-col gap-2">
      <h2 id="ai-title" className="text-label">
        {t('demo.ai.title')}
      </h2>
      <p className="text-caption text-ink-muted">{t('demo.ai.caption')}</p>
      {calls.isError && <p role="alert" className="text-body">{t('demo.ai.failed')}</p>}
      {rows.length === 0 && !calls.isError ? (
        <p className="rounded-md border border-border bg-surface p-3 text-body">{t('demo.ai.empty')}</p>
      ) : (
        // As tall as the phones and scrolls inside, so keyboard users must be able to reach it: axe's
        // scrollable-region-focusable needs tabIndex, which the lint rule below doesn't know about.
        // oxlint-disable-next-line jsx-a11y/no-noninteractive-tabindex
        <section tabIndex={0} aria-labelledby="ai-title" className="max-h-[740px] overflow-auto rounded-md border border-border bg-surface">
          <table className="w-full text-left text-caption">
            <caption className="sr-only">{t('demo.ai.title')}</caption>
            <thead className="sticky top-0 bg-surface">
              <tr className="border-b border-border">
                <th scope="col" className="p-2">{t('demo.ai.time')}</th>
                <th scope="col" className="p-2">{t('demo.ai.step')}</th>
                <th scope="col" className="p-2">{t('demo.ai.model')}</th>
                <th scope="col" className="p-2">{t('demo.ai.tokens')}</th>
                <th scope="col" className="p-2">{t('demo.ai.latency')}</th>
                <th scope="col" className="p-2">{t('demo.ai.verdict')}</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((call) => (
                <CallRow key={call.id} call={call} language={i18n.language} />
              ))}
            </tbody>
          </table>
        </section>
      )}
    </section>
  )
}

function CallRow({ call, language }: { call: AiCall; language: string }) {
  const { t } = useTranslation()
  return (
    <tr className="border-b border-divider align-top">
      <td className="p-2 whitespace-nowrap">{formatSchoolTime(call.createdAt, language)}</td>
      <td className="p-2">
        {t(`demo.purpose.${call.purpose}`, { defaultValue: call.purpose })}
        {call.promptVersion !== null && (
          <span className="block text-ink-muted">{t('demo.ai.prompt', { id: call.purpose, version: call.promptVersion })}</span>
        )}
      </td>
      <td className="p-2 whitespace-nowrap">{call.model}</td>
      <td className="p-2 whitespace-nowrap">
        {call.inputTokens > 0 || call.outputTokens > 0
          ? t('demo.ai.tokenCount', { input: call.inputTokens, output: call.outputTokens })
          : '–'}
      </td>
      <td className="p-2 whitespace-nowrap">{t('demo.ai.seconds', { s: (call.latencyMs / 1000).toFixed(1) })}</td>
      <td className="p-2">
        {call.error ? (
          <span className="text-error">{t('demo.ai.error', { error: call.error })}</span>
        ) : (
          (call.verdict ?? '–')
        )}
      </td>
    </tr>
  )
}

function ResetDialog({
  pending,
  failed,
  onClose,
  onConfirm,
}: {
  pending: boolean
  failed: boolean
  onClose: () => void
  onConfirm: () => void
}) {
  const { t } = useTranslation()
  const dialogRef = useRef<HTMLDialogElement>(null)
  const keepRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    const opener = document.activeElement
    const dialog = dialogRef.current
    if (dialog && !dialog.open) {
      // jsdom has no showModal; real browsers do.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal()
      } else {
        dialog.setAttribute('open', '')
      }
    }
    keepRef.current?.focus()
    return () => {
      if (opener instanceof HTMLElement && opener.isConnected) {
        opener.focus()
      }
    }
  }, [])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="reset-title"
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      className="fixed inset-x-0 bottom-0 top-auto m-0 mx-auto w-full max-w-md rounded-t-md border border-border bg-surface p-4 pb-6 text-ink backdrop:bg-ink/40"
    >
      <h2 id="reset-title" className="text-title">
        {t('demo.reset.title')}
      </h2>
      <p className="mt-3 text-body">{t('demo.reset.body')}</p>
      {failed && (
        <p role="alert" className="mt-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('demo.reset.failed')}
        </p>
      )}
      <div className="mt-4 flex flex-col gap-2">
        <button
          type="button"
          disabled={pending}
          onClick={onConfirm}
          className="min-h-target-primary w-full rounded-md bg-primary px-4 text-label text-on-primary disabled:opacity-60"
        >
          {pending ? t('demo.reset.resetting') : t('demo.reset.confirm')}
        </button>
        <button
          ref={keepRef}
          type="button"
          onClick={onClose}
          className="min-h-target w-full rounded-md border border-border bg-surface px-4 text-label"
        >
          {t('common.cancel')}
        </button>
      </div>
    </dialog>
  )
}
