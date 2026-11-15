import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { formatSchoolTime } from '../today/schoolTime'
import type { AiCall } from './types'
import { useAiCalls } from './useDemo'

/** Each Claude and Sarvam call since the reset: what for, which model and prompt version, and the verdict. */
export function UnderTheHood({ since }: { since: string | null }) {
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
                <th scope="col" className="p-2">{t('demo.ai.for')}</th>
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
      <td className="p-2">
        {call.children.length > 0 || call.otherChildren > 0
          ? [...call.children, ...(call.otherChildren > 0 ? [t('demo.ai.otherClass', { count: call.otherChildren })] : [])].join(', ')
          : '–'}
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
