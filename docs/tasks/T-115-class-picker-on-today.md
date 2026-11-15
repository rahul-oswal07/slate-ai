# T-115 · Class dropdown on Today for teachers in more than one class

| | |
|---|---|
| Status | Proposed · after judging |
| Issue | #226 |
| Owner | R |
| Screens | T1-today, app-shell |
| Source | Rahul, 15 Nov; size M |

## What

A teacher can belong to more than one class (`ClassTeachers`), but the app shows only one: `AuthStore.GetMeAsync` returns the first class (the one she is main teacher of comes first; "one class per teacher in the MVP") and sign-in writes that one `class_id` into the session cookie. Today gets a **class dropdown above the date**, and the whole app follows the class she picks (Today, Week, Missed, Approve, Settings, the bottom nav's badge).

**Rahul's answers (15 Nov):**
- **Class name:** a new `Class.Name` (e.g. "3A"), because two sections of the same grade and medium would look the same. Each option reads "3A · Grade 3 · Kannada". Migration, seed and [`database.md`](../database.md) change together.
- **Always shown:** the dropdown is there even with one class, with that class selected.
- **Seed:** a small second class (a few fictional children) whose main teacher is Monika, so signing in as Monika shows two classes. Shobha's demo story and Reset are unchanged (Reset deletes by class ID in `DemoStore`, so the second class survives it). Monika's default class becomes her own, by the main-teacher-first rule.

**Design (from the code, 15 Nov):**
- **Session:** sign-in writes one `class_id` claim per class she belongs to. `ClassOwnerFilter` and the `GetClassId()` checks in `DemoEndpoints`, `AudioEndpoints` and `CatchUpApprovalEndpoints` become "is she in this class". The offline outbox stores full URLs with the class in them, so marks saved offline in one class still sync after she switches to another. Claims are refreshed on `GET /api/me` if her classes changed since sign-in.
- **API:** `/api/me` adds `classes: [{ id, name, grade, medium }]`; `classId` stays as the default class.
- **Web:** the selected class is client state (localStorage, per teacher), read through one `useClassId()` hook that replaces the ~12 `useMe().data?.classId` reads; a class she is no longer in falls back to the default. Query keys already include the class ID, so switching refetches. The demo flags and the Profile screen's grade and medium follow the selected class.

## Acceptance criteria

- A teacher in two classes sees both in the dropdown above the date on Today; picking one switches every screen to that class, and the choice survives a reload.
- A teacher in one class sees the dropdown with her class selected.
- Every class endpoint accepts any class she belongs to and answers 404 for any other.
- A mark saved offline in class A reaches class A after she switches to class B.
- Shobha's demo story and Reset work as before; Monika sees two classes.
- Strings in all locale files.

## How it's tested

xUnit: claims hold every class, filter allows her classes and 404s others, `/api/me` lists classes; Vitest: dropdown with one and two classes, switching, fallback for a class she left; Db test for the migration and seed (ask before adding, per CLAUDE.md); 360 px screenshots and axe in the PR; existing e2e specs pass; `verify.sh`

## Notes

- 15 Nov: proposed by Rahul; parked until after judging, because it changes how sign-in checks a teacher's class and the finale build merges on 15 Nov (finale 18 Nov).
