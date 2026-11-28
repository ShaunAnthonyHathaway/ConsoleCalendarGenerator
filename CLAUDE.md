# ConsoleCalendarGenerator

Entry point for coding agents. This file is deliberately short; the detail is in [docs/ai-context](docs/ai-context/).

## What this is

A small .NET library, published on NuGet as `ConsoleCalendarGenerator`, that draws month and year calendars in
console applications. It is .NET only.

## Current state

- **Released:** `ConsoleCalendarGenerator` 1.0.0 on NuGet (June 2023); 2.0.0 on GitHub Releases only
  (2026-11-27); and 2.0.1 on GitHub Releases (2026-11-27) and on NuGet (2026-11-28). **nuget.org has 1.0.0
  and 2.0.1, both listed. 2.0.1 is the current version, and 1.0.0 is deprecated there since 2026-11-28.**
  - **2.0.0 is on GitHub only and is never pushed to NuGet:** its public documentation was rejected after the
    GitHub release was published, so its package is not authorized for NuGet (D-85). No runtime or API defect
    was involved. Its release is published and immutable, and its tag `v2.0.0` is on
    `117df06e5b6d684da6891cd3ac0310e5aa81ec98`.
  - **2.0.1 is released on GitHub and published on NuGet,** and both are verified. Its GitHub release (id
    461246889) is published, immutable and marked Latest, with the four files of the candidate, and its tag
    `v2.0.1` (annotated, tag object `248b5eb0c643e65e3565fd6a01d77d37b84e7868`) is on the release commit
    `bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd`. Its package is
    <https://www.nuget.org/packages/ConsoleCalendarGenerator/2.0.1>: the candidate's package, to which
    nuget.org added its signature and nothing else.
  - **1.0.0 is deprecated on NuGet and stays listed and downloadable** (D-92). With Shaun's approval it was
    deprecated on 2026-11-28: reason Legacy, alternate `ConsoleCalendarGenerator` 2.0.1, and the message
    "Version 2.0.1 is the current supported release and retains the original ConsoleCalendar API." It was
    not unlisted and not deleted: projects that use it restore it as before, and its consumers are shown the
    notice. It is no longer supported.
  - **Never edit, replace, delete or recreate either release, its four files or its tag.** From the tag on,
    any change to what the package contains is a new version.
- **`master` is the source of the release `2.0.1`,** which replaces 2.0.0 as the 2.x version for NuGet: package
  version `2.0.1`, assembly version `2.0.0.0` as for all of 2.x, file version `2.0.1.0`, no version suffix.
  Its runtime behavior and public API are those of 2.0.0; what changed is the public presentation. Every build
  of it says it is 2.0.1, and **no build is the release.** The release is the **candidate**, the bundle the
  Linux job of the first push run for the release commit uploaded (D-79, D-87): it was designated, and its
  four files are the files of the GitHub release. `master` has moved past the release commit, and a commit
  after it changes nothing of the release. An earlier candidate, built from `097eace`, was superseded before
  any tag and is never released. The candidate's record is kept outside the repository. The SHA-256 of its
  four files are in `eng/publication.sh`, as the facts the publication checks (D-89). Never build, name or
  substitute another.
- **The publication of 2.0.1 to NuGet is done** (D-88, D-91). `.github/workflows/publish-nuget.yml` ran once,
  on 2026-11-28, with Shaun's approval: run 46137844158, started by hand from `master`, in its first attempt.
  It published the package and the symbol package of the GitHub release 2.0.1, those exact files, through
  NuGet Trusted Publishing, and then verified what nuget.org serves. No API key exists and no secret is
  stored. **That run is never run again, and the workflow is never started again for 2.0.1:** the version is
  published, and a published version is never pushed twice. The workflow publishes 2.0.1 and nothing else; a
  later version needs it changed in a reviewed pull request, and then Shaun's explicit approval to start it,
  which the existence of the workflow never is. Its publishing job uses the GitHub environment
  `nuget-production`, and nuget.org has one Trusted Publishing policy, which names this repository, that
  workflow file and that environment and allows new versions of `ConsoleCalendarGenerator` only. See
  [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#publication-to-nugetorg).
- **Production code:** the legacy class and the modern API, in one assembly. The modern API is everything in the
  `ConsoleCalendarGenerator` namespace.
  - The legacy class `CalendarGenerator.ConsoleCalendar`, in `ConsoleCalendarGenerator/ConsoleCalendar.cs`. It
    is a compatibility facade: it takes the position of every day from `MonthLayout` and keeps everything else
    about how 1.0.0 draws a month (title, colors, the exact console calls, exceptions) in private code.
    Version 2.0 corrects five of its behaviors (rule 2). **All five are implemented: the constructor colors
    (D-57), the exception for a null list (D-58), the line terminator of the last row (D-59), the one blank
    line after every month (D-60) and the Gregorian title (D-61).** In everything else its behavior is
    identical to published 1.0.0, which the tests verify.
  - The layout model, in `ConsoleCalendarGenerator/Layout/`: `MonthLayout`, `MonthLayoutWeek` and
    `MonthLayoutCell`. It places the days of a month in weeks and weekday columns and does nothing else.
  - Text rendering, in `ConsoleCalendarGenerator/Rendering/`: `MonthRenderOptions`, `MonthRenderer` and the
    result types `RenderedMonth`, `RenderedLine` and `RenderedSegment`. It turns a month into lines of text
    made of segments that say what each piece of text is. It writes nothing and has no colors.
  - Day states, in `ConsoleCalendarGenerator/States/`: `DayState`, a name and a visible marker that
    `MonthRenderer` puts on chosen days.
  - The theme, in `ConsoleCalendarGenerator/Themes/`: `CalendarTheme`, which maps segments and state names to
    optional console colors and does nothing else.
  - Output, in `ConsoleCalendarGenerator/Output/`: `MonthWriter` for any `TextWriter`, and `ConsoleMonthWriter`,
    the only modern type that uses the console.
- **Phase:** the foundations (CI, characterization tests, build and packaging, this documentation), every layer
  of the modern API for a single month, the legacy facade and the Campertron parity fixtures are complete. The
  fixtures (in `ConsoleCalendarGenerator.Tests/Parity/Campertron/`) show that the public modern API reproduces
  Campertron's live calendar renderer; no production code was needed for that. The user-facing material is
  complete as well (see the next point). The 2.0 groundwork is complete: the 2.0 policy below, the version
  identity, and the two things in the next point. All five corrections are done; no further runtime change
  is approved. The final 2.0 documentation is done as well: the README with its migration section, the
  changelog, the XML comments and the package's description. The release infrastructure is done too: CI's
  actions are pinned to commits, and every build makes and checks the four files of a release, uses the
  assemblies directly, upgrades a program from the published 1.0.0, rehearses the check that follows
  publication, and checks that the repository agrees with itself about its version. Release preparation for
  2.0.1 is done: the version, the README's links at the tag `v2.0.1`, the changelog's sections and the notes
  of the release. The candidate, the tag, the GitHub release and the NuGet publication of 2.0.1 are done and
  verified, and the documents were brought up to date afterwards. One file of the repository can publish, the
  publication workflow, and only when it is started by hand; no script can.
- **The 1.0.0 reference and the migration tests (test code only):**
  `ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs` is the source of 1.0.0, frozen.
  **Never edit it**; a test compares it byte for byte with the original. The tests in
  `ConsoleCalendarGenerator.Tests/Migration/` run the library and that source side by side and require the
  library to do what 1.0.0 did, apart from the five corrections.
- **Public documents:** `README.md` (also the readme of the package), `CHANGELOG.md`, `CONTRIBUTING.md` and
  `SECURITY.md` at the root, the notes of the release in `docs/release-notes/2.0.1.md`, and one image in
  `docs/images/`. Every public type and member has XML documentation, and no warning is suppressed. The code
  in the README is copied from `ConsoleCalendarGeneratorExample/Examples.cs` and its output from
  `expected-output.txt` beside it; the tests in `ConsoleCalendarGenerator.Tests/Documentation/` compare them,
  so change them together. **The README is packed into the package and can never be edited there.** It says
  which version it documents, gives the installation command with that version, and says nothing about whether
  the version is published. Three of its links are at the tag `v2.0.1`, which exists, and resolve. The
  changelog's heading for 2.0.1 has the date of its publication to NuGet, 2026-11-28, a date in Shaun's time
  zone like every date of the project. What is finalized after a publication, and was for this one, is in
  [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#what-is-finalized-after-publication).
- **How public text is written (D-84).** Shaun rejected the public wording of 2.0.0 and set the rule: positive,
  direct, concise, confident, about what the library does for its user. In public,
  `CalendarGenerator.ConsoleCalendar` is "the original API", never "legacy", and nothing judges it or version
  1.0.0: no "got wrong", "corrects", "broken", "flawed"; version 2 "updates five observable behaviors". The
  types of the `ConsoleCalendarGenerator` namespace are "the structured rendering API". Nothing public
  narrates how the project is run: no release state ("not yet published", "if the command gives you"), no
  candidate, approval, audit, decision or pull request number, test count, tooling, Campertron or Python.
  `PublicDocumentationTests` checks the exact rejected phrases; the tone is judged by reading. These notes
  keep "legacy class" and "modern API" as internal names, and nothing in them is public text: no public
  document links to `CLAUDE.md` or `docs/ai-context/`, and a test fails on such a link.
- **Weekday labels and operating systems (decided, D-54):** labels taken from a culture are the day names .NET
  has on the machine, and they differ: for `en-US`, two letters on Linux and macOS, one letter on Windows. That
  stays as it is. Labels supplied with `WithWeekdayLabels` are the same everywhere, and the README's quick
  start and every example supply theirs, so they print the same text on all three systems. Do not add a label
  mode or change `WeekdayLabelStyle` for this.
- **Publication (decided, D-55, D-85, D-88, D-91 and D-92):** no 1.x release and no prerelease exists or is
  authorized. 2.0.1 is published and verified, and 1.0.0 is deprecated in its favor and stays listed. **The
  release program of ConsoleCalendarGenerator 2.0.1 is complete,** and no further publication is planned or
  authorized. Nothing in the repository deprecates, unlists or deletes a version.
- **Repository settings (made 2026-11-27, D-82):** a published release is immutable; private vulnerability
  reporting is on, and `SECURITY.md` sends reports there (D-81); secret scanning and push protection are on;
  Actions must be pinned to full commit SHAs; a ruleset on `master` requires the three CI checks by their
  exact names (`Build (ubuntu-latest)`, `Build (windows-latest)`, `Build (macos-latest)`) and blocks force
  pushes and deletion; a ruleset on tags `v*` blocks moving and deleting a tag. So every change reaches
  `master` through a pull request with the three checks green, and renaming a CI job stops every merge until
  the ruleset is changed. The repository's description on GitHub was set, with Shaun's approval, to the
  sentence that describes the 2.x library (D-87). Since 2026-11-28 there is one environment,
  `nuget-production` (D-88): Shaun is its required reviewer, only `master` may deploy to it, administrators
  cannot bypass it, and it holds no secret and one variable, the nuget.org user name. The record is in
  [development.md](docs/ai-context/development.md#repository-settings).
- **`SECURITY.md` states the present,** unlike the README: the 2.x line is on GitHub Releases and on NuGet and
  is supported; 1.0.0 is on NuGet, deprecated, and not supported. Its table names no patch version of 2.x. It
  is on `master` only. It is the one public document that states the deprecation: as a fact, in its table,
  with no reason and no judgment of 1.0.0 (D-84).
- **Next phase:** none. The release program is complete and no question is open. Future work begins only
  through a roadmap item that Shaun scopes and approves: do not start a new version, and do not plan or
  prepare Campertron's adoption from here. Trusted Publishing stays configured for a later release, and the
  publication workflow stays manual and written for 2.0.1. See [roadmap.md](docs/ai-context/roadmap.md).

| Project | Path | Target frameworks |
|---|---|---|
| Library (the package) | `ConsoleCalendarGenerator/` | `net10.0;netstandard2.1` |
| Example | `ConsoleCalendarGeneratorExample/` | `net10.0` |
| Tests | `ConsoleCalendarGenerator.Tests/` | `net11.0` |

## Commands

Run from the repository root. The SDK is pinned by `global.json` (11.0.100, later 11.0 feature bands allowed).

```bash
dotnet restore ConsoleCalendarGenerator.sln
dotnet build ConsoleCalendarGenerator.sln --configuration Release --no-restore -warnaserror

# Tests against the library's net10.0 build, then the terminal tests (Linux), then the netstandard2.1 build.
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory!=Terminal"
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory=Terminal"
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-restore -p:LibraryTargetFramework=netstandard2.1 -warnaserror --filter "TestCategory!=Terminal"

# Pack (this also validates the API against published 1.0.0), then check the result.
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/verify-package.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package            # add --native-aot where a native toolchain is installed
bash eng/upgrade-smoke.sh artifacts/package             # a program upgraded from the published 1.0.0
bash eng/release-state.sh                               # the repository agrees with itself about its version

# The four files of a release. Needs a build made with -p:ContinuousIntegrationBuild=true and a checkout with
# nothing uncommitted. What this makes is a bundle, never the candidate.
bash eng/release-bundle.sh artifacts/package artifacts/release-bundle
bash eng/release-bundle-tamper-test.sh artifacts/release-bundle
bash eng/direct-reference-smoke.sh artifacts/release-bundle

# The checks of the NuGet publication as a dry run. It reads GitHub and nuget.org and compares; it needs gh and
# jq. Since 2.0.1 is published it stops at its third stage, as it must: nuget.org lists the version. The second
# line shows that the checks refuse every wrong case, and uses no network.
bash eng/publication-preflight.sh --version 2.0.1 --tag v2.0.1 --release-id 461246889 --release-commit bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd
bash eng/publication-preflight-selftest.sh
```

The build must finish with 0 warnings. None of these commands publishes anything, and none of them starts the
publication workflow: that is never a command to run in passing.

## Rules

1. **Look before acting.** Check `git status`, the current branch and recent history, and read the files you are
   about to change. If the repository and these documents disagree, the repository is right: fix the document in
   the same pull request.
2. **2.0 corrects exactly five behaviors of `CalendarGenerator.ConsoleCalendar`, and everything else about it is
   protected.** The five (D-57 to D-61): constructor arguments color the element they are named for (done); a
   null list throws `ArgumentNullException` for `dates`, still before the month and year are checked (done); the last
   row of a month ends with the console's line terminator (done); every month is followed by exactly one blank
   line (done); the title names the Gregorian month (done). All five are made, and there is no sixth. Every
   other output, console call, color, exception and side effect stays as in 1.0.0. See
   [compatibility.md](docs/ai-context/compatibility.md).
3. **An approved difference is made with all four of these, or not at all:** a decision in `decisions.md`, a
   focused pull request for that one difference, a migration test, and changelog and migration documentation.
   Do not implement, test as done or document as done a correction before its own pull request. A test that
   pins 1.0.0 behavior which a correction is about changes only in the pull request for that correction; the
   1.0.0 side of it then lives in `Historical/`. A recorded digest is never recorded again or bent to let a
   correction through (D-71).
4. **No other breaking change.** No public signature and no existing parameter name changes (the setters keep
   `TitleColor`), nothing is removed, nothing is marked obsolete. The modern API is unchanged for 2.0: nothing
   added, removed or changed. Package validation against 1.0.0 stays on with no suppression. What 1.0.0 did
   stays recorded: the frozen source and the tests in `Historical/` are never changed to match the library.
5. **Publication needs Shaun's explicit approval, each time.** That covers tags, GitHub releases, NuGet
   publication, any manual workflow dispatch or rerun that could publish, publication recovery, and repository
   settings. A published version is never changed, and what is published is the exact files that were
   validated, never a rebuild (D-72). **The candidate is one bundle and no other** (D-79): the Linux artifact
   of the first push run for the release commit, in its first attempt. Never run that workflow run again,
   never rebuild the candidate, and never use a local, pull-request, Windows or macOS build in its place; if
   it is not good, there is no candidate until a new reviewed commit has its own first push run. The CI
   workflow keeps read-only permission, no secret and no manual trigger, and every action of both workflows
   stays pinned to a full commit SHA (D-75). **The publication workflow is started only by hand and only with
   that approval, and a run of it is never run again.** It ran once, for 2.0.1, and is not started again for
   that version. It publishes only the approved release, whose identity and checksums are in
   `eng/publication.sh`; no API key is created and no secret is stored (D-88, D-89). **1.0.0 is deprecated on
   nuget.org and stays listed (D-92): it is not unlisted or deleted, and its deprecation is not undone or
   edited, without Shaun's explicit approval for that action.** If a
   publication fails after the push, nothing is pushed again, replaced or unlisted: what happens next is
   Shaun's decision. The GitHub releases of 2.0.0 and 2.0.1, their files and their tags are never edited,
   replaced or deleted, and the package of 2.0.0 is never pushed to NuGet (D-85). A repository setting or a
   ruleset is changed, and a ruleset's bypass used, only with that approval (D-82); the environment
   `nuget-production` and the Trusted Publishing policy on nuget.org are settings in that sense. See
   [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md).
6. **Campertron is never modified from this repository or this workflow.** Adopting this package in Campertron is
   done in Shaun's separate Campertron project and chat. 2.0.1 is published and verified, which was its
   precondition; it is still not planned, started or prepared from here. The parity fixtures and what the
   modern API renders for them do not change.
7. **No Python.** Do not propose, build or document a Python version or interoperability layer.
8. **One focused phase per pull request,** scoped by Shaun beforehand. Do not start the next phase unprompted.
9. **Keep these documents true.** A change that makes any of them wrong updates them in the same pull request.
   The same goes for the public documents: a change users can see is added to `CHANGELOG.md`, and no
   document says that a version is published, tagged or released before it is. The sections of released
   versions in the changelog are closed; after 2.0.1 a change users can see belongs to a later version and
   goes under a new heading. Public text follows D-84.

## What to read first

| Before working on | Read |
|---|---|
| Anything | [roadmap.md](docs/ai-context/roadmap.md), [decisions.md](docs/ai-context/decisions.md) |
| The library's design, new layout or rendering code | [architecture.md](docs/ai-context/architecture.md), [open-questions.md](docs/ai-context/open-questions.md) |
| Anything that touches `ConsoleCalendar` or its public API | [compatibility.md](docs/ai-context/compatibility.md) |
| Tests | [testing.md](docs/ai-context/testing.md) |
| The README, the example, the screenshot, XML comments | [development.md](docs/ai-context/development.md#the-readme-the-example-and-the-screenshot), [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#the-readme-on-nugetorg) |
| Building, CI, SDK or framework upgrades | [development.md](docs/ai-context/development.md) |
| The package, versions, releases, the release bundle | [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md) |
| A workflow, a pinned action | [development.md](docs/ai-context/development.md#ci) |
| The NuGet publication: its workflow, its checks, the policy on nuget.org | [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#publication-to-nugetorg) |
| Repository settings, rulesets, the environment, the names of the CI checks | [development.md](docs/ai-context/development.md#repository-settings) |
