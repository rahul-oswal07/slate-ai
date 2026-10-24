import { render, screen, within } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { DayStrip } from './DayStrip'
import { stepStates } from './stepStates'

const i18n = initI18n()

describe('stepStates', () => {
  it('makes the first step not done the current one', () => {
    expect(stepStates(false, false, false)).toEqual(['current', 'upcoming', 'upcoming'])
    expect(stepStates(true, false, false)).toEqual(['done', 'current', 'upcoming'])
    expect(stepStates(true, true, false)).toEqual(['done', 'done', 'current'])
    expect(stepStates(true, true, true)).toEqual(['done', 'done', 'done'])
  })
})

describe('DayStrip', () => {
  it('says each step and its state in words', async () => {
    await i18n.changeLanguage('en')
    render(
      <I18nextProvider i18n={i18n}>
        <DayStrip attendanceDone attendanceTime="12:00" lessonDone={false} tonightDone={false} />
      </I18nextProvider>,
    )
    const steps = within(screen.getByRole('list', { name: 'Today’s steps' })).getAllByRole('listitem')
    expect(steps.map((s) => s.textContent)).toEqual([
      'Attendance, done12:00',
      'Lesson, nowNext',
      'Tonight, later6 pm',
    ])
  })

  it('shows Done under the lesson once it is confirmed', async () => {
    await i18n.changeLanguage('en')
    render(
      <I18nextProvider i18n={i18n}>
        <DayStrip attendanceDone attendanceTime={null} lessonDone tonightDone={false} />
      </I18nextProvider>,
    )
    expect(screen.getByText('Done')).toBeInTheDocument()
  })
})
