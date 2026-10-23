import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api, NetworkError } from '../../api/client'
import { db, saveSnapshot } from '../../offline/db'
import type { Ledger } from './types'

export const ledgerKey = (classId: string) => ['ledger', classId] as const

/** The ledger (FR-05). Offline it shows the last snapshot with its time; never a blank screen (T4). */
export function useLedger(classId: string) {
  return useQuery({
    queryKey: ledgerKey(classId),
    queryFn: async () => {
      const key = ledgerKey(classId).join(':')
      try {
        const ledger = await api<Ledger>(`/api/classes/${classId}/ledger`)
        await saveSnapshot(key, ledger)
        return ledger
      } catch (error) {
        const snapshot = error instanceof NetworkError ? await db.snapshots.get(key) : undefined
        if (!snapshot) {
          throw error
        }
        return { ...(snapshot.value as Ledger), savedAt: snapshot.savedAt }
      }
    },
    enabled: classId !== '',
    retry: false,
  })
}

/**
 * Deletes all of a child's data (FR-15). Online only (D-70): no queue, so the dialog never promises "later". One
 * clientRequestId per dialog, so Try again is a replay, not a second delete.
 */
export function useDeleteChild(classId: string, childId: string) {
  const queryClient = useQueryClient()
  const requestId = useRef(crypto.randomUUID())
  return useMutation({
    mutationFn: () =>
      api<void>(`/api/classes/${classId}/children/${childId}?clientRequestId=${requestId.current}`, { method: 'DELETE' }),
    // Refetch without waiting and without editing the cache now: removing the child at once would unmount the dialog
    // before its own onSuccess (announce, back to the list) runs.
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ledgerKey(classId) }),
  })
}
