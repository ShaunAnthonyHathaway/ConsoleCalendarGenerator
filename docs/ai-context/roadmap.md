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
| Candidate designation | Downloaded the Linux `release-bundle` of the first push run for the release commit `117df06` (run 46083727520, first attempt), ran every check of a release on those files and kept them, read-only, with their record, outside the repository ([D-83](decisions.md#d-83-the-release-commit-is-117df06-and-master-moves-on-without-changing-the-release)). Nothing was built, tagged, released or published. | none | none |
| Repository settings and security policy | Turned on, with Shaun's approval and through GitHub's API: immutable releases, private vulnerability reporting, secret scanning with push protection, and the requirement that Actions are pinned to full commit SHAs; created a ruleset for `master` (the three CI checks, no force push, no deletion) and one for tags `v*` (no move, no deletion) ([D-82](decisions.md#d-82-repository-settings-protect-master-release-tags-and-published-releases)). `SECURITY.md` now sends reports to private vulnerability reporting and states where the release stands ([D-81](decisions.md#d-81-security-reports-go-through-githubs-private-vulnerability-reporting)). No runtime code, package, workflow, tag, release or publication changed; the candidate is untouched. | #21 | see `git log` |
| Tag `v2.0.0` | Created the annotated, unsigned tag `v2.0.0` on the release commit `117df06` and pushed that one ref, with Shaun's approval. Nothing else changed. | none | none |
| GitHub release 2.0.0 | Created a draft release for the tag with the candidate's four files, verified the downloaded files byte for byte, and published it. It is immutable and marked Latest, and GitHub's release attestation verifies. Nothing was pushed to NuGet. | none | none |
| Public presentation and 2.0.1 | Shaun rejected the public wording of 2.0.0 after its GitHub release, before any NuGet publication. The release stays as it is and 2.0.0 is not pushed to NuGet ([D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget)). This phase rewrote everything a user reads, from scratch: the README, the changelog, `SECURITY.md`, `CONTRIBUTING.md`, the package description and tags, the XML comments of `ConsoleCalendar`, the example program and the notes of the release ([D-84](decisions.md#d-84-public-text-is-positive-direct-and-about-the-library), [D-86](decisions.md#d-86-a-release-has-written-notes-and-the-readme-names-its-version)). It set the version to 2.0.1 with assembly version 2.0.0.0 and file version 2.0.1.0, and redesigned the documentation tests to check facts and the rejected phrases. No runtime code and no public API changed. | #22 | see `git log` |
| First candidate of 2.0.1 | Designated the Linux `release-bundle` of the first push run (46094403379) for the merge commit of #22, `097eace`, with every check of a release passing. It was superseded before any tag by the wording changes below and is kept, unreleased, as a record. | none | none |
| Editorial review and wording changes | A read-only review of everything public in that candidate approved the presentation and named ten passages that could be better. Shaun approved all ten: four in the README, one in the XML comments of `ConsoleCalendar`, the table of `SECURITY.md`, the order and one sentence of the release notes, and three in `CONTRIBUTING.md`, which no longer links to the maintenance notes. The repository's description on GitHub was set to the sentence that describes the 2.x library. No runtime code and no public API changed; the version stays 2.0.1 ([D-87](decisions.md#d-87-the-release-commit-of-201-is-the-merge-of-the-final-wording-changes)). | #23 | see `git log` |
| Candidate of 2.0.1 | Designated the Linux `release-bundle` of the first push run (46097347304) for the merge commit of #23, `bff3ee1`, with every check of a release passing. It is kept, read-only, with its record, outside the repository, beside the superseded one. Nothing was built, tagged, released or published. | none | none |
| Tag `v2.0.1` | Created the annotated, unsigned tag `v2.0.1` on the release commit `bff3ee1` and pushed that one ref, with Shaun's approval. Nothing else changed. | none | none |
| GitHub release 2.0.1 | Created a draft release for the tag with the candidate's four files and the written notes, verified the downloaded files byte for byte, and published it. It is immutable and marked Latest in place of 2.0.0, which is otherwise as it was, and GitHub's release attestation verifies. Nothing was pushed to NuGet. | none | none |
| NuGet Trusted Publishing | Set up the publication of 2.0.1 to NuGet, without publishing: a workflow that is started only by hand and publishes the files of the GitHub release as they are (`publish-nuget.yml`); the scripts that check everything before the push and what nuget.org serves after it, with a self-test that shows each wrong case refused; the GitHub environment `nuget-production`; and one Trusted Publishing policy on nuget.org, for that workflow and environment and for new versions of this package only ([D-88](decisions.md#d-88-201-goes-to-nuget-through-trusted-publishing-from-a-workflow-that-is-started-by-hand), [D-89](decisions.md#d-89-the-workflow-publishes-one-approved-release-and-its-facts-are-in-the-repository), [D-90](decisions.md#d-90-the-environment-allows-the-default-branch-asks-its-reviewer-and-holds-no-secret)). No API key was created and no secret is stored. These notes were brought up to date with the tag and the GitHub release of 2.0.1. No runtime code, public API, package or release changed, and the workflow was not started. | #24 | see `git log` |

All of the above happened on 2026-11-27, except the setup of the NuGet publication, on 2026-11-28. The layout
model was the first phase that added production code. The
behavior of the legacy class was unchanged in every phase before the constructor colors. With the Gregorian
title, **all five approved runtime corrections are made**; no other runtime change is approved. Version 2.0.0
holds that work and is released on GitHub. Version 2.0.1 is the same library with its public presentation
rewritten. It is released on GitHub as well, and is the version that goes to NuGet.

## Remaining sequence

2.0.1 is released on GitHub: its candidate was designated, the tag `v2.0.1` is on the release commit
`bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd`, and its GitHub release is published, immutable and marked Latest.
What is left is NuGet. Each item is an action of its own and needs Shaun's separate approval before it starts.

1. **The NuGet publication:** one manual run of `publish-nuget.yml` for `master`, with the four values of the
   approved publication. It checks everything, waits for the review of its environment, publishes the package
   and the symbol package of the GitHub release as they are, and then verifies what nuget.org serves
   ([packaging-and-releases.md](packaging-and-releases.md#publication-to-nugetorg),
   [D-80](decisions.md#d-80-after-publication-the-served-package-is-compared-by-its-entries-and-the-symbols-through-the-symbol-server)).
2. **Finalizing on `master`:** the date in the changelog and the table in `SECURITY.md`.
3. If Shaun decides it, the **deprecation of 1.0.0**
   ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)), and a factual handoff for the
   Campertron project.

The steps in full are in
[packaging-and-releases.md](packaging-and-releases.md#planned-20-publication-sequence).

What this sequence does and does not say:

- **2.0.0 and 2.0.1 are released on GitHub and stay exactly as they are.** Neither release, none of their
  files and neither tag is ever edited, replaced, deleted or recreated, and the package of 2.0.0 is never
  pushed to NuGet ([D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget)).
- **nuget.org has 1.0.0 and nothing else.** The publication workflow exists and has not run, and that it
  exists authorizes nothing: starting it is the publication, and needs its own approval. No 1.x release and no
  prerelease was ever published ([D-55](decisions.md#d-55-the-next-published-version-is-20)).
- **Only the candidate is published.** The workflow downloads the files of the GitHub release, which are the
  candidate's, and accepts no others. Nothing is rebuilt, and the candidate's workflow run is never run again.
- A released version is never changed. A mistake after a release means a new version, as with 2.0.0. If a
  publication fails after the push, nothing is pushed again or taken back without Shaun's decision.
- 1.0.0 stays on NuGet and supported until 2.0.1 is published and verified.
- Changing Campertron is not part of this roadmap. After a published 2.x package has been verified,
  Campertron's adoption of it is handled in Shaun's separate Campertron project and chat.
- Nothing is added to the modern API on the way, and no whole-year rendering
  ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).

## Next action

**The NuGet publication of 2.0.1** (item 1), only on Shaun's explicit approval to start the workflow. Read
[packaging-and-releases.md](packaging-and-releases.md) first:
[Publication to nuget.org](packaging-and-releases.md#publication-to-nugetorg),
[After publication](packaging-and-releases.md#after-publication-what-is-checked-and-what-cannot-be) and
[Release boundaries](packaging-and-releases.md#release-boundaries).

What that starts from:

- `master` is the source of 2.0.1, and `bash eng/release-state.sh` passes on it. The release commit is behind
  the head of `master`; the commits after it changed nothing that is in the package.
- The candidate's four files are the files of the GitHub release. Their SHA-256 are in `eng/publication.sh`.
  The candidate's record is outside the repository, in the folder the phase that designated it names, beside
  the folder of 2.0.0 and the folder of the superseded candidate.
- `bash eng/publication-preflight.sh` with the four values of the publication is the dry run: it passes when
  everything is ready, and it publishes nothing. The command is in `CLAUDE.md`.
- The repository settings of a release are made and recorded in
  [development.md](development.md#repository-settings): a published release is immutable, `master` takes
  changes only through a pull request with the three checks green, a tag `v*` cannot be moved or deleted, and
  the environment `nuget-production` lets in `master` alone and asks its reviewer.
- nuget.org has one Trusted Publishing policy, for this repository, the workflow file `publish-nuget.yml` and
  that environment. It was active when it was created. If nuget.org ever shows it otherwise, that is for
  Shaun to look at on nuget.org: do not create a second policy.
- There are two workflows, with every action pinned to a commit. `ci.yml` has read-only permission, no secrets
  and no manual trigger, and publishes nothing. `publish-nuget.yml` is started only by hand. **A workflow run
  that made a candidate must never be run again, and neither must a run of the publication workflow.**
- One question is open, about what follows the publication:
  [Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20).

Known and not yet dealt with:

- **`SECURITY.md` and the changelog state the present.** They are edited on `master` when 2.0.1 is on
  nuget.org: the rows of the table, and the date in the heading.
- **The publication workflow has never run.** Its checks were run against the real release as a dry run, and
  against recorded answers in which one thing at a time is wrong. Its login, its push and its verification of
  a 2.x package that nuget.org serves happen for the first time when 2.0.1 is published.
- No force push was tried on `master`, no tag was moved or deleted to try the tag ruleset, and no run was let
  into the environment: the rulesets and the environment are known from what GitHub's API returns for them,
  and the required checks from pull requests.
- That the console writer puts the caller's colors back is not verified on Windows
  ([D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes)). The
  README no longer says so; it is a fact about the tests, recorded here.
- Whether 1.0.0 is deprecated afterwards is open.

Settled and not to be reopened: the five corrections and nothing else; no renamed setter parameters, no
`[Obsolete]`, no removed legacy class; `netstandard2.1` stays; culture-derived weekday labels may differ
between operating systems, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
