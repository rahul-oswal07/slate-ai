/** The literal placeholder the approved templates' buttons put before the token (T-104), decoded or still encoded. */
const STRAY_PREFIXES = ['{{1}}', '%7B%7B1%7D%7D']

/**
 * The family link's token without the stray `{{1}}` that the approved WhatsApp templates' buttons put in front of it
 * (their URL reads `…/p/{{1}}` + `{{1}}`; T-104). Monika is fixing the templates; links already sent keep working.
 */
export function cleanToken(token: string): string {
  let clean = token
  for (let changed = true; changed; ) {
    changed = false
    for (const prefix of STRAY_PREFIXES) {
      if (clean.toUpperCase().startsWith(prefix.toUpperCase())) {
        clean = clean.slice(prefix.length)
        changed = true
      }
    }
  }
  return clean
}
