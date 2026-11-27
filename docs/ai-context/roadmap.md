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
| User-facing material | Replaced the README, the example application and the screenshots; removed the duplicated docs; added `CHANGELOG.md`, `CONTRIBUTING.md` and `SECURITY.md`; documented the legacy class in XML and removed the last warning suppression; added tests that keep the README, the example and the documentation file true. No runtime behavior or public API changed. | #11 | `59e4b45` |
| Deterministic quick start | Settled Q-12 in documentation: the README's quick start and the example program supply their weekday labels and print the same text on Linux, Windows and macOS; one example keeps the culture's own labels and says they depend on the system. Example, README, tests and these notes only. | #12 | `083924f` |
| 2.0 scope and readiness audit | Read-only review of where the project stood, what 2.0 should contain and what a release needs. Delivered to Shaun outside the repository; what he approved of it is recorded as D-56 to D-69. | none | none |
| 2.0 groundwork | Replaced the 1.x compatibility rules with the 2.0 migration policy; changed the development version to `2.0.0-dev` and the assembly and file version to `2.0.0.0`; froze the source of 1.0.0 in the tests as a historical reference; added the migration tests that compare the library with it. No runtime behavior and no public API changed, and none of the five corrections was made. | #13 | `09ffd05` |
| Constructor colors (D-57) | The first runtime change of 2.0: in the four constructors that take colors, each argument colors the element it is named for. Only the assignments and the comments of those constructors changed in the library. The migration tests recognize exactly that difference from 1.0.0; the 1.0.0 behavior stays recorded against the frozen source. Also defined one exact scenario for running the original sample, and a check that a program compiled against 1.0.0 runs with the new library unrebuilt. | #14 | `fcc6d5c` |
| Null list (D-58) | The second runtime change of 2.0: the two methods that take a list throw `ArgumentNullException` for `dates` when it is null, where 1.0.0 threw `NullReferenceException`. Still first, with nothing written and nothing touched. Only a null check in each of the two methods and their comments changed in the library. The migration tests recognize exactly that difference; the one recorded digest that covers null lists became an explicit comparison with the frozen source. | #15 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
behavior of the legacy class was unchanged in every phase before the constructor colors.

## Remaining sequence

The 2.0 implementation sequence, in this order. Each item is one focused pull request, and each needs its own
brief from Shaun before it starts.

The order of the corrections is the one Shaun approved when he reviewed the groundwork: the constructor colors
and the null list, which are done; then the line terminator and the blank line, together; then the Gregorian
title.

1. **The line terminator of the last row and one blank line after every month,** in one pull request
   ([D-59](decisions.md#d-59-the-last-row-of-a-month-ends-with-the-consoles-line-terminator),
   [D-60](decisions.md#d-60-every-month-is-followed-by-exactly-one-blank-line)). Both are about how a month
   ends, in the same few lines of the class.
2. **Gregorian titles in the legacy class** ([D-61](decisions.md#d-61-legacy-titles-name-the-gregorian-month)).
3. **2.0 documentation and migration guidance, and release readiness:** the final README, changelog and
   migration notes, the links to the tag `v2.0.0`
   ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)), the final version number, and
   a release-readiness audit.
4. **Repository settings,** as a phase Shaun approves separately: private vulnerability reporting
   ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)) and whatever else the
   readiness audit names.
5. **Publication and verification:** a manual push of the exact validated artifacts
   ([D-69](decisions.md#d-69-20-is-published-by-hand-from-the-validated-artifacts)), each step approved
   separately, then verification from nuget.org and a factual handoff for the Campertron project.

Items 3 to 5 are the planned release steps of
[packaging-and-releases.md](packaging-and-releases.md#planned-20-publication-sequence).

What this sequence does and does not say:

- **Nothing is published before 2.0, and nothing before its final approval.** No 1.x release and no prerelease
  is authorized ([D-55](decisions.md#d-55-the-next-published-version-is-20)).
- Publishing 2.0 on NuGet is planned. It is **not authorized yet**. No tag, release or publication is.
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.
- Nothing is added to the modern API on the way, and no whole-year rendering
  ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).

## Next action

**The line terminator and the blank line, together** (item 1,
[D-59](decisions.md#d-59-the-last-row-of-a-month-ends-with-the-consoles-line-terminator) and
[D-60](decisions.md#d-60-every-month-is-followed-by-exactly-one-blank-line)), and only after Shaun has scoped
it. Read [compatibility.md](compatibility.md), above all
[How a correction is made](compatibility.md#how-a-correction-is-made), and
[testing.md](testing.md#the-migration-tests) first. The pull requests for the constructor colors and for the
null list are the two worked examples: one states its correction as an equivalent use of 1.0.0, the other as a
change of the observation.

What that pull request starts from:

- Apart from the constructor colors and the exception for a null list, `master` behaves exactly as 1.0.0 does.
  `LegacyMigrationTests` requires it, and fails as soon as a month ends differently, until each of the two
  corrections is given its function in `Migration/ApprovedCorrection.cs`. Both are changes of the observation:
  no use of 1.0.0 ends a month the corrected way. They are two decisions and stay two entries, applied in
  order, although one pull request implements them.
- This is the first correction that changes text, and it changes the end of every month drawn. A scratch run of
  all five corrections together during the groundwork failed, among others, tests in `Legacy/LineEndingTests`,
  `Legacy/GenerateYearTests` and `Facade/FacadeCompatibilityTests` and the published text digest. The helper
  `Support/LegacyReference`, which builds the expected text of a month for many legacy tests, encodes both
  1.0.0 behaviors and has to be looked at first.
- The three library-side digests that are left cannot survive it
  ([D-71](decisions.md#d-71-a-recorded-digest-is-never-bent-to-let-a-correction-through),
  [testing.md](testing.md#when-a-correction-changes-a-recorded-digest)).
- The original sample draws months that end on a Sunday, so what it writes changes:
  `eng/consumer-smoke/OriginalSample/expected-text.txt` is split into what 1.0.0 writes and what 2.0 writes
  ([packaging-and-releases.md](packaging-and-releases.md#the-original-sample)), and the comparison of the raw
  terminal bytes with the published 1.0.0, which today allows color sequences to differ, has to allow exactly
  the new difference as well.
- The example program draws March 2027 with the legacy class. It ends on a Wednesday, so its text is not
  expected to change where the console's terminator is a line feed; on Windows its last row will end
  differently. Check the example's expected output and the README's legacy output block on all three systems.
- The README's "Changes from 1.0.0" section and the "Breaking changes" section of `CHANGELOG.md` get their
  entries. `Historical/` is not touched.

What the later items have to deal with, none of it to be done earlier:

- **The README is packed into the package and currently says the modern API is not published** and that the
  legacy API works exactly as in 1.0.0. Its "Status", "Installation" and "Legacy API" sections, the
  "Unreleased" heading of `CHANGELOG.md`, the supported versions in `SECURITY.md` and the version checks in
  `Documentation/PublicDocumentationTests` change with the corrections they describe or in the change that
  prepares the release, and not before.
- **The class-level XML remarks of `ConsoleCalendar` still speak of what "every 1.x version" does.** That is
  not false, but it no longer describes the assembly it ships in. The correction pull requests change only the
  comments of what they correct; the remarks are rewritten with the final documentation.
- **The README's image and file links point at the `master` branch** until the change that prepares the release
  switches them to the tag ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)).
- **There is no private channel for security reports**
  ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)). Turning one on is a repository
  setting and needs Shaun.
- The repository's description on GitHub still reads as it did for 1.0.0. It is a repository setting.
- That the console writer puts the caller's colors back is not verified on Windows
  ([D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes)); the
  README says so.
- How the publication is authenticated and whether 1.0.0 is deprecated afterwards are open
  ([Q-14](open-questions.md#q-14-how-is-the-publication-authenticated),
  [Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)).

Settled and not to be reopened: the five corrections and nothing else; no renamed setter parameters, no
`[Obsolete]`, no removed legacy class; `netstandard2.1` stays; culture-derived weekday labels may differ
between operating systems, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
