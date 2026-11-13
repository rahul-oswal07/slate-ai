# Competency list: Grade 3 maths, the whole textbook (T-81)

Spec: D-01, D-14, D-22, D-89, FR-03, FR-06, FR-07 · Data: `data/curriculum/grade3-maths.json` · Names and pages: extracted by Rahul (Claude) on 1 Nov from the textbook PDFs; † names and key words to confirm with a teacher (D-14)

## Source

Karnataka state Grade 3 maths textbook, **Kannada medium, KTBS, Part 1 and Part 2**. The PDFs are named "2026-27". Rahul chose this book on 1 Nov (D-89). It replaces the list built on 20 Oct (T-22) from a different edition, which had a Grade 2 bridge course, place value to 9999 and subtraction in Part 2.

The PDFs are not in the repo: they are marked "not to be republished", and the repo is public. Pages below are **printed** page numbers, which is what a child sees (PDF page − 10 in Part 1, − 2 in Part 2).

**Edition note:** the PDFs' own pages carry dates from 2013–2016: a 2016 calendar (Part 2 p 92), bills dated 2016 that charge VAT (p 64–66), and an attendance sheet dated 2013 (p 110). They may be a reprint of an older edition. Check against a child's book before relying on page numbers in a real class.

| Book | Chapter | Printed pages |
|---|---|---|
| Part 1 | 1. ಆಕೃತಿಗಳು (shapes) | 1–17 |
| Part 1 | 2. ಸಂಖ್ಯೆಗಳು (numbers to 999) | 18–66 |
| Part 1 | 3. ಸಂಕಲನ (addition) | 67–95 |
| Part 1 | 4. ವ್ಯವಕಲನ (subtraction) | 96–123 |
| Part 1 | 5. ಗುಣಾಕಾರ (multiplication) | 124–150 |
| Part 2 | 6. ಭಾಗಾಕಾರ (division) | 1–12 |
| Part 2 | 7. ಮಾನಸಿಕ ಲೆಕ್ಕಾಚಾರ (mental maths) | 13–25 |
| Part 2 | 8. ಭಿನ್ನರಾಶಿ ಸಂಖ್ಯೆಗಳು (fractions) | 26–44 |
| Part 2 | 9. ಹಣ (money) | 45–67 |
| Part 2 | 10. ಅಳತೆಗಳು (length, weight, capacity, time) | 68–104 |
| Part 2 | 11. ದತ್ತಾಂಶ ನಿರ್ವಹಣೆ (data handling) | 105–114 |
| Part 2 | 12. ನಮೂನೆಗಳು (patterns) | 115–142 |

## Competencies (60)

Kannada names are the book's section headings. † = my wording, where the book has no single heading for the lesson or two were joined. Each lesson in the JSON also has Telugu, Tamil and Hindi names: drafts for native review (T-26), except the 9 kept unchanged from the old list. **Practice** says how code makes the lesson's sums. A lesson marked "new" has no rule yet, so it gets **no catch-up** (FR-06, T-81): it still shows in the lesson picker, the ledger and the evening message.

Ten IDs kept from the old list mean the same lesson here: `M3.PV.HTO`, `M3.PV.EXPAND`, `M3.NUM.COMPARE` (now biggest and smallest), `M3.NUM.ORDER`, `M3.ADD.NOCARRY`, `M3.ADD.CARRY`, `M3.ADD.WORD`, `M3.SUB.NOBORROW`, `M3.SUB.BORROW`, `M3.SUB.WORD`. Removed: `M2.PV.TENS`, `M2.ADD.2D`, `M2.SUB.2D` (no bridge course), `M3.PV.4D` (numbers stop at 999), `M3.PV.FACE` and `M3.SUB.CHECK` (no section for them).

| Ch | ID | Kannada | English | Part · page | Ex | Practice |
|---|---|---|---|---|---|---|
| 1 | M3.GEO.2D | ಎರಡು ಆಯಾಮಗಳುಳ್ಳ ಆಕೃತಿಗಳು | 2-D shapes: sides, corners, diagonals | 1 · 3 | 1.1 | new: shape |
| 1 | M3.GEO.3D | ಮೂರು ಆಯಾಮಗಳುಳ್ಳ ಆಕೃತಿಗಳು | 3-D shapes: solid and hollow cylinders | 1 · 5 | 1.1 | new: shape |
| 1 | M3.GEO.TILES | ಹಾಸುಗಲ್ಲುಗಳು | Tiles that fit and do not fit | 1 · 9 | 1.2 | new: shape |
| 1 | M3.GEO.MAP | ನಕಾಶೆ ಜ್ಞಾನ | Reading a simple map | 1 · 12 | 1.3 | new: map |
| 1 | M3.GEO.DRAW | ಚುಕ್ಕಿ ಆಕೃತಿ ಮತ್ತು ಟ್ಯಾನ್‌ಗ್ರಾಮ್ † | Dot grids, straight and curved lines, tangram | 1 · 14 | 1.4 | new: shape |
| 2 | M3.NUM.COUNT999 | ಎಣಿಸೋಣ ಬಾ (0–999) | Counting to 999 | 1 · 18 | 2.1 | count, 3 digits |
| 2 | M3.NUM.NAMES | ಸಂಖ್ಯೆಗಳ ಹೆಸರು † | Number names to 999 | 1 · 28 | 2.1 | new: numberName |
| 2 | M3.PV.HTO | ಸ್ಥಾನಬೆಲೆ: ನೂರು, ಹತ್ತು, ಬಿಡಿ † | Hundreds, tens and ones | 1 · 35 | 2.2 | count, 3 digits |
| 2 | M3.PV.EXPAND | ವಿಸ್ತರಣಾ ರೂಪ | Expanded form | 1 · 42 | 2.2 | expanded, 3 digits |
| 2 | M3.NUM.SEQUENCE | ಜಿಗಿಯುತ್ತಾ ಕಲಿ | Before, after, between, skip counting | 1 · 47 | 2.3 | new: sequence |
| 2 | M3.NUM.COMPARE | ಅತಿ ದೊಡ್ಡ ಸಂಖ್ಯೆ | Biggest and smallest | 1 · 52 | 2.4 | biggest, 3 digits |
| 2 | M3.NUM.ORDER | ಏರಿಕೆ – ಇಳಿಕೆ | Ascending and descending order | 1 · 57 | 2.4 | smallest, 3 digits |
| 2 | M3.NUM.FORM | ಸಂಖ್ಯಾ ರಚನೆ | Biggest and smallest number from 3 digits | 1 · 61 | 2.5 | new: formNumber |
| 2 | M3.NUM.LINE | ಸಂಖ್ಯಾರೇಖೆ | Number line | 1 · 64 | 2.6 | new: numberLine |
| 3 | M3.ADD.QUICK | ಸುಲಭವಾಗಿ ಕೂಡುವುದು | Adding 2-digit numbers by tens and ones | 1 · 67 | – | add, 2 digits, no carry |
| 3 | M3.ADD.WORD | ಕಥಾ ಸಮಸ್ಯೆಗಳು (ಸಂಕಲನ) † | Addition word problems | 1 · 71 | 3.1 | add, 3 digits, any carry |
| 3 | M3.ADD.NOCARRY | ದಶಕ ರಹಿತ ಸಂಕಲನ | 3-digit addition without carrying | 1 · 77 | 3.2 | add, 3 digits, no carry |
| 3 | M3.ADD.CARRY | ದಶಕ ಸಹಿತ ಸಂಕಲನ | 3-digit addition with carrying | 1 · 80 | 3.2 | add, 3 digits, any carry |
| 3 | M3.ADD.MAKE | ಸಮಸ್ಯಾ ರಚನೆ (ಸಂಕಲನ) | Making sums for a given total | 1 · 89 | 3.2 | new: makeSum |
| 3 | M3.ADD.ESTIMATE | ಅಂದಾಜು ಮೊತ್ತ | Rounding to the nearest ten, estimating a sum | 1 · 91 | 3.3 | new: round |
| 4 | M3.SUB.WORD | ಕಥಾ ಸಮಸ್ಯೆಗಳು (ವ್ಯವಕಲನ) † | Subtraction word problems | 1 · 96 | 4.1 | sub, 3 digits, borrow tens |
| 4 | M3.SUB.NOBORROW | ದಶಕ ರಹಿತ ವ್ಯವಕಲನ | 3-digit subtraction without borrowing | 1 · 104 | 4.2 | sub, 3 digits, no borrow |
| 4 | M3.SUB.BORROW | ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ | 3-digit subtraction with borrowing | 1 · 108 | 4.2 | sub, 3 digits, borrow tens |
| 4 | M3.SUB.MAKE | ಸಮಸ್ಯಾ ರಚನೆ (ವ್ಯವಕಲನ) | Making sums for a given difference | 1 · 117 | 4.3 | new: makeSum |
| 4 | M3.SUB.ESTIMATE | ಅಂದಾಜು ವ್ಯತ್ಯಾಸ | Estimating a difference | 1 · 121 | 4.4 | new: round |
| 5 | M3.MUL.REPEAT | ಪುನರಾವರ್ತಿತ ಸಂಕಲನ | Multiplication as repeated addition | 1 · 124 | 5.1 | new: mul |
| 5 | M3.MUL.SIGN | ಗುಣಾಕಾರದ ಚಿಹ್ನೆ | The × sign; multiplicand, multiplier, product | 1 · 128 | 5.2 | new: mul |
| 5 | M3.MUL.TABLES | ಗುಣಾಕಾರ ಕೋಷ್ಟಕ | Tables 1 to 10 | 1 · 131 | 5.3 | new: mul |
| 5 | M3.MUL.DAILY | ನಿತ್ಯ ಜೀವನದಲ್ಲಿ ಗುಣಾಕಾರ | Multiplication word problems | 1 · 137 | 5.4 | new: mul |
| 5 | M3.MUL.2D1D | ಎರಡಂಕಿ ಸಂಖ್ಯೆಗಳ ಗುಣಲಬ್ಧ | 2-digit × 1-digit, no carrying | 1 · 141 | 5.5 | new: mul |
| 5 | M3.MUL.2D1D.CARRY | ದಶಕ ಸಹಿತವಾಗಿ ಗುಣಿಸುವುದು | 2-digit × 1-digit with carrying | 1 · 144 | 5.6 | new: mul |
| 5 | M3.MUL.2D2D | ಎರಡಂಕಿ × ಎರಡಂಕಿ ಗುಣಾಕಾರ † | 2-digit × 2-digit | 1 · 147 | 5.7 | new: mul |
| 6 | M3.DIV.SHARE | ಸಮನಾಗಿ ಹಂಚುವುದು ಮತ್ತು ಗುಂಪು ಮಾಡುವುದು | Sharing equally, making groups | 2 · 1 | 6.1 | new: div |
| 6 | M3.DIV.MULREL | ಭಾಗಾಕಾರ ಮತ್ತು ಗುಣಾಕಾರಗಳ ಸಂಬಂಧ | Division and multiplication are linked | 2 · 5 | 6.2 | new: div |
| 6 | M3.DIV.TABLES | ಮಗ್ಗಿ ಬಳಸಿ ಭಾಗಿಸು | Dividing with the tables | 2 · 8 | 6.3 | new: div |
| 7 | M3.MENT.ADDSUB | ಮನಸ್ಸಿನಲ್ಲಿ ಲೆಕ್ಕ ಮಾಡು | Adding and subtracting in your head | 2 · 13 | 7.1 | add, 2 digits, any carry |
| 7 | M3.MENT.DOUBLE | ಎರಡು ಪಟ್ಟು | Doubling (to 100) | 2 · 21 | 7.2 | new: double |
| 8 | M3.FRAC.HALF | ಅರ್ಧ | Half | 2 · 26 | – | new: fraction |
| 8 | M3.FRAC.QUARTER | ಕಾಲು ಮತ್ತು ಮುಕ್ಕಾಲು † | Quarter and three quarters | 2 · 30 | 8.1 | new: fraction |
| 8 | M3.FRAC.SYMBOL | ಅರ್ಧ, ಕಾಲು, ಮುಕ್ಕಾಲು – ಸಾಂಖ್ಯಿಕ ರೂಪ | Writing ½, ¼, ¾ | 2 · 39 | 8.2 | new: fraction |
| 9 | M3.MONEY.COINS | ರೂಪಾಯಿಯನ್ನು ಪೈಸೆಗೆ ಪರಿವರ್ತಿಸುವುದು | Coins and notes; ₹1 = 100 paise | 2 · 45 | 9.1 | new: money |
| 9 | M3.MONEY.WRITE | ಹಣವನ್ನು ಬರೆಯುವುದು † | Writing money in words and numbers | 2 · 50 | 9.2 | new: money |
| 9 | M3.MONEY.CONVERT | ರೂಪಾಯಿಗಳನ್ನು ಪೈಸೆಗಳಿಗೆ ಪರಿವರ್ತಿಸುವುದು | Rupees to paise | 2 · 52 | 9.3 | new: money |
| 9 | M3.MONEY.ADDSUB | ಹಣದ ಸಂಕಲನ ಮತ್ತು ವ್ಯವಕಲನ | Adding and subtracting money | 2 · 55 | 9.4 | new: money |
| 9 | M3.MONEY.BILL | ದರ ಪಟ್ಟಿ ಮತ್ತು ಸರಳ ರಶೀದಿ † | Price lists and simple bills | 2 · 62 | 9.5 | new: money |
| 10 | M3.MEAS.CM | ಅಳತೆ ಪಟ್ಟಿ: ಸೆಂಟಿಮೀಟರ್ † | Length in centimetres | 2 · 68 | 10.1 | new: measure |
| 10 | M3.MEAS.METRE | ಮೀಟರ್ ಅಳತೆ ಪಟ್ಟಿ | 1 m = 100 cm | 2 · 74 | 10.2 | new: measure |
| 10 | M3.MEAS.ESTIMATE | ಅಂದಾಜು ಅಳತೆ † | Estimating length | 2 · 78 | 10.3 | new: measure |
| 10 | M3.MEAS.WEIGHT | ತೂಕ | Weight and the balance | 2 · 80 | 10.4 | new: measure |
| 10 | M3.MEAS.CAPACITY | ಗಾತ್ರ | Capacity | 2 · 86 | 10.5 | new: measure |
| 10 | M3.TIME.CALENDAR | ಕಾಲ: ಕ್ಯಾಲೆಂಡರ್ † | Calendar: days, months, dates | 2 · 91 | – | new: time |
| 10 | M3.TIME.CLOCK | ಸಮಯ ಸೂಚಕ | Reading the clock (hours) | 2 · 97 | – | new: time |
| 10 | M3.TIME.ORDER | ಕಾಲಾನುಕ್ರಮ † | The day's events in order | 2 · 101 | – | new: time |
| 11 | M3.DATA.TALLY | ಗೀಟುಗಳಿಂದ ದತ್ತಾಂಶ † | Tally marks | 2 · 105 | 11.1 | new: data |
| 11 | M3.DATA.PICTO | ಚಿತ್ರಾಲೇಖ | Pictographs | 2 · 110 | 11.1 | new: data |
| 12 | M3.PAT.SYMMETRY | ಸರಳ ಸಮಮಿತಿ ಆಕೃತಿ | Symmetry | 2 · 116 | 12.1 | new: pattern |
| 12 | M3.PAT.SHAPE | ಜ್ಯಾಮಿತಿ ಮತ್ತು ಚಿತ್ರ ನಮೂನೆಗಳು † | Shape, colour and size patterns | 2 · 121 | 12.4 | new: pattern |
| 12 | M3.PAT.NUMBER | ಸಂಖ್ಯಾ ನಮೂನೆಗಳು (ಸಮ ಮತ್ತು ಬೆಸ) † | Number patterns, odd and even | 2 · 125 | 12.2 | new: sequence |
| 12 | M3.PAT.SPLIT | ಸಂಖ್ಯಾ ವಿಭಜನೆ | Splitting a number in different ways | 2 · 130 | 12.3 | new: makeSum |
| 12 | M3.PAT.TABLES | ಗುಣಾಕಾರ ಕೋಷ್ಟಕದಲ್ಲಿರುವ ಸಂಖ್ಯಾ ನಮೂನೆಗಳು | Patterns in the tables | 2 · 140 | 12.5 | new: sequence |

## Practice kinds still to build

- **Fits today (13 lessons):** counting, place value, expanded form, comparing and ordering, addition, subtraction, mental addition.
- **Number kinds code could make (22 lessons):** `mul` (7), `div` (3), `sequence` (3), `makeSum` (3), `round` (2), `double`, `formNumber`, `numberLine`, `numberName`. Start with `mul` and `div`: chapters 5–6 are a big part of the year.
- **Need pictures, not just numbers (25 lessons):** shapes, maps, fractions, money, measurement, time, data, patterns. The 3-picture check can't be scored from a number for these; they need a picture set per lesson (`data/pictures/`) or a different check.

## Notes for generated sums

- **Carrying is not staged.** The book's carry examples carry twice early on (466 + 176 = 642), so `M3.ADD.CARRY` uses `any`.
- **Borrowing across a zero is in the book** (Ex 4.2: 900 − 275), but code still never borrows across a zero. That's a safe subset; widen it with a new rule if teachers want it.
- Addition sums and subtraction minuends stay ≤ 999, as in the book.

## Words to use in scripts and buddy cards

These match the book, so a child hears the same word at home and in class. Each lesson's `keyWordsKn` lists its own; FR-07 picks 3.

| Kannada | English | Note |
|---|---|---|
| ಬಿಡಿ / ಹತ್ತು / ನೂರು | ones / tens / hundreds | Column heads of the ಸ್ಥಾನಬೆಲೆ table |
| ಮಣಿಕಟ್ಟು | abacus | Place value, Ch 2 |
| ವಿಸ್ತರಣಾ ರೂಪ | expanded form | Also a second method for adding and subtracting |
| ಸಂಕಲ್ಯ / ಸಂಕಲಕ / ಮೊತ್ತ | addend / addend / sum | Part 1 p 85 |
| ವ್ಯವಕಲ್ಯ / ವ್ಯವಕಲಕ / ವ್ಯತ್ಯಾಸ | minuend / subtrahend / difference | Part 1 p 114 |
| **ದಶಕ** | the carried or borrowed ten | ದಶಕ ಸಹಿತ / ರಹಿತ name the carry and borrow lessons |
| ಗುಣ್ಯ / ಗುಣಕ / ಗುಣಲಬ್ಧ | multiplicand / multiplier / product | Part 1 p 129 |
| ಭಾಜ್ಯ / ಭಾಜಕ / ಭಾಗಲಬ್ಧ / ಶೇಷ | dividend / divisor / quotient / remainder | Part 2 p 8 |
| ಅಂದಾಜು | estimate | Rounding to the nearest ten (Ch 3–4) |

## Nalikali demo lesson (built in T-87, D-90, D-93)

Source: **Nalikali Grade 3 Maths 2026-27, Part 1, printed page 4**, bridge course (ಸೇ.ಬಂ = ಸೇತುಬಂಧ). Read from Monika's PDFs on 13 Nov. The book's words and numbers are copied as facts; no page image goes in the repo.

| Field | Value |
|---|---|
| ID | `M3.BRIDGE.COMPARE` (in `grade3-maths.json`, chapter 0 "ಸೇತುಬಂಧ / Bridge course", `textbookPage.book` `nalikali`, both activities in `exercises`; answers computed by `ExerciseAnswers`; the closest D-89 lesson is `M3.NUM.COMPARE`, 3-digit) |
| Kannada / English | ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ / More – less – equal: comparing 2-digit numbers |
| Book | Nalikali Maths, Grade 3, 2026-27 · Part 1 · bridge course · p 4 |
| Before / after | p 3 counting forwards and backwards (ಮುಮ್ಮುಖ – ಹಿಮ್ಮುಖ ಎಣಿಕೆ) · p 5 ascending and descending order (ಏರಿಕೆ – ಇಳಿಕೆ) · p 6 place value with bundles (ಸ್ಥಾನ ಬೆಲೆ) |
| Key words | ಹೆಚ್ಚು (more) · ಕಡಿಮೆ (less) · ಸಮ (equal) · ಹೋಲಿಸು (compare) · ಅತಿ ದೊಡ್ಡ / ಅತಿ ಚಿಕ್ಕ (biggest / smallest) · ಹತ್ತರ ಗುಂಪು / ಬಿಡಿಗಳು (bundles of ten / loose ones, p 6) |
| Learning outcome | To extract: the book's outcomes table (e.g. p 92) |

**Activity 1** (ಸೂಚನೆ: ಕೊಟ್ಟಿರುವ ಸಂಖ್ಯೆಗಳಲ್ಲಿ ಅತಿ ಚಿಕ್ಕ ಸಂಖ್ಯೆಗೆ ವೃತ್ತವನ್ನು ಮತ್ತು ಅತಿ ದೊಡ್ಡ ಸಂಖ್ಯೆಗೆ ಚೌಕವನ್ನು ಹಾಕೋಣ): in each leaf, circle the smallest number and put a square round the biggest.

| Leaf | Numbers | Smallest | Biggest |
|---|---|---|---|
| 1 | 3, 38, 64, 24 | 3 | 64 |
| 2 | 36, 26, 16, 46 | 16 | 46 |
| 3 | 82, 18, 59, 41 | 18 | 82 |
| 4 | 71, 81, 65, 56 | 56 | 81 |
| 5 | 50, 90, 40, 70 | 40 | 90 |

**Activity 2** (ಸೂಚನೆ: ಕೊಟ್ಟಿರುವ ಸಂಖ್ಯೆಗಳನ್ನು ಹೋಲಿಸಿ ಹೆಚ್ಚು (>), ಕಡಿಮೆ (<), ಸಮ (=) ಚಿಹ್ನೆ ಹಾಕೋಣ): compare and write >, < or =. Answers computed, not printed in the book.

| Pair | Sign | Pair | Sign |
|---|---|---|---|
| 48 · 33 | > | 48 · 84 | < |
| 75 · 57 | > | 50 · 78 | < |
| 86 · 86 | = | 68 · 68 | = |
| 69 · 71 | < | 33 · 66 | < |
| 26 · 26 | = | 99 · 86 | > |

48 · 84 and 75 · 57 swap the digits: they test whether the child reads tens before ones.

**For today's message (D-91), proposed content:**
- **Concept** (our wording; p 4 has no explanation, the method is p 6's bundles): "To see which number is more, look at the bundles of ten first: 48 has 4 bundles, 33 has 3, so 48 is more. If the bundles are the same, look at the loose ones."
- **Activity**, parent-led: 1. You: make 48 and 33 with bundles of ten sticks and loose sticks. 2. Your child: says which is more and puts a card with >, < or = between them. 3. Make 84: which is more now, 48 or 84?
- **Picture question:** "48 ☐ 33", the child taps >, < or = (a new question type, T-90); pairs come from Activity 2; code knows the answer.
- **Video:** one checked DIKSHA link on comparing numbers (Monika to find and check).
- **Homework:** Nalikali Maths Part 1, page 4.

## Open

- [ ] D-90: the D-89 pages don't match Nalikali 2026-27; re-point all lessons after the final (T-87 did only the demo lesson; the others still use `book: textbook`)
- [ ] A teacher confirms the † names and the key words (D-14)
- [ ] Native review of the Telugu, Tamil and Hindi names (T-26)
- [ ] Check the page numbers against a child's book (edition note above)
- [ ] NIPUN outcome codes: the book prints none; `nipunOutcome` stays null (nothing reads it)
- [ ] Practice kinds for the 47 lessons without one (above)
