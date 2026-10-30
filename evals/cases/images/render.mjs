// Renders the synthetic A1 photo cases (T-64) to PNG: a textbook page, a blackboard, a page of word problems.
// Stand-ins until real teacher photos replace them. Run from src/web (uses its Playwright):
//   cd src/web && node ../../evals/cases/images/render.mjs
import { createRequire } from 'node:module'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const { chromium } = createRequire(join(process.cwd(), '/'))('@playwright/test')
const out = dirname(fileURLToPath(import.meta.url))

const page = (title, lines) => `
  <body style="margin:0;background:#f4efe2;font-family:'Noto Sans Kannada',sans-serif;color:#222">
    <div style="padding:40px 48px;transform:rotate(-1.5deg)">
      <div style="font-size:20px;color:#666">ಅಧ್ಯಾಯ 5 · ಗಣಿತ 3ನೇ ತರಗತಿ</div>
      <h1 style="font-size:40px;margin:12px 0 24px">${title}</h1>
      ${lines.map((l) => `<div style="font-size:32px;margin:14px 0">${l}</div>`).join('')}
    </div>
  </body>`

const board = (lines) => `
  <body style="margin:0;background:#2f4a3a;font-family:'Comic Sans MS',cursive;color:#f1f1e8">
    <div style="padding:48px">
      ${lines.map((l) => `<div style="font-size:44px;margin:22px 0">${l}</div>`).join('')}
    </div>
  </body>`

const cases = {
  'sub-borrow-page.png': page('ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ', [
    'ಅಭ್ಯಾಸ 5.3 &nbsp; ಕಳೆಯಿರಿ:',
    '1) &nbsp; 438 − 229 = ____',
    '2) &nbsp; 652 − 378 = ____',
    '3) &nbsp; 341 − 186 = ____',
    '<span style="font-size:24px">ಬಿಡಿ ಸ್ಥಾನದಲ್ಲಿ ಕಳೆಯಲು ಆಗದಿದ್ದರೆ, ಹತ್ತರ ಸ್ಥಾನದಿಂದ ಎರವಲು ಪಡೆಯಿರಿ.</span>',
  ]),
  'expand-board.png': board([
    '30-10-2026 &nbsp;&nbsp;&nbsp; Ravi - monitor',
    '385 = 300 + 80 + 5',
    '427 = ___ + ___ + ___',
    '659 = ___ + ___ + ___',
  ]),
  'add-word-page.png': page('ಹೇಳಿಕೆ ರೂಪದ ಸಮಸ್ಯೆಗಳು', [
    '1) ಶಾಲೆಯಲ್ಲಿ 324 ಹುಡುಗರು ಮತ್ತು 258 ಹುಡುಗಿಯರು ಇದ್ದಾರೆ. ಒಟ್ಟು ಎಷ್ಟು ಮಕ್ಕಳು?',
    '2) ಮದುವೆಗೆ ಮೊದಲ ದಿನ 230, ಎರಡನೇ ದಿನ 565 ಅತಿಥಿಗಳು ಬಂದರು. ಒಟ್ಟು ಎಷ್ಟು?',
    '3) ರೈತನ ಬಳಿ 146 ಮಾವಿನ ಹಣ್ಣು ಮತ್ತು 237 ಸೀಬೆ ಹಣ್ಣು ಇವೆ. ಒಟ್ಟು ಎಷ್ಟು ಹಣ್ಣುಗಳು?',
  ]),
}

const browser = await chromium.launch()
const tab = await browser.newPage({ viewport: { width: 900, height: 640 } })
for (const [file, html] of Object.entries(cases)) {
  await tab.setContent(html)
  await tab.screenshot({ path: join(out, file) })
}
await browser.close()
