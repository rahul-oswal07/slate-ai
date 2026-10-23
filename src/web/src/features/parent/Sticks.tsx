import { splitSticks } from './sticksText'

const HEIGHT = 48

/** One loose stick. */
function Stick() {
  return (
    <svg width={6} height={HEIGHT} viewBox={`0 0 6 ${HEIGHT}`} focusable="false">
      <rect x={1} y={0} width={4} height={HEIGHT} rx={2} className="fill-stick" />
    </svg>
  )
}

/** Ten sticks tied with a band. */
function Ten() {
  return (
    <svg width={22} height={HEIGHT} viewBox={`0 0 22 ${HEIGHT}`} focusable="false">
      {[0, 4, 8, 12, 16].map((x) => (
        <rect key={x} x={x + 1} y={0} width={3} height={HEIGHT} rx={1.5} className="fill-stick" />
      ))}
      <rect x={0} y={HEIGHT / 2 - 3} width={22} height={6} rx={2} className="fill-primary" />
    </svg>
  )
}

/** Ten bundles of ten tied together: wider, with two bands. */
function Hundred() {
  return (
    <svg width={44} height={HEIGHT} viewBox={`0 0 44 ${HEIGHT}`} focusable="false">
      {Array.from({ length: 11 }, (_, i) => (
        <rect key={i} x={i * 4} y={0} width={3} height={HEIGHT} rx={1.5} className="fill-stick" />
      ))}
      <rect x={0} y={HEIGHT / 3 - 3} width={44} height={6} rx={2} className="fill-primary" />
      <rect x={0} y={(HEIGHT * 2) / 3 - 3} width={44} height={6} rx={2} className="fill-primary" />
    </svg>
  )
}

/**
 * The sticks picture, drawn by code from the number (D-07, P2/P3): hundreds, tens and loose sticks in groups that
 * wrap on a narrow phone. Hidden from screen readers: the text from `sticksText` goes next to it.
 */
export function Sticks({ n }: { n: number }) {
  const { hundreds, tens, ones } = splitSticks(n)
  const groups = [
    { key: 'h', count: hundreds, Item: Hundred },
    { key: 't', count: tens, Item: Ten },
    { key: 'o', count: ones, Item: Stick },
  ].filter((g) => g.count > 0)
  return (
    <div aria-hidden="true" className="flex flex-wrap items-end justify-center gap-x-4 gap-y-2">
      {groups.map(({ key, count, Item }) => (
        <div key={key} className="flex flex-wrap gap-1">
          {Array.from({ length: count }, (_, i) => (
            <Item key={i} />
          ))}
        </div>
      ))}
    </div>
  )
}
