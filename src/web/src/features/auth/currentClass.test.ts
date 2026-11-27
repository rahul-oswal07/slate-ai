import { teacher } from '../../test/renderApp'
import { chosenSchoolDate, setSchoolDate } from '../today/schoolDate'
import { istToday } from '../today/schoolTime'
import { classesOf, currentClass, selectClass, subscribeClass } from './currentClass'
import { saveCachedMe, type ClassInfo, type Me } from './session'

const classB: ClassInfo = { id: 'class-b', name: '3B', grade: 3, medium: 'kn', classTeacherName: 'Monika', demo: null }
const classA: ClassInfo = {
  id: teacher.classId,
  name: '3A',
  grade: 3,
  medium: 'kn',
  classTeacherName: 'Shobha',
  demo: { sampleVoiceNote: false, schoolDate: true },
}
// Monika (T-115): main teacher of 3B, her default, and a co-teacher of the demo class 3A.
const monika: Me = { ...teacher, teacherId: 'monika', displayName: 'Monika', classId: classB.id, classes: [classB, classA] }

function yesterday() {
  const date = new Date(`${istToday()}T12:00:00+05:30`)
  date.setUTCDate(date.getUTCDate() - 1)
  return istToday(date)
}

describe('currentClass (T-115)', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    setSchoolDate(null)
    localStorage.clear()
  })

  it('is her default class until she picks another, then that one', () => {
    expect(currentClass(monika)).toBe(classB)

    selectClass('monika', classA.id)

    expect(currentClass(monika)).toBe(classA)
  })

  it('keeps each teacher’s choice apart on a shared phone', () => {
    selectClass('monika', classA.id)

    expect(currentClass({ ...monika, teacherId: 'savitha' })).toBe(classB)
  })

  it('falls back to her default for a class she is no longer in', () => {
    selectClass('monika', 'a-class-she-left')

    expect(currentClass(monika)).toBe(classB)
  })

  it('reads a me cached before T-115 as its one class, and is null without a class', () => {
    const old: Me = { ...teacher, grade: 3, medium: 'kn', classTeacherName: 'Shobha' }

    expect(classesOf(old)).toEqual([
      { id: teacher.classId, name: '', grade: 3, medium: 'kn', classTeacherName: 'Shobha', demo: null },
    ])
    expect(currentClass(old)?.id).toBe(teacher.classId)
    expect(currentClass({ ...teacher, classId: null })).toBeNull()
    expect(currentClass(null)).toBeNull()
  })

  it('tells subscribers when she picks a class', () => {
    const listener = vi.fn<() => void>()
    const unsubscribe = subscribeClass(listener)

    selectClass('monika', classA.id)
    unsubscribe()
    selectClass('monika', classB.id)

    expect(listener).toHaveBeenCalledTimes(1)
  })

  it('acts as a past school day only while the demo class is open', () => {
    // T-134's switch belongs to the demo class: in her own 3B the app is on the real day and sends no X-School-Date.
    saveCachedMe(monika)
    selectClass('monika', classA.id)
    setSchoolDate(yesterday())
    expect(chosenSchoolDate()).toBe(yesterday())

    selectClass('monika', classB.id)
    expect(chosenSchoolDate()).toBeNull()

    selectClass('monika', classA.id)
    expect(chosenSchoolDate()).toBe(yesterday())
  })
})
