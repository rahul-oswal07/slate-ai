/**
 * A sum to work out, written once as "28 + 15 = ?". The stored question is the sum alone ("28 + 15"); one that
 * already ends in "?" or "= ?" never gets a second one (T-101).
 */
export function sumQuestion(question: string): string {
  return `${question.replace(/[\s=?]+$/u, '').trim()} = ?`
}

/** The picture check's question: "43 − 18" reads as a sum to work out; "347" or "4 in 347" are shown as they are. */
export function checkQuestion(question: string): string {
  return /[+−-]/.test(question) ? sumQuestion(question) : question
}
