import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { LessonLinksAdminScreen } from './LessonLinksAdminScreen'
import type { AdminLessonContent, AdminLessonLink, AdminLessonLinks } from './useAdmin'

const i18n = initI18n()

const lessons: AdminLessonLink[] = [
  { id: 'M3.BRIDGE.COMPARE', nameKn: 'ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ', nameEn: 'More, less, equal', textbookPage: { book: 'nalikali', part: 1, page: 4 }, url: null },
  { id: 'M3.BRIDGE.PLACE', nameKn: 'ಸ್ಥಾನಬೆಲೆ', nameEn: 'Place value', textbookPage: { book: 'nalikali', part: 1, page: 6 }, url: null },
]

const compare: AdminLessonContent = {
  description: 'Look at the bundles of ten first.',
  examples: ['52 and 25: 52 has 5 bundles of ten.', '63 and 67: look at the loose sticks.'],
  source: 'textbook',
}

/** GET lists the lessons with their content; PUT lesson-content stores it, refuses braces (400) and blank removes it (204). */
function stubApi(initial: Record<string, AdminLessonContent> = {}) {
  const stored = new Map(Object.entries(initial))
  const puts: { url: string; body: { description: string; examples: string[] } }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const url = String(input)
      if (init?.method === 'PUT' && url.includes('/lesson-content/')) {
        const body = JSON.parse(String(init.body)) as { description: string; examples: string[] }
        puts.push({ url, body })
        const id = decodeURIComponent(url.split('/').pop() ?? '')
        const examples = body.examples.map((e) => e.trim()).filter((e) => e !== '')
        if (body.description.includes('{')) {
          return Response.json({ errors: { description: ['No { or }.'] } }, { status: 400 })
        }
        if (body.description.trim() === '' && examples.length === 0) {
          stored.delete(id)
          return new Response(null, { status: 204 })
        }
        const saved: AdminLessonContent = { description: body.description.trim(), examples, source: 'edited' }
        stored.set(id, saved)
        return Response.json(saved)
      }
      const list: AdminLessonLinks = {
        chapters: [
          {
            number: 0,
            nameKn: 'ಸೇತುಬಂಧ',
            nameEn: 'Bridge course',
            lessons: lessons.map((lesson) => ({ ...lesson, content: stored.get(lesson.id) ?? null })),
          },
        ],
      }
      return Response.json(list)
    }),
  )
  return { puts, stored }
}

function renderScreen() {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/admin/lesson-links']}>
        <LessonLinksAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** Opens the chapter and returns the lesson's list item. */
async function lessonItem(name: string) {
  await userEvent.click(await screen.findByRole('button', { name: /Bridge course/ }))
  const item = screen.getByText(name).closest('li')
  if (!item) {
    throw new Error(`no list item for ${name}`)
  }
  return item
}

describe('LessonContentEditor (T-141)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('counts the described lessons and shows a lesson’s description, examples and source', async () => {
    stubApi({ 'M3.BRIDGE.COMPARE': compare })
    renderScreen()

    expect(await screen.findByText(/0 of 2 linked · 1 described/)).toBeInTheDocument()
    const item = await lessonItem('More, less, equal')
    await userEvent.click(within(item).getByText('About this lesson'))

    expect(within(item).getByText('From the textbook')).toBeInTheDocument()
    expect(within(item).getByText('Look at the bundles of ten first.')).toBeInTheDocument()
    expect(within(item).getAllByRole('listitem')).toHaveLength(2)
  })

  it('a lesson without one says so and adding one saves it as edited, one example per line', async () => {
    const api = stubApi()
    renderScreen()
    const item = await lessonItem('Place value')
    await userEvent.click(within(item).getByText('About this lesson'))
    expect(within(item).getByText(/No description yet/)).toBeInTheDocument()

    await userEvent.click(within(item).getByRole('button', { name: 'Add a description' }))
    await userEvent.type(within(item).getByLabelText(/^Description/), 'Hundreds, tens and ones.')
    await userEvent.type(within(item).getByLabelText(/^Worked examples/), '352 is 3 hundreds.{enter}{enter}5 tens.')
    await userEvent.click(within(item).getByRole('button', { name: 'Save' }))

    expect(await within(item).findByText('Saved')).toBeInTheDocument()
    expect(api.puts[0]?.url).toContain('/api/admin/lesson-content/M3.BRIDGE.PLACE')
    expect(api.puts[0]?.body.examples).toEqual(['352 is 3 hundreds.', '', '5 tens.'])
    expect(await within(item).findByText('Edited here')).toBeInTheDocument()
    expect(within(item).getByText('Hundreds, tens and ones.')).toBeInTheDocument()
  })

  it('refused text stays in the form and says how to fix it; Cancel puts the saved text back', async () => {
    stubApi({ 'M3.BRIDGE.COMPARE': compare })
    renderScreen()
    const item = await lessonItem('More, less, equal')
    await userEvent.click(within(item).getByText('About this lesson'))
    await userEvent.click(within(item).getByRole('button', { name: 'Edit' }))

    const description = within(item).getByLabelText(/^Description/)
    await userEvent.clear(description)
    await userEvent.type(description, 'Hello {{child}')
    await userEvent.click(within(item).getByRole('button', { name: 'Save' }))

    expect(await within(item).findByRole('alert')).toHaveTextContent(/without \{ or \}/)
    expect(description).toHaveAttribute('aria-invalid', 'true')

    await userEvent.click(within(item).getByRole('button', { name: 'Cancel' }))
    expect(within(item).getByText('Look at the bundles of ten first.')).toBeInTheDocument()
  })
})
