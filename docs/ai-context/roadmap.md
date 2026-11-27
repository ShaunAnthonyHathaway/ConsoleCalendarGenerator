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
| Legacy facade | Rebuilt `CalendarGenerator.ConsoleCalendar` on `MonthLayout`, with its own grid algorithm removed and every published behavior kept in private code. Shown identical to the original implementation call by call. | #9 | `9e036cc` |
| Campertron parity fixtures | Recorded 47 calls of Campertron's live calendar renderer at its commit `3ded294` and added tests that reproduce them with the public modern API. Tests and documentation only; no production code changed. | #10 | `3cbeb29` |
| User-facing material | Replaced the README, the example application and the screenshots; removed the duplicated docs; added `CHANGELOG.md`, `CONTRIBUTING.md` and `SECURITY.md`; documented the legacy class in XML and removed the last warning suppression; added tests that keep the README, the example and the documentation file true. No runtime behavior or public API changed. | #11 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
legacy class is unchanged in all of them.

## Remaining sequence (planned)

In this order. Each item is one or more focused pull requests.

1. **Release-readiness validation.**
2. **1.x release decision,** and any prerelease work Shaun approves.
3. **2.0 breaking corrections.**
4. **2.0 documentation, migration guidance, release readiness, NuGet publication and verification.**

What this sequence does and does not say:

- Which version number each intermediate release gets is not fixed. It depends on evidence at the time and on
  Shaun's explicit approval. See [open-questions.md](open-questions.md).
- Publishing 2.0 on NuGet is planned. It is **not authorized yet**. See
  [packaging-and-releases.md](packaging-and-releases.md).
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.

## Next action

Recommended: **a release-readiness audit for 1.x** (item 1), ending in the decision whether a 1.1 release comes
before 2.0 (item 2), and only after Shaun has reviewed the user-facing material and scoped that phase. Writing
the documentation and validating the example found nothing in the package that needs correcting first: every
capability the README describes exists and is tested, and every example compiles against both builds. Read
[packaging-and-releases.md](packaging-and-releases.md) and [open-questions.md](open-questions.md) first. What
that phase has to deal with, none of it decided here:

- **The README is packed into the package and currently says the modern API is not published.** Its "Status"
  and "Installation" sections, the "Unreleased" heading of `CHANGELOG.md`, the supported versions in
  `SECURITY.md` and the version checks in `Documentation/PublicDocumentationTests` all have to change in the
  change that prepares a release, and not before.
- **The README's image is served from the `master` branch.** A published package would show whatever that file
  is at the time; whether to point a release at a tag is open
  ([Q-10](open-questions.md#q-10-what-do-the-links-of-a-published-readme-point-at)).
- **There is no private channel for security reports**
  ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)). Turning one on is a repository
  setting and needs Shaun.
- Whether legacy members are marked obsolete (Q-05) and what ships in 1.x (Q-07) are still open.
- The repository's description on GitHub still reads as it did for 1.0.0. It is a repository setting.
- That the console writer puts the caller's colors back is not verified on Windows
  ([D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes)); the
  README says so.
