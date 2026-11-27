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
| Final 2.0 documentation and migration guidance | Brought every document in line with the completed implementation, while the repository stays on `2.0.0-dev` and unpublished: the README with one migration section (the five corrections, and what did not change), the changelog's "Unreleased" section as the proposed changelog of 2.0, the XML comments of the legacy class, the package's description, tags and release notes, `SECURITY.md` and `CONTRIBUTING.md`, and these notes. Classified the README's links for the later switch to the tag, and wrote the release preparation checklist and the immutable-release plan. No runtime behavior and no public API changed; the only changes to library files are comments and package metadata. | #18 | `4a6e049` |
| Release infrastructure and supply-chain hardening | Pinned the three actions of the workflow to the full SHA of a commit. Added the machinery of a release and made CI run it on every build for the development version: the release bundle (the package, the symbol package, a reproducible archive of the assemblies, SHA-256 checksums), its check and a test that a damaged bundle is refused; a program that references an assembly of the archive directly; a program upgraded from the published 1.0.0; the substitution of the README's links, as data and as one command; and a rehearsal of release preparation that changes nothing. Decided against artifact attestations for now. No runtime behavior, public API, version or package metadata changed, and nothing was published, tagged or designated a candidate. | #19 | `99aec37` |
| Release-readiness audit | Read-only review of the merged result of the release infrastructure, reported to Shaun outside the repository. It found the repository ready for release preparation and named what later steps still need: a decision on which build is the candidate, a check of the published package that allows for nuget.org's signature, release immutability, and a credential. | none | none |
| Release preparation | Made `master` the source of the release 2.0.0: removed the version suffix; moved the three links of the README that are part of the version to the tag `v2.0.0` with the one command made for it; rewrote the README's status so that it stays true after publication; turned the changelog's section into the section of 2.0.0, with "not yet published" where the date will go; added the check of a published package, which allows for nuget.org's signature and fetches the symbols from nuget.org's symbol server, and the script that designates the candidate. No runtime code and no public API changed. Nothing was tagged, released or published. The candidate is designated after the merge, outside the repository. | #20 | see `git log` |

All of the above happened on 2026-11-27. The layout model was the first phase that added production code. The
behavior of the legacy class was unchanged in every phase before the constructor colors. With the Gregorian
title, **all five approved runtime corrections are made**; no other runtime change is approved. With the final
documentation, the work on 2.0 that is not about releasing it is done, and with the release infrastructure the
tools for releasing it are in place and run on every build.

## Remaining sequence

The implementation of 2.0, its documentation, the machinery of its release and its preparation are complete:
`master` is the source of 2.0.0. What is left is the release itself. Each item is an action of its own and
needs Shaun's separate approval before it starts.

1. **The candidate is designated,** right after the release-preparation change is merged: the Linux
   `release-bundle` artifact of the first push run for its merge commit, checked, and recorded outside the
   repository ([D-79](decisions.md#d-79-the-candidate-is-the-linux-bundle-of-the-first-push-run-for-the-release-commit)).
   Whether that has happened is in the report of that phase, not in this repository.
2. **Repository settings,** approved separately: release immutability, which has to be on before the GitHub
   release is published; private vulnerability reporting
   ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)), with `SECURITY.md`; and,
   as Shaun chooses, rulesets for `master` and for tags, secret scanning and the Actions setting that requires
   SHA pins.
3. **The tag** `v2.0.0` on the release commit.
4. **The GitHub release,** as a draft with the candidate's four files and the notes, then published.
5. **The NuGet publication** of the candidate's package and symbol package
   ([Q-14](open-questions.md#q-14-how-is-the-publication-authenticated)).
6. **Verification** with `eng/verify-published-package.sh`
   ([D-80](decisions.md#d-80-after-publication-the-served-package-is-compared-by-its-entries-and-the-symbols-through-the-symbol-server)),
   and the date in the changelog.
7. If Shaun decides it, the **deprecation of 1.0.0**
   ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)), and a factual handoff for the
   Campertron project.

The steps in full are in
[packaging-and-releases.md](packaging-and-releases.md#planned-20-publication-sequence).

What this sequence does and does not say:

- **Nothing is published.** No tag, release or publication is authorized. No 1.x release and no prerelease was
  ever published ([D-55](decisions.md#d-55-the-next-published-version-is-20)).
- **Only the candidate may be released.** Every build calls itself 2.0.0; one is the release. It is never
  rebuilt and its workflow run is never run again.
- A published version is never changed. A mistake after publication means a new version.
- Changing Campertron is not part of this roadmap. After a published 2.0 package has been verified, Campertron's
  adoption of it is handled in Shaun's separate Campertron project and chat.
- Nothing is added to the modern API on the way, and no whole-year rendering
  ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).

## Next action

**The repository settings and the tag** (items 2 and 3), and only on Shaun's separate approval of each. Read
[packaging-and-releases.md](packaging-and-releases.md) first: [The candidate](packaging-and-releases.md#the-candidate),
[Planned 2.0 publication sequence](packaging-and-releases.md#planned-20-publication-sequence) and
[Release boundaries](packaging-and-releases.md#release-boundaries).

What that starts from:

- `master` is the source of 2.0.0, and `bash eng/release-state.sh` passes on it.
- The candidate's record, with the release commit, the run and the checksums, is outside the repository: in
  the report of the phase that designated it and in the folder that phase names. Do not look for it here, and
  do not take any checksum from a CI log as the release's without that record.
- There is one workflow, `ci.yml`, with every action pinned to a commit. It has read-only permission, no
  secrets and no manual trigger, and it publishes nothing. Nothing in the repository can push a package,
  create a tag or create a release. **The workflow run that made the candidate must never be run again.**
- A commit on `master` after the release commit does not change the release: the tag belongs on the release
  commit. Such a commit is expected for `SECURITY.md` when private reporting is turned on, and for the date in
  the changelog.
- Three questions are open, all about the steps to come: [Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on),
  [Q-14](open-questions.md#q-14-how-is-the-publication-authenticated) and
  [Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20).

Known and not yet dealt with:

- **There is no private channel for security reports**
  ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)). Turning one on is a repository
  setting and needs Shaun.
- **Release immutability is off for the repository.** It is a setting, and it only protects releases published
  after it is turned on.
- `master` has no branch protection and no ruleset, and no rule protects tags.
- The repository's description on GitHub still reads as it did for 1.0.0. It is a repository setting.
- That the console writer puts the caller's colors back is not verified on Windows
  ([D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes)); the
  README says so.
- How the publication is authenticated and whether 1.0.0 is deprecated afterwards are open.

Settled and not to be reopened: the five corrections and nothing else; no renamed setter parameters, no
`[Obsolete]`, no removed legacy class; `netstandard2.1` stays; culture-derived weekday labels may differ
between operating systems, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
