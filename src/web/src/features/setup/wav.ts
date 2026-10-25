/** What the API expects for a greeting (Features/Greeting): 16 kHz, mono, 16-bit PCM in a WAV file. */
export const GREETING_SAMPLE_RATE = 16_000

/** Writes mono samples in [-1, 1] as a 16-bit PCM WAV file. */
export function encodeWav(samples: Float32Array, sampleRate: number): Blob {
  const buffer = new ArrayBuffer(44 + samples.length * 2)
  const view = new DataView(buffer)
  const text = (offset: number, value: string) => {
    for (let i = 0; i < value.length; i++) {
      view.setUint8(offset + i, value.charCodeAt(i))
    }
  }
  text(0, 'RIFF')
  view.setUint32(4, 36 + samples.length * 2, true)
  text(8, 'WAVE')
  text(12, 'fmt ')
  view.setUint32(16, 16, true) // PCM header size
  view.setUint16(20, 1, true) // PCM
  view.setUint16(22, 1, true) // mono
  view.setUint32(24, sampleRate, true)
  view.setUint32(28, sampleRate * 2, true) // bytes per second
  view.setUint16(32, 2, true) // bytes per frame
  view.setUint16(34, 16, true) // bits per sample
  text(36, 'data')
  view.setUint32(40, samples.length * 2, true)
  samples.forEach((sample, i) => {
    const clamped = Math.max(-1, Math.min(1, sample))
    view.setInt16(44 + i * 2, clamped < 0 ? clamped * 0x8000 : clamped * 0x7fff, true)
  })
  return new Blob([buffer], { type: 'audio/wav' })
}

/**
 * Turns what MediaRecorder produced (webm/opus or mp4, depending on the phone) into the WAV the API takes: the browser
 * decodes it, an OfflineAudioContext resamples and mixes it to mono at 16 kHz. The server has no ffmpeg, so this is
 * done here (about 320 KB for ten seconds).
 */
export async function recordingToWav(recording: Blob): Promise<Blob> {
  const decoder = new AudioContext()
  try {
    const decoded = await decoder.decodeAudioData(await recording.arrayBuffer())
    const length = Math.max(1, Math.ceil(decoded.duration * GREETING_SAMPLE_RATE))
    const offline = new OfflineAudioContext(1, length, GREETING_SAMPLE_RATE)
    const source = offline.createBufferSource()
    source.buffer = decoded
    source.connect(offline.destination)
    source.start()
    const rendered = await offline.startRendering()
    return encodeWav(rendered.getChannelData(0), GREETING_SAMPLE_RATE)
  } finally {
    void decoder.close()
  }
}
