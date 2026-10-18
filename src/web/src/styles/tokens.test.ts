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

const darkStart = tokensCss.indexOf('@media (prefers-color-scheme: dark)')
const light = readTheme(tokensCss.slice(0, darkStart))
const dark = { ...light, ...readTheme(tokensCss.slice(darkStart)) }

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
  ]),
  // Rule 1: primary text only on canvas and surface, never on panel.
  ['primary', 'canvas', TEXT],
  ['primary', 'surface', TEXT],
  ['on-primary', 'primary', TEXT],
  ['on-primary', 'primary-strong', TEXT],
  ['error', 'error-bg', TEXT],
  ['ink', 'error-bg', TEXT],
  ['on-error', 'error', TEXT],
  ...statuses.map((s): [string, string, number] => [`status-${s}`, `status-${s}-bg`, TEXT]),
]

describe.each([
  ['light', light],
  ['dark', dark],
])('%s theme contrast', (_name, theme) => {
  it.each(pairs)('%s on %s ≥ %d:1', (fg, bg, min) => {
    const a = theme[fg]
    const b = theme[bg]
    expect(a, `--color-${fg} missing`).toBeDefined()
    expect(b, `--color-${bg} missing`).toBeDefined()
    expect(contrast(a ?? '', b ?? '')).toBeGreaterThanOrEqual(min)
  })
})

it('dark theme overrides every colour except decorative ones', () => {
  const overridden = Object.keys(readTheme(tokensCss.slice(darkStart)))
  const expected = Object.keys(light).filter((name) => !name.startsWith('illustration-'))
  expect(overridden.sort()).toEqual(expected.sort())
})
