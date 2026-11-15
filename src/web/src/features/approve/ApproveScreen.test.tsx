import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { curriculum, pkg, week } from './fixtures'
import type { CatchUpWeek } from './types'

const i18n = initI18n()
const packagesUrl = `GET /api/classes/${teacher.classId}/packages`

function setup(packages: CatchUpWeek | FakeRoute, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [packagesUrl]: typeof packages === 'function' ? packages : () => ({ status: 200, body: packages }),
    ...extra,
  })
}

describe('ApproveScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists one card per catch-up with language, children, lessons, duration and status', async () => {
    setup(week(pkg(), pkg({ id: 'pkg-kn', language: 'kn', status: 'needsReview', children: pkg().children.slice(0, 1) })))
    renderApp('/approve?tab=catch-ups')

    expect(await screen.findByRole('heading', { level: 1, name: 'Check and approve' })).toHaveFocus()
    const telugu = await screen.findByRole('link', { name: /^In Telugu · 2 children Ravi, Mahesh / })
    expect(telugu).toHaveAttribute('href', '/approve/pkg-te')
    expect(await within(telugu).findByText('3-digit subtraction with borrowing · 1:29')).toBeInTheDocument()
    expect(within(telugu).getByText('Ready')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /^In Kannada · 1 child / })).toHaveTextContent('Needs review')
  })

  it('each card link is named by the text it shows, so voice control and screen readers agree (WCAG 2.5.3)', async () => {
    setup(week(pkg(), pkg({ id: 'pkg-ap', language: 'kn', status: 'approved', children: pkg().children.slice(0, 1) })))
    renderApp('/approve?tab=catch-ups')

    await screen.findByRole('heading', { level: 1, name: 'Check and approve' })
    const cards = await screen.findAllByRole('link', { name: /^In (Telugu|Kannada)/ })
    expect(cards).toHaveLength(2)
    for (const card of cards) {
      const shown = (card.textContent ?? '').replace(/\s+/g, ' ').trim()
      expect(card).toHaveAccessibleName(expect.stringContaining(shown))
    }
    expect(cards[1]).toHaveAccessibleName(expect.stringContaining('Goes after 6 pm'))
  })

  it('the Approve tab counts catch-ups waiting for her, not generating or failed ones', async () => {
    setup(week(pkg(), pkg({ id: 'b', status: 'needsReview' }), pkg({ id: 'c', status: 'generating' }), pkg({ id: 'd', status: 'failed' })))
    renderApp('/approve?tab=catch-ups')

    const nav = await screen.findByRole('navigation', { name: 'Main' })
    expect(await within(nav).findByRole('link', { name: 'Approve, 2 waiting' })).toBeInTheDocument()
  })

  it('with nothing yet, she can make this week’s catch-ups', async () => {
    let generated = false
    const calls = setup(() => ({ status: 200, body: generated ? week(pkg({ status: 'generating' })) : week() }), {
      [`POST /api/classes/${teacher.classId}/catchups/generate`]: () => {
        generated = true
        return { status: 202, body: week(pkg({ status: 'generating' })) }
      },
    })
    renderApp('/approve?tab=catch-ups')

    await userEvent.click(await screen.findByRole('button', { name: 'Make this week’s catch-ups' }))

    expect(await screen.findByRole('link', { name: /Generating…/ })).toBeInTheDocument()
    expect(calls.filter((c) => c.key.startsWith('POST')).length).toBe(1)
  })

  it('all approved: says how many families get them after 6 pm (counted by the API)', async () => {
    setup(week(pkg({ status: 'approved', familiesReachable: 3 }), pkg({ id: 'b', status: 'sent', familiesSent: 2 })))
    renderApp('/approve?tab=catch-ups')

    expect(await screen.findByText('All 2 approved. 5 families get them after 6 pm.')).toBeInTheDocument()
  })

  it('all sent (the demo sends on Approve): says they have them, not "after 6 pm"', async () => {
    setup(week(pkg({ status: 'sent', familiesSent: 1 })))
    renderApp('/approve?tab=catch-ups')

    expect(await screen.findByText('All approved and sent. Families reached: 1.')).toBeInTheDocument()
  })

  it('one family: "1 family gets them", not "1 families" (T-100)', async () => {
    setup(week(pkg({ status: 'approved', familiesReachable: 1, children: pkg().children.slice(0, 1) })))
    renderApp('/approve?tab=catch-ups')

    expect(await screen.findByText('All 1 approved. 1 family gets them after 6 pm.')).toBeInTheDocument()
  })

  it('cards come by action needed: hers to check, then failed, then being written, approved and sent (T-100)', async () => {
    setup(
      week(
        pkg({ id: 'sent', language: 'en', status: 'sent', familiesSent: 1 }),
        pkg({ id: 'approved', language: 'hi', status: 'approved' }),
        pkg({ id: 'generating', language: 'kn', status: 'generating' }),
        pkg({ id: 'failed', language: 'ta', status: 'failed' }),
        pkg({ id: 'ready', language: 'te', status: 'ready' }),
      ),
    )
    renderApp('/approve?tab=catch-ups')

    const cards = await screen.findAllByRole('link', { name: /^In / })
    expect(cards.map((c) => c.getAttribute('href'))).toEqual([
      '/approve/ready',
      '/approve/failed',
      '/approve/generating',
      '/approve/approved',
      '/approve/sent',
    ])
  })

  it('a failed card has Try again, which writes a new version; no other card has it (T-100)', async () => {
    const regenerated = pkg({ id: 'failed', language: 'ta', status: 'generating' })
    const calls = setup(week(pkg(), pkg({ id: 'failed', language: 'ta', status: 'failed' })), {
      'POST /api/packages/failed/regenerate': () => ({ status: 202, body: regenerated }),
    })
    renderApp('/approve?tab=catch-ups')

    const tryAgain = await screen.findByRole('button', { name: 'Try again' })
    expect(tryAgain).toHaveAccessibleDescription(/^In Tamil · 2 children/)
    expect(tryAgain).toHaveClass('btn-secondary')

    await userEvent.click(tryAgain)

    expect(await screen.findByRole('link', { name: /^In Tamil .*Generating…/ })).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/failed/regenerate')?.body).toMatchObject({ hint: null })
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('cannot load: says so and offers Try again', async () => {
    setup(() => ({ status: 500 }))
    renderApp('/approve?tab=catch-ups')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load.')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
