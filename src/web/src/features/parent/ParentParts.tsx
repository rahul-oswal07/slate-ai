import { useEffect, useRef, useState, type ButtonHTMLAttributes, type ReactNode, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import type { Homework } from './types'
import { useStop } from './useParentPage'

/** Screen header: Back (except on the first step) and the title, which takes focus when the step opens. */
export function ParentHeader({
  title,
  onBack,
  headingRef,
}: {
  title: string
  onBack?: () => void
  headingRef?: RefObject<HTMLHeadingElement | null>
}) {
  const { t } = useTranslation()
  return (
    <header className="flex items-center gap-2 border-b border-border bg-surface px-2 py-2">
      {onBack ? (
        <button
          type="button"
          onClick={onBack}
          aria-label={t('parent.back')}
          className="flex min-h-target min-w-target items-center justify-center rounded-md text-ink"
        >
          <Icon name="arrowLeft" />
        </button>
      ) : (
        <span className="w-2" />
      )}
      <h1 ref={headingRef} tabIndex={-1} className="text-title">
        {title}
      </h1>
    </header>
  )
}

/** Step n of 4 (D-08): dots, the current one a pill. */
export function StepDots({ step }: { step: number }) {
  const { t } = useTranslation()
  return (
    <div className="flex items-center justify-center gap-2">
      <span className="sr-only">{t('parent.steps', { n: step })}</span>
      {[1, 2, 3, 4].map((n) => (
        <span
          key={n}
          aria-hidden="true"
          className={
            n === step
              ? 'h-[10px] w-[26px] rounded-full bg-primary'
              : n < step
                ? 'size-[10px] rounded-full bg-primary'
                : 'size-[10px] rounded-full border border-border bg-divider'
          }
        />
      ))}
    </div>
  )
}

/** NFR-08: every step with AI-written words says so, with the bot icon (not the reward star, C-25). */
export function AiLabel({ teacher }: { teacher: string }) {
  const { t } = useTranslation()
  return (
    <p className="flex items-center justify-center gap-1.5 text-label text-primary-strong">
      <Icon name="bot" size={18} />
      {t('parent.aiLabel', { teacher })}
    </p>
  )
}

/** The child's own textbook page (D-22). */
export function HomeworkCard({ homework, teacher }: { homework: Homework; teacher: string }) {
  const { t } = useTranslation()
  return (
    <section className="flex items-start gap-3 rounded-md border border-border bg-surface p-4">
      <Icon name="book" size={28} className="shrink-0 text-primary-strong" />
      <div>
        <h2 className="text-label">{t('parent.activity.homeworkTitle', { page: homework.page })}</h2>
        <p className="text-caption text-ink-muted">
          {homework.part === null
            ? t('parent.activity.homeworkBodyNoPart', { teacher })
            : t('parent.activity.homeworkBody', { part: homework.part, teacher })}
        </p>
      </div>
    </section>
  )
}

/** Primary full-width button used for Next / Finish. */
export function PrimaryButton({ children, ...props }: { children: ReactNode } & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type="button"
      {...props}
      className="min-h-target-primary w-full rounded-md bg-primary px-4 text-label text-on-primary disabled:opacity-60"
    >
      {children}
    </button>
  )
}

/**
 * "Stop these messages" (FR-19, D-30): a quiet link, last on the screen, and one confirmation in a neutral dialog
 * (no alarm style, C-27). "No, keep them coming" has focus first; Esc and the backdrop close it.
 */
export function StopControl({ token, childName }: { token: string; childName: string }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex min-h-target w-full items-center justify-center gap-1.5 text-caption text-ink-muted underline"
      >
        <Icon name="bellOff" size={16} />
        {t('parent.stop.link')}
      </button>
      {open && <StopDialog token={token} childName={childName} onClose={() => setOpen(false)} />}
    </>
  )
}

function StopDialog({ token, childName, onClose }: { token: string; childName: string; onClose: () => void }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const stop = useStop(token)
  const dialogRef = useRef<HTMLDialogElement>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  })

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
    cancelRef.current?.focus()
    const onBackdrop = (event: MouseEvent) => {
      if (event.target === dialog) {
        onCloseRef.current()
      }
    }
    dialog?.addEventListener('click', onBackdrop)
    return () => {
      dialog?.removeEventListener('click', onBackdrop)
      if (opener instanceof HTMLElement && opener.isConnected) {
        opener.focus()
      }
    }
  }, [])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="stop-title"
      aria-describedby="stop-body"
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      className="fixed inset-x-0 bottom-0 top-auto m-0 mx-auto w-full max-w-md rounded-t-md border border-border bg-surface p-4 pb-6 text-ink backdrop:bg-ink/40"
    >
      <h2 id="stop-title" className="text-title">
        {t('parent.stop.title', { child: childName })}
      </h2>
      <p id="stop-body" className="mt-2 text-body text-ink-muted">
        {t('parent.stop.body')}
      </p>
      {stop.isError && (
        <p role="alert" className="mt-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('parent.stop.failed')}
        </p>
      )}
      <div className="mt-4 flex flex-col gap-2">
        <PrimaryButton
          disabled={stop.isPending}
          onClick={() =>
            stop.mutate(true, {
              onSuccess: () => announce(t('parent.announce.stopped')),
            })
          }
        >
          {t('parent.stop.confirm')}
        </PrimaryButton>
        <button
          ref={cancelRef}
          type="button"
          onClick={onClose}
          className="min-h-target w-full rounded-md text-label text-primary-strong"
        >
          {t('parent.stop.cancel')}
        </button>
      </div>
    </dialog>
  )
}

/** P4 stopped state: no stop link, no step dots; one tap starts the messages again. */
export function StoppedView({ token, childName }: { token: string; childName: string }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const restart = useStop(token)
  const headingRef = useRef<HTMLHeadingElement>(null)
  useEffect(() => headingRef.current?.focus(), [])
  return (
    <section aria-labelledby="stopped-title" className="flex flex-col items-center gap-4 px-4 py-10 text-center">
      <Icon name="bellOff" size={56} className="text-ink-muted" />
      <h1 id="stopped-title" ref={headingRef} tabIndex={-1} className="text-display">
        {t('parent.stop.doneTitle')}
      </h1>
      <p className="text-body-lg">{t('parent.stop.doneBody', { child: childName })}</p>
      {restart.isError && (
        <p role="alert" className="w-full border-l-4 border-l-error bg-error-bg p-3 text-left text-body text-ink">
          {t('parent.stop.restartFailed')}
        </p>
      )}
      <button
        type="button"
        disabled={restart.isPending}
        onClick={() => restart.mutate(false, { onSuccess: () => announce(t('parent.announce.restarted')) })}
        className="min-h-target-primary w-full rounded-md border border-border px-4 text-label text-ink disabled:opacity-60"
      >
        {t('parent.stop.restart')}
      </button>
    </section>
  )
}
