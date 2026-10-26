import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, type FakeRoute } from '../../test/renderApp'
import type { ParentEvening } from './types'

const i18n = initI18n()
const token = 'ZyXwVuTsRqPoNmLkJiHgFe'
const pageUrl = `GET /api/p/${token}`
const reasonUrl = `POST /api/p/${token}/reason`

function message(overrides: Partial<ParentEvening> = {}): ParentEvening {
  return {
    kind: 'evening',
    language: 'en',
    childName: 'Ravi',
    teacherName: 'Shobha',
    text: 'We missed Ravi today. In class we learned subtraction. Homework: textbook Part 2, page 15.',
    greeting: null,
    stopped: false,
    reason: null,
    ...overrides,
  }
}

function setup(current: () => ParentEvening, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({ [pageUrl]: () => ({ status: 200, body: current() }), ...extra })
}

describe('Reason for absence (FR-21)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('the evening message offers “Is everything OK? Tell us”, which opens five pictures and nothing chosen', async () => {
    setup(() => message())
    renderApp(`/p/${token}`)

    expect(await screen.findByText(/We missed Ravi today\. In class/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Is everything OK? Tell us' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Shobha teacher' })).toHaveFocus()
    expect(screen.getByText('We missed Ravi today. Is everything OK?')).toBeInTheDocument()
    const group = screen.getByRole('group', { name: 'We missed Ravi today. Is everything OK?' })
    const pictures = within(group).getAllByRole('button')
    expect(pictures.map((b) => b.textContent)).toEqual([
      'Unwell',
      'Family travel or work',
      'Festival or function',
      'Needed at home',
      'Other',
    ])
    expect(pictures.every((b) => b.getAttribute('aria-pressed') === 'false')).toBe(true)
    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled()
    expect(screen.getByText('Tap one picture')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Stop these messages' })).toBeInTheDocument()
  })

  it('Send posts the category only; the family sees Thank you and who sees it', async () => {
    let current = message()
    const calls = setup(() => current, {
      [reasonUrl]: ({ body }) => {
        const category = (body as { category: ParentEvening['reason'] }).category
        current = { ...current, reason: category }
        return { status: 200, body: { category } }
      },
    })
    renderApp(`/p/${token}?view=reason`)

    await userEvent.click(await screen.findByRole('button', { name: 'Unwell' }))
    await userEvent.click(screen.getByRole('button', { name: 'Festival or function' }))
    expect(screen.getByRole('button', { name: 'Festival or function' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Unwell' })).toHaveAttribute('aria-pressed', 'false')
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Thank you' })).toBeInTheDocument()
    expect(screen.getByText('Only Shobha teacher sees this.')).toBeInTheDocument()
    const sent = calls.filter((c) => c.key === reasonUrl)
    expect(sent).toHaveLength(1)
    expect(Object.keys(sent[0]?.body as object).sort()).toEqual(['category', 'clientRequestId'])
    expect(sent[0]?.body).toMatchObject({ category: 'festival' })

    await userEvent.click(screen.getByRole('button', { name: 'OK' }))
    expect(await screen.findByText('Thank you for telling us.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Is everything OK? Tell us' })).toBeNull()
  })

  it('Skip sends nothing and goes back to the message', async () => {
    const calls = setup(() => message())
    renderApp(`/p/${token}?view=reason`)

    await userEvent.click(await screen.findByRole('button', { name: 'Unwell' }))
    await userEvent.click(screen.getByRole('button', { name: 'Skip' }))

    expect(await screen.findByRole('button', { name: 'Is everything OK? Tell us' })).toBeInTheDocument()
    expect(calls.some((c) => c.key === reasonUrl)).toBe(false)
  })

  it('a failed send keeps the choice; Try again reuses the same request ID', async () => {
    let online = false
    const calls = setup(() => message(), {
      [reasonUrl]: () => (online ? { status: 200, body: { category: 'neededAtHome' } } : 'offline'),
    })
    renderApp(`/p/${token}?view=reason`)

    await userEvent.click(await screen.findByRole('button', { name: 'Needed at home' }))
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not send. Your choice is still here.')
    expect(screen.getByRole('button', { name: 'Needed at home' })).toHaveAttribute('aria-pressed', 'true')
    online = true
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Thank you' })).toBeInTheDocument()
    const ids = calls
      .filter((c) => c.key === reasonUrl)
      .map((c) => (c.body as { clientRequestId: string }).clientRequestId)
    expect(ids).toHaveLength(2)
    expect(new Set(ids).size).toBe(1)
  })

  it('reopened: the message says thank you, and Change shows the reason already given', async () => {
    setup(() => message({ reason: 'travelOrWork' }))
    renderApp(`/p/${token}`)

    expect(await screen.findByText('Thank you for telling us.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Change' }))

    expect(await screen.findByRole('button', { name: 'Family travel or work' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Send' })).toBeEnabled()
  })

  it('a family that stopped messages sees Messages stopped, not the reason question', async () => {
    setup(() => message({ stopped: true }))
    renderApp(`/p/${token}?view=reason`)

    expect(await screen.findByRole('heading', { level: 1, name: 'Messages stopped' })).toBeInTheDocument()
    expect(screen.queryByText('We missed Ravi today. Is everything OK?')).toBeNull()
  })

  it('opens in Telugu with the gender-neutral question (D-72)', async () => {
    setup(() => message({ language: 'te' }))
    renderApp(`/p/${token}?view=reason`)

    expect(await screen.findByText('ఈ రోజు మేము Raviని గుర్తు చేసుకున్నాం. అంతా బాగుందా?')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'ఒంట్లో బాగాలేదు' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'వద్దు, వదిలేయండి' })).toBeInTheDocument()
  })
})
