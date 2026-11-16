#!/usr/bin/env node
// Puts the DIKSHA link of each lesson on a server's /admin/lesson-links (T-138), from data/lesson-links/diksha-kn.json.
//
//   node scripts/set-lesson-links.mjs --base https://slateai-staging.azurewebsites.net            # dry run: what would change
//   node scripts/set-lesson-links.mjs --base https://slateai-staging.azurewebsites.net --apply    # do it
//
// Options:
//   --base <url>      the server (required). Admin pages need no key (D-112).
//   --file <path>     the links file (default data/lesson-links/diksha-kn.json).
//   --apply           write the links; without it nothing is changed.
//   --topic-only      only links that teach the lesson's own skill (match "topic"), not the chapter videos.
//   --overwrite       replace a link someone already set on the page; by default those are kept.
//
// A lesson whose url is null in the file is left alone (never cleared). Each write sends a new clientRequestId, so a
// rerun is safe: a link already set to the same URL is skipped. Exits 1 if any write fails or the file names a lesson
// the server doesn't know.
import { readFile } from 'node:fs/promises'
import { randomUUID } from 'node:crypto'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const args = process.argv.slice(2)
const flag = (name) => args.includes(name)
const option = (name, fallback) => {
  const i = args.indexOf(name)
  return i >= 0 && args[i + 1] ? args[i + 1] : fallback
}

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const base = option('--base')?.replace(/\/+$/, '')
const file = path.resolve(root, option('--file', 'data/lesson-links/diksha-kn.json'))
const apply = flag('--apply')
const topicOnly = flag('--topic-only')
const overwrite = flag('--overwrite')

if (!base) {
  console.error('Usage: node scripts/set-lesson-links.mjs --base <server url> [--apply] [--topic-only] [--overwrite] [--file <path>]')
  process.exit(2)
}

/** What to do for each lesson of the file, given the links the server has now. Pure. */
function plan(lessons, current, { topicOnly, overwrite }) {
  return lessons.map((lesson) => {
    const id = lesson.competencyId
    if (!(id in current)) return { id, action: 'unknown' }
    if (!lesson.url) return { id, action: 'none', why: lesson.reason }
    if (topicOnly && lesson.match !== 'topic') return { id, action: 'skip', why: 'chapter video (--topic-only)' }
    const now = current[id]
    if (now === lesson.url) return { id, action: 'same' }
    if (now && !overwrite) return { id, action: 'keep', why: `already has ${now}` }
    return { id, action: now ? 'replace' : 'set', url: lesson.url, title: lesson.title, match: lesson.match }
  })
}

const { lessons } = JSON.parse(await readFile(file, 'utf8'))
const response = await fetch(`${base}/api/admin/lesson-links`)
if (!response.ok) {
  console.error(`GET ${base}/api/admin/lesson-links → ${response.status}`)
  process.exit(1)
}
const { chapters } = await response.json()
const current = Object.fromEntries(chapters.flatMap((c) => c.lessons).map((l) => [l.id, l.url ?? null]))

const steps = plan(lessons, current, { topicOnly, overwrite })
let failed = 0
for (const step of steps) {
  if (step.action === 'set' || step.action === 'replace') {
    let result = 'would ' + step.action
    if (apply) {
      const put = await fetch(`${base}/api/admin/lesson-links/${encodeURIComponent(step.id)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ clientRequestId: randomUUID(), url: step.url }),
      })
      result = put.ok ? step.action : `FAILED ${put.status} ${(await put.text()).slice(0, 120)}`
      if (!put.ok) failed++
    }
    console.log(`${step.id.padEnd(20)} ${result.padEnd(14)} [${step.match}] ${step.title}`)
  } else if (step.action === 'unknown') {
    failed++
    console.log(`${step.id.padEnd(20)} UNKNOWN        not a lesson on ${base}`)
  } else {
    console.log(`${step.id.padEnd(20)} ${step.action.padEnd(14)} ${step.why ?? ''}`)
  }
}

const count = (action) => steps.filter((s) => s.action === action).length
console.log(
  `\n${apply ? 'Applied' : 'Dry run (add --apply to write)'} on ${base}: ` +
    `${count('set')} set, ${count('replace')} replaced, ${count('same')} already set, ${count('keep')} kept, ` +
    `${count('skip')} skipped, ${count('none')} without a link` +
    (failed ? `, ${failed} FAILED` : ''),
)
process.exit(failed ? 1 : 0)
