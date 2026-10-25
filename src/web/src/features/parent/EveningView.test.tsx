import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp } from '../../test/renderApp'
import type { ParentEvening } from './types'

const i18n = initI18n()
const token = 'ZyXwVuTsRqPoNmLkJiHgFe'

function evening(overrides: Partial<ParentEvening> = {}): ParentEvening {
  return {
    kind: 'evening',
    language: 'en',
    childName: 'Ravi',
    teacherName: 'Shobha',
    text: 'We missed Ravi today.',
    greeting: { url: `/api/p/${token}/greeting`, seconds: 8.4 },
    stopped: false,
    reason: null,
    ...overrides,
  }
}

describe('Evening message page: the teacher’s greeting', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows her voice first, with her name and length, and plays the token URL', async () => {
    const urls: string[] = []
    vi.stubGlobal(
      'Audio',
      class {
        onended: (() => void) | null = null
        constructor(url: string) {
          urls.push(url)
        }
        play = () => Promise.resolve()
        pause = () => {}
      },
    )
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening() }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByText('Shobha teacher’s voice · 0:08')).toBeInTheDocument()
    const play = screen.getByRole('button', { name: 'Play the greeting' })
    await userEvent.click(play)

    expect(urls).toEqual([`/api/p/${token}/greeting`])
    expect(play).toHaveAttribute('aria-pressed', 'true')
    await userEvent.click(play)
    expect(play).toHaveAttribute('aria-pressed', 'false')
  })

  it('has no voice row when she has not recorded one', async () => {
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening({ greeting: null }) }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByText('We missed Ravi today.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Play the greeting' })).not.toBeInTheDocument()
  })
})
