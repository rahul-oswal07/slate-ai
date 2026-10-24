import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * One accent word in a heading (D-77). In English it is handwritten (Caveat, Latin only) with a chalk squiggle drawn
 * on beneath it; in Kannada, Telugu and Tamil it stays in the heading face with a plain amber underline. Decorative:
 * the heading reads the same to a screen reader.
 */
export function AccentWord({ children }: { children: ReactNode }) {
  const { i18n } = useTranslation()
  const handwritten = i18n.language === 'en'
  return (
    <span className={`relative inline-block leading-tight ${handwritten ? 'font-hand text-[1.25em] font-bold' : ''}`}>
      {children}
      {handwritten ? (
        <svg
          aria-hidden="true"
          viewBox="0 0 100 8"
          preserveAspectRatio="none"
          className="pointer-events-none absolute inset-x-0 -bottom-1 h-2 w-full overflow-visible"
          fill="none"
        >
          <path
            pathLength="100"
            d="M1 5 Q 13 0 25 5 T 50 5 T 75 5 T 99 5"
            strokeWidth="3.5"
            strokeLinecap="round"
            className="draw-on stroke-accent"
            vectorEffect="non-scaling-stroke"
          />
        </svg>
      ) : (
        <span aria-hidden="true" className="pointer-events-none absolute inset-x-0 -bottom-0.5 h-[3px] rounded-pill bg-accent" />
      )}
    </span>
  )
}

/**
 * A title with one word picked out as the accent. The whole sentence stays in one piece for screen readers (and for
 * queries by text); the split, decorated copy is what the eye sees, and is hidden from assistive technology.
 */
export function AccentTitle({ title, accent }: { title: string; accent?: string }) {
  const at = accent ? title.indexOf(accent) : -1
  if (!accent || at < 0) {
    return <>{title}</>
  }
  return (
    <>
      <span className="sr-only">{title}</span>
      <span aria-hidden="true">
        {title.slice(0, at)}
        <AccentWord>{accent}</AccentWord>
        {title.slice(at + accent.length)}
      </span>
    </>
  )
}
