import { render } from '@testing-library/react'
import { Sticks } from './Sticks'

/** The rows the picture draws: one entry per row, the svg widths in it. */
function rows(n: number) {
  const { container } = render(<Sticks n={n} />)
  const picture = container.firstElementChild as HTMLElement
  return Array.from(picture.children).map((row) => Array.from(row.querySelectorAll('svg')).map((svg) => svg.getAttribute('width')))
}

describe('Sticks (D-76)', () => {
  it('draws 999 in two rows: nine flat hundreds, then nine tens and nine loose sticks', () => {
    const [hundreds = [], rest = []] = rows(999)

    expect(hundreds).toEqual(Array(9).fill('24'))
    expect(rest).toEqual([...Array(9).fill('16'), ...Array(9).fill('5')])
  })

  it('keeps every row of 999 narrower than a 360 px tile with the numeral shown', () => {
    // 360 − page padding 32 − tile border and padding 28 − numeral and gap ≈ 248 px for the picture.
    const widths = { '24': 24, '16': 16, '5': 5 }
    const [hundreds = [], rest = []] = rows(999)
    const row = (items: (string | null)[], groups: number) =>
      items.reduce((sum, w) => sum + widths[w as keyof typeof widths], 0) + (items.length - groups) * 2 + (groups - 1) * 12

    expect(row(hundreds, 1)).toBeLessThanOrEqual(248)
    expect(row(rest, 2)).toBeLessThanOrEqual(248)
  })

  it('leaves out rows with nothing in them', () => {
    expect(rows(300)).toEqual([Array(3).fill('24')])
    expect(rows(43)).toEqual([[...Array(4).fill('16'), ...Array(3).fill('5')]])
  })

  it('is hidden from screen readers: the text name goes next to it', () => {
    const { container } = render(<Sticks n={347} />)

    expect(container.firstElementChild).toHaveAttribute('aria-hidden', 'true')
  })
})
