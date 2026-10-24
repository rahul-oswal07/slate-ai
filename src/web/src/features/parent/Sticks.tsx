import type { ComponentType } from 'react'
import { splitSticks } from './sticksText'

const HEIGHT = 48

/** A hundred lies flat, so up to 9 of them fit in one row on a 360 px phone (D-76). */
const FLAT = 28

/** One loose stick. */
function Stick() {
  return (
    <svg width={5} height={HEIGHT} viewBox={`0 0 5 ${HEIGHT}`} focusable="false">
      <rect x={0.5} y={0} width={4} height={HEIGHT} rx={2} className="fill-stick" />
    </svg>
  )
}

/** Ten sticks tied with a band. */
function Ten() {
  return (
    <svg width={16} height={HEIGHT} viewBox={`0 0 16 ${HEIGHT}`} focusable="false">
      {[0, 3, 6, 9, 12].map((x) => (
        <rect key={x} x={x + 1} y={0} width={2} height={HEIGHT} rx={1} className="fill-stick" />
      ))}
      <rect x={0} y={HEIGHT / 2 - 3} width={16} height={6} rx={2} className="fill-primary" />
    </svg>
  )
}

/** Ten bundles of ten tied together, lying flat: short and wide, with two bands. */
function Hundred() {
  return (
    <svg width={24} height={FLAT} viewBox={`0 0 24 ${FLAT}`} focusable="false">
      {Array.from({ length: 8 }, (_, i) => (
        <rect key={i} x={i * 3} y={0} width={2.5} height={FLAT} rx={1} className="fill-stick" />
      ))}
      <rect x={0} y={5} width={24} height={5} rx={2} className="fill-primary" />
      <rect x={0} y={FLAT - 10} width={24} height={5} rx={2} className="fill-primary" />
    </svg>
  )
}

/** One kind of stick, side by side. */
function Group({ count, Item }: { count: number; Item: ComponentType }) {
  return (
    <div className="flex flex-wrap justify-center gap-0.5">
      {Array.from({ length: count }, (_, i) => (
        <Item key={i} />
      ))}
    </div>
  )
}

/**
 * The sticks picture, drawn by code from the number (D-07, P2/P3): hundreds in one row, then tens and loose sticks
 * in a row below, so even 999 is two rows and three P3 choices stay short (D-76). Hidden from screen readers: the
 * text from `sticksText` goes next to it.
 */
export function Sticks({ n }: { n: number }) {
  const { hundreds, tens, ones } = splitSticks(n)
  return (
    <div aria-hidden="true" className="flex flex-col items-center gap-2">
      {hundreds > 0 && <Group count={hundreds} Item={Hundred} />}
      {tens + ones > 0 && (
        <div className="flex flex-wrap items-end justify-center gap-x-3 gap-y-2">
          {tens > 0 && <Group count={tens} Item={Ten} />}
          {ones > 0 && <Group count={ones} Item={Stick} />}
        </div>
      )}
    </div>
  )
}
