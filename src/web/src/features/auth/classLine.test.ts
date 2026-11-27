import { initI18n } from '../../i18n'
import type { ClassInfo } from './session'
import { classLine } from './classLine'

const i18n = initI18n()

const group: ClassInfo = { id: 'c', name: '', grade: 3, medium: 'kn', classTeacherName: null }

describe('classLine (T-135)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it("reads grade, medium and the main teacher's class", () => {
    expect(classLine({ ...group, grade: 3, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBe(
      "Grade 3 · ಕನ್ನಡ medium · Shobha's class",
    )
  })

  it('leaves out the teacher when not known, and is null without a grade or medium', () => {
    expect(classLine({ ...group, grade: 3, medium: 'kn' }, i18n.t)).toBe('Grade 3 · ಕನ್ನಡ medium')
    expect(classLine({ ...group, grade: null, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBeNull()
    expect(classLine(null, i18n.t)).toBeNull()
  })

  it('starts with the section when the class has one (T-115)', () => {
    expect(classLine({ ...group, name: '3B', classTeacherName: 'Monika' }, i18n.t)).toBe(
      "3B · Grade 3 · ಕನ್ನಡ medium · Monika's class",
    )
  })

  it('is in the UI language', async () => {
    await i18n.changeLanguage('kn')
    expect(classLine({ ...group, grade: 3, medium: 'kn', classTeacherName: 'Shobha' }, i18n.t)).toBe(
      '3ನೇ ತರಗತಿ · ಕನ್ನಡ ಮಾಧ್ಯಮ · Shobha ಅವರ ತರಗತಿ',
    )
  })
})
