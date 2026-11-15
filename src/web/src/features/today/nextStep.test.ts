import { todaysMessage } from '../approve/fixtures'
import { nextStep } from './nextStep'

const day = (overrides: Partial<Parameters<typeof nextStep>[0]> = {}) => ({
  lessonStatus: null,
  noteQueued: false,
  todays: null,
  answersSent: 0,
  ...overrides,
})

/** The one next step on Today (T-96): worked out by code, in the day's order. */
describe('nextStep', () => {
  it('no lesson yet, or a note that could not be heard: record it', () => {
    expect(nextStep(day())).toBe('record')
    expect(nextStep(day({ lessonStatus: 'unclear' }))).toBe('record')
  })

  it('a note still waiting to upload: nothing to do now', () => {
    expect(nextStep(day({ noteQueued: true }))).toBeNull()
  })

  it.each(['processing', 'mapped', 'low'] as const)('a note heard but not confirmed (%s): confirm it', (status) => {
    expect(nextStep(day({ lessonStatus: status }))).toBe('confirm')
  })

  it.each(['generating', 'ready', 'failed'] as const)('today’s message %s and not approved: check it', (status) => {
    expect(nextStep(day({ lessonStatus: 'confirmed', todays: todaysMessage({ status }) }))).toBe('check')
  })

  it('today’s message approved and waiting for 6 pm: nothing to do now', () => {
    expect(nextStep(day({ lessonStatus: 'confirmed', todays: todaysMessage({ status: 'approved' }) }))).toBeNull()
  })

  it('confirmed without a today’s message: nothing to do now', () => {
    expect(nextStep(day({ lessonStatus: 'confirmed' }))).toBeNull()
  })

  it('once families have it: see the answers, ahead of everything else', () => {
    const sent = todaysMessage({ status: 'approved', sent: true })
    expect(nextStep(day({ lessonStatus: 'confirmed', todays: sent, answersSent: 2 }))).toBe('answers')
  })

  it('a new note after confirming does not hide today’s message waiting for her', () => {
    expect(nextStep(day({ lessonStatus: 'mapped', todays: todaysMessage({ status: 'ready' }) }))).toBe('check')
  })
})
