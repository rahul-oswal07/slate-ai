import { describe, expect, it } from 'vitest'
import { encodeWav } from './wav'

async function bytes(blob: Blob) {
  return new DataView(await blob.arrayBuffer())
}

describe('encodeWav', () => {
  it('writes a 16 kHz mono 16-bit PCM header', async () => {
    const wav = await bytes(encodeWav(new Float32Array(16_000), 16_000))

    expect(String.fromCharCode(wav.getUint8(0), wav.getUint8(1), wav.getUint8(2), wav.getUint8(3))).toBe('RIFF')
    expect(String.fromCharCode(wav.getUint8(8), wav.getUint8(9), wav.getUint8(10), wav.getUint8(11))).toBe('WAVE')
    expect(wav.getUint16(20, true)).toBe(1) // PCM
    expect(wav.getUint16(22, true)).toBe(1) // mono
    expect(wav.getUint32(24, true)).toBe(16_000)
    expect(wav.getUint16(34, true)).toBe(16)
    expect(wav.getUint32(40, true)).toBe(32_000) // one second of 16-bit samples
    expect(wav.byteLength).toBe(44 + 32_000)
  })

  it('scales samples to 16 bit and clips anything outside [-1, 1]', async () => {
    const wav = await bytes(encodeWav(Float32Array.of(0, 1, -1, 2, -2, 0.5), 16_000))

    expect([0, 1, 2, 3, 4, 5].map((i) => wav.getInt16(44 + i * 2, true))).toEqual([0, 32767, -32768, 32767, -32768, 16383])
  })

  it('has the type the API accepts', () => {
    expect(encodeWav(new Float32Array(1), 16_000).type).toBe('audio/wav')
  })
})
