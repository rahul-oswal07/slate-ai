import { initI18n } from '../../i18n'
import { pageText } from './lessonText'

const i18n = initI18n()

describe('pageText, where the child finds the lesson', () => {
  it('names Nalikali for a lesson re-pointed to it (T-87, D-90)', async () => {
    await i18n.changeLanguage('en')
    expect(pageText(i18n.t, { book: 'nalikali', part: 1, page: 4 })).toBe('Nalikali Maths · Part 1 · p 4')

    await i18n.changeLanguage('kn')
    expect(pageText(i18n.t, { book: 'nalikali', part: 1, page: 4 })).toBe('ನಲಿ-ಕಲಿ ಗಣಿತ · ಭಾಗ 1 · ಪುಟ 4')
  })

  it('keeps the plain page for the other lessons', async () => {
    await i18n.changeLanguage('en')
    expect(pageText(i18n.t, { book: 'textbook', part: 1, page: 52 })).toBe('Part 1, page 52')
    expect(pageText(i18n.t, { book: 'textbook', part: null, page: 4 })).toBe('Page 4')
    expect(pageText(i18n.t, { book: 'workbook', part: 1, page: 3 })).toBe('Workbook page 3')
    expect(pageText(i18n.t, null)).toBe('')
  })
})
