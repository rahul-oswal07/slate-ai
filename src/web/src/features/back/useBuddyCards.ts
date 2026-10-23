import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, NetworkError } from '../../api/client'
import { db, saveSnapshot } from '../../offline/db'
import type { BuddyCard, BuddyCards } from './types'

export const buddyKey = (classId: string, date: string) => ['buddy-cards', classId, date] as const

/** Today's buddy cards. Offline it shows the last snapshot, so cards stay readable in class (FR-12). */
export function useBuddyCards(classId: string, date: string) {
  return useQuery({
    queryKey: buddyKey(classId, date),
    queryFn: async () => {
      const key = buddyKey(classId, date).join(':')
      try {
        const cards = await api<BuddyCards>(`/api/classes/${classId}/buddy-cards/${date}`)
        await saveSnapshot(key, cards)
        return cards
      } catch (error) {
        const snapshot = error instanceof NetworkError ? await db.snapshots.get(key) : undefined
        if (!snapshot) {
          throw error
        }
        return { ...(snapshot.value as BuddyCards), savedAt: snapshot.savedAt }
      }
    },
    enabled: classId !== '',
    retry: false,
  })
}

/**
 * "Brought homework" and "Buddy time done" (D-22). Online only (D-70): a failure leaves the card as it was and says
 * so. Each tap is its own request (a later tap replaces an earlier one on the server).
 */
export function useSaveTicks(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ childId, ...ticks }: { childId: string; homeworkReturned?: boolean; done?: boolean }) =>
      api<BuddyCard>(`/api/classes/${classId}/buddy-cards/${date}/${childId}`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), ...ticks },
      }),
    onSuccess: (card) => {
      queryClient.setQueryData<BuddyCards>(buddyKey(classId, date), (cards) =>
        cards && { ...cards, cards: cards.cards.map((c) => (c.childId === card.childId ? card : c)) },
      )
      void saveSnapshot(buddyKey(classId, date).join(':'), queryClient.getQueryData(buddyKey(classId, date)))
    },
  })
}
