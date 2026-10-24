/** GET /api/demo: the demo class, whose family the parent phone shows (Rohan's, in English), and the last reset. */
export interface DemoInfo {
  classId: string
  parentChildId: string
  lastResetAt: string | null
}

/** POST /api/demo/reset. `preMade`: a pre-made catch-up was restored as Ready. */
export interface DemoReset {
  resetAt: string
  preMade: boolean
}

/** One link on the simulated parent phone. */
export interface FamilyMessage {
  kind: 'evening' | 'catchUp'
  date: string
  sentAt: string
  token: string
}

/** One row of "Under the hood". No prompt text or names exist to show. */
export interface AiCall {
  id: string
  createdAt: string
  purpose: string
  model: string
  promptVersion: number | null
  inputTokens: number
  outputTokens: number
  cacheReadTokens: number
  latencyMs: number
  verdict: string | null
  error: string | null
}
