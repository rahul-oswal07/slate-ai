import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { initI18n } from '../../i18n'
import { todaysMessage } from '../approve/fixtures'
import type { TodaysMessage } from '../approve/todaysTypes'
import { TodaysMessageCard } from './TodaysMessageCard'

const i18n = initI18n()

const show = (message: TodaysMessage) =>
  render(
    <MemoryRouter>
      <TodaysMessageCard message={message} />
    </MemoryRouter>,
  )

describe("Today's message on Today (T-108)", () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it.each(['generating', 'ready', 'failed'] as const)(
    'not approved (%s): it is waiting for her, with the one button to Approve',
    (status) => {
      show(todaysMessage({ status }))

      const card = screen.getByRole('region', { name: 'Today’s message is waiting for you' })
      expect(within(card).getByText('Until you approve it, families get the plain evening message.')).toBeInTheDocument()
      expect(within(card).getByRole('link', { name: 'Check and approve' })).toHaveAttribute('href', '/approve')
      expect(screen.queryByText(/approved ·/)).not.toBeInTheDocument()
    },
  )

  it('approved: says so and when it goes, and never asks her to approve again', () => {
    show(todaysMessage({ status: 'approved', approvedVersion: 1 }))

    const card = screen.getByRole('region', { name: 'Today’s message: approved · goes after 6 pm' })
    expect(within(card).getByRole('link', { name: 'See today’s message' })).toHaveAttribute('href', '/approve')
    expect(screen.queryByRole('link', { name: 'Check and approve' })).not.toBeInTheDocument()
    expect(screen.queryByText(/plain evening message/)).not.toBeInTheDocument()
  })

  it('an edit after approving clears it: waiting again', () => {
    show(todaysMessage({ status: 'ready', version: 2, approvedVersion: null }))

    expect(screen.getByRole('region', { name: 'Today’s message is waiting for you' })).toBeInTheDocument()
  })

  it('sent: nothing here (the answers card shows it)', () => {
    const { container } = show(todaysMessage({ status: 'approved', approvedVersion: 1, sent: true }))

    expect(container).toBeEmptyDOMElement()
  })
})
