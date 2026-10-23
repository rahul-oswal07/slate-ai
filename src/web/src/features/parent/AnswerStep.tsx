import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAnnounce } from '../../components/useAnnounce'
import { PrimaryButton } from './ParentParts'
import { Sticks } from './Sticks'
import { sticksText } from './sticksText'
import type { ParentCatchUp } from './types'
import { useAnswer } from './useParentPage'

/** "43 − 18" reads as a sum to work out; "347" or "4 in 347" are shown as they are. */
const asQuestion = (question: string) => (/[+−-]/.test(question) ? `${question} = ?` : question)

/**
 * P3 Answer: three stick pictures in the order code shuffled them; the numeral shows only on the one tapped (D-07,
 * C-10). Tapping another changes the choice; "This one, send" posts it. The family never sees right or wrong (D-20).
 * Online only (D-70): a failed send keeps the choice and offers Try again.
 */
export function AnswerStep({ token, page, onSent }: { token: string; page: ParentCatchUp; onSent: () => void }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const answer = useAnswer(token)
  const [chosen, setChosen] = useState<number | null>(null)
  const check = page.check
  if (!check) {
    return null
  }

  const sending = answer.isPending
  function send() {
    if (chosen === null) {
      return
    }
    announce(t('parent.announce.sending'))
    answer.mutate(chosen, {
      onSuccess: () => {
        announce(t('parent.announce.sent'))
        onSent()
      },
    })
  }

  return (
    <div className="flex flex-col gap-4">
      <p lang={check.prompt ? page.language : undefined} className="text-center text-title">
        {check.prompt ?? t('parent.answer.prompt', { child: page.childName })}
      </p>
      <p className="text-center text-display">{asQuestion(check.question)}</p>
      <ul aria-label={t('parent.answer.options')} className="flex flex-col gap-3">
        {check.options.map((option) => {
          const picked = option.id === chosen
          const name = sticksText(t, option.number)
          return (
            <li key={option.id}>
              <button
                type="button"
                aria-pressed={picked}
                aria-label={picked ? `${name}, ${option.number}` : name}
                disabled={sending}
                onClick={() => {
                  answer.reset()
                  setChosen(option.id)
                }}
                className={`flex min-h-target-picture w-full items-center gap-3 rounded-md p-3 focus-visible:outline-offset-[-4px] disabled:opacity-60 ${
                  picked ? 'border-4 border-primary bg-panel' : 'border-2 border-border bg-surface'
                }`}
              >
                <span className="flex-1">
                  <Sticks n={option.number} />
                </span>
                {picked && (
                  <span aria-hidden="true" className="text-title font-bold text-ink">
                    {option.number}
                  </span>
                )}
              </button>
            </li>
          )
        })}
      </ul>

      <div className="sticky bottom-0 -mx-4 flex flex-col gap-1 border-t border-divider bg-canvas px-4 py-3">
        {/* In the sticky footer, so the failure is never hidden behind it under tall picture tiles. */}
        {answer.isError && (
          <p role="alert" className="mb-2 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
            {t('parent.answer.failed')}
          </p>
        )}
        {chosen === null ? (
          <p className="text-center text-caption text-ink-muted">{t('parent.answer.hintTap')}</p>
        ) : (
          <>
            <PrimaryButton onClick={send} disabled={sending}>
              {sending ? t('parent.answer.sending') : answer.isError ? t('parent.answer.retry') : t('parent.answer.send')}
            </PrimaryButton>
            <p className="text-center text-caption text-ink-muted">{t('parent.answer.hintChange')}</p>
          </>
        )}
      </div>
    </div>
  )
}
