import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

  it('is one line, “N earned a star · See all”; See all lists the children in the order given (T-99, D-118)', async () => {
    const { container } = show({ finished: true, schoolDays: 6, children: [child({}), child({ childId: 'c2', rollNo: 9, firstName: 'Kiran' })] })

    expect(screen.getByRole('heading', { name: 'Stars' })).toBeInTheDocument()
    expect(screen.getByText('2 earned a star')).toBeInTheDocument()
    const seeAll = screen.getByRole('button', { name: 'See all' })
    expect(seeAll).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByText('Came to school every day this week:')).not.toBeVisible()

    await userEvent.click(seeAll)
    expect(seeAll).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Came to school every day this week:')).toBeVisible()
    const items = within(screen.getByRole('list')).getAllByRole('listitem')
    expect(items.map((i) => i.getAttribute('aria-label'))).toEqual(['Asha, roll 3', 'Kiran, roll 9'])
    expect(screen.getByText('Say well done in class.')).toBeVisible()
    expect(screen.getByText('A star for coming to school every day of the week. Each week starts fresh.')).toBeVisible()
    // Besides the roll numbers, the only number is the class count: nothing per child, no totals over weeks.
    expect(container.textContent?.replace(/(3|9)\. /g, '').replace('2 earned a star', '')).not.toMatch(/\d/)
  })

  it('says “so far” for the current week', async () => {
    show({ finished: false, schoolDays: 3, children: [child({})] })

    expect(screen.getByRole('heading', { name: 'Stars this week' })).toBeInTheDocument()
    expect(screen.getByText('1 on track so far')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'See all' }))
    expect(screen.getByText('Every day so far this week:')).toBeVisible()
  })

  it('shows no names and no list when nobody earned one, and never says a star was lost', () => {
    const { container } = show({ finished: true, schoolDays: 6, children: [] })

    expect(screen.getByText('No stars this week. Every week is a new start.')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'See all' })).not.toBeInTheDocument()
    expect(container.textContent).not.toMatch(/lost|missed|broke/i)
  })

  it('is hidden when the week has no register', () => {
    const { container } = show(null)
    expect(container).toBeEmptyDOMElement()
    const missing = show(undefined)
    expect(missing.container).toBeEmptyDOMElement()
  })
})
