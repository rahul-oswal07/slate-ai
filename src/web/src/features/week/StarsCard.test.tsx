import { render, screen, within } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { StarsCard } from './StarsCard'
import type { StarChild, WeekStars } from './types'

const i18n = initI18n()

const child = (o: Partial<StarChild>): StarChild => ({
  childId: 'c1', rollNo: 3, firstName: 'Asha', ...o,
})

function show(stars: WeekStars | null | undefined) {
  return render(
    <I18nextProvider i18n={i18n}>
      <StarsCard stars={stars} />
    </I18nextProvider>,
  )
}

describe('StarsCard', () => {
  beforeAll(() => i18n.changeLanguage('en'))

  it('lists the children present every day of the finished week in the order given, with no counts (D-118)', () => {
    const { container } = show({ finished: true, schoolDays: 6, children: [child({}), child({ childId: 'c2', rollNo: 9, firstName: 'Kiran' })] })

    expect(screen.getByRole('heading', { name: 'Stars' })).toBeInTheDocument()
    expect(screen.getByText('Came to school every day this week:')).toBeInTheDocument()
    const items = within(screen.getByRole('list')).getAllByRole('listitem')
    expect(items.map((i) => i.getAttribute('aria-label'))).toEqual(['Asha, roll 3', 'Kiran, roll 9'])
    expect(screen.getByText('Say well done in class.')).toBeInTheDocument()
    expect(screen.getByText('A star for coming to school every day of the week. Each week starts fresh.')).toBeInTheDocument()
    // The only digits are the roll numbers: no totals, no counts.
    expect(container.textContent?.replace(/(3|9)\. /g, '')).not.toMatch(/\d/)
  })

  it('says “so far” for the current week', () => {
    show({ finished: false, schoolDays: 3, children: [child({})] })

    expect(screen.getByRole('heading', { name: 'Stars this week' })).toBeInTheDocument()
    expect(screen.getByText('Every day so far this week:')).toBeInTheDocument()
  })

  it('shows no names and no list when nobody earned one, and never says a star was lost', () => {
    const { container } = show({ finished: true, schoolDays: 6, children: [] })

    expect(screen.getByText('No stars this week. Every week is a new start.')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
    expect(container.textContent).not.toMatch(/lost|missed|broke/i)
  })

  it('is hidden when the week has no register', () => {
    const { container } = show(null)
    expect(container).toBeEmptyDOMElement()
    const missing = show(undefined)
    expect(missing.container).toBeEmptyDOMElement()
  })
})
