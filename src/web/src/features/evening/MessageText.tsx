import type { TextPart } from './types'

/** The message with each code-filled slot underlined (T0 A-20: a filled example, never `{child}`). */
export function MessageText({ parts, lang }: { parts: TextPart[]; lang: string }) {
  return (
    <p lang={lang} className="text-body text-ink">
      {parts.map((part, i) =>
        part.slot ? (
          <span key={i} className="font-semibold text-primary-strong underline decoration-2 underline-offset-4">
            {part.text}
          </span>
        ) : (
          <span key={i}>{part.text}</span>
        ),
      )}
    </p>
  )
}
