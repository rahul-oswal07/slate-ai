import { QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { initI18n } from '../i18n'
import { chosenSchoolDate, setSchoolDate } from '../features/today/schoolDate'
import { istToday } from '../features/today/schoolTime'
import { createQueryClient } from './queryClient'
import { BackToToday, SchoolDateNav, SchoolWeekNav } from './SchoolDateNav'
import { fakeApi, teacher } from '../test/renderApp'

const i18n = initI18n()

function fromToday(days: number) {
  const date = new Date(`${istToday()}T12:00:00+05:30`)
  date.setUTCDate(date.getUTCDate() + days)
  return istToday(date)
}

function renderNav(mode: 'day' | 'week', { demo = true, schoolDate = true } = {}) {
  fakeApi({
    'GET /api/me': () => ({ status: 200, body: { ...teacher, demo: demo ? { sampleVoiceNote: false, schoolDate } : null } }),
  })
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter>
        <h1>
          <SchoolDateNav mode={mode}>
            <span>{mode === 'day' ? 'Today, the day' : 'Week of the week'}</span>
          </SchoolDateNav>
        </h1>
        <BackToToday />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('SchoolDateNav (T-134, D-120)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    setSchoolDate(null)
    vi.unstubAllGlobals()
  })

  it('puts arrows beside the date: back a day, forward again, and Back to today only on a past day', async () => {
    const user = userEvent.setup()
    renderNav('day')

    const back = await screen.findByRole('button', { name: 'Previous day' })
    expect(screen.getByRole('button', { name: 'Next day' })).toBeDisabled()
    expect(screen.queryByRole('button', { name: 'Back to today' })).not.toBeInTheDocument()

    await user.click(back)
    await user.click(back)
    expect(chosenSchoolDate()).toBe(fromToday(-2))
    await user.click(screen.getByRole('button', { name: 'Next day' }))
    expect(chosenSchoolDate()).toBe(fromToday(-1))

    await user.click(screen.getByRole('button', { name: 'Back to today' }))
    expect(chosenSchoolDate()).toBeNull()
  })

  it('opens the calendar from the date itself, and picking today is the real day', async () => {
    const user = userEvent.setup()
    renderNav('day')

    const date = await screen.findByRole('button', { name: 'Today, the day, Choose a day' })
    const calendar = screen.getByLabelText('Choose a day')
    expect(calendar).toHaveAttribute('type', 'date')
    expect(calendar).toHaveAttribute('max', istToday())
    await user.click(date)

    // jsdom has no native calendar: the picker sets the whole value at once.
    fireEvent.change(calendar, { target: { value: fromToday(-9) } })
    expect(chosenSchoolDate()).toBe(fromToday(-9))
    fireEvent.change(calendar, { target: { value: istToday() } })
    expect(chosenSchoolDate()).toBeNull()
  })

  it('in a week steps 7 days', async () => {
    const user = userEvent.setup()
    renderNav('week')

    await user.click(await screen.findByRole('button', { name: 'Previous week' }))
    expect(chosenSchoolDate()).toBe(fromToday(-7))
    await user.click(screen.getByRole('button', { name: 'Next week' }))
    expect(chosenSchoolDate()).toBeNull()
  })

  it.each([
    ['the admin switch is off', { schoolDate: false }],
    ['outside the demo class', { demo: false }],
  ])('is just the date when %s', async (_, options) => {
    renderNav('day', options)

    expect(await screen.findByText('Today, the day')).toBeInTheDocument()
    await new Promise((resolve) => setTimeout(resolve, 50))
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it.each([
    [true, 1],
    [false, 0],
  ])('under Missed and Back today the week shows only with the switch on (%s)', async (schoolDate, shown) => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: { ...teacher, demo: { sampleVoiceNote: false, schoolDate } } }),
    })
    render(
      <QueryClientProvider client={createQueryClient()}>
        <MemoryRouter>
          <SchoolWeekNav />
        </MemoryRouter>
      </QueryClientProvider>,
    )

    await new Promise((resolve) => setTimeout(resolve, 50))
    expect(screen.queryAllByRole('button', { name: /^Week of .*, Choose a day$/ })).toHaveLength(shown)
  })
})
