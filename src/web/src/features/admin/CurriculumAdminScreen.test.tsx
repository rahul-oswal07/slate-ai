import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { CurriculumAdminScreen } from './CurriculumAdminScreen'
import type { Curricula, TextbookImport } from './useCurricula'

const i18n = initI18n()

const base: TextbookImport = {
  id: 'i1',
  title: 'KTBS Grade 3 maths, Part 1',
  grade: 3,
  subject: 'maths',
  medium: 'kn',
  book: 'textbook',
  part: 1,
  pageCount: 120,
  status: 'ready',
  stage: null,
  stepsDone: 30,
  stepsTotal: 30,
  error: null,
  notes: [],
  curriculumId: 'c1',
  lessons: 31,
  flagged: 4,
}

const curricula: Curricula = {
  curricula: [
    { id: 'c1', title: 'KTBS Grade 3 maths, Part 1', grade: 3, subject: 'maths', medium: 'kn', status: 'published', lessons: 31, needsReview: 0, flagged: 4, classes: 0 },
  ],
  classes: [{ id: 'k1', label: 'Shobha · Grade 3', curriculumId: null }],
  builtInLessons: 61,
}

/** The admin API: imports, curricula and classes; uploads, retries and class changes as the test says. */
function stubApi({ imports = [base], assign = 200 }: { imports?: TextbookImport[]; assign?: number } = {}) {
  const calls: { method: string; url: string; body: BodyInit | null | undefined }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const url = String(input)
      calls.push({ method, url, body: init?.body })
      if (url.endsWith('/api/admin/curriculum-imports') && method === 'GET') {
        return Response.json(imports)
      }
      if (url.endsWith('/api/admin/curriculum-imports') && method === 'POST') {
        return Response.json({ ...base, id: 'i2', status: 'queued', curriculumId: null }, { status: 202 })
      }
      if (url.includes('/retry')) {
        return Response.json({ ...imports[0], status: 'queued' })
      }
      if (url.endsWith('/api/admin/curricula')) {
        return Response.json(curricula)
      }
      if (url.includes('/api/admin/classes/')) {
        return assign === 409
          ? Response.json({ missing: ['M3.ADD.CARRY', 'M3.SUB.BORROW'] }, { status: 409 })
          : Response.json({ id: 'k1', label: 'Shobha · Grade 3', curriculumId: 'c1' })
      }
      return new Response(null, { status: 404 })
    }),
  )
  return calls
}

function renderScreen() {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/admin/curriculum']}>
        <CurriculumAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CurriculumAdminScreen (T-146, T-147)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows each import with its progress and a link to check a ready draft', async () => {
    stubApi({
      imports: [
        base,
        { ...base, id: 'i2', title: 'Part 2', status: 'running', stage: 'lessons', stepsDone: 7, stepsTotal: 25, curriculumId: null, lessons: null, flagged: null },
        { ...base, id: 'i3', title: 'Part 3', status: 'failed', error: 'error:Unavailable reading chapter 3', curriculumId: null, lessons: null, flagged: null },
      ],
    })
    renderScreen()

    expect(await screen.findByText('Ready to check: 31 lessons, 4 with something to look at.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Check the lessons' })).toHaveAttribute('href', '/admin/curriculum/c1')
    expect(screen.getByText('Reading: step 7 of 25.')).toBeInTheDocument()
    expect(screen.getByText(/Stopped \(error:Unavailable reading chapter 3\)/)).toBeInTheDocument()
    expect(screen.getAllByText('120 pages')).toHaveLength(3)
  })

  it('a failed import can be tried again, and discarding asks first', async () => {
    const calls = stubApi({ imports: [{ ...base, status: 'failed', error: 'error:stale', curriculumId: null }] })
    renderScreen()

    await userEvent.click(await screen.findByRole('button', { name: 'Try again' }))
    await userEvent.click(screen.getByRole('button', { name: 'Discard' }))

    await waitFor(() => expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/i1/retry'))).toBe(true))
    expect(screen.getByRole('button', { name: 'Yes, discard the book and its draft' })).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'DELETE')).toBe(false)
  })

  it('uploads the book with its details as a form, and asks for the PDF first', async () => {
    const calls = stubApi({ imports: [] })
    renderScreen()

    await userEvent.type(await screen.findByLabelText('Name of the book'), 'Grade 3 maths')
    await userEvent.click(screen.getByRole('button', { name: 'Upload and read' }))
    expect(await screen.findByRole('alert')).toHaveTextContent("Choose the book's PDF.")

    const pdf = new File(['%PDF-1.7'], 'book.pdf', { type: 'application/pdf' })
    await userEvent.upload(screen.getByLabelText('The book as a PDF (up to 100 MB)'), pdf)
    await userEvent.click(screen.getByRole('button', { name: 'Upload and read' }))

    await waitFor(() => expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/curriculum-imports'))).toBe(true))
    const form = calls.find((c) => c.method === 'POST')?.body as FormData
    expect(form.get('title')).toBe('Grade 3 maths')
    expect(form.get('grade')).toBe('3')
    expect(form.get('medium')).toBe('kn')
    expect((form.get('pdf') as File).name).toBe('book.pdf')
  })

  it('a class moves to a published curriculum, and a move that would lose taught lessons says which', async () => {
    const calls = stubApi({ assign: 409 })
    renderScreen()

    const select = await screen.findByRole('combobox', { name: 'Shobha · Grade 3' })
    expect(within(select).getByRole('option', { name: 'Built in (61 lessons)' })).toBeInTheDocument()
    await userEvent.selectOptions(select, 'c1')

    expect(await screen.findByRole('alert')).toHaveTextContent('M3.ADD.CARRY, M3.SUB.BORROW')
    const put = calls.find((c) => c.method === 'PUT')
    expect(JSON.parse(String(put?.body))).toMatchObject({ curriculumId: 'c1' })
  })
})
