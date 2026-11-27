/** The five sections, the same on both Approve tabs (T-89, T-103): today's message and each catch-up. */
export type SectionKind = 'concept' | 'activity' | 'picture' | 'video' | 'note'

/** The order she reads them in. */
export const SECTION_ORDER: SectionKind[] = ['concept', 'activity', 'video', 'picture', 'note']
