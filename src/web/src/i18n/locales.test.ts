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

  it.each(Object.keys(locales))('%s uses the same {{placeholders}} as en', (code) => {
    const placeholders = (value: string) => (value.match(/{{\w+}}/g) ?? []).sort().join(',')
    const english = flatten(en)
    const mismatched = Object.entries(flatten(locales[code] ?? {}))
      .filter(([key, value]) => placeholders(value) !== placeholders(english[key] ?? ''))
      .map(([key]) => key)
    expect(mismatched).toEqual([])
  })

  // Language names and strings that are only placeholders and numbers read the same in every language.
  const sameAsEnglish = [
    'demo.ai.prompt',
    'language.en',
    'language.hi',
    'language.kn',
    'language.ta',
    'language.te',
    'mic.timer',
    'mic.timerGreeting',
    'missed.lessonMeta',
    'today.attendance.tile',
  ]

  it.each(['te', 'ta', 'hi'])('%s has no English left outside names and numbers', (code) => {
    const english = flatten(en)
    const untranslated = Object.entries(flatten(locales[code] ?? {}))
      .filter(([key, value]) => value === english[key] && !sameAsEnglish.includes(key))
      .map(([key]) => key)
    expect(untranslated).toEqual([])
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

  // A key used in the code but missing from en shows as the raw key on screen (demo.reset.bodyVideo, T-83).
  // Catches literal keys anywhere in an expression, e.g. t(video ? 'a.b' : 'a.c').
  it('has every key the code uses', () => {
    const sources = import.meta.glob<string>(['../**/*.{ts,tsx}', '!../**/*.test.{ts,tsx}'], {
      query: '?raw',
      import: 'default',
      eager: true,
    })
    const english = flatten(en)
    const sections = new Set(Object.keys(en))
    const known = (key: string) =>
      key in english || `${key}_one` in english || `${key}_other` in english ||
      Object.keys(english).some((k) => k.startsWith(`${key}.`))
    const missing = Object.entries(sources).flatMap(([file, text]) =>
      [...text.matchAll(/['"`]([a-z][A-Za-z]*(?:\.[A-Za-z0-9_]+)+)['"`]/g)]
        .map((m) => m[1] ?? '')
        .filter((key) => sections.has(key.split('.')[0] ?? '') && !known(key))
        .map((key) => `${file}: ${key}`),
    )
    expect(missing).toEqual([])
  })
})
