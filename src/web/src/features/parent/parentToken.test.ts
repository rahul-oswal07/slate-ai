import { cleanToken } from './parentToken'

describe('cleanToken (T-104)', () => {
  it.each([
    ['AbCdEfGh', 'AbCdEfGh'],
    ['{{1}}AbCdEfGh', 'AbCdEfGh'],
    ['%7B%7B1%7D%7DAbCdEfGh', 'AbCdEfGh'],
    ['%7b%7b1%7d%7dAbCdEfGh', 'AbCdEfGh'],
    ['{{1}}{{1}}AbCdEfGh', 'AbCdEfGh'],
    ['Ab{{1}}Cd', 'Ab{{1}}Cd'],
    ['', ''],
  ])('%s → %s', (raw, clean) => {
    expect(cleanToken(raw)).toBe(clean)
  })
})
