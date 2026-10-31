import { render, screen } from '@testing-library/react'
import { initI18n } from '../../i18n'
import { AttendanceCard } from './AttendanceCard'
import type { AttendanceDay } from './types'

const i18n = initI18n()
const note = 'Demo only: schools are usually shut today. The demo class has attendance every day so you can try it.'

const day = (overrides: Partial<AttendanceDay>): AttendanceDay => ({
  date: '2026-11-01',
  status: 'sats',
  receivedAt: '2026-11-01T12:00:00+05:30',
  corrected: false,
  satsDiffers: false,
  syncAt: null,
  retryAt: null,
  absentChildIds: ['c4'],
  children: [{ id: 'c4', rollNo: 4, firstName: 'Ravi' }],
  ...overrides,
})

describe('AttendanceCard demo day off (D-84)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it('says it is for the demo only on a day the school is shut', () => {
    render(<AttendanceCard day={day({ demoDayOff: true })} onEdit={() => {}} />)

    expect(screen.getByText(note)).toBeInTheDocument()
    expect(screen.getByText('Ravi')).toBeInTheDocument()
  })

  it('says it before the register is in too', () => {
    render(
      <AttendanceCard
        day={day({ status: 'waiting', syncAt: '12:00:00', retryAt: '14:00:00', absentChildIds: [], demoDayOff: true })}
        onEdit={() => {}}
      />,
    )

    expect(screen.getByText(note)).toBeInTheDocument()
  })

  it('says nothing on a school day or from an older offline copy', () => {
    const { rerender } = render(<AttendanceCard day={day({ demoDayOff: false })} onEdit={() => {}} />)
    expect(screen.queryByText(note)).not.toBeInTheDocument()

    rerender(<AttendanceCard day={day({})} onEdit={() => {}} />)
    expect(screen.queryByText(note)).not.toBeInTheDocument()
  })
})
