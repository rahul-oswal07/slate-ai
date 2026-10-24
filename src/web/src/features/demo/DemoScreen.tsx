import { Fragment, type ReactNode, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { LanguageSwitcher } from '../../components/LanguageSwitcher'
import { formatSchoolTime } from '../today/schoolTime'
import type { AiCall, FamilyMessage } from './types'
import { storedResetAt, useAiCalls, useBringAttendance, useDemoInfo, useFamily, useResetDemo, useSendNow } from './useDemo'

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
  const sendNow = useSendNow()
  const [panel, setPanel] = useState<Panel>('teacher')
  const [frame, setFrame] = useState(0)
  const [resetAt, setResetAt] = useState<string | null>(storedResetAt)
  const [confirming, setConfirming] = useState(false)
  const [status, setStatus] = useState('')

  const reload = () => setFrame((n) => n + 1)

  if (info.isError) {
    return (
      <Page>
        <div role="alert" className="flex flex-col gap-3 rounded-card border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('demo.loginFailed')}
          <button
            type="button"
            onClick={() => void info.refetch()}
            className="btn-secondary w-auto self-start"
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
      <section aria-labelledby="demo-guide" className="card flex flex-col gap-3">
        <h2 id="demo-guide" className="text-title font-extrabold">
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
            className="btn-primary w-auto"
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
            className="btn-secondary w-auto"
          >
            {t('demo.bringAttendance')}
          </button>
          <button
            type="button"
            disabled={sendNow.isPending}
            onClick={() =>
              sendNow.mutate(undefined, {
                onSuccess: (sent) => {
                  setStatus(
                    sent.eveningBlocked
                      ? `${t(`demo.sendNow.blocked.${sent.eveningBlocked}`)} ${t('demo.sendNow.catchUps', { n: sent.catchUpsSent })}`
                      : sent.eveningSent + sent.catchUpsSent === 0
                        ? t('demo.sendNow.nothing')
                        : t('demo.sendNow.sent', { evening: sent.eveningSent, catchUps: sent.catchUpsSent }),
                  )
                  reload()
                },
                onError: () => setStatus(t('demo.sendNow.failed')),
              })
            }
            className="btn-secondary w-auto"
          >
            {t('demo.sendNow.button')}
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
            className={`min-h-target flex-1 rounded-button border-2 px-2 text-label font-bold ${
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
    <div className="min-h-dvh font-sans text-ink">
      {/* Labelled: the teacher app in its frame has its own header and main. */}
      <header aria-label={t('demo.title')} className="border-b border-edge px-4 py-3">
        <h1 className="text-title font-extrabold">{t('demo.title')}</h1>
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
      <h2 className="text-label font-extrabold">{title}</h2>
      <p className="text-caption text-ink-muted">{caption}</p>
      <div className="h-[740px] w-full max-w-[360px] overflow-hidden rounded-sheet border-[3px] border-ink bg-surface">
        {children}
      </div>
    </section>
  )
}

/**
 * Rohan's family's phone as a chat (D-77, surface B): a stand-in for a messaging app, so judges see the real
 * hand-off. No logo and no brand name. Each link a family was sent is a bubble; opening one slides the family's page
 * up under a thin bar, as an in-app browser does.
 */
function ParentPhone({ childId }: { childId: string }) {
  const { t, i18n } = useTranslation()
  const family = useFamily(childId)
  const [open, setOpen] = useState<FamilyMessage | null>(null)
  // Oldest first, as a chat reads; the API sends newest first.
  const messages = [...(family.data ?? [])].reverse()

  return (
    <Phone title={t('demo.parentPhone')} caption={t('demo.parentCaption')}>
      <div className="relative flex size-full flex-col overflow-hidden">
        {/* Underneath the opened page the chat is inert, but keeps its scroll position. */}
        <div inert={open !== null} className="flex min-h-0 flex-1 flex-col">
          <div className="flex h-14 shrink-0 items-center gap-3 bg-chat-bar px-3 text-chat-bar-ink">
            <span aria-hidden="true" className="flex size-10 items-center justify-center rounded-full border-2 border-chat-bar-ink bg-accent text-label font-extrabold text-on-accent">
              S
            </span>
            <p className="text-label font-extrabold">{t('demo.chat.messages')}</p>
          </div>
          <ul className="chat-wallpaper flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto bg-chat-wall p-3">
            {family.isError && (
              <li role="alert" className="chat-bubble self-start text-body">
                {t('demo.familyFailed')}
              </li>
            )}
            {messages.length === 0 && !family.isError && (
              <li className="chat-bubble self-start text-body">{t('demo.noMessages')}</li>
            )}
            {messages.map((message, i) => (
              <Fragment key={message.token}>
                {message.date !== messages[i - 1]?.date && (
                  <li className="self-center rounded-pill border border-chat-edge bg-chat-pill px-3 text-caption font-semibold text-chat-meta">
                    {shortDate(message.date, i18n.language)}
                  </li>
                )}
                <li className="max-w-[86%] self-start">
                  <button type="button" onClick={() => setOpen(message)} className="chat-bubble block w-full text-left">
                    <span className="block text-body font-bold">
                      {t(message.kind === 'evening' ? 'demo.message.evening' : 'demo.message.catchUp', {
                        date: shortDate(message.date, i18n.language),
                      })}
                    </span>
                    <span className="mt-1 flex min-h-target items-center justify-between border-t border-chat-edge text-body font-bold text-chat-link underline">
                      {t('demo.message.open')}
                      <Icon name="chevronRight" size={20} />
                    </span>
                    <span className="block text-right text-caption text-chat-meta">
                      {formatSchoolTime(message.sentAt, i18n.language)}
                    </span>
                  </button>
                </li>
              </Fragment>
            ))}
          </ul>
        </div>
        {open && (
          <div className="slide-up absolute inset-0 z-10 flex flex-col bg-canvas">
            <div className="flex h-12 shrink-0 items-center border-b border-edge bg-surface">
              <button
                type="button"
                onClick={() => setOpen(null)}
                aria-label={t('demo.backToMessages')}
                className="flex size-12 shrink-0 items-center justify-center text-ink"
              >
                <Icon name="x" size={22} />
              </button>
              <div className="min-w-0 flex-1 text-center">
                <p className="truncate text-label font-extrabold leading-tight">
                  {t(open.kind === 'evening' ? 'demo.chat.pageEvening' : 'demo.chat.pageLesson')}
                </p>
                <p className="truncate text-caption leading-tight text-ink-muted">{window.location.host}</p>
              </div>
              <span className="w-12 shrink-0" />
            </div>
            <iframe title={t('demo.parentPage')} src={`/p/${open.token}`} className="w-full flex-1" />
          </div>
        )}
      </div>
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
      <h2 id="ai-title" className="text-label font-extrabold">
        {t('demo.ai.title')}
      </h2>
      <p className="text-caption text-ink-muted">{t('demo.ai.caption')}</p>
      {calls.isError && <p role="alert" className="text-body">{t('demo.ai.failed')}</p>}
      {rows.length === 0 && !calls.isError ? (
        <p className="card text-body">{t('demo.ai.empty')}</p>
      ) : (
        // As tall as the phones and scrolls inside, so keyboard users must be able to reach it: axe's
        // scrollable-region-focusable needs tabIndex, which the lint rule below doesn't know about.
        // oxlint-disable-next-line jsx-a11y/no-noninteractive-tabindex
        <section tabIndex={0} aria-labelledby="ai-title" className="card max-h-[740px] overflow-auto p-0">
          <table className="w-full text-left text-caption">
            <caption className="sr-only">{t('demo.ai.title')}</caption>
            <thead className="sticky top-0 bg-surface">
              <tr className="border-b-2 border-border">
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
          <span className="chip bg-error-bg text-error">
            <Icon name="alert" size={16} />
            {t('demo.ai.error', { error: call.error })}
          </span>
        ) : call.verdict ? (
          <span className="chip bg-status-caught-up-bg text-status-caught-up">
            <Icon name="check" size={16} />
            {call.verdict}
          </span>
        ) : (
          '–'
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
      className="slide-up fixed inset-x-0 bottom-0 top-auto m-0 mx-auto w-full max-w-md rounded-t-sheet border-2 border-b-0 border-border bg-surface p-4 pb-6 text-ink backdrop:bg-canvas/60"
    >
      <h2 id="reset-title" className="text-title font-extrabold">
        {t('demo.reset.title')}
      </h2>
      <p className="mt-3 text-body">{t('demo.reset.body')}</p>
      {failed && (
        <p role="alert" className="mt-3 rounded-md border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('demo.reset.failed')}
        </p>
      )}
      <div className="mt-4 flex flex-col gap-2">
        <button
          type="button"
          disabled={pending}
          onClick={onConfirm}
          className="btn-primary"
        >
          {pending ? t('demo.reset.resetting') : t('demo.reset.confirm')}
        </button>
        <button
          ref={keepRef}
          type="button"
          onClick={onClose}
          className="btn-secondary"
        >
          {t('common.cancel')}
        </button>
      </div>
    </dialog>
  )
}
