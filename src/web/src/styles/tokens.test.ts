/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { join } from 'node:path'

// Read from disk: Vitest blanks CSS imports, even with ?raw.
const tokensCss = readFileSync(join(import.meta.dirname, 'tokens.css'), 'utf8')

// D-17: all text ≥ 7:1 (WCAG 1.4.6 AAA), UI ≥ 3:1 (1.4.11), in both themes.
const TEXT = 7
const UI = 3

type Theme = Record<string, string>

function readTheme(block: string): Theme {
  const theme: Theme = {}
  for (const [, name, value] of block.matchAll(/--color-([a-z-]+):\s*(#[0-9a-f]{6})/gi)) {
    if (name && value) theme[name] = value
  }
  return theme
}

// The file has two colour blocks: light in @theme, dark in the media query (D-77: slate & chalk is the base look).
const darkStart = tokensCss.indexOf('@media (prefers-color-scheme: dark)')
const fontsStart = tokensCss.indexOf('/* Fonts by language')
if (darkStart < 0 || fontsStart < darkStart) {
  throw new Error('tokens.css: expected the light block, then the dark media query, then the font rules')
}
const darkBlock = readTheme(tokensCss.slice(darkStart, fontsStart))
const light = readTheme(tokensCss.slice(0, darkStart))
const dark = { ...light, ...darkBlock }

function luminance(hex: string): number {
  const channels = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255)
  const [r = 0, g = 0, b = 0] = channels.map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4))
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

function contrast(a: string, b: string): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x) as [number, number]
  return (hi + 0.05) / (lo + 0.05)
}

const backgrounds = ['canvas', 'surface', 'panel']
const statuses = ['sent', 'listened', 'caught-up', 'needs-buddy', 'no-phone', 'needs-ear', 'stopped', 'queued', 'error']

// [foreground, background, minimum]
const pairs: [string, string, number][] = [
  ...backgrounds.flatMap((bg): [string, string, number][] => [
    ['ink', bg, TEXT],
    ['ink-muted', bg, TEXT],
    ['primary-strong', bg, TEXT],
    ['error', bg, TEXT],
    ['border', bg, UI],
    ['focus', bg, UI],
    ['warning', bg, UI],
    ['stick', bg, UI],
  ]),
  // Rule 1: primary is not text on any background (5.9:1 on the slate canvas in light): primary-strong does that.
  ['on-primary', 'primary', TEXT],
  ['on-primary', 'primary-strong', TEXT],
  ['error', 'error-bg', TEXT],
  ['ink', 'error-bg', TEXT],
  ['on-error', 'error', TEXT],
  ...statuses.map((s): [string, string, number] => [`status-${s}`, `status-${s}-bg`, TEXT]),
]

function check(theme: Theme, list: [string, string, number][]) {
  it.each(list)('%s on %s ≥ %d:1', (fg, bg, min) => {
    const a = theme[fg]
    const b = theme[bg]
    expect(a, `--color-${fg} missing`).toBeDefined()
    expect(b, `--color-${bg} missing`).toBeDefined()
    expect(contrast(a ?? '', b ?? '')).toBeGreaterThanOrEqual(min)
  })
}

describe.each([
  ['light', light],
  ['dark', dark],
])('%s theme contrast', (_name, theme) => {
  check(theme, pairs)
})

// Amber (D-77): a hero action fill (play, mic), the tally and the star. Text or an icon on it needs the full ratio.
describe.each([
  ['light', light],
  ['dark', dark],
])('amber %s', (_name, theme) => {
  check(theme, [['on-accent', 'accent', TEXT]])
  // Rule 2: in light mode amber alone is 1.3:1 on the canvas, so a fill carries a 3px ink outline (ink on canvas).
  // In dark mode amber stands out on its own.
  it.each(backgrounds)('amber fill is visible on %s', (bg) => {
    const edge = _name === 'light' ? theme['ink'] : theme['accent']
    expect(contrast(edge ?? '', theme[bg] ?? '')).toBeGreaterThanOrEqual(UI)
  })
})

// The family phone's chat view (D-77): its own palette, the same in every scope.
describe.each([
  ['light', light],
  ['dark', dark],
])('chat palette %s contrast', (_name, theme) => {
  check(theme, [
    ['chat-bar-ink', 'chat-bar', TEXT],
    ['chat-text', 'chat-bubble', TEXT],
    ['chat-meta', 'chat-bubble', TEXT],
    ['chat-link', 'chat-bubble', TEXT],
    ['chat-meta', 'chat-pill', TEXT],
    ['chat-edge', 'chat-wall', UI],
  ])
})

it('dark theme overrides every colour except decorative ones and amber (the same in both)', () => {
  const overridden = Object.keys(darkBlock)
  const expected = Object.keys(light).filter((name) => !name.startsWith('illustration-') && name !== 'accent')
  expect(overridden.sort()).toEqual(expected.sort())
})

it('turns every animation and transition off under prefers-reduced-motion (D-77 rule 7)', () => {
  const rule = /@media \(prefers-reduced-motion: reduce\)\s*\{[^@]*\*[^{]*\{[^}]*animation:\s*none[^}]*transition:\s*none/
  expect(tokensCss).toMatch(rule)
})

describe('Hindi (D-78)', () => {
  const fontsCss = readFileSync(join(import.meta.dirname, 'fonts.css'), 'utf8')

  it('gets the larger Indic type scale', () => {
    expect(tokensCss).toMatch(/:where\(:lang\(kn\), :lang\(te\), :lang\(ta\), :lang\(hi\)\)/)
  })

  it('uses Noto Sans Devanagari for text and Baloo 2 (which ships a devanagari subset) for headings', () => {
    expect(tokensCss).toMatch(/:where\(:lang\(hi\)\)\s*\{[^}]*--font-sans:\s*"Noto Sans Devanagari"[^}]*--font-heading:\s*"Baloo 2"/)
  })

  it('loads the Devanagari body font in the weights the pages use', () => {
    expect(fontsCss).toContain('@fontsource/noto-sans-devanagari/400.css')
    expect(fontsCss).toContain('@fontsource/noto-sans-devanagari/600.css')
  })
})
