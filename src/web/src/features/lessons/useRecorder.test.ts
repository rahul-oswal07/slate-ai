import { act, renderHook } from '@testing-library/react'
import { installFakeMedia } from '../../test/fakeMedia'
import { useRecorder } from './useRecorder'

async function setup(allow = true) {
  installFakeMedia({ allow })
  const onRecorded = vi.fn<(note: Blob) => void>()
  const hook = renderHook(() => useRecorder(onRecorded))
  return { ...hook, onRecorded }
}

const tick = async (ms: number) => {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms)
  })
}

describe('useRecorder (D-31)', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval', 'performance'] })
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('tap to start, tap to stop: hands over the note', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => {
      result.current.buttonHandlers.onPointerDown()
      result.current.buttonHandlers.onPointerUp()
      result.current.buttonHandlers.onClick()
    })
    expect(result.current.status).toBe('recording')

    await tick(5000)
    expect(result.current.elapsedMs).toBeGreaterThanOrEqual(4750)
    act(() => {
      result.current.buttonHandlers.onPointerDown()
      result.current.buttonHandlers.onPointerUp()
      result.current.buttonHandlers.onClick()
    })

    expect(result.current.status).toBe('idle')
    expect(onRecorded).toHaveBeenCalledOnce()
    expect(onRecorded.mock.calls[0]?.[0]).toBeInstanceOf(Blob)
  })

  it('press and hold: releasing stops it', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => result.current.buttonHandlers.onPointerDown())
    await tick(2000)
    act(() => {
      result.current.buttonHandlers.onPointerUp()
      result.current.buttonHandlers.onClick()
    })

    expect(onRecorded).toHaveBeenCalledOnce()
    expect(result.current.status).toBe('idle')
  })

  it('Enter/Space (a click without pointer events) toggles', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => result.current.buttonHandlers.onClick())
    await tick(1500)
    act(() => result.current.buttonHandlers.onClick())

    expect(onRecorded).toHaveBeenCalledOnce()
  })

  it('under 1 second is too short and nothing is kept', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => result.current.buttonHandlers.onClick())
    await tick(400)
    act(() => result.current.buttonHandlers.onClick())

    expect(result.current.status).toBe('tooShort')
    expect(onRecorded).not.toHaveBeenCalled()
  })

  it('stops by itself at 30 seconds', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => result.current.buttonHandlers.onClick())
    await tick(30_000)

    expect(result.current.status).toBe('idle')
    expect(onRecorded).toHaveBeenCalledOnce()
  })

  it('a gesture cancel or switching apps throws the recording away', async () => {
    const { result, onRecorded } = await setup()

    await act(async () => result.current.buttonHandlers.onClick())
    await tick(3000)
    act(() => result.current.buttonHandlers.onPointerCancel())

    expect(result.current.status).toBe('cancelled')

    await act(async () => result.current.buttonHandlers.onClick())
    await tick(3000)
    act(() => {
      Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' })
      document.dispatchEvent(new Event('visibilitychange'))
    })
    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' })

    expect(result.current.status).toBe('cancelled')
    expect(onRecorded).not.toHaveBeenCalled()
  })

  it('a blocked microphone is reported', async () => {
    const { result } = await setup(false)

    await act(async () => result.current.buttonHandlers.onClick())

    expect(result.current.status).toBe('blocked')
  })
})
