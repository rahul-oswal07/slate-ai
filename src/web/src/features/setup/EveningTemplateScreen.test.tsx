import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import type { MessageTemplate } from '../evening/types'

const i18n = initI18n()
const templateUrl = `/api/classes/${teacher.classId}/message-template`

const parts = (child: string, lesson: string, page: string, words: [string, string, string, string]) => [
  { text: words[0], slot: null },
  { text: child, slot: 'child' },
  { text: words[1], slot: null },
  { text: lesson, slot: 'lesson' },
  { text: words[2], slot: null },
  { text: page, slot: 'page' },
  { text: words[3], slot: null },
]

function template(status: 'draft' | 'approved' = 'draft'): MessageTemplate {
  return {
    version: '1',
    status,
    approvedAt: status === 'approved' ? '2026-10-22T10:00:00Z' : null,
    languages: [
      {
        language: 'en',
        example: parts('Ravi', 'subtraction with borrowing', 'Part 2, page 15', [
          'We missed ',
          ' today. In class we learned ',
          '. Homework: textbook ',
          '.',
        ]),
      },
      {
        language: 'te',
        example: parts('Ravi', 'పది తీసుకుని తీసివేత', 'భాగం 2, పేజీ 15', ['ఈ రోజు ', ' … ', ' … ', '.']),
      },
    ],
  } as MessageTemplate
}

function setup(routes: Record<string, FakeRoute>) {
  return fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }), ...routes })
}

describe('EveningTemplateScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows the message filled with an example, slots marked, and the Telugu version', async () => {
    setup({ [`GET ${templateUrl}`]: () => ({ status: 200, body: template() }) })
    renderApp('/setup/evening')

    expect(await screen.findByRole('heading', { level: 1, name: 'The evening message on a day of absence' })).toHaveFocus()
    expect(screen.getByText('Step 2 of 2')).toBeInTheDocument()
    expect(screen.getByText(/No AI writes this message/)).toBeInTheDocument()
    expect(await screen.findByText('subtraction with borrowing')).toHaveClass('underline')
    expect(screen.queryByText(/\{child\}/)).not.toBeInTheDocument()
    const telugu = screen.getByRole('heading', { name: 'Telugu version' }).closest('div')
    expect(telugu).not.toBeNull()
    expect(within(telugu as HTMLElement).getByText('పది తీసుకుని తీసివేత')).toBeInTheDocument()
  })

  it('Approve sends the version once and opens Today', async () => {
    const calls = setup({
      [`GET ${templateUrl}`]: () => ({ status: 200, body: template() }),
      [`POST ${templateUrl}/approve`]: () => ({ status: 200, body: template('approved') }),
    })
    renderApp('/setup/evening')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve this message' }))

    expect(await screen.findByText(/You approved this message/)).toBeInTheDocument()
    const approve = calls.filter((c) => c.key === `POST ${templateUrl}/approve`)
    expect(approve).toHaveLength(1)
    expect(approve[0]?.body).toMatchObject({ version: '1' })
    await userEvent.click(screen.getByRole('button', { name: 'Go to Today' }))
    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })

  it('Not now approves nothing and opens Today', async () => {
    const calls = setup({ [`GET ${templateUrl}`]: () => ({ status: 200, body: template() }) })
    renderApp('/setup/evening')

    await userEvent.click(await screen.findByRole('button', { name: 'Not now · you can change it in Settings' }))

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
    expect(calls.some((c) => c.key.startsWith('POST'))).toBe(false)
  })

  it('offline: says nothing was approved', async () => {
    setup({
      [`GET ${templateUrl}`]: () => ({ status: 200, body: template() }),
      [`POST ${templateUrl}/approve`]: () => 'offline',
    })
    renderApp('/setup/evening')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve this message' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No network. Nothing was approved.')
  })

  it('409: the wording changed, so it shows the latest', async () => {
    let loads = 0
    setup({
      [`GET ${templateUrl}`]: () => ({ status: 200, body: { ...template(), version: ++loads === 1 ? '1' : '2' } }),
      [`POST ${templateUrl}/approve`]: () => ({ status: 409, body: { ...template(), version: '2' } }),
    })
    renderApp('/setup/evening')

    await userEvent.click(await screen.findByRole('button', { name: 'Approve this message' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed on another device. Showing the latest.')
    expect(loads).toBeGreaterThan(1)
  })
})
