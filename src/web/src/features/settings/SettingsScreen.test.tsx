import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import type { MessageTemplate } from '../evening/types'

const i18n = initI18n()
const templateUrl = `GET /api/classes/${teacher.classId}/message-template`
const withGreeting = { ...teacher, greeting: { url: '/api/audio/abc', seconds: 8.4 } }

const template = (status: 'draft' | 'approved'): MessageTemplate => ({
  version: '3',
  status,
  approvedAt: status === 'approved' ? '2026-10-22T10:00:00Z' : null,
  languages: [],
})

function setup(me: object, status: 'draft' | 'approved' = 'approved') {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: me }),
    [templateUrl]: () => ({ status: 200, body: template(status) }),
  })
}

const card = async (name: string) => (await screen.findByRole('heading', { level: 2, name })).closest('section') as HTMLElement

describe('SettingsScreen (T-71)', () => {
  const played: string[] = []

  beforeEach(async () => {
    localStorage.clear()
    played.length = 0
    await i18n.changeLanguage('en')
    vi.stubGlobal(
      'Audio',
      class {
        onended: (() => void) | null = null
        constructor(src: string) {
          played.push(src)
        }
        play = () => Promise.resolve()
        pause = () => {}
      },
    )
  })

  afterEach(() => vi.unstubAllGlobals())

  it('plays her greeting and offers to record it again', async () => {
    setup(withGreeting)
    renderApp('/settings')

    expect(await screen.findByRole('heading', { level: 1, name: 'Settings' })).toHaveFocus()
    const greeting = await card('My greeting')
    expect(within(greeting).getByText(/^Recorded · 8 s/)).toBeInTheDocument()
    expect(within(greeting).getByRole('link', { name: 'Record again' })).toHaveAttribute('href', '/settings/greeting')

    await userEvent.click(within(greeting).getByRole('button', { name: 'Play my greeting' }))

    expect(played).toEqual(['/api/audio/abc'])
    expect(within(greeting).getByRole('button', { name: 'Play my greeting' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('without a greeting it says so and offers to record one', async () => {
    setup(teacher)
    renderApp('/settings')

    const greeting = await card('My greeting')
    expect(within(greeting).getByText(/^Not recorded yet/)).toBeInTheDocument()
    expect(within(greeting).queryByRole('button')).not.toBeInTheDocument()
    expect(within(greeting).getByRole('link', { name: 'Record my greeting' })).toHaveAttribute('href', '/settings/greeting')
  })

  it('shows the evening message as approved with its date, and links to review it', async () => {
    setup(withGreeting, 'approved')
    renderApp('/settings')

    const evening = await card('Evening message')
    expect(await within(evening).findByText('Approved · Oct 22')).toBeInTheDocument()
    expect(within(evening).getByRole('link', { name: 'Review the message' })).toHaveAttribute('href', '/settings/evening')
  })

  it('says when the evening message still needs her approval', async () => {
    setup(withGreeting, 'draft')
    renderApp('/settings')

    const evening = await card('Evening message')
    expect(
      await within(evening).findByText('Needs your approval. No evening messages go until you approve it.'),
    ).toBeInTheDocument()
  })

  it('keeps Today the active tab and leads back to it', async () => {
    setup(withGreeting)
    renderApp('/settings')

    await screen.findByRole('heading', { level: 1, name: 'Settings' })
    expect(screen.getByRole('link', { name: '‹ Today' })).toHaveAttribute('href', '/')
    const nav = screen.getByRole('navigation', { name: 'Main' })
    expect(within(nav).getByRole('link', { current: 'page' })).toHaveAttribute('href', '/')
  })
})
