# Design tokens

Owner: Monika · Decisions: D-04, D-17 (`docs/spec.md` §8) · Code: [`src/web/src/styles/tokens.css`](../../src/web/src/styles/tokens.css) · Test: [`tokens.test.ts`](../../src/web/src/styles/tokens.test.ts)

Source: the Build Kit design system (02-DESIGN §2), which continues the Ideate deck's look: warm canvas, deep ink, one teal accent. The kit calls the teal `accent`; in our code it is **`primary`**.

## Targets

- **All text ≥ 7:1** (WCAG 1.4.6, AAA) at every size and weight, in both themes. We don't use the large-text exception, so any token is safe for any text, and Kannada/Telugu/Tamil vowel signs stay crisp in sunlight.
- **UI ≥ 3:1** (1.4.11): borders, icons next to a word, the focus ring.
- **Decorative** (dividers, illustration warm `#F59E0B`): no requirement; never used to carry meaning.

`tokens.test.ts` recomputes every pair below from `tokens.css` and fails `./scripts/verify.sh` if one drops under its target.

## Usage rules

1. **No `primary` text on `panel`** (light: 6.6:1). On panels use `primary-strong`.
2. **`warning` is for icons and borders only**, never text. Alert text stays `ink`.
3. **`panel` and `surface` are almost the same shade (1.15:1)**, so panels and cards always have a `border`. Never rely on shadow or tint alone.
4. **Focus ring: 3 px `focus` outline with a 2 px offset** (set globally in `index.css`). The ring is only 1.1:1 against a primary button, so the gap is what makes it visible.
5. **Status is always icon + word**, never colour alone. In greyscale the tags look alike; the icon and word tell them apart.
6. The original Ideate teal `#0F766E` (5.2:1) is for illustrations only.

## Dark theme

Follows the phone's setting (`prefers-color-scheme`), including phones that switch to dark in battery saver. **No in-app toggle yet** (deferred: in-app System/Light/Dark choice, pre-paint script, Save-Data and forced-colours handling). Dark is designed, not inverted: warm near-black (never `#000`), off-white text (never `#FFF`, to reduce glow for people with astigmatism), and light teal buttons with dark text.

## Colours

Ratios measured with the WCAG formula. Background order: canvas · surface · panel.

| Token (Tailwind class) | Light | Ratio | Dark | Ratio | Use |
|---|---|---|---|---|---|
| `canvas` | `#FAFAF7` | — | `#151513` | — | Page background |
| `surface` | `#FFFFFF` | — | `#1E1F1C` | — | Cards, sheets |
| `panel` | `#E6F2F0` | — | `#11302C` | — | AI panels, tinted sections |
| `ink` | `#1A1A2E` | 16.3 · 17.1 · 14.9 | `#EDECE6` | 15.5 · 14.0 · 12.0 | Primary text |
| `ink-muted` | `#495059` | 7.8 · 8.2 · 7.1 | `#B4BAC2` | 9.4 · 8.5 · 7.2 | Secondary text, captions |
| `border` | `#7C8591` | 3.6 · 3.7 · 3.3 | `#7A828C` | 4.7 · 4.3 · 3.6 | Input and card borders |
| `divider` | `#E5E7EB` | decorative | `#2E302C` | decorative | Never the only separator |
| `primary` | `#115E59` | 7.3 · 7.6 · *6.6* | `#2DD4BF` | 9.8 · 8.9 · 7.6 | Primary buttons, links, AI badge |
| `primary-strong` | `#134E4A` | 9.1 · 9.5 · 8.3 | `#5EEAD4` | 12.4 · 11.2 · 9.6 | Pressed state; teal text on panel |
| `on-primary` | `#FFFFFF` | 7.6 on primary | `#0B1F1D` | 9.2 on primary | Text on primary fills |
| `focus` | `#1D4ED8` | 6.4 · 6.7 · 5.9 | `#93C5FD` | 10.1 · 9.2 · 7.9 | Focus ring |
| `error` | `#991B1B` | 8.0 · 8.3 · 7.3; 7.6 on error-bg | `#FCA5A5` | 9.6 · 8.7 · 7.5; 8.5 on error-bg | Error text and icon; danger fill |
| `error-bg` | `#FEF2F2` | — | `#3A1515` | — | Error banner |
| `on-error` | `#FFFFFF` | 8.3 on error | `#2A0A0A` | 9.7 on error | Danger button text |
| `warning` | `#B45309` | 4.8 · 5.0 · 4.4 | `#FBBF24` | 11.0 · 9.9 · 8.5 | Low-quality alert icon and border only |
| `illustration-warm` | `#F59E0B` | decorative | same | decorative | Illustrations only |

### Status tags (`text-status-x` on `bg-status-x-bg`)

| Status | Lucide icon | Word (EN) | Light text / bg | Ratio | Dark text / bg | Ratio |
|---|---|---|---|---|---|---|
| `sent` | `send` | Sent | `#1E40AF` / `#EFF6FF` | 8.0 | `#BFDBFE` / `#172A4A` | 10.1 |
| `listened` | `headphones` | Listened | `#134E4A` / `#E6F2F0` | 8.3 | `#99F6E4` / `#11302C` | 11.2 |
| `caught-up` | `circle-check` | Caught up | `#14532D` / `#DCFCE7` | 8.3 | `#BBF7D0` / `#143223` | 11.5 |
| `needs-buddy` | `users` | Needs buddy | `#78350F` / `#FEF3C7` | 8.2 | `#FDE68A` / `#3A2A0B` | 11.1 |
| `no-phone` | `smartphone-off` + `id-card` | No phone · buddy card | `#5B21B6` / `#EDE9FE` | 7.6 | `#DDD6FE` / `#2B1E4F` | 10.8 |
| `needs-ear` | `ear` | Needs your ear | `#7C2D12` / `#FFEDD5` | 8.2 | `#FED7AA` / `#3D200F` | 11.0 |
| `stopped` | `bell-off` | Stopped by family | `#374151` / `#F3F4F6` | 9.4 | `#E5E7EB` / `#2A2C30` | 11.3 |
| `queued` | `cloud-off` | Saved · will send | `#495059` / `#F1F5F9` | 7.4 | `#CBD2DA` / `#24272B` | 9.8 |
| `error` | `circle-alert` | (sentence) | `#991B1B` / `#FEF2F2` | 7.6 | `#FECACA` / `#3A1515` | 11.2 |
| `missed` | `calendar-x` | Missed lesson | same pair as `stopped` | 9.4 | same as `stopped` | 11.3 |
| `ready` | `circle-check` | Ready | same pair as `listened` | 8.3 | same as `listened` | 11.2 |
| `needs-review` | `circle-alert` | Needs review | same pair as `needs-ear` | 8.2 | same as `needs-ear` | 11.0 |
| `approved` | `check` | Approved | same pair as `caught-up` | 8.3 | same as `caught-up` | 11.5 |
| `generating` | `clock` | Generating… | same pair as `queued` | 7.4 | same as `queued` | 9.8 |
| `generation-failed` | `circle-alert` | Generation failed | same pair as `error` | 7.6 | same as `error` | 11.2 |

The last six rows (D-35) reuse existing pairs, so no new contrast pairs are needed; in `tokens.css` they are aliases of the pair named. The icon and word tell them apart (rule 5). `needs-ear` was for the dropped review queue (FR-18) and is kept only as the colour source for `needs-review`.

**Picture-question sticks (D-07, D-35):** drawn in `warning` (`#B45309` light, `#FBBF24` dark) with the band in `primary`. Illustration only, never text; both meet 3:1 against `surface` (non-text contrast).

Words are draft English; Kannada, Telugu and Tamil go into the locale files when the components are built.

## Type

Latin sizes by default; Kannada, Telugu and Tamil (`lang="kn|te|ta"`) automatically get one step larger with ~1.7 line height, because conjuncts and vowel signs need room. Classes: `text-display`, `text-title`, `text-body-lg`, `text-body`, `text-label`, `text-caption`.

| Token | Latin (size/line) | Indic (size/line) | Weight | Use |
|---|---|---|---|---|
| `display` | 28/36 | 30/50 | 700 | Screen title, one per screen |
| `title` | 22/30 | 24/40 | 600 | Card titles |
| `body-lg` | 18/28 | 20/34 | 400 | Key sentences, scripts |
| `body` | 16/24 | 18/30 | 400 | Default |
| `label` | 16/20 | 18/26 | 600 | Buttons, chips, status words |
| `caption` | 14/20 | 16/26 | 400 | Hints, English subtitles; never smaller |

No all-caps and no letter-spacing on Indic text. Never fix the height of a text container. Numerals: Latin digits.

## Size and shape

| Token | Value | Class example | Use |
|---|---|---|---|
| `target` | 48 px | `min-h-target` | Every interactive element |
| `target-primary` | 56 px | `min-h-target-primary` | Primary button (full width) |
| `target-mic` | 96 px | `size-target-mic` | Mic button |
| `target-picture` | 120 px | `size-target-picture` | Child's picture choice (NFR-04 says ≥ 96; 120 is the design target) |
| `radius-sm` | 8 px | `rounded-sm` | Chips |
| `radius-md` | 12 px | `rounded-md` | Cards, buttons |

Keep 8 px between adjacent targets.

## Screenshots (360 px)

| Light | Dark |
|---|---|
| ![Shell, light theme](screenshots/T-23-shell-360-light.png) | ![Shell, dark theme](screenshots/T-23-shell-360-dark.png) |
