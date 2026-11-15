import { screen } from '@testing-library/react'
import { initI18n } from '../i18n'
import { fakeApi, renderApp, teacher } from '../test/renderApp'

const i18n = initI18n()

const signedIn = { 'GET /api/me': () => ({ status: 200, body: teacher }) }

/** The teacher header (T-109, T-114): one row, fixed at the top, the tagline under the name. */
describe('AppShell header (T-114)', () => {
  beforeEach(() => localStorage.clear())

  afterEach(async () => {
    vi.unstubAllGlobals()
    await i18n.changeLanguage('en')
  })

  it('stays at the top while she scrolls, on the page background', async () => {
    await i18n.changeLanguage('en')
    fakeApi(signedIn)
    renderApp('/')

    const header = (await screen.findByText('Tappida Paatha')).closest('header')

    expect(header).toHaveClass('sticky', 'top-0', 'bg-canvas')
  })

  it.each([
    ['en', 'The missed lesson'],
    ['te', 'తప్పిన పాఠం'],
    ['ta', 'தவறிய பாடம்'],
    ['hi', 'छूटा हुआ पाठ'],
  ])('%s: the tagline has no "at home"', async (language, tagline) => {
    await i18n.changeLanguage(language)
    fakeApi(signedIn)
    renderApp('/')

    expect(await screen.findByText(tagline)).toBeInTheDocument()
  })

  it('kn: no tagline, because the name already says it', async () => {
    await i18n.changeLanguage('kn')
    fakeApi(signedIn)
    renderApp('/')

    const header = (await screen.findByText('ತಪ್ಪಿದ ಪಾಠ')).closest('header')

    expect(header?.querySelectorAll('p')).toHaveLength(1)
    expect(header).not.toHaveTextContent('ಮನೆಯಲ್ಲೇ')
  })
})
