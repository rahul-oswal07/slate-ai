# T10 Settings · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Settings for the greeting and the evening message (T-71).
- Opened from the profile menu in the header on every teacher screen (T-109).
- **Reset today**, demo class only: "Reset today" → "Yes, reset today" clears the school day's lesson, today's message, lesson audio, tonight's messages with their answers, reasons, buddy ticks and register changes, and puts the register back as the simulated SATS sent it; earlier days, children and catch-ups stay. Every screen reloads after it (T-143, D-124).
- The kn, te, ta and hi strings of Reset today are drafts for Monika (T-143).

## Known gaps and drafts

- Not built from the card: families, withdraw voice consent, help (roadmap, D-70).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T10-settings" docs/tasks/[TM]-*.md
```
