import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { CurriculumReviewScreen } from './CurriculumReviewScreen'
import type { AdminCurriculumLesson, CurriculumView } from './useCurricula'

const i18n = initI18n()

const lesson = (id: string, sequence: number, reviewed: boolean, extra: Partial<AdminCurriculumLesson> = {}): AdminCurriculumLesson => ({
  lesson: {
    id,
    nameKn: 'ಕೂಡು',
    nameEn: id === 'M3.NUM.TENS' ? 'Tens and ones' : 'Addition with carrying',
    prerequisites: [],
    chapter: 1,
    sequence,
    examples: [],
    textbookPage: { book: 'textbook', part: 1, page: 30 + sequence },
    exercises: id === 'M3.ADD.CARRY' ? { add: [[248, 438]] } : null,
  },
  answers: { compare: [], extremes: [], add: id === 'M3.ADD.CARRY' ? [686] : [], sub: [] },
  extras: { pages: [7], printedPages: [34], description: 'Adds with a carry.', examples: ['36 + 47 is 83.'], otherExercises: [], pictures: [{ page: 7, printedPage: 34, position: 'top', description: 'Bundles of ten sticks', teaches: 'Tens' }] },
  reviewStatus: reviewed ? 'reviewed' : 'needs-review',
  flags: id === 'M3.ADD.CARRY' ? ['Exercise left out (add 600, 500): exercises.add: each sum must be 999 or less.'] : [],
  ...extra,
})

const view = (reviewed: boolean): CurriculumView => ({
  summary: { id: 'c1', title: 'KTBS Grade 3 maths', grade: 3, subject: 'maths', medium: 'kn', status: 'draft', lessons: 2, needsReview: reviewed ? 0 : 1, flagged: 1, classes: 0 },
  book: 'textbook',
  part: 1,
  notes: ['Chapter numbers were missing or out of order in the book as read, so they were numbered in order.'],
  chapters: [{ number: 1, nameKn: 'ಕೂಡುವುದು', nameEn: 'Addition' }],
  lessons: [lesson('M3.NUM.TENS', 1, true), lesson('M3.ADD.CARRY', 2, reviewed)],
  publishProblems: reviewed ? [] : ['M3.ADD.CARRY: not reviewed yet.'],
})

function stubApi() {
  let reviewed = false
  let published = false
  const calls: { method: string; url: string; body: unknown }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const url = String(input)
      const body = init?.body ? JSON.parse(String(init.body)) : null
      calls.push({ method, url, body })
      if (method === 'PUT') {
        if ((body as { prerequisites: string[] }).prerequisites.includes('M3.NOPE')) {
          return Response.json({ errors: { prerequisites: ['Only lessons that come earlier in this curriculum.'] } }, { status: 400 })
        }
        reviewed = (body as { reviewed: boolean }).reviewed
        return Response.json(view(reviewed))
      }
      const current = () => (published ? { ...view(true), summary: { ...view(true).summary, status: 'published' } } : view(reviewed))
      if (method === 'POST') {
        published = true
      }
      return Response.json(current())
    }),
  )
  return calls
}

function renderScreen() {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/admin/curriculum/c1']}>
        <Routes>
          <Route path="/admin/curriculum/:id" element={<CurriculumReviewScreen />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CurriculumReviewScreen (T-147)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows the book notes, each lesson with its review state and notes, and why it can not be published yet', async () => {
    stubApi()
    renderScreen()

    expect(await screen.findByRole('heading', { name: 'KTBS Grade 3 maths' })).toBeInTheDocument()
    expect(screen.getByText(/Chapter numbers were missing/)).toBeInTheDocument()
    expect(screen.getByText('Draft: 2 lessons, 1 still to check.')).toBeInTheDocument()
    expect(screen.getByText('M3.ADD.CARRY: not reviewed yet.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Publish' })).toBeDisabled()
    expect(screen.getByText('1 note')).toBeInTheDocument()
    expect(screen.getByText('To check')).toBeInTheDocument()
  })

  it('a lesson is edited with code’s answers beside its exercises, marked checked, and then the draft publishes', async () => {
    const calls = stubApi()
    renderScreen()

    const name = await screen.findByText('Addition with carrying')
    await userEvent.click(name)
    const item = name.closest('li')
    if (!item) {
      throw new Error('The lesson is not in a list item.')
    }
    const row = within(item)
    expect(row.getByText('Answers: 686')).toBeInTheDocument()
    expect(row.getByDisplayValue('Bundles of ten sticks')).toBeInTheDocument()
    await userEvent.click(row.getByRole('checkbox', { name: 'Tens and ones' }))
    await userEvent.click(row.getByRole('checkbox', { name: 'Checked: ready to publish' }))
    await userEvent.click(row.getByRole('button', { name: 'Save' }))

    expect(await row.findByText('Saved.')).toBeInTheDocument()
    const put = calls.find((c) => c.method === 'PUT')
    expect(put?.url).toContain('/api/admin/curricula/c1/lessons/M3.ADD.CARRY')
    expect(put?.body).toMatchObject({ prerequisites: ['M3.NUM.TENS'], reviewed: true, exercises: { add: [[248, 438]] } })

    await userEvent.click(await screen.findByRole('button', { name: 'Publish' }))
    await waitFor(() => expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/publish'))).toBe(true))
    expect(await screen.findByText('Published: 2 lessons, followed by 0 classes.')).toBeInTheDocument()
  })
})
