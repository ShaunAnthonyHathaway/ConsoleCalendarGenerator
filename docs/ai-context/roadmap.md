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
| Null list (D-58) | The second runtime change of 2.0: the two methods that take a list throw `ArgumentNullException` for `dates` when it is null, where 1.0.0 threw `NullReferenceException`. Still first, with nothing written and nothing touched. Only a null check in each of the two methods and their comments changed in the library. The migration tests recognize exactly that difference; the one recorded digest that covers null lists became an explicit comparison with the frozen source. | #15 | `042c3bc` |
| Line terminator and blank line (D-59, D-60) | The third and fourth runtime changes of 2.0, in one pull request because both change the same calls at the end of a month: the last row of days ends with the console's line terminator, where 1.0.0 wrote a literal line feed, and every month is followed by exactly one blank line, where 1.0.0 wrote two after a month ending on a Sunday. Two lines of the library. The first corrections that change text: the remaining library-side digest comparisons became explicit comparisons with the frozen source, and the original sample has an expected text for each version. | #16 | `09e4034` |
| Gregorian title (D-61) | The fifth and last runtime change of 2.0: the title of a legacy month is the current culture's name for the Gregorian month that is drawn, where 1.0.0 wrote the culture's name for the month of the same number in its own calendar. Nothing changes under a culture whose calendar is Gregorian. One changed line and two small private methods in the library. The migration tests recognize exactly that difference, in the text of title calls only; the 1.0.0 behavior is recorded against the frozen source. The consumer smoke tests now check the title under every culture of the system and print which cultures it has. | #17 | `9b99fa5` |
| Final 2.0 documentation and migration guidance | Brought every document in line with the completed implementation, while the repository stays on `2.0.0-dev` and unpublished: the README with one migration section (the five corrections, and what did not change), the changelog's "Unreleased" section as the proposed changelog of 2.0, the XML comments of the legacy class, the package's description, tags and release notes, `SECURITY.md` and `CONTRIBUTING.md`, and these notes. Classified the README's links for the later switch to the tag, and wrote the release preparation checklist and the immutable-release plan. No runtime behavior and no public API changed; the only changes to library files are comments and package metadata. | #18 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
behavior of the legacy class was unchanged in every phase before the constructor colors. With the Gregorian
title, **all five approved runtime corrections are made**; no other runtime change is approved. With the final
documentation, the work on 2.0 that is not about releasing it is done.

## Remaining sequence

The implementation of 2.0 and its documentation are complete. What is left is the release, and none of it is
runtime work. Each item is a phase or an action of its own, and each needs its own brief or approval from
Shaun before it starts.

1. **Release infrastructure and supply-chain hardening.** How the release files are produced and protected.
   What it contains is for Shaun's brief.
2. **Release-readiness audit.**
3. **Release preparation:** the final version 2.0.0, the README's links at the tag `v2.0.0`
   ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)), its text, the changelog. The
   list is the [release preparation checklist](packaging-and-releases.md#release-preparation-checklist).
4. **The candidate,** built once from the approved release commit, with its hashes recorded, and its
   validation ([D-72](decisions.md#d-72-the-release-is-200-built-once-and-published-as-the-files-that-were-validated)).
5. **Repository settings,** approved separately: private vulnerability reporting
   ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)) and whatever else the
   readiness audit names.
6. **The tag, the GitHub release and the NuGet publication,** each approved separately, of the exact files
   that were validated ([D-69](decisions.md#d-69-20-is-published-by-hand-from-the-validated-artifacts)).
7. **Verification** from nuget.org, and then, if Shaun decides it, the deprecation of 1.0.0
   ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)), and a factual handoff for the
   Campertron project.

The steps in full are in
[packaging-and-releases.md](packaging-and-releases.md#planned-20-publication-sequence).

What this sequence does and does not say:

- **Nothing is published before 2.0, and nothing before its final approval.** No 1.x release and no prerelease
  is authorized ([D-55](decisions.md#d-55-the-next-published-version-is-20)).
- Publishing 2.0 on NuGet is planned. It is **not authorized yet**. No tag, release or publication is.
- A published version is never changed. A mistake after publication means a new version.
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.
- Nothing is added to the modern API on the way, and no whole-year rendering
  ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).

## Next action

**Release infrastructure and supply-chain hardening** (item 1), and only after Shaun has scoped it. Read
[packaging-and-releases.md](packaging-and-releases.md) first, above all
[Release boundaries](packaging-and-releases.md#release-boundaries), and
[development.md](development.md#ci) for what CI is today.

What that phase starts from:

- There is one workflow, `ci.yml`. It has read-only permission, no secrets and no manual trigger, and it
  publishes nothing. Nothing in the repository can push a package, create a tag or create a release.
- The package is `2.0.0-dev`. Every pull request builds it, validates its API against 1.0.0, verifies its
  contents and installs it into separate applications, on three operating systems.
- The documents say 2.0 is complete in the repository and not published. They change again only in release
  preparation, by the checklist.
- Three questions are open, all about the release: [Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on),
  [Q-14](open-questions.md#q-14-how-is-the-publication-authenticated) and
  [Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20).

Known and not yet dealt with, none of it part of the next phase unless Shaun puts it there:

- **There is no private channel for security reports**
  ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)). Turning one on is a repository
  setting and needs Shaun.
- The repository's description on GitHub still reads as it did for 1.0.0. It is a repository setting.
- That the console writer puts the caller's colors back is not verified on Windows
  ([D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes)); the
  README says so.
- How the publication is authenticated and whether 1.0.0 is deprecated afterwards are open.

Settled and not to be reopened: the five corrections and nothing else; no renamed setter parameters, no
`[Obsolete]`, no removed legacy class; `netstandard2.1` stays; culture-derived weekday labels may differ
between operating systems, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
