import { act, render, screen } from '@testing-library/react'
import { AnnouncerProvider } from './Announcer'
import { useAnnounce } from './useAnnounce'

function Say({ message }: { message: string }) {
  const announce = useAnnounce()
  return (
    <button type="button" onClick={() => announce(message)}>
      say
    </button>
  )
}

describe('AnnouncerProvider', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => vi.useRealTimers())

  it('puts the message in the polite live region a moment later', () => {
    render(
      <AnnouncerProvider>
        <Say message="Attendance saved" />
      </AnnouncerProvider>,
    )

    act(() => screen.getByRole('button', { name: 'say' }).click())
    expect(screen.getByTestId('announcer')).toBeEmptyDOMElement()
    act(() => vi.advanceTimersByTime(50))

    expect(screen.getByTestId('announcer')).toHaveTextContent('Attendance saved')
  })

  it('leaves no timer behind when the app goes away before the message is shown', () => {
    const { unmount } = render(
      <AnnouncerProvider>
        <Say message="Greeting saved" />
      </AnnouncerProvider>,
    )

    act(() => screen.getByRole('button', { name: 'say' }).click())
    unmount()

    expect(vi.getTimerCount()).toBe(0)
  })
})
