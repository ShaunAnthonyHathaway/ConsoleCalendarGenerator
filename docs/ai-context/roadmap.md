# Roadmap

Where the project is and what comes next. Phases are scoped and approved by Shaun one at a time; nothing below
authorizes starting a phase.

Phase labels are not GitHub pull request numbers. The repository's first discussion took number 1, so the first
phase's pull request is #2.

## Completed

| Phase | What it did | Pull request | Merge commit |
|---|---|---|---|
| Phase 0 audit | Read-only audit of the repository, the published package and Campertron's calendar code. Produced the plan the later phases follow. | none | none |
| PR 1: CI foundation | Removed the stale GitHub Pages deploy workflow, added build CI on Linux, Windows and macOS, pinned the SDK in `global.json`. | #2 | `b5411eb` |
| Pages disabled | The repository's GitHub Pages setting was turned off (a setting change, approved by Shaun; no commit). | none | none |
| PR 2: characterization tests | Added `ConsoleCalendarGenerator.Tests`, which records the behavior of published 1.0.0. | #3 | `879d513` |
| PR 3: build and packaging foundations | Multi-targeted the library, added package metadata, symbols, package validation against 1.0.0, package verification and consumer smoke tests. | #4 | `af70943` |
| AI-maintenance documentation | Added `CLAUDE.md` and `docs/ai-context/`. | #5 | `cb55109` |
| Layout model | Added `MonthLayout`, `MonthLayoutWeek` and `MonthLayoutCell`, a pure model of a month as weeks and weekday columns for any first day of the week. | #6 | `fbf7885` |
| Options and text rendering | Added `MonthRenderOptions`, `MonthRenderer` and the result types `RenderedMonth`, `RenderedLine` and `RenderedSegment`: deterministic text and segments that say what each piece of text is, with no states, colors or output. | #7 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
legacy class is unchanged in all of them.

## Remaining sequence (planned)

In this order. Each item is one or more focused pull requests.

1. **States and output.** Day states, text markers, a theme that maps segment kinds and states to colors, and a
   `TextWriter`/console writer.
2. **Legacy facade.** `CalendarGenerator.ConsoleCalendar` reimplemented over the new code, with every
   characterization test passing unchanged.
3. **Campertron parity fixtures,** inside this repository only. See [testing.md](testing.md#planned-tests).
4. **User-facing material.** Full replacement of the README, the example, the duplicated docs, the screenshots
   and the rest of the user-facing material, plus XML documentation of the legacy class.
5. **Release-readiness validation.**
6. **1.x release decision,** and any prerelease work Shaun approves.
7. **2.0 breaking corrections.**
8. **2.0 documentation, migration guidance, release readiness, NuGet publication and verification.**

What this sequence does and does not say:

- Which version number each intermediate release gets is not fixed. It depends on evidence at the time and on
  Shaun's explicit approval. See [open-questions.md](open-questions.md).
- Publishing 2.0 on NuGet is planned. It is **not authorized yet**. See
  [packaging-and-releases.md](packaging-and-releases.md).
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.

## Next action

Begin day states, markers, the theme and output, and only after Shaun has reviewed text rendering and scoped
that phase. Read [architecture.md](architecture.md) and [open-questions.md](open-questions.md) first; Q-04 is
the question that phase has to answer.
