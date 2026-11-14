# T0 Set-up · spec

First run: Welcome, her greeting with voice consent, approving the evening message once.

| | |
|---|---|
| Route | `/welcome`, `/welcome/greeting`, `/setup/evening`, `/setup/greeting` |
| Web | `features/setup/`, `features/evening/` |
| API | `Features/Greeting/`, `Features/EveningMessages/`, `Features/Auth/` |
| Design card (Monika) | [`T0-setup.md`](../../design/T0-setup.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T0 Onboarding | Consent, record 10-second greeting once ("Ravi, this is Lakshmi teacher…") | Record → play back → save | Recording · recorded (play / re-record / delete) · consent given · missing step · greeting deleted · mic permission denied (continue without greeting) · offline (greeting saved, uploads later) |

Every state, with copy and screenshots, is in the design card.

## Requirements

No requirement of its own. The evening message she approves here is FR-20 ([P0 Evening message](../P0-evening-message/spec.md)); sign-in and the first-run gate are D-40.

## Decisions cited

D-04, D-17, D-21, D-27, D-29, D-31, D-34, D-38, D-39, D-40, D-44, D-47, D-48, D-49, D-50, D-51, D-52, D-70.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
