import { describe, expect, it } from 'vitest'
import { checkQuestion, sumQuestion } from './sumQuestion'

describe('sumQuestion', () => {
  it.each([
    ['28 + 15', '28 + 15 = ?'],
    ['52 − 27', '52 − 27 = ?'],
    ['28 + 15 = ?', '28 + 15 = ?'],
    ['28 + 15?', '28 + 15 = ?'],
    ['28 + 15? = ?', '28 + 15 = ?'],
    ['28 + 15 =', '28 + 15 = ?'],
    ['  119 + 210  ', '119 + 210 = ?'],
  ])('writes "%s" with one "="', (question, shown) => {
    expect(sumQuestion(question)).toBe(shown)
    expect(sumQuestion(question).match(/=/g)).toHaveLength(1)
  })
})

describe('checkQuestion', () => {
  it('asks a sum as a sum', () => {
    expect(checkQuestion('43 − 18')).toBe('43 − 18 = ?')
    expect(checkQuestion('103 + 463 = ?')).toBe('103 + 463 = ?')
  })

  it('leaves a number or a place-value question as it is', () => {
    expect(checkQuestion('347')).toBe('347')
    expect(checkQuestion('4 in 347')).toBe('4 in 347')
  })
})
