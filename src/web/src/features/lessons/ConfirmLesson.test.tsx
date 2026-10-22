import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { installFakeMedia } from '../../test/fakeMedia'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from '../today/schoolTime'
import type { Curriculum, LessonDay, LessonStatus } from './types'

const i18n = initI18n()
const date = istToday()
const dayUrl = `/api/classes/${teacher.classId}/lessons/${date}`

const lesson = (id: string, nameEn: string, sequence: number, page: number) => ({
  id, nameKn: `ಕ ${id}`, nameEn, sequence, textbookPage: { book: 'textbook' as const, part: 2, page },
})
const curriculum: Curriculum = {
  chapters: [{
    number: 5, nameKn: 'ಕಳೆಯುವ ಮೋಜು', nameEn: 'The fun of subtracting',
    competencies: [
      lesson('M3.SUB.NOBORROW', 'Subtraction without borrowing', 13, 6),
      lesson('M3.SUB.CHECK', 'Check subtraction by adding', 14, 4),
      lesson('M3.SUB.BORROW', 'Subtraction with borrowing', 15, 15),
      lesson('M3.SUB.WORD', 'Subtraction word problems', 16, 18),
    ],
  }],
}

const voice = (status: LessonStatus, ids: string[] = []): LessonDay => ({
  date,
  lesson: { competencyIds: ids, source: 'voice', status, summaryKn: 'ಕಳೆಯುವಿಕೆ', summaryEn: 'Subtraction, checked by adding', examplesUsed: ['865 - 432'] },
  nextInBook: 'M3.SUB.BORROW',
})

function api(day: () => LessonDay, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET ${dayUrl}`]: () => ({ status: 200, body: day() }),
    [`GET /api/classes/${teacher.classId}/attendance/${date}`]: () => ({ status: 200, body: { date, status: 'waiting', receivedAt: null, corrected: false, satsDiffers: false, syncAt: '12:00:00', retryAt: '14:00:00', absentChildIds: [], children: [] } }),
    ...extra,
  })
}

describe('T2 Lesson confirm', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.notes.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows processing, then the matched lessons all ticked once A1 is done', async () => {
    let state = voice('processing')
    api(() => state)
    renderApp('/lesson')

    expect(await screen.findByText('Matching your note to the textbook…')).toBeInTheDocument()
    state = voice('mapped', ['M3.SUB.NOBORROW', 'M3.SUB.CHECK'])

    const group = await screen.findByRole('group', { name: 'Today’s lessons' }, { timeout: 4000 })
    expect(within(group).getAllByRole('checkbox').every((box) => (box as HTMLInputElement).checked)).toBe(true)
    expect(screen.getByRole('button', { name: 'Confirm 2 lessons' })).toBeEnabled()
    expect(screen.getByText('AI-assisted')).toBeInTheDocument()
    expect(screen.getByText('Subtraction, checked by adding')).toBeInTheDocument()
    expect(screen.queryByText(/0\.\d/)).not.toBeInTheDocument()
  })

  it('unticking updates the button; with none ticked it is disabled and says why', async () => {
    api(() => voice('mapped', ['M3.SUB.NOBORROW', 'M3.SUB.CHECK']))
    renderApp('/lesson')

    await userEvent.click(await screen.findByRole('checkbox', { name: /Check subtraction by adding/ }))
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeEnabled()

    await userEvent.click(screen.getByRole('checkbox', { name: /Subtraction without borrowing/ }))
    const confirm = screen.getByRole('button', { name: 'Confirm' })
    expect(confirm).toBeDisabled()
    expect(confirm).toHaveAccessibleDescription('Keep at least one lesson')
  })

  it('Confirm sends only the ticked lessons as a voice lesson and returns to Today', async () => {
    let saved: unknown
    api(() => voice('mapped', ['M3.SUB.NOBORROW', 'M3.SUB.CHECK']), {
      [`PUT ${dayUrl}`]: ({ body }) => {
        saved = body
        return { status: 200, body: voice('confirmed', ['M3.SUB.NOBORROW']) }
      },
    })
    renderApp('/lesson')

    await userEvent.click(await screen.findByRole('checkbox', { name: /Check subtraction by adding/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }))

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
    await waitFor(() => expect(saved).toMatchObject({ competencyIds: ['M3.SUB.NOBORROW'], source: 'voice' }))
  })

  it('"Add another lesson" ticks more, up to 3', async () => {
    api(() => voice('mapped', ['M3.SUB.NOBORROW', 'M3.SUB.CHECK']))
    renderApp('/lesson')

    await userEvent.click(await screen.findByRole('button', { name: 'Add another lesson' }))
    const sheet = screen.getByRole('dialog')
    expect(within(sheet).getByRole('checkbox', { name: /Subtraction without borrowing/ })).toBeChecked()
    await userEvent.click(within(sheet).getByRole('checkbox', { name: /Subtraction with borrowing/ }))

    expect(within(sheet).getByText('Up to 3 lessons')).toBeInTheDocument()
    expect(within(sheet).getByRole('checkbox', { name: /word problems/ })).toBeDisabled()
    await userEvent.click(within(sheet).getByRole('button', { name: 'Done' }))

    expect(screen.getByRole('button', { name: 'Confirm 3 lessons' })).toBeInTheDocument()
  })

  it('low confidence: two choices, one tap confirms', async () => {
    let saved: unknown
    api(() => voice('low', ['M3.SUB.BORROW', 'M3.SUB.WORD', 'M3.SUB.CHECK']), {
      [`PUT ${dayUrl}`]: ({ body }) => {
        saved = body
        return { status: 200, body: voice('confirmed', ['M3.SUB.WORD']) }
      },
    })
    renderApp('/lesson')

    expect(await screen.findByText('Looks like one of these. Please choose.')).toBeInTheDocument()
    expect(screen.getAllByRole('radio')).toHaveLength(2)
    await userEvent.click(screen.getByRole('radio', { name: /word problems/ }))

    await waitFor(() => expect(saved).toMatchObject({ competencyIds: ['M3.SUB.WORD'], source: 'voice' }))
  })

  it('could not hear: says so and offers the list and saying it again', async () => {
    api(() => voice('unclear'))
    renderApp('/lesson')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not hear the note clearly.')
    await userEvent.click(screen.getByRole('button', { name: 'Pick from the list' }))
    expect(screen.getByRole('dialog', { name: 'Chapter 5 · The fun of subtracting' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Say it again' })).toHaveAttribute('href', '/')
  })

  it('Today shows the mic, and "Note saved · Open" while a note is matched', async () => {
    installFakeMedia()
    api(() => voice('processing'))
    renderApp('/')

    expect(await screen.findByText('Note saved')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Open' })).toHaveAttribute('href', '/lesson')
  })

  it('after a note could not be heard, Today offers the mic again ("Say it again")', async () => {
    installFakeMedia()
    api(() => voice('unclear'))
    renderApp('/')

    // Wait until the day's lesson has loaded (the pick button is enabled only then).
    const pick = await screen.findByRole('button', { name: 'Or pick the lesson from the list' })
    await waitFor(() => expect(pick).toBeEnabled())
    expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument()
    expect(screen.queryByText('Note saved')).not.toBeInTheDocument()
  })

  it('Today with a blocked mic offers the list instead', async () => {
    installFakeMedia({ allow: false })
    api(() => ({ date, lesson: null, nextInBook: 'M3.SUB.BORROW' }))
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Record' }))

    expect(await screen.findByText(/The microphone is off for this app/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pick from the list' })).toBeInTheDocument()
  })
})
