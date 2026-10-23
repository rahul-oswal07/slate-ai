import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon, type IconName } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { ParentHeader, PrimaryButton, StopControl } from './ParentParts'
import { REASON_CATEGORIES, type ParentEvening, type ReasonCategory } from './types'
import { useReason } from './useParentPage'

const ICONS: Record<ReasonCategory, IconName> = {
  unwell: 'thermometer',
  travelOrWork: 'bus',
  festival: 'flame',
  neededAtHome: 'house',
  other: 'messageCircle',
}

/**
 * "Is everything OK?" (FR-21, D-23): five picture buttons, Send, Skip. Optional and caring: no symptoms asked, only
 * the category is stored, only the teacher sees it. Skip sends nothing. Online only (D-70); a failed send keeps the
 * choice and offers Try again. Voice note and A7 are cut (D-63).
 */
export function ReasonView({ token, message, onDone }: { token: string; message: ParentEvening; onDone: () => void }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const reason = useReason(token)
  const [chosen, setChosen] = useState<ReasonCategory | null>(message.reason)
  const [sent, setSent] = useState(false)
  const headingRef = useRef<HTMLHeadingElement>(null)

  useEffect(() => {
    headingRef.current?.focus()
  }, [sent])

  if (sent) {
    return (
      <section aria-labelledby="reason-thanks" className="flex flex-col items-center gap-4 px-4 py-10 text-center">
        <Icon name="checkCircle" size={56} className="text-primary-strong" />
        <h1 id="reason-thanks" ref={headingRef} tabIndex={-1} className="text-display">
          {t('parent.reason.thanks')}
        </h1>
        <p className="text-body-lg">{t('parent.reason.private', { teacher: message.teacherName })}</p>
        <PrimaryButton onClick={onDone}>{t('parent.reason.ok')}</PrimaryButton>
      </section>
    )
  }

  const sending = reason.isPending
  function send() {
    if (chosen === null) {
      return
    }
    reason.mutate(chosen, {
      onSuccess: () => {
        announce(t('parent.announce.reasonSent'))
        setSent(true)
      },
    })
  }

  return (
    <>
      <ParentHeader
        title={t('parent.reason.title', { teacher: message.teacherName })}
        onBack={onDone}
        headingRef={headingRef}
      />
      <div className="flex flex-col gap-4 px-4 pt-4">
        <p id="reason-question" className="text-title">
          {t('parent.reason.question', { child: message.childName })}
        </p>
        <fieldset aria-labelledby="reason-question" disabled={sending}>
          <legend className="sr-only">{t('parent.reason.options')}</legend>
          <div className="grid grid-cols-2 gap-2">
            {REASON_CATEGORIES.map((category) => {
              const picked = category === chosen
              return (
                <button
                  key={category}
                  type="button"
                  aria-pressed={picked}
                  onClick={() => {
                    reason.reset()
                    setChosen(category)
                  }}
                  className={`flex min-h-target-picture flex-col items-center justify-center gap-2 rounded-md p-3 text-center text-label focus-visible:outline-offset-[-4px] disabled:opacity-60 ${
                    picked ? 'border-4 border-primary bg-panel' : 'border-2 border-border bg-surface'
                  }`}
                >
                  <Icon name={ICONS[category]} size={34} className="text-primary-strong" />
                  {t(`parent.reason.category.${category}`)}
                </button>
              )
            })}
          </div>
        </fieldset>

      </div>

      <div className="sticky bottom-0 mt-4 flex flex-col gap-1 border-t border-divider bg-canvas px-4 py-3">
        {/* In the sticky footer, so the failure is never hidden behind it on a short phone. */}
        {reason.isError && (
          <p role="alert" className="mb-2 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
            {t('parent.reason.failed')}
          </p>
        )}
        <PrimaryButton onClick={send} disabled={chosen === null || sending}>
          {sending ? t('parent.reason.sending') : reason.isError ? t('parent.reason.retry') : t('parent.reason.send')}
        </PrimaryButton>
        {chosen === null && <p className="text-center text-caption text-ink-muted">{t('parent.reason.hint')}</p>}
        <button type="button" onClick={onDone} className="min-h-target w-full rounded-md text-label text-primary-strong">
          {t('parent.reason.skip')}
        </button>
      </div>
      <div className="px-4 pb-4">
        <StopControl token={token} childName={message.childName} />
      </div>
    </>
  )
}
