import Dexie, { type EntityTable } from 'dexie'

/** A write waiting to reach the server. `id` is its clientRequestId, so a resend is a no-op (NFR-02). */
export type OutboxItem = {
  id: string
  url: string
  /** Defaults to POST. */
  method?: 'POST' | 'PUT'
  body: unknown
  createdAt: number
  /** A voice note: sent as multipart (`file` + `clientRequestId`) instead of JSON. */
  audio?: { data: ArrayBuffer; type: string; fileName: string }
}

/** Her recording, kept on the phone only for playback on T2; deleted once the lesson is confirmed. */
export type Note = { date: string; data: ArrayBuffer; type: string; savedAt: number }

/** A write the server refused (4xx other than 401); kept so nothing she did silently disappears. */
export type FailedItem = OutboxItem & { status: number; failedAt: number }

/** Last server answer for a screen, shown when offline. */
export type Snapshot = { key: string; value: unknown; savedAt: number }

export const db = new Dexie('slate') as Dexie & {
  outbox: EntityTable<OutboxItem, 'id'>
  failed: EntityTable<FailedItem, 'id'>
  snapshots: EntityTable<Snapshot, 'key'>
  notes: EntityTable<Note, 'date'>
}

db.version(1).stores({
  outbox: 'id, createdAt',
  failed: 'id, failedAt',
  snapshots: 'key',
})

db.version(2).stores({ notes: 'date' })

export async function saveSnapshot(key: string, value: unknown) {
  await db.snapshots.put({ key, value, savedAt: Date.now() })
}
