import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from '../today/schoolTime'
import type { Curriculum } from '../lessons/types'
import { todaysMessage as message } from './fixtures'

const i18n = initI18n()
const base = `/api/classes/${teacher.classId}/todays-message/${istToday()}`

const curriculum: Curriculum = {
  chapters: [
    {
      number: 0,
      nameKn: 'ಸೇತುಬಂಧ',
      nameEn: 'Bridge course',
      competencies: [
        {
          id: 'M3.BRIDGE.COMPARE',
          nameKn: 'ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ',
          nameEn: 'More, less, equal',
          sequence: 0,
          textbookPage: { book: 'nalikali', part: 1, page: 4 },
        },
      ],
    },
  ],
}

function setup(todays: FakeRoute, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: istToday(), packages: [] } }),
    [`GET ${base}`]: todays,
    ...extra,
  })
}

describe("Approve → Today's message (T-89)", () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it("opens on Today's message, with Catch-ups as the second tab", async () => {
    const user = userEvent.setup()
    setup(() => ({ status: 200, body: message() }))
    renderApp('/approve')

    const today = await screen.findByRole('tab', { name: "Today's message" })
    expect(today).toHaveAttribute('aria-selected', 'true')
    await user.click(screen.getByRole('tab', { name: 'Catch-ups' }))

    expect(screen.getByRole('tab', { name: 'Catch-ups' })).toHaveAttribute('aria-selected', 'true')
    expect(await screen.findByText('No catch-ups yet this week.')).toBeInTheDocument()
  })

  it('without a message yet: says when it is made and links to Today', async () => {
    setup(() => ({ status: 204 }))
    renderApp('/approve')

    expect(await screen.findByText(/made when you confirm today's lesson/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to Today' })).toHaveAttribute('href', '/')
  })

  it('a 404 is an error, never "no message yet"', async () => {
    setup(() => ({ status: 404 }))
    renderApp('/approve')

    expect(await screen.findByRole('button', { name: 'Try again' })).toBeInTheDocument()
    expect(screen.queryByText(/made when you confirm today's lesson/)).not.toBeInTheDocument()
  })

  it('while it is written: says so and shows no approve button', async () => {
    setup(() => ({ status: 200, body: message({ status: 'generating', version: 0, languages: [] }) }))
    renderApp('/approve')

    expect(await screen.findByText("Writing today's message…")).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve and send' })).not.toBeInTheDocument()
  })

  it('failed: says the plain message goes, and Try again writes a new draft', async () => {
    const user = userEvent.setup()
    const calls = setup(() => ({ status: 200, body: message({ status: 'failed', languages: [] }) }), {
      [`POST ${base}/generate`]: () => ({ status: 202, body: message({ status: 'generating', languages: [] }) }),
    })
    renderApp('/approve')

    expect(await screen.findByRole('alert')).toHaveTextContent('plain evening message')
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(calls.map((c) => c.key)).toContain(`POST ${base}/generate`)
  })

  it('ready: the book and page, one section open at a time, and who else gets it', async () => {
    const user = userEvent.setup()
    setup(() => ({ status: 200, body: message() }))
    renderApp('/approve')

    expect(await screen.findByRole('heading', { name: 'More, less, equal' })).toBeInTheDocument()
    expect(screen.getByText('Nalikali Maths · Part 1 · p 4')).toBeInTheDocument()
    const concept = screen.getByRole('button', { name: 'Concept' })
    const activity = screen.getByRole('button', { name: 'Activity at home' })
    expect(concept).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ.')).toBeInTheDocument()

    await user.click(activity)

    expect(activity).toHaveAttribute('aria-expanded', 'true')
    expect(concept).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByText('75 ಮತ್ತು 57 ಕ್ಕೆ ಕಟ್ಟು ಮಾಡಿ.')).toBeInTheDocument()
    expect(screen.getByText('Also goes in Hindi to 1 family')).toBeInTheDocument()
  })

  it('Approve needs the tick, sends the version she read and takes her back to Today (T-108)', async () => {
    const user = userEvent.setup()
    let current = message()
    const calls = setup(() => ({ status: 200, body: current }), {
      [`POST ${base}/approve`]: () => {
        current = message({ status: 'approved', approvedVersion: 1 })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve')

    const approve = await screen.findByRole('button', { name: 'Approve and send' })
    expect(approve).toBeDisabled()
    await user.click(screen.getByRole('checkbox', { name: "I've checked this" }))
    await user.click(approve)

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
    await waitFor(() =>
      expect(screen.getByTestId('announcer')).toHaveTextContent('Approved. Goes with the evening message after 6 pm.'),
    )
    expect(calls.find((c) => c.key === `POST ${base}/approve`)?.body).toMatchObject({ version: 1 })
  })

  it('a refused Approve (it changed meanwhile) keeps her on Approve with the latest', async () => {
    const user = userEvent.setup()
    setup(() => ({ status: 200, body: message() }), {
      [`POST ${base}/approve`]: () => ({ status: 409, body: message({ version: 2 }) }),
    })
    renderApp('/approve')

    await user.click(await screen.findByRole('checkbox', { name: "I've checked this" }))
    await user.click(screen.getByRole('button', { name: 'Approve and send' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed meanwhile')
    expect(screen.getByRole('heading', { level: 1, name: 'Check and approve' })).toBeInTheDocument()
    expect(screen.getByText(/Version 2/)).toBeInTheDocument()
  })

  it('approved: read only when she opens it again', async () => {
    setup(() => ({ status: 200, body: message({ status: 'approved', approvedVersion: 1 }) }))
    renderApp('/approve')

    expect(await screen.findByText('Approved. Goes with the evening message after 6 pm.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve and send' })).not.toBeInTheDocument()
  })

  it('removing a section makes a new version, so her tick is gone', async () => {
    const user = userEvent.setup()
    let current = message()
    const calls = setup(() => ({ status: 200, body: current }), {
      [`PATCH ${base}/sections/concept`]: () => {
        current = message({ version: 2, removed: ['concept'] })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve')

    await user.click(await screen.findByRole('checkbox', { name: "I've checked this" }))
    await user.click(screen.getByRole('button', { name: 'Remove' }))

    expect(await screen.findByText('Removed: not sent')).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: "I've checked this" })).not.toBeChecked()
    expect(screen.getByRole('button', { name: 'Approve and send' })).toBeDisabled()
    expect(calls.find((c) => c.key === `PATCH ${base}/sections/concept`)?.body).toMatchObject({ removed: true })
  })

  it('her edit is saved with the version she edited, then the new version is written', async () => {
    const user = userEvent.setup()
    let current = message()
    const calls = setup(() => ({ status: 200, body: current }), {
      [`PUT ${base}/sections/concept`]: () => {
        current = message({ status: 'generating', edited: true })
        return { status: 202, body: current }
      },
    })
    renderApp('/approve')

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const box = screen.getByRole('textbox', { name: 'Concept (Kannada)' })
    await user.clear(box)
    await user.type(box, 'ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Writing the new version from your Kannada…')).toBeInTheDocument()
    expect(calls.find((c) => c.key === `PUT ${base}/sections/concept`)?.body).toMatchObject({
      version: 1,
      text: 'ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.',
    })
  })

  it("a refused edit says why and which of the book's numbers she can use; nothing changes", async () => {
    const user = userEvent.setup()
    setup(() => ({ status: 200, body: message() }), {
      [`PUT ${base}/sections/concept`]: () => ({
        status: 400,
        body: { errors: { problem: ['numbers'], detail: ['kn: numbers not allowed: 73'], allowed: ['4', '33', '48', '75'] } },
      }),
    })
    renderApp('/approve')

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    await user.type(screen.getByRole('textbox', { name: 'Concept (Kannada)' }), ' 73')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("Use the book's numbers only. You can use: 33, 48, 75.")
    expect(screen.getByRole('textbox', { name: 'Concept (Kannada)' })).toHaveAccessibleDescription(/book's numbers/)
  })

  it('her note: Add my note shows the placeholder', async () => {
    const user = userEvent.setup()
    setup(() => ({ status: 200, body: message() }))
    renderApp('/approve')

    await user.click(await screen.findByRole('button', { name: 'My note' }))
    await user.click(screen.getByRole('button', { name: 'Add my note' }))

    expect(screen.getByRole('textbox', { name: 'Your note to families (Kannada)' })).toHaveAttribute(
      'placeholder',
      'e.g. We wrote numbers on leaves and compared them',
    )
  })

  it("the picture question: she picks another pair from the book's list, never types numbers", async () => {
    const user = userEvent.setup()
    let current = message()
    const calls = setup(() => ({ status: 200, body: current }), {
      [`PUT ${base}/picture`]: () => {
        current = message({ version: 2, picture: { kind: 'compare', a: 86, b: 86, answer: '=', removed: false }, pictureIndex: 3 })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve')

    await user.click(await screen.findByRole('button', { name: 'Picture question' }))
    expect(screen.getByText(/48 ☐ 33/)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Choose another pair' }))
    const pairs = screen.getByRole('group', { name: "Pairs from the book's page" })
    expect(within(pairs).getByRole('button', { name: '48 and 33' })).toHaveAttribute('aria-pressed', 'true')
    await user.click(within(pairs).getByRole('button', { name: '86 and 86' }))

    await waitFor(() => expect(screen.getByText(/86 ☐ 86/)).toBeInTheDocument())
    expect(calls.find((c) => c.key === `PUT ${base}/picture`)?.body).toMatchObject({ version: 1, index: 3 })
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('sent: read only', async () => {
    setup(() => ({ status: 200, body: message({ status: 'approved', approvedVersion: 1, sent: true }) }))
    renderApp('/approve')

    expect(await screen.findByText('Sent to the families.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Remove' })).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })
})
