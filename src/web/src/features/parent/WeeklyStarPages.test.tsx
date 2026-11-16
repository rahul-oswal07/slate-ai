import { readFileSync } from 'node:fs'
import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp } from '../../test/renderApp'
import type { ParentWeeklyStar } from './types'

const i18n = initI18n()
const token = 'StArStArStArStArStArSt'

function star(overrides: Partial<ParentWeeklyStar> = {}): ParentWeeklyStar {
  return {
    kind: 'weeklyStar',
    language: 'en',
    childName: 'Asha',
    teacherName: 'Shobha',
    weekOf: '2026-11-16',
    stopped: false,
    message: 'Asha came to school every day this week. Thank you for sending Asha to school every day.',
    badge: 'A star for Asha! For coming to school every day this week.',
    ...overrides,
  }
}

/** The weekly star page (T-125, D-118): the message, then the badge card. No voice, no numbers. */
describe('Weekly star page', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('screen 1 is the message she approved with one button to the badge; Back returns', async () => {
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: star() }) })
    const { container } = renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { level: 1, name: 'A star this week' })).toHaveFocus()
    expect(screen.getByText('Message from Shobha teacher')).toBeInTheDocument()
    expect(screen.getByText(/Asha came to school every day this week\./)).toHaveAttribute('lang', 'en')
    expect(screen.queryByRole('button', { name: /play/i })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Stop these messages/i })).toBeInTheDocument()
    expect(container.textContent).not.toMatch(/\d/)

    await userEvent.click(screen.getByRole('button', { name: 'Open to see your star badge' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Star badge' })).toHaveFocus()
    expect(screen.getByText('A star for Asha! For coming to school every day this week.')).toBeInTheDocument()
    const badge = screen.getByTestId('star-badge')
    expect(badge).toHaveAttribute('aria-hidden', 'true')
    // One burst of confetti that then rests (no infinite animation); tokens.css turns it off with reduced motion.
    expect(badge.querySelectorAll('.confetti-burst').length).toBeGreaterThan(0)
    expect(container.textContent).not.toMatch(/\d/)

    await userEvent.click(screen.getByRole('button', { name: 'Back' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'A star this week' })).toBeInTheDocument()
  })

  it('the burst plays once, about 2 seconds, and stays still after', () => {
    const css = readFileSync('src/styles/index.css', 'utf8')
    const rule = /\.confetti-burst\s*\{\s*animation:\s*confetti-burst\s+([\d.]+)s[^;]*;/.exec(css)
    expect(rule).not.toBeNull()
    expect(Number(rule?.[1])).toBeLessThanOrEqual(2)
    expect(rule?.[0]).not.toMatch(/infinite/)
    expect(readFileSync('src/styles/tokens.css', 'utf8')).toMatch(/prefers-reduced-motion: reduce[\s\S]*animation: none !important/)
  })

  it('in the family’s language', async () => {
    fakeApi({
      [`GET /api/p/${token}`]: () => ({
        status: 200,
        body: star({ language: 'kn', message: 'Asha ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ.' }),
      }),
    })
    renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { level: 1, name: 'ಈ ವಾರದ ನಕ್ಷತ್ರ' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /ನಿಮ್ಮ ನಕ್ಷತ್ರ ಬ್ಯಾಡ್ಜ್ ನೋಡಲು ತೆರೆಯಿರಿ/ })).toBeInTheDocument()
  })

  it('a family who stopped messages sees the stopped page, not the star', async () => {
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: star({ stopped: true, message: null, badge: null }) }) })
    const { container } = renderApp(`/p/${token}`)

    await screen.findByRole('heading', { level: 1 })
    expect(within(container).queryByText(/A star for/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Open to see your star badge' })).not.toBeInTheDocument()
  })
})
