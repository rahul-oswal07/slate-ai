import { fakeApi } from '../test/renderApp'
import { db } from './db'
import { enqueue, flush } from './outbox'

async function queue(...ids: string[]) {
  for (const id of ids) {
    await enqueue({ id, url: `/api/${id}`, body: { clientRequestId: id } })
  }
}

describe('outbox', () => {
  beforeEach(async () => {
    await Promise.all([db.outbox.clear(), db.failed.clear()])
  })

  afterEach(() => vi.unstubAllGlobals())

  it('sends oldest first and empties the queue', async () => {
    const calls = fakeApi({
      'POST /api/a': () => ({ status: 200, body: {} }),
      'POST /api/b': () => ({ status: 200, body: {} }),
    })
    await queue('a', 'b')

    expect(await flush()).toBe(2)
    expect(calls.map((c) => c.key)).toEqual(['POST /api/a', 'POST /api/b'])
    expect(await db.outbox.count()).toBe(0)
  })

  it('stops at a network failure and keeps everything from there', async () => {
    fakeApi({ 'POST /api/a': () => 'offline', 'POST /api/b': () => ({ status: 200, body: {} }) })
    await queue('a', 'b')

    expect(await flush()).toBe(0)
    expect(await db.outbox.count()).toBe(2)
  })

  it('keeps writes when the session has expired (401)', async () => {
    fakeApi({ 'POST /api/a': () => ({ status: 401 }) })
    await queue('a')

    await flush()

    expect(await db.outbox.count()).toBe(1)
  })

  it('moves a refused write to failed and carries on', async () => {
    fakeApi({ 'POST /api/a': () => ({ status: 400 }), 'POST /api/b': () => ({ status: 200, body: {} }) })
    await queue('a', 'b')

    expect(await flush()).toBe(1)
    expect(await db.outbox.count()).toBe(0)
    expect((await db.failed.get('a'))?.status).toBe(400)
  })

  it('concurrent flushes send each write once', async () => {
    const calls = fakeApi({ 'POST /api/a': () => ({ status: 200, body: {} }) })
    await queue('a')

    await Promise.all([flush(), flush()])

    expect(calls).toHaveLength(1)
  })
})

describe('outbox voice notes', () => {
  beforeEach(async () => {
    await db.outbox.clear()
  })

  afterEach(() => vi.unstubAllGlobals())

  it('sends a voice note as multipart with its clientRequestId', async () => {
    let sent: FormData | undefined
    vi.stubGlobal('fetch', vi.fn(async (_url: RequestInfo | URL, init?: RequestInit) => {
      sent = init?.body as FormData
      return new Response('{}', { status: 202, headers: { 'Content-Type': 'application/json' } })
    }))
    await enqueue({
      id: 'note-1',
      url: '/api/classes/c/lessons/2026-10-22/audio',
      body: undefined,
      audio: { data: new Uint8Array([1, 2, 3]).buffer, type: 'audio/webm', fileName: 'note.webm' },
    })

    expect(await flush()).toBe(1)
    expect(sent?.get('clientRequestId')).toBe('note-1')
    expect(sent?.get('file')).toBeInstanceOf(Blob)
  })
})
