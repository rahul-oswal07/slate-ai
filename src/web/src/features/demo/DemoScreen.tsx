import { type ReactNode, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Page, Phone } from './DemoParts'
import { later } from './demoText'
import { ParentPhone } from './FamilyPhone'
import { UnderTheHood } from './UnderTheHood'
import { storedResetAt, useBringAttendance, useDemoInfo, useResetDemo, useSendNow } from './useDemo'

const PANELS = ['teacher', 'parent', 'ai'] as const
type Panel = (typeof PANELS)[number]

const STEPS = ['today', 'lesson', 'evening', 'approve', 'parent', 'after'] as const

/**
 * /demo (FR-14, T-18, T-93): the teacher's phone and Manjunath's family's phone (Kannada) side by side, a step guide, Reset and the
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

  const { parentChildId, parentChildName, parentLanguage, lastResetAt } = info.data

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
        {/* Each panel on its own, for a second screen or a real phone (T-126). */}
        <ul className="flex flex-wrap gap-x-4 gap-y-1 text-body">
          <li>
            <Link to={`/demo/family/${parentChildId}`} className="font-bold text-primary-strong underline">
              {t('demo.openAlone.family', { name: parentChildName })}
            </Link>
          </li>
          <li>
            <Link to="/demo/ai" className="font-bold text-primary-strong underline">
              {t('demo.openAlone.ai')}
            </Link>
          </li>
        </ul>
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
            className={`min-h-target min-w-0 flex-1 rounded-button border-2 px-2 text-label font-bold [overflow-wrap:anywhere] ${
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
          <ParentPhone key={`parent-${frame}`} childId={parentChildId} name={parentChildName} language={parentLanguage} />
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
