/** A stand-in MediaRecorder + getUserMedia for jsdom, driven by the test's (fake) clock. */
export class FakeMediaRecorder {
  static isTypeSupported = (type: string) => type.startsWith('audio/webm')
  state: 'inactive' | 'recording' = 'inactive'
  mimeType = 'audio/webm'
  ondataavailable: ((event: { data: Blob }) => void) | null = null
  onstop: (() => void) | null = null

  start() {
    this.state = 'recording'
  }

  stop() {
    this.state = 'inactive'
    this.ondataavailable?.({ data: new Blob(['voice'], { type: 'audio/webm' }) })
    this.onstop?.()
  }
}

/** Installs the fakes; `allow: false` makes getUserMedia reject like a blocked mic. */
export function installFakeMedia({ allow = true }: { allow?: boolean } = {}) {
  vi.stubGlobal('MediaRecorder', FakeMediaRecorder)
  const stream = { getTracks: () => [{ stop: () => {} }] }
  Object.defineProperty(navigator, 'mediaDevices', {
    configurable: true,
    value: {
      getUserMedia: () =>
        allow ? Promise.resolve(stream) : Promise.reject(new DOMException('denied', 'NotAllowedError')),
    },
  })
}
