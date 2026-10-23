export type PackageStatus = 'generating' | 'ready' | 'needsReview' | 'approved' | 'sent' | 'failed'

export type CompressionItem = { id: string; reason: string | null }

export type AssignedChild = {
  childId: string
  firstName: string
  sent: boolean
  reachable: boolean
  missedCompetencyIds: string[]
  rationale: { targets: CompressionItem[]; dropped: CompressionItem[]; source: 'ai' | 'rule'; verdict: string }
}

export type AudioSegment = { kind: 'greeting' | 'disclosure' | 'script'; url: string; seconds: number }

export type CatchUpPackage = {
  id: string
  language: 'kn' | 'te' | 'ta'
  targetCompetencyIds: string[]
  status: PackageStatus
  scriptVersion: number
  verdict: string | null
  script: string | null
  keyWordsKn: string[]
  glossKn: string | null
  glossEn: string | null
  activity: { question: string; instruction: string | null }
  check: { competencyId: string; question: string; options: number[]; correctIndex: number; prompt: string | null }
  children: AssignedChild[]
  review: { passed: boolean; attempts: number; checks: { name: string; passed: boolean; detail: string }[] } | null
  audio: { segments: AudioSegment[]; totalSeconds: number; totalBytes: number; disclosureText: string } | null
  familiesSent: number
  familiesReachable: number
  approvedAt: string | null
  sentAt: string | null
}

export type CatchUpWeek = { weekOf: string; packages: CatchUpPackage[] }
