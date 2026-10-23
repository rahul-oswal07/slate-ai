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
    renderApp('/approve')

    expect(await screen.findByRole('heading', { level: 1, name: 'This week’s catch-ups' })).toHaveFocus()
    const telugu = await screen.findByRole('link', { name: /^In Telugu · 2 children\. Ravi, Mahesh\./ })
    expect(telugu).toHaveAttribute('href', '/approve/pkg-te')
    expect(await within(telugu).findByText('3-digit subtraction with borrowing · 1:29')).toBeInTheDocument()
    expect(within(telugu).getByText('Ready')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /^In Kannada · 1 child\./ })).toHaveTextContent('Needs review')
  })

  it('the Approve tab counts catch-ups waiting for her, not generating or failed ones', async () => {
    setup(week(pkg(), pkg({ id: 'b', status: 'needsReview' }), pkg({ id: 'c', status: 'generating' }), pkg({ id: 'd', status: 'failed' })))
    renderApp('/approve')

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
    renderApp('/approve')

    await userEvent.click(await screen.findByRole('button', { name: 'Make this week’s catch-ups' }))

    expect(await screen.findByRole('link', { name: /Generating…/ })).toBeInTheDocument()
    expect(calls.filter((c) => c.key.startsWith('POST')).length).toBe(1)
  })

  it('all approved: says how many families get them after 6 pm (counted by the API)', async () => {
    setup(week(pkg({ status: 'approved', familiesReachable: 3 }), pkg({ id: 'b', status: 'sent', familiesSent: 2 })))
    renderApp('/approve')

    expect(await screen.findByText('All 2 approved. 5 families get them after 6 pm.')).toBeInTheDocument()
  })

  it('cannot load: says so and offers Try again', async () => {
    setup(() => ({ status: 500 }))
    renderApp('/approve')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load.')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
