import en from './locales/en.json'
import hi from './locales/hi.json'
import kn from './locales/kn.json'
import ta from './locales/ta.json'
import te from './locales/te.json'

type Tree = { [key: string]: string | Tree }

function flatten(tree: Tree, prefix = ''): Record<string, string> {
  return Object.entries(tree).reduce<Record<string, string>>((acc, [key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return typeof value === 'string' ? { ...acc, [path]: value } : { ...acc, ...flatten(value, path) }
  }, {})
}

const locales: Record<string, Tree> = { kn, en, te, ta, hi }
const reference = Object.keys(flatten(en)).sort()

describe('locale files', () => {
  it.each(Object.keys(locales))('%s has exactly the same keys as en', (code) => {
    expect(Object.keys(flatten(locales[code] ?? {})).sort()).toEqual(reference)
  })

  it.each(Object.keys(locales))('%s has no empty strings', (code) => {
    const empty = Object.entries(flatten(locales[code] ?? {})).filter(([, value]) => value.trim() === '')
    expect(empty).toEqual([])
  })
})

describe('language switching', () => {
  it('Hindi is a supported language and sets the page language', async () => {
    const { initI18n, SUPPORTED_LANGUAGES, isLanguage } = await import('./index')
    const i18n = initI18n()

    expect(SUPPORTED_LANGUAGES).toContain('hi')
    expect(isLanguage('hi')).toBe(true)
    await i18n.changeLanguage('hi')
    expect(document.documentElement.lang).toBe('hi')
    expect(i18n.t('language.hi')).toBe('हिन्दी')
    await i18n.changeLanguage('en')
  })
})
