import { useCallback, useEffect, useRef, useState } from 'react'

export type RecorderStatus = 'idle' | 'recording' | 'tooShort' | 'cancelled' | 'blocked'

export const MAX_MS = 30_000
export const MIN_MS = 1_000

/** Long enough to count as press-and-hold rather than a tap (D-31: both work). */
const HOLD_MS = 600

const TYPES = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4', 'audio/ogg']

function supportedType() {
  return TYPES.find((type) => typeof MediaRecorder !== 'undefined' && MediaRecorder.isTypeSupported(type)) ?? ''
}

/**
 * The teacher's mic (FR-02, D-31, T1 card): tap to start and tap to stop, or press and hold; auto-stop at 30 s;
 * under 1 s is "too short" and nothing is kept; a call, gesture cancel or app switch cancels; a blocked mic
 * (D-34) is reported so the card can offer the list instead. `onRecorded` gets the finished note. `maxMs` shortens
 * the limit (the greeting is 10 s).
 */
export function useRecorder(onRecorded: (note: Blob) => void, { maxMs = MAX_MS }: { maxMs?: number } = {}) {
  const [status, setStatus] = useState<RecorderStatus>('idle')
  const [elapsedMs, setElapsedMs] = useState(0)
  const recorder = useRef<MediaRecorder | null>(null)
  const startedAt = useRef(0)
  const pressedAt = useRef(0)
  const ignoreClick = useRef(false)
  const cancelled = useRef(false)
  const timers = useRef<number[]>([])
  const onRecordedRef = useRef(onRecorded)

  useEffect(() => {
    onRecordedRef.current = onRecorded
  })

  const clearTimers = () => {
    timers.current.forEach((id) => window.clearInterval(id))
    timers.current = []
  }

  const stop = useCallback(() => {
    if (recorder.current?.state === 'recording') {
      recorder.current.stop()
    }
  }, [])

  const cancel = useCallback(() => {
    if (recorder.current?.state === 'recording') {
      cancelled.current = true
      recorder.current.stop()
    }
  }, [])

  const start = useCallback(async () => {
    if (recorder.current?.state === 'recording') {
      return
    }
    let stream: MediaStream
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    } catch {
      // NotAllowedError (denied), or no microphone at all: either way, offer the list.
      setStatus('blocked')
      return
    }

    const type = supportedType()
    const media = new MediaRecorder(stream, type ? { mimeType: type } : undefined)
    const chunks: Blob[] = []
    cancelled.current = false
    media.ondataavailable = (event) => chunks.push(event.data)
    media.onstop = () => {
      clearTimers()
      stream.getTracks().forEach((track) => track.stop())
      const duration = performance.now() - startedAt.current
      if (cancelled.current) {
        setStatus('cancelled')
      } else if (duration < MIN_MS) {
        setStatus('tooShort')
      } else {
        setStatus('idle')
        onRecordedRef.current(new Blob(chunks, { type: media.mimeType || type || 'audio/webm' }))
      }
      setElapsedMs(0)
      recorder.current = null
    }

    recorder.current = media
    startedAt.current = performance.now()
    media.start()
    setStatus('recording')
    setElapsedMs(0)
    timers.current.push(
      window.setInterval(() => setElapsedMs(performance.now() - startedAt.current), 250),
      window.setTimeout(stop, maxMs),
    )
  }, [stop, maxMs])

  // A blocked mic is known before she taps when the browser can tell us.
  useEffect(() => {
    void navigator.permissions
      ?.query({ name: 'microphone' as PermissionName })
      .then((permission) => {
        if (permission.state === 'denied') {
          setStatus('blocked')
        }
      })
      .catch(() => {})
  }, [])

  // Switching apps (or a call) cancels a recording; nothing is kept.
  useEffect(() => {
    const onHidden = () => {
      if (document.visibilityState === 'hidden') {
        cancel()
      }
    }
    document.addEventListener('visibilitychange', onHidden)
    return () => {
      document.removeEventListener('visibilitychange', onHidden)
      cancel()
      clearTimers()
    }
  }, [cancel])

  /** Pointer and click handlers for the mic button: tap/tap, press-and-hold, and Enter/Space (click). */
  const buttonHandlers = {
    onPointerDown: () => {
      if (recorder.current?.state !== 'recording') {
        pressedAt.current = performance.now()
        ignoreClick.current = true
        void start()
      } else {
        ignoreClick.current = false
      }
    },
    onPointerUp: () => {
      if (recorder.current?.state === 'recording' && performance.now() - pressedAt.current > HOLD_MS && ignoreClick.current) {
        stop() // released after a hold
      }
    },
    onPointerCancel: cancel,
    onClick: () => {
      if (ignoreClick.current) {
        ignoreClick.current = false
        return
      }
      if (recorder.current?.state === 'recording') {
        stop()
      } else {
        void start()
      }
    },
  }

  return { status, elapsedMs, start, stop, cancel, buttonHandlers, reset: () => setStatus('idle') }
}
