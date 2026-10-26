import { render, screen, within } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { StarsCard } from './StarsCard'
import type { StarChild, WeekStars } from './types'

const i18n = initI18n()

const child = (o: Partial<StarChild>): StarChild => ({
  childId: 'c1', rollNo: 3, firstName: 'Asha', attendance: 3, catchUps: 1, total: 4, ...o,
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

  it('lists the children who earned the finished week’s star in the order given, with a spelled-out name', () => {
    show({ finished: true, schoolDays: 6, children: [child({}), child({ childId: 'c2', rollNo: 9, firstName: 'Kiran', attendance: 1, catchUps: 0, total: 1 })] })

    expect(screen.getByRole('heading', { name: 'Stars' })).toBeInTheDocument()
    expect(screen.getByText('2 children earned a star this week.')).toBeInTheDocument()
    const items = within(screen.getByRole('list')).getAllByRole('listitem')
    expect(items.map((i) => i.getAttribute('aria-label'))).toEqual([
      'Asha, roll 3, 4 stars this term: 3 for coming to school, 1 for a catch-up',
      'Kiran, roll 9, 1 star this term: 1 for coming to school, 0 for a catch-up',
    ])
    expect(screen.getByText('Say well done in class.')).toBeInTheDocument()
  })

  it('says “so far” for the current week and uses the singular', () => {
    show({ finished: false, schoolDays: 3, children: [child({})] })

    expect(screen.getByRole('heading', { name: 'Stars this week' })).toBeInTheDocument()
    expect(screen.getByText('So far 1 child is on track for a star.')).toBeInTheDocument()
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
