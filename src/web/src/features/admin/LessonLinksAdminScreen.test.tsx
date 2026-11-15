import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { LessonLinksAdminScreen } from './LessonLinksAdminScreen'
import type { AdminLessonLink, AdminLessonLinks } from './useAdmin'

const i18n = initI18n()
const diksha = 'https://diksha.gov.in/play/content/do_1'

const lessons: AdminLessonLink[] = [
  { id: 'M3.BRIDGE.COMPARE', nameKn: 'ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ', nameEn: 'More, less, equal', textbookPage: { book: 'nalikali', part: 1, page: 4 }, url: null },
  { id: 'M3.BRIDGE.PLACE', nameKn: 'ಸ್ಥಾನಬೆಲೆ', nameEn: 'Place value', textbookPage: { book: 'nalikali', part: 1, page: 6 }, url: null },
  { id: 'M3.SUB.BORROW', nameKn: 'ಎರವಲು', nameEn: 'Subtraction with borrowing', textbookPage: { book: 'textbook', part: 2, page: 15 }, url: null },
]

/** The admin API (no key, D-112): GET lists the stored links; PUT stores one (blank removes it) or refuses non-https (400). */
function stubApi(initial: Record<string, string> = {}) {
  const stored = new Map(Object.entries(initial))
  const calls: { method: string; url: string; body: { url?: string } | null }[] = []
  const list = (): AdminLessonLinks => {
    const withUrl = lessons.map((lesson) => ({ ...lesson, url: stored.get(lesson.id) ?? null }))
    return {
      chapters: [
        { number: 0, nameKn: 'ಸೇತುಬಂಧ', nameEn: 'Bridge course', lessons: withUrl.slice(0, 2) },
        { number: 5, nameKn: 'ಕಳೆಯುವ ಮೋಜು', nameEn: 'The fun of subtracting', lessons: withUrl.slice(2) },
      ],
    }
  }
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const body = init?.body ? (JSON.parse(String(init.body)) as { url?: string }) : null
      calls.push({ method, url: String(input), body })
      if (method === 'PUT') {
        const id = decodeURIComponent(String(input).split('/').pop() ?? '')
        const sent = (body?.url ?? '').trim()
        if (sent !== '' && !sent.startsWith('https://')) {
          return Response.json({ errors: { url: ['Use a full https link.'] } }, { status: 400 })
        }
        if (sent === '') {
          stored.delete(id)
        } else {
          stored.set(id, sent)
        }
        return Response.json({ ...lessons.find((l) => l.id === id), url: sent || null })
      }
      return Response.json(list())
    }),
  )
  return { calls, stored }
}

function renderAt(url = '/admin/lesson-links') {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <LessonLinksAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const chapter = (name: RegExp) => screen.findByRole('button', { name })

describe('LessonLinksAdminScreen (T-94)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('chapters start folded, each with how many lessons have a link', async () => {
    stubApi({ 'M3.BRIDGE.COMPARE': diksha })
    renderAt()

    const bridge = await chapter(/Bridge course/)
    expect(bridge).toHaveAttribute('aria-expanded', 'false')
    expect(bridge).toHaveTextContent('1 of 2 linked')
    expect(await chapter(/Chapter 5 · The fun of subtracting/)).toHaveTextContent('0 of 1 linked')
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()

    await userEvent.click(bridge)

    expect(screen.getByRole('textbox', { name: /More, less, equal/ })).toHaveValue(diksha)
    expect(screen.getByRole('textbox', { name: /Place value/ })).toHaveValue('')
  })

  it('search opens the chapters with a match and hides the rest, by English or Kannada name', async () => {
    stubApi()
    renderAt()
    const search = await screen.findByRole('searchbox', { name: 'Search lessons' })
    expect(search).toHaveAttribute('placeholder', 'Lesson name or ID, e.g. compare')

    await userEvent.type(search, 'borrow')

    expect(screen.getByRole('textbox', { name: /Subtraction with borrowing/ })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Bridge course/ })).not.toBeInTheDocument()

    await userEvent.clear(search)
    await userEvent.type(search, 'ಸ್ಥಾನ')
    expect(screen.getByRole('textbox', { name: /Place value/ })).toBeInTheDocument()
    expect(screen.queryByRole('textbox', { name: /More, less, equal/ })).not.toBeInTheDocument()

    await userEvent.clear(search)
    await userEvent.type(search, 'nothing like this')
    expect(screen.getByText('No lessons match.')).toBeInTheDocument()
  })

  it('"Only lessons without a link" leaves out the linked ones', async () => {
    stubApi({ 'M3.BRIDGE.COMPARE': diksha })
    renderAt()

    await userEvent.click(await screen.findByRole('checkbox', { name: 'Only lessons without a link' }))

    expect(screen.queryByRole('textbox', { name: /More, less, equal/ })).not.toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /Place value/ })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /Subtraction with borrowing/ })).toBeInTheDocument()
  })

  it('links typed in several rows are saved together', async () => {
    const { calls, stored } = stubApi()
    renderAt()
    await userEvent.click(await chapter(/Bridge course/))
    await userEvent.click(screen.getByRole('button', { name: /Chapter 5/ }))

    await userEvent.type(screen.getByRole('textbox', { name: /More, less, equal/ }), diksha)
    await userEvent.type(screen.getByRole('textbox', { name: /Subtraction with borrowing/ }), 'https://www.youtube.com/watch?v=abc')
    expect(screen.getByText('2 unsaved changes')).toBeInTheDocument()
    expect(screen.getAllByText('Unsaved')).toHaveLength(2)

    await userEvent.click(screen.getByRole('button', { name: 'Save all' }))

    expect(await screen.findByText('Saved 2 links')).toBeInTheDocument()
    expect(calls.filter((c) => c.method === 'PUT').map((c) => c.url)).toEqual([
      '/api/admin/lesson-links/M3.BRIDGE.COMPARE',
      '/api/admin/lesson-links/M3.SUB.BORROW',
    ])
    expect(stored.get('M3.BRIDGE.COMPARE')).toBe(diksha)
    await waitFor(() => expect(screen.getByRole('button', { name: /Bridge course/ })).toHaveTextContent('1 of 2 linked'))
  })

  it('a refused link stays as typed with how to fix it; the others are saved', async () => {
    stubApi()
    renderAt()
    await userEvent.click(await chapter(/Bridge course/))
    const bad = screen.getByRole('textbox', { name: /More, less, equal/ })

    await userEvent.type(bad, 'http://diksha.gov.in/x')
    await userEvent.type(screen.getByRole('textbox', { name: /Place value/ }), diksha)
    await userEvent.click(screen.getByRole('button', { name: 'Save all' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Use a full https link')
    expect(bad).toHaveValue('http://diksha.gov.in/x')
    expect(bad).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('1 unsaved change')).toBeInTheDocument()
  })

  it('clearing a saved link removes it', async () => {
    const { calls, stored } = stubApi({ 'M3.BRIDGE.COMPARE': diksha })
    renderAt()
    await userEvent.click(await chapter(/Bridge course/))

    await userEvent.clear(screen.getByRole('textbox', { name: /More, less, equal/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Save all' }))

    expect(await screen.findByText('Saved 1 link')).toBeInTheDocument()
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ url: '' })
    expect(stored.has('M3.BRIDGE.COMPARE')).toBe(false)
  })

  it('Discard puts back what was saved', async () => {
    const { calls } = stubApi({ 'M3.BRIDGE.COMPARE': diksha })
    renderAt()
    await userEvent.click(await chapter(/Bridge course/))
    const input = screen.getByRole('textbox', { name: /More, less, equal/ })

    await userEvent.clear(input)
    await userEvent.type(input, 'https://other.example/x')
    await userEvent.click(screen.getByRole('button', { name: 'Discard' }))

    expect(input).toHaveValue(diksha)
    expect(screen.queryByRole('button', { name: 'Save all' })).not.toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT')).toBe(false)
  })
})
