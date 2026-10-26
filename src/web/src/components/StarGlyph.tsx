/** A chalk-drawn star for the stars card and the Done line (D-81). Decorative: the text beside it says it. */
export function StarGlyph({ size = 18, className = '' }: { size?: number; className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 48 48"
      width={size}
      height={size}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={`shrink-0 ${className}`}
    >
      <path
        d="M24 6 L29.5 18.5 L43 20 L33 29 L36 42 L24 35.5 L12 42 L15 29 L5 20 L18.5 18.5 Z"
        strokeWidth="4"
        className="fill-accent stroke-ink"
      />
    </svg>
  )
}
