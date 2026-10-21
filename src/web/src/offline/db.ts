import Dexie, { type EntityTable } from 'dexie'

/** A write waiting to reach the server. `id` is its clientRequestId, so a resend is a no-op (NFR-02). */
export type OutboxItem = {
  id: string
  url: string
  body: unknown
  createdAt: number
}

/** A write the server refused (4xx other than 401); kept so nothing she did silently disappears. */
export type FailedItem = OutboxItem & { status: number; failedAt: number }

/** Last server answer for a screen, shown when offline. */
export type Snapshot = { key: string; value: unknown; savedAt: number }

export const db = new Dexie('slate') as Dexie & {
  outbox: EntityTable<OutboxItem, 'id'>
  failed: EntityTable<FailedItem, 'id'>
  snapshots: EntityTable<Snapshot, 'key'>
}

db.version(1).stores({
  outbox: 'id, createdAt',
  failed: 'id, failedAt',
  snapshots: 'key',
})

export async function saveSnapshot(key: string, value: unknown) {
  await db.snapshots.put({ key, value, savedAt: Date.now() })
}

export async function loadSnapshot<T>(key: string): Promise<T | null> {
  return ((await db.snapshots.get(key))?.value as T | undefined) ?? null
}
