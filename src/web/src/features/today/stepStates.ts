export type StepState = 'done' | 'current' | 'upcoming'

/** Which of the day's three steps is where (T-49): the first one not done is the current one. */
export function stepStates(attendanceDone: boolean, lessonDone: boolean, tonightDone: boolean): StepState[] {
  const done = [attendanceDone, lessonDone, tonightDone]
  const current = done.indexOf(false)
  return done.map((d, i) => (d ? 'done' : i === current ? 'current' : 'upcoming'))
}
