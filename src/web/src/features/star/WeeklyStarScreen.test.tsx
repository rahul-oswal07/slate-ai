import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import type { WeeklyStarWording } from './types'

const i18n = initI18n()
const url = `/api/classes/${teacher.classId}/weekly-star`

const parts = (before: string, after: string) => [
  { text: before, slot: null },
  { text: 'Asha', slot: 'child' as const },
  { text: after, slot: null },
]

function wording(approved = false): WeeklyStarWording {
  return {
    version: '1',
    approved,
    languages: [
      {
        language: 'en',
        whatsApp: parts('Class update – ', '. Weekly attendance: present every school day this week.'),
        message: parts('', ' came to school every day this week.'),
        badge: parts('A star for ', '! For coming to school every day this week.'),
      },
      {
        language: 'kn',
        whatsApp: parts('ತರಗತಿಯ ಮಾಹಿತಿ – ', '.'),
        message: parts('', ' ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ.'),
        badge: parts('', ' ಗೆ ಒಂದು ನಕ್ಷತ್ರ!'),
      },
    ],
  }
}

function setup(routes: Record<string, FakeRoute>) {
  return fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }), ...routes })
}

/** T0 step 3 and Settings: the weekly star she approves once (T-125, D-118, guardrail 3). */
describe('WeeklyStarScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows the WhatsApp text and both page screens filled with a child, the name marked, and the Kannada page', async () => {
    setup({ [`GET ${url}`]: () => ({ status: 200, body: wording() }) })
    renderApp('/setup/star')

    expect(await screen.findByText('Step 3 of 3')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'The weekly star for families' })).toHaveFocus()
    const whatsApp = (await screen.findByRole('heading', { name: 'WhatsApp message' })).closest('div') as HTMLElement
    expect(within(whatsApp).getByText('Asha')).toHaveClass('underline')
    expect(screen.getByText(/came to school every day this week\./)).toBeInTheDocument()
    expect(screen.getByText(/For coming to school every day this week\./)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Kannada version' })).toBeInTheDocument()
    expect(screen.queryByText(/\{child\}/)).not.toBeInTheDocument()
  })

  it('Approve sends the version once, then she goes to Today', async () => {
    const calls = setup({
      [`GET ${url}`]: () => ({ status: 200, body: wording() }),
      [`POST ${url}/approve`]: () => ({ status: 200, body: wording(true) }),
    })
    renderApp('/setup/star')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve the weekly star' }))

    expect(await screen.findByText(/You approved this\. It goes on Saturday evening/)).toBeInTheDocument()
    const approve = calls.filter((c) => c.key === `POST ${url}/approve`)
    expect(approve).toHaveLength(1)
    expect(approve[0]?.body).toMatchObject({ version: '1' })
    await userEvent.click(screen.getByRole('button', { name: 'Go to Today' }))
    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })

  it('a changed wording (409) says so and shows the latest', async () => {
    setup({
      [`GET ${url}`]: () => ({ status: 200, body: wording() }),
      [`POST ${url}/approve`]: () => ({ status: 409, body: wording() }),
    })
    renderApp('/setup/star')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve the weekly star' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed on another device. Showing the latest.')
  })

  it('from Settings: no step label, and it leads back to Settings', async () => {
    setup({ [`GET ${url}`]: () => ({ status: 200, body: wording(true) }) })
    renderApp('/settings/star')

    await userEvent.click(await screen.findByRole('button', { name: 'Back to Settings' }))

    expect(screen.queryByText('Step 3 of 3')).not.toBeInTheDocument()
    expect(await screen.findByRole('heading', { level: 1, name: 'Settings' })).toBeInTheDocument()
  })

  it('Settings says when it still needs her approval', async () => {
    setup({ [`GET ${url}`]: () => ({ status: 200, body: wording() }) })
    renderApp('/settings')

    const card = (await screen.findByRole('heading', { name: 'Weekly star' })).closest('section') as HTMLElement
    expect(await within(card).findByText('Needs your approval. No star messages go until you approve it.')).toBeInTheDocument()
    expect(within(card).getByRole('link', { name: 'Review the weekly star' })).toHaveAttribute('href', '/settings/star')
  })
})
