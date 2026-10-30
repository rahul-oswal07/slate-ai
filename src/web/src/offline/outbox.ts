import { useLiveQuery } from 'dexie-react-hooks'
import { useEffect } from 'react'
import { api, ApiError, NetworkError } from '../api/client'
import { db, type OutboxItem } from './db'

export async function enqueue(item: Omit<OutboxItem, 'createdAt'>) {
  await db.outbox.put({ ...item, createdAt: Date.now() })
}

let running: Promise<number> | null = null

/**
 * Sends queued writes oldest first. Stops at the first network failure or 401 (sign-in needed) and keeps
 * the rest; any other refusal moves the item to `failed`. Resolves to the number sent. Concurrent calls
 * share one run.
 */
export function flush(): Promise<number> {
  running ??= (async () => {
    let sent = 0
    try {
      for (const item of await db.outbox.orderBy('createdAt').toArray()) {
        try {
          await api(item.url, item.audio || item.photo ? { method: 'POST', form: toForm(item) } : { method: item.method ?? 'POST', body: item.body })
          await db.outbox.delete(item.id)
          sent++
        } catch (error) {
          if (error instanceof NetworkError || (error instanceof ApiError && error.status === 401)) {
            break
          }
          const status = error instanceof ApiError ? error.status : 0
          await db.transaction('rw', db.outbox, db.failed, async () => {
            await db.failed.put({ ...item, status, failedAt: Date.now() })
            await db.outbox.delete(item.id)
          })
        }
      }
    } finally {
      running = null
    }
    return sent
  })()
  return running
}

function toForm(item: OutboxItem) {
  const form = new FormData()
  const audio = item.audio
  if (audio) {
    form.append('file', new Blob([audio.data], { type: audio.type }), audio.fileName)
  }
  const photo = item.photo
  if (photo) {
    form.append('photo', new Blob([photo.data], { type: photo.type }), photo.fileName)
  }
  form.append('clientRequestId', item.id)
  return form
}

/** Writes still waiting to send; drives the sync badge. */
export function useOutboxCount() {
  return useLiveQuery(() => db.outbox.count(), [], 0)
}

/** Flushes on start and whenever the network comes back; `onSent` runs after anything was delivered. */
export function useOutboxSync(onSent: () => void) {
  useEffect(() => {
    const run = () => {
      void flush().then((sent) => {
        if (sent > 0) {
          onSent()
        }
      })
    }
    run()
    window.addEventListener('online', run)
    return () => window.removeEventListener('online', run)
  }, [onSent])
}

/** Queued writes for a URL, oldest first (lets a screen show what hasn't been sent yet). */
export function pendingFor(url: string) {
  return db.outbox.where('createdAt').above(0).filter((item) => item.url === url).sortBy('createdAt')
}
