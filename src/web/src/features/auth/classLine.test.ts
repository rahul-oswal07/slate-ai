import { initI18n } from '../../i18n'
import { teacher } from '../../test/renderApp'
import { classLine } from './classLine'

const i18n = initI18n()

describe('classLine (T-135)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it("reads grade, medium and the main teacher's class", () => {
    expect(classLine({ ...teacher, grade: 3, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBe(
      "Grade 3 · ಕನ್ನಡ medium · Shobha's class",
    )
  })

  it('leaves out the teacher when not known, and is null without a grade or medium', () => {
    expect(classLine({ ...teacher, grade: 3, medium: 'kn' }, i18n.t)).toBe('Grade 3 · ಕನ್ನಡ medium')
    expect(classLine({ ...teacher, grade: null, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBeNull()
    expect(classLine(null, i18n.t)).toBeNull()
  })

  it('is in the UI language', async () => {
    await i18n.changeLanguage('kn')
    expect(classLine({ ...teacher, grade: 3, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBe(
      '3ನೇ ತರಗತಿ · ಕನ್ನಡ ಮಾಧ್ಯಮ · Shobha ಅವರ ತರಗತಿ',
    )
  })
})
