# T-22 Competency list: Grade 3 maths, place value → carrying → borrowing

Spec: D-01, D-14, D-22, FR-03, FR-06, FR-07 · Feeds: T-05 (`data/curriculum/grade3-maths.json`) · Owner: Monika (names, pages) · Reviewer: Rahul (graph has no cycles)

## Source

Karnataka state textbook **ಮೂರನೇ ತರಗತಿ ಗಣಿತ (Nali-Kali, Kannada medium), 2026–27**, Part 1 and Part 2 (KTBS). The PDFs are not in the repo (they are marked "not to be republished" and the repo is public). Page numbers below are **printed** page numbers, which is what a child sees.

| Book | Chapter | Printed pages | Taught (book's own plan, Part 1 p 92) |
|---|---|---|---|
| Part 1 | ಸೇತುಬಂಧ (bridge course: Grade 2 revision) | 1–25 | June–July |
| Part 1 | 2. ಎಣಿಸೋಣ – ಕುಣಿಯೋಣ (place value to 9999) | 40–60 | August |
| Part 1 | 3. ಸಂಖ್ಯೆಗಳೊಂದಿಗೆ ಆಟ (compare, order, count on/back) | 61–74 | August–September |
| Part 1 | 4. ಕೂಡಿ ನಲಿಯೋಣ (3-digit addition, sum ≤ 999) | 75–92 | September |
| Part 2 | 5. ಕಳೆಯುವ ಮೋಜು (3-digit subtraction) | 1–19 | after Part 1 (≈ October) |

Chapter 1 (shapes) is out of scope.

## What changed from D-01

D-01 assumed 2-digit numbers (tens and ones). **The Grade 3 book teaches addition and subtraction with 3-digit numbers only.** 2-digit sums appear only as revision in the bridge course, and carrying and borrowing are never taught there. So the main chain follows the book (Ch 2 → 4 → 5), and the 2-digit skills become Grade 2 prerequisite nodes (`M2.*`). Decided by Monika on 20 Oct. This is also what teachers in user testing will be teaching after the Dasara break.

## Competencies (16)

Kannada names are the book's words where it has a heading for the skill. † = proposed wording not printed in the book; Monika to confirm with a teacher (D-14).

| Seq | ID | Kannada name | English name | Prerequisites | Homework page (D-22) | Textbook examples |
|---|---|---|---|---|---|---|
| 1 | `M2.PV.TENS` | ಹತ್ತು ಮತ್ತು ಬಿಡಿ (ಸ್ಥಾನ ಬೆಲೆ) | Place value: tens and ones | — | Part 1 p 6 | Ten-rods and unit cubes → groups of ten and ones, e.g. 1 rod + 3 cubes = 13 |
| 2 | `M2.ADD.2D` | ಎರಡಂಕಿ ಸಂಖ್ಯೆಗಳ ಸಂಕಲನ † | 2-digit addition | `M2.PV.TENS` | Part 1 p 10 | 25 + 13 coconuts; 12 + 18 |
| 3 | `M2.SUB.2D` | ಎರಡಂಕಿ ಸಂಖ್ಯೆಗಳ ವ್ಯವಕಲನ † | 2-digit subtraction | `M2.PV.TENS` | Part 1 p 13 | 66 + 23 cricket balls, 34 sold: 89 − 34 |
| 4 | `M3.PV.HTO` | ನೂರು, ಹತ್ತು, ಬಿಡಿ | Hundreds, tens and ones (to 999) | `M2.PV.TENS` | Part 1 p 42 | 1 hundred + 2 tens + 1 one = 121; 2 hundreds + 1 ten + 3 ones |
| 5 | `M3.PV.EXPAND` | ಸಂಖ್ಯೆಗಳನ್ನು ಬಿಡಿಸಿ ಬರೆಯೋಣ | Expanded form | `M3.PV.HTO` | Part 1 p 45 | 385 = 300 + 80 + 5; 427; 280 |
| 6 | `M3.PV.FACE` | ಸ್ಥಾನಬೆಲೆ ಮತ್ತು ಮುಖಬೆಲೆ | Place value and face value | `M3.PV.HTO` | Part 1 p 49 | 89: face value 8, place value 80; the 0 in 509 |
| 7 | `M3.PV.4D` | 9999 ರವರೆಗಿನ ಸಂಖ್ಯೆಗಳು (ಸಾವಿರ) † | Numbers to 9999 (thousands) | `M3.PV.EXPAND` | Part 1 p 55 | 3475 = 3000 + 400 + 70 + 5; 2009 in words |
| 8 | `M3.NUM.COMPARE` | ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ | Compare 3-digit numbers (>, <, =) | `M3.PV.HTO` | Part 1 p 65 | 737 □ 738; 605 □ 650 |
| 9 | `M3.NUM.ORDER` | ಏರಿಕೆ ಕ್ರಮ – ಇಳಿಕೆ ಕ್ರಮ | Ascending and descending order | `M3.NUM.COMPARE` | Part 1 p 68 (ascending), p 69 (descending) | 303 508 609 401; 248 844 768 646 |
| 10 | `M3.ADD.NOCARRY` | ದಶಕ ರಹಿತ ಸಂಕಲನ † | 3-digit addition without carrying | `M3.PV.HTO`, `M2.ADD.2D` | Part 1 p 80 (or p 78 tables) | 718 + 221; 452 + 224 |
| 11 | `M3.ADD.CARRY` | ದಶಕ ಸಹಿತ ಸಂಕಲನ † | 3-digit addition with carrying | `M3.ADD.NOCARRY`, `M3.PV.EXPAND` | Part 1 p 85 (or p 83 tables) | 317 + 536; 247 + 365 |
| 12 | `M3.ADD.WORD` | ಹೇಳಿಕೆ ರೂಪದ ಸಮಸ್ಯೆಗಳು (ಸಂಕಲನ) | Addition word problems | `M3.ADD.CARRY` | Part 1 p 88 | 324 boys and 258 girls: how many children?; 230 + 565 guests |
| 13 | `M3.SUB.NOBORROW` | ದಶಕ ರಹಿತ ವ್ಯವಕಲನ † | 3-digit subtraction without borrowing | `M3.PV.HTO`, `M2.SUB.2D` | Part 2 p 6 | 532 − 401; 448 − 335 |
| 14 | `M3.SUB.CHECK` | ತಾಳೆ ನೋಡುವುದು (ಕೂಡಿಸುವುದು ಮತ್ತು ಕಳೆಯುವುದರ ಸಹಸಂಬಂಧ) | Check subtraction by adding | `M3.SUB.NOBORROW`, `M3.ADD.NOCARRY` | Part 2 p 4–5 | 626 − 615 = 011; check 011 + 615 = 626 |
| 15 | `M3.SUB.BORROW` | ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ | 3-digit subtraction with borrowing | `M3.SUB.NOBORROW`, `M3.PV.EXPAND` | Part 2 p 15 | 438 − 229; 452 − 358 (borrows twice) |
| 16 | `M3.SUB.WORD` | ಹೇಳಿಕೆ ರೂಪದ ಸಮಸ್ಯೆಗಳು (ವ್ಯವಕಲನ) | Subtraction word problems | `M3.SUB.BORROW` | Part 2 p 18 | Book of 257 pages, 152 read: how many left?; 875 − 673 stamps |

The graph has no cycles. Several nodes are reachable by more than one path (e.g. `M3.ADD.CARRY` reaches `M3.PV.HTO` through both `M3.ADD.NOCARRY` and `M3.PV.EXPAND`), which T-05 needs for its multi-path tests. `M3.PV.4D` is a leaf: nothing in the chain needs numbers above 999.

## NIPUN codes

**Open.** The textbook prints no NIPUN or competency codes. Its only outcomes are the Kannada learning-outcome table on Part 1 p 92 (one per chapter, e.g. Ch 4: "ಮೊತ್ತವು 999 ಮೀರದಂತೆ … ಸಂಕಲನ ಮಾಡುವರು": "add, with the sum not exceeding 999, and apply it to everyday problems"). The `NipunOutcome` field needs a separate source (NIPUN Bharat Grade 3 numeracy goals). Until then T-05 can leave it empty; nothing in the MVP reads it.

## Rules for homework pages

- **Don't use Part 2 pp 7–8 as "no borrowing" homework.** Some sums there need borrowing (342 − 287, 780 − 712) before borrowing is taught on p 10.
- **Don't use Part 1 p 81** for no-carry homework: 815 + 424 = 1239 goes past 999.
- **Avoid Part 1 p 89 Q "which two total 250?"**: no pair in its table adds to 250.
- **Avoid Part 1 pp 5, 7 and 73**: they need vehicle plates, shop prices or a family Aadhaar number. A 5-minute task must work with only the book, and Aadhaar digits are personal data.
- **Borrowing across zero** (e.g. 302 − 157) is never taught. Generated sums for `M3.SUB.BORROW` must not need it.
- **Carrying is not staged.** The book's first carry example (365 + 248) carries twice. Generated sums can start with a single carry in the ones place, then move to double carries.
- Generated sums stay ≤ 999 for both addition sums and subtraction minuends, as in the book.

## Words to use in scripts and buddy cards

These match the book, so a child hears the same word at home and in class. Use them as the "3 Kannada key words" in FR-07.

| Kannada | English | Note |
|---|---|---|
| ಬಿಡಿ / ಹತ್ತು / ನೂರು / ಸಾವಿರ | ones / tens / hundreds / thousands | Column heads ಬಿ / ಹ / ನೂ |
| ಸ್ಥಾನಬೆಲೆ / ಮುಖಬೆಲೆ | place value / face value | |
| ಸ್ಥಾನಬೆಲೆ ಕೋಷ್ಟಕ | place-value table | Main written method for carry and borrow |
| ಬಿಡಿಸಿ ಬರೆ | write in expanded form | |
| ಕೂಡಿಸು / ಮೊತ್ತ / ಒಟ್ಟು | add / sum / total | |
| ಕಳೆ / ವ್ಯತ್ಯಾಸ / ಉಳಿದ | subtract / difference / remaining | |
| **ದಶಕ** | the carried or borrowed ten | Carry: "ದಶಕ 1 ನ್ನು ಹತ್ತರ ಸ್ಥಾನಕ್ಕೆ ಕೂಡೋಣ" (p 82). Borrow: "ದಶಕ ಪಡೆಯೋಣ" (Part 2 p 12, 15). The book never uses ಎರವಲು. |
| ತಾಳೆ ನೋಡು | check the answer | |
| ಅಬಾಕಸ್ / ಮಣಿ | abacus / bead | Used for no-carry only |

## Open

- [ ] Monika: confirm the † names with a teacher (D-14)
- [ ] NIPUN outcome codes: source not chosen
- [ ] Rahul: check the graph and build `data/curriculum/grade3-maths.json` (T-05)
