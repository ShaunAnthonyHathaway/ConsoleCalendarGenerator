# ConsoleCalendarGenerator

Entry point for coding agents. This file is deliberately short; the detail is in [docs/ai-context](docs/ai-context/).

## What this is

A small .NET library, published on NuGet as `ConsoleCalendarGenerator`, that draws month and year calendars in
console applications. It is .NET only.

## Current state

- **Published:** only `ConsoleCalendarGenerator` 1.0.0 (June 2023). Nothing newer has been released.
- **`master` is the source of the release `2.0.0`,** with assembly and file version `2.0.0.0` and no version
  suffix. Every build of it says it is 2.0.0. **It is not published:** no tag, no GitHub release and no package
  on nuget.org exists for it. Only one build of it is the release, the **candidate**: the bundle the Linux job
  of the first push run for the release commit uploaded. **The release commit is
  `117df06e5b6d684da6891cd3ac0310e5aa81ec98` and the candidate is designated** (run 46083727520, first attempt;
  D-83). `master` has moved on from that commit and will again: **the tag `v2.0.0` belongs on the release
  commit, never on a later one,** and a bundle CI makes of a later commit is not the candidate, although it
  also says 2.0.0. The candidate's record and its checksums are **not in the repository**. Never build, name
  or substitute another, and do not run `eng/designate-candidate.sh` again.
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
  publication, and checks that the repository agrees with itself about its version. Release preparation is
  done: the version, the README's links at the tag `v2.0.0`, the changelog's section. **No tag, no release
  and no publication exists,** and nothing in the repository can publish.
- **The 1.0.0 reference and the migration tests (test code only):**
  `ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs` is the source of 1.0.0, frozen.
  **Never edit it**; a test compares it byte for byte with the original. The tests in
  `ConsoleCalendarGenerator.Tests/Migration/` run the library and that source side by side and require the
  library to do what 1.0.0 did, apart from the five corrections.
- **Public documents:** `README.md` (also the readme of the package), `CHANGELOG.md`, `CONTRIBUTING.md` and
  `SECURITY.md`, all at the root, and one image in `docs/images/`. Every public type and member has XML
  documentation, the legacy class included, and no warning is suppressed. The code in the README is copied from
  `ConsoleCalendarGeneratorExample/Examples.cs` and its output from `expected-output.txt` beside it; the tests in
  `ConsoleCalendarGenerator.Tests/Documentation/` compare them, so change them together. The README's section
  "Migrating from 1.0.0 to 2.0" is the one place that tells what 1.0.0 did. **The README is packed into the
  package and can never be edited there,** so it names version 2.0.0 and says nothing that publication would
  make false: it never says by itself that 2.0.0 is or is not published (D-78). Three of its links are at the
  tag `v2.0.0` and answer "not found" until the tag exists, which is expected. The changelog's heading says
  "not yet published" until the date of publication is known; that and the few other things to finalize
  afterwards are in
  [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#what-is-finalized-after-publication).
- **Weekday labels and operating systems (decided, D-54):** labels taken from a culture are the day names .NET
  has on the machine, and they differ: for `en-US`, two letters on Linux and macOS, one letter on Windows. That
  stays as it is. Labels supplied with `WithWeekdayLabels` are the same everywhere, and the README's quick
  start and the example supply theirs, so they print the same text on all three systems. One example keeps the
  culture's own labels on purpose. Do not add a label mode or change `WeekdayLabelStyle` for this.
- **Publication (decided, D-55 and D-69):** nothing is published before 2.0, and nothing before its final
  approval. No 1.x release, no prerelease, no tag and no release is authorized.
- **Repository settings (made 2026-11-27, D-82):** a published release is immutable; private vulnerability
  reporting is on, and `SECURITY.md` sends reports there (D-81); secret scanning and push protection are on;
  Actions must be pinned to full commit SHAs; a ruleset on `master` requires the three CI checks by their
  exact names (`Build (ubuntu-latest)`, `Build (windows-latest)`, `Build (macos-latest)`) and blocks force
  pushes and deletion; a ruleset on tags `v*` blocks moving and deleting a tag. So every change reaches
  `master` through a pull request with the three checks green, and renaming a CI job stops every merge until
  the ruleset is changed. The record is in
  [development.md](docs/ai-context/development.md#repository-settings).
- **`SECURITY.md` states the present,** unlike the README: that 1.0.0 is the only published package and that
  2.0.0 has a candidate and is not yet published. It is on `master` only and is edited when that changes.
- **Next phase:** the separately approved steps of the release itself: the tag on the release commit, the
  GitHub release, the NuGet publication, verification. See [roadmap.md](docs/ai-context/roadmap.md). Each
  needs its own approval; start none unprompted.

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
```

The build must finish with 0 warnings. None of these commands publishes anything.

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
   it is not good, there is no candidate until a new reviewed commit has its own first push run. The workflow
   keeps read-only permission, no secret and no manual trigger, and every action stays pinned to a full commit
   SHA (D-75). A repository setting or a ruleset is changed, and a ruleset's bypass used, only with that
   approval (D-82). See [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md).
6. **Campertron is never modified from this repository or this workflow.** Adopting this package in Campertron is
   done in Shaun's separate Campertron project and chat, after 2.0 is published and verified. The parity
   fixtures and what the modern API renders for them do not change.
7. **No Python.** Do not propose, build or document a Python version or interoperability layer.
8. **One focused phase per pull request,** scoped by Shaun beforehand. Do not start the next phase unprompted.
9. **Keep these documents true.** A change that makes any of them wrong updates them in the same pull request.
   The same goes for the public documents: a change users can see is added to `CHANGELOG.md`, and no
   document says that a version is published, tagged or released before it is. After 2.0.0, a change users
   can see belongs to a later version and goes under a new heading of the changelog; the section of 2.0.0 is
   closed apart from its date.

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
| The workflow, a pinned action | [development.md](docs/ai-context/development.md#ci) |
| Repository settings, rulesets, the names of the CI checks | [development.md](docs/ai-context/development.md#repository-settings) |
