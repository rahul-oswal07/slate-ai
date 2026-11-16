import { Fragment, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { clock } from '../parent/clock'
import { VoiceNote } from '../parent/VoiceNote'
import { formatSchoolTime } from '../today/schoolTime'
import { Phone } from './DemoParts'
import { familyTitle, shortDate } from './demoText'
import type { FamilyMessage } from './types'
import { useFamily } from './useDemo'

/**
 * The story family's phone as a chat (D-77, surface B): a stand-in for a messaging app, so judges see the real
 * hand-off. No logo and no brand name. Each link a family was sent is a bubble; opening one slides the family's page
 * up under a thin bar, as an in-app browser does.
 */
export function ParentPhone({ childId, name, language }: { childId: string; name: string; language: string }) {
  const { t } = useTranslation()
  return (
    <Phone title={familyTitle(t, name, language)} caption={t('demo.parentCaption')}>
      <FamilyChat childId={childId} name={name} />
    </Phone>
  )
}

/** The chat itself, filling whatever holds it: the phone frame in /demo, or the whole screen at /demo/family (T-126). */
export function FamilyChat({ childId, name }: { childId: string; name: string }) {
  const { t, i18n } = useTranslation()
  const family = useFamily(childId)
  const [open, setOpen] = useState<FamilyMessage | null>(null)
  // Oldest first, as a chat reads; the API sends newest first.
  const messages = [...(family.data ?? [])].reverse()

  return (
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
              <li className="flex max-w-[86%] flex-col gap-1 self-start">
                {message.greetingSeconds != null && (
                  <VoiceNote
                    url={`/api/p/${message.token}/greeting`}
                    label={t('demo.message.voice', { duration: clock(message.greetingSeconds) })}
                  />
                )}
                <button type="button" onClick={() => setOpen(message)} className="chat-bubble block w-full text-left">
                  <span className="block text-body font-bold">
                    {t(`demo.message.${message.kind}`, {
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
                {/* What the page's own <title> says, in every language (T-116). */}
                {t('demo.chat.pageTitle')}
              </p>
              <p className="truncate text-caption leading-tight text-ink-muted">{window.location.host}</p>
            </div>
            <span className="w-12 shrink-0" />
          </div>
          <iframe title={t('demo.parentPage', { name })} src={`/p/${open.token}`} className="w-full flex-1" />
        </div>
      )}
    </div>
  )
}

