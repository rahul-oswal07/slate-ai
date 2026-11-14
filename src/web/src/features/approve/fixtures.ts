import type { Curriculum } from '../lessons/types'
import { istToday } from '../today/schoolTime'
import type { TodaysMessage } from './todaysTypes'
import type { CatchUpPackage, CatchUpWeek } from './types'

export const curriculum: Curriculum = {
  chapters: [
    {
      number: 5,
      nameKn: 'ಕಳೆಯುವ ಮೋಜು',
      nameEn: 'The fun of subtracting',
      competencies: [
        { id: 'M3.SUB.MAKE', nameKn: 'ಸಮಸ್ಯಾ ರಚನೆ', nameEn: 'Making subtraction sums', sequence: 14, textbookPage: null },
        { id: 'M3.SUB.BORROW', nameKn: 'ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ', nameEn: '3-digit subtraction with borrowing', sequence: 15, textbookPage: null },
      ],
    },
  ],
}

export function pkg(overrides: Partial<CatchUpPackage> = {}): CatchUpPackage {
  return {
    id: 'pkg-te',
    language: 'te',
    targetCompetencyIds: ['M3.SUB.BORROW'],
    status: 'ready',
    scriptVersion: 2,
    verdict: 'verified',
    script: 'ఈ రోజు ...',
    keyWordsKn: ['ಕಳೆ', 'ದಶಕ', 'ಬಿಡಿ'],
    glossKn: 'ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ. ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸು',
    glossEn: 'Subtraction with borrowing. Show your teacher.',
    activity: { question: '523 − 168', instruction: 'Make 523 with sticks.' },
    check: { competencyId: 'M3.SUB.BORROW', question: '434 − 171', options: [363, 263, 273], correctIndex: 1, prompt: 'Which picture?' },
    children: [
      {
        childId: 'c-ravi',
        firstName: 'Ravi',
        sent: false,
        reachable: true,
        missedCompetencyIds: ['M3.SUB.MAKE', 'M3.SUB.BORROW'],
        rationale: {
          targets: [
            { id: 'M3.SUB.BORROW', reason: 'Word problems next week need borrowing.', reasonKn: 'ಮುಂದಿನ ವಾರದ ಪದ ಸಮಸ್ಯೆಗಳಿಗೆ ಎರವಲು ಬೇಕು.' },
          ],
          dropped: [{ id: 'M3.SUB.MAKE', reason: 'Making sums can wait.' }],
          source: 'ai',
          verdict: 'valid',
        },
      },
      {
        childId: 'c-mahesh',
        firstName: 'Mahesh',
        sent: false,
        reachable: false,
        missedCompetencyIds: ['M3.SUB.BORROW'],
        rationale: { targets: [{ id: 'M3.SUB.BORROW', reason: null }], dropped: [], source: 'rule', verdict: 'rule:no-candidates' },
      },
    ],
    review: { passed: true, attempts: 1, checks: [{ name: 'audio', passed: true, detail: '55 seconds, 151 KB' }] },
    audio: {
      segments: [
        { kind: 'disclosure', url: '/api/audio/d1', seconds: 5.3 },
        { kind: 'script', url: '/api/audio/s1', seconds: 84 },
      ],
      totalSeconds: 89.3,
      totalBytes: 150000,
      disclosureText: 'ఇది మీ బిడ్డ టీచర్ పంపిన పాఠం.',
    },
    familiesSent: 0,
    familiesReachable: 1,
    approvedAt: null,
    sentAt: null,
    ...overrides,
  }
}

export const week = (...packages: CatchUpPackage[]): CatchUpWeek => ({ weekOf: '2026-10-19', packages })

/** Today's message, ready for her to check: Nalikali p 4 in Kannada and Hindi. */
export function todaysMessage(overrides: Partial<TodaysMessage> = {}): TodaysMessage {
  return {
    date: istToday(),
    status: 'ready',
    version: 1,
    approvedVersion: null,
    approvedAt: null,
    sent: false,
    verdict: 'valid',
    competencyIds: ['M3.BRIDGE.COMPARE'],
    picture: { kind: 'compare', a: 48, b: 33, answer: '>', removed: false },
    video: null,
    note: null,
    removed: [],
    languages: [
      { language: 'kn', families: 1, concept: 'ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ.', conceptClipUrl: '/api/audio/c1', steps: ['75 ಮತ್ತು 57 ಕ್ಕೆ ಕಟ್ಟು ಮಾಡಿ.', 'ಯಾವುದು ಹೆಚ್ಚು?'] },
      { language: 'hi', families: 1, concept: 'पहले दस की गड्डियाँ देखिए।', conceptClipUrl: '/api/audio/c2', steps: ['एक', 'दो'] },
    ],
    pairs: [
      [48, 33],
      [48, 84],
      [75, 57],
      [86, 86],
    ],
    pictureIndex: 0,
    edited: false,
    ...overrides,
  }
}
