import { render, screen } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { LevelTag } from './LevelTag'
import type { Level } from './useTodaysAnswers'

const i18n = initI18n()

describe('LevelTag (T-97)', () => {
  beforeAll(() => i18n.changeLanguage('en'))

  it.each([
    ['gotIt', 'Got it', 'level-got-it'],
    ['needsHelp', 'Needs help', 'level-needs-help'],
    ['notAnswered', 'Not answered yet', 'level-not-answered'],
  ] as [Level, string, string][])('%s: an icon, the word, and its own colour token', (level, word, token) => {
    render(
      <I18nextProvider i18n={i18n}>
        <LevelTag level={level} />
      </I18nextProvider>,
    )
    const tag = screen.getByText(word)
    // Never colour alone: the word is there and an icon with it.
    expect(tag.querySelector('svg')).not.toBeNull()
    expect(tag).toHaveClass(`text-${token}`, `bg-${token}-bg`, `border-${token}`)
  })

  it('puts a lesson name before the word for a lesson chip (T-98)', () => {
    render(
      <I18nextProvider i18n={i18n}>
        <LevelTag level="gotIt" label="Addition with carrying" />
      </I18nextProvider>,
    )
    expect(screen.getByText('Addition with carrying · Got it')).toBeInTheDocument()
  })
})
