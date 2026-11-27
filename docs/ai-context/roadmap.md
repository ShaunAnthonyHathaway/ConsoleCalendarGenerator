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
| Options and text rendering | Added `MonthRenderOptions`, `MonthRenderer` and the result types `RenderedMonth`, `RenderedLine` and `RenderedSegment`: deterministic text and segments that say what each piece of text is, with no states, colors or output. | #7 | `1e71486` |
| States, themes and output | Added `DayState` with visible markers, two `Render` overloads that put states on days, `CalendarTheme`, and the writers `MonthWriter` and `ConsoleMonthWriter`. | #8 | `64995b6` |
| Legacy facade | Rebuilt `CalendarGenerator.ConsoleCalendar` on `MonthLayout`, with its own grid algorithm removed and every published behavior kept in private code. Shown identical to the original implementation call by call. | #9 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
legacy class is unchanged in all of them.

## Remaining sequence (planned)

In this order. Each item is one or more focused pull requests.

1. **Campertron parity fixtures,** inside this repository only. See [testing.md](testing.md#planned-tests).
2. **User-facing material.** Full replacement of the README, the example, the duplicated docs, the screenshots
   and the rest of the user-facing material, plus XML documentation of the legacy class. It must show explicit
   culture selection prominently, because the default culture is the invariant one
   ([D-32](decisions.md#d-32-rendering-defaults-are-fixed-and-read-nothing-from-the-process)).
3. **Release-readiness validation.**
4. **1.x release decision,** and any prerelease work Shaun approves.
5. **2.0 breaking corrections.**
6. **2.0 documentation, migration guidance, release readiness, NuGet publication and verification.**

What this sequence does and does not say:

- Which version number each intermediate release gets is not fixed. It depends on evidence at the time and on
  Shaun's explicit approval. See [open-questions.md](open-questions.md).
- Publishing 2.0 on NuGet is planned. It is **not authorized yet**. See
  [packaging-and-releases.md](packaging-and-releases.md).
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.

## Next action

Recommended: **Campertron parity fixtures** (item 1), and only after Shaun has reviewed the legacy facade and
scoped that phase. No further compatibility correction is known to be needed first: the facade matched the
original implementation in every one of 12,986 compared scenarios and byte for byte on a terminal. Read
[testing.md](testing.md#planned-tests) and
[architecture.md](architecture.md#what-campertron-needs-from-the-package) first, and re-read Campertron's
current code. Things that phase will meet, none of them decided here:

- Campertron's renderer is expected to map onto the package as: `MonthRenderOptions` with cells three wide, a
  left-aligned `"Y"` title with the legend as `TitleSuffix`, an indent, and one `DayState` whose marker is `*`;
  `CalendarTheme.GetColor` for the color of each segment. That mapping is an expectation from reading the code,
  not a tested fact; the fixtures are what test it.
- Campertron's title follows the culture's own calendar and the package's is always Gregorian
  ([D-45](decisions.md#d-45-no-public-compatibility-profile)). Fixtures under cultures with the Gregorian
  calendar are expected to match; under `fa-IR` or `th-TH` the titles differ on purpose.
- Campertron itself is never modified from this repository.
