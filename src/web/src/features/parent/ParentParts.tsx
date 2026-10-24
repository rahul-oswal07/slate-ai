import { useEffect, useRef, useState, type ButtonHTMLAttributes, type ReactNode, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { AccentTitle } from '../../components/AccentWord'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import type { Homework } from './types'
import { useStop } from './useParentPage'

/**
 * Screen header: Back (except on the first step) and the title, which takes focus when the step opens. On the
 * canvas, with no bar and no rule (D-77); `accent` picks one word of the title out in handwriting or an amber underline.
 */
export function ParentHeader({
  title,
  accent,
  onBack,
  headingRef,
}: {
  title: string
  accent?: string
  onBack?: () => void
  headingRef?: RefObject<HTMLHeadingElement | null>
}) {
  const { t } = useTranslation()
  return (
    <header className="flex items-center gap-2 px-2 py-2">
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
      <h1 ref={headingRef} tabIndex={-1} className="text-title font-extrabold">
        <AccentTitle title={title} accent={accent} />
      </h1>
    </header>
  )
}

/**
 * Step n of 4 (D-08, D-77): a chalk tally. Done strokes are chalk, the current one amber (with an ink outline in
 * light mode, where amber alone is 1.3:1), the rest dashed; on the last step a slash crosses all four. "n/4" is
 * visible and "Step n of 4" is what a screen reader hears. It only counts up.
 */
export function StepDots({ step }: { step: number }) {
  const { t } = useTranslation()
  // Four 3px strokes, 28px tall, 12px apart, each leaning a little like chalk (D-77).
  const x = (n: number) => 8 + (n - 1) * 12
  const stroke = (n: number) => ({ x1: x(n) + 1.5, y1: 2, x2: x(n) - 1.5, y2: 30 })
  return (
    <div className="flex items-center justify-start gap-4">
      <span className="sr-only">{t('parent.steps', { n: step })}</span>
      <svg aria-hidden="true" width="56" height="32" viewBox="0 0 56 32" fill="none" strokeLinecap="round" className="overflow-visible">
        {[1, 2, 3, 4].map((n) => {
          if (n === step && step < 4) {
            return (
              <g key={n} data-stroke="current">
                <line {...stroke(n)} strokeWidth="8" className="stroke-ink dark:stroke-transparent" />
                <line {...stroke(n)} pathLength="100" strokeWidth="4" className="draw-on stroke-accent" />
              </g>
            )
          }
          return n < step || step === 4 ? (
            <line key={n} data-stroke="done" {...stroke(n)} strokeWidth="3" className="stroke-ink" />
          ) : (
            <line key={n} data-stroke="todo" {...stroke(n)} strokeWidth="3" strokeDasharray="1 6" className="stroke-chalk" />
          )
        })}
        {step === 4 && (
          <g data-stroke="slash">
            <line x1="-2" y1="26" x2="54" y2="6" strokeWidth="8" className="stroke-ink dark:stroke-transparent" />
            <line x1="-2" y1="26" x2="54" y2="6" pathLength="100" strokeWidth="4" className="draw-on stroke-accent" />
          </g>
        )}
      </svg>
      <span aria-hidden="true" className="text-body font-extrabold text-ink">
        {step}/4
      </span>
    </div>
  )
}

/**
 * NFR-08: every step with AI-written words says so, with the bot icon (not the reward star, C-25). A quiet outlined
 * pill with the same sentence as before (D-77).
 */
export function AiLabel({ teacher }: { teacher: string }) {
  const { t } = useTranslation()
  return (
    <p className="flex items-center gap-2 rounded-pill border-[1.5px] border-border bg-surface px-4 py-2 text-label text-ink">
      <Icon name="bot" size={22} className="shrink-0" />
      {t('parent.aiLabel', { teacher })}
    </p>
  )
}

/** The child's own textbook page (D-22). */
export function HomeworkCard({ homework, teacher }: { homework: Homework; teacher: string }) {
  const { t } = useTranslation()
  return (
    <section className="card flex items-start gap-3">
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
    <button type="button" {...props} className="btn-primary">
      {children}
    </button>
  )
}

/** Secondary full-width button: a panel fill and a teal border, for "Read the words" and Skip-style choices. */
export function SecondaryButton({ children, ...props }: { children: ReactNode } & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button type="button" {...props} className="btn-secondary">
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
        className="btn-link w-full justify-center"
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
      className="slide-up fixed inset-x-0 bottom-0 top-auto m-0 mx-auto w-full max-w-md rounded-t-sheet border-2 border-b-0 border-border bg-surface p-4 pb-6 text-ink backdrop:bg-canvas/60"
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
          className="btn-link w-full justify-center no-underline"
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
    <section aria-labelledby="stopped-title" className="mx-4 my-8 card flex flex-col items-center gap-4 py-8 text-center">
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
        className="btn-secondary"
      >
        {t('parent.stop.restart')}
      </button>
    </section>
  )
}
