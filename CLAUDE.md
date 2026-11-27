# ConsoleCalendarGenerator

Entry point for coding agents. This file is deliberately short; the detail is in [docs/ai-context](docs/ai-context/).

## What this is

A small .NET library, published on NuGet as `ConsoleCalendarGenerator`, that draws month and year calendars in
console applications. It is .NET only.

## Current state

- **Published:** only `ConsoleCalendarGenerator` 1.0.0 (June 2023). Nothing newer has been released.
- **`master`:** builds an unpublished development package of 2.0, `2.0.0-dev`, with assembly and file version
  `2.0.0.0`. The final version number is not set, and no prerelease is published.
- **Production code:** the legacy class and the modern API, in one assembly. The modern API is everything in the
  `ConsoleCalendarGenerator` namespace.
  - The legacy class `CalendarGenerator.ConsoleCalendar`, in `ConsoleCalendarGenerator/ConsoleCalendar.cs`. It
    is a compatibility facade: it takes the position of every day from `MonthLayout` and keeps everything else
    about how 1.0.0 draws a month (title, colors, the exact console calls, exceptions) in private code.
    Version 2.0 corrects five of its behaviors (rule 2). **Four are implemented: the constructor colors (D-57),
    the exception for a null list (D-58), the line terminator of the last row (D-59) and the one blank line
    after every month (D-60).** Only the title (D-61) is still to come. In everything else its behavior is
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
  identity, and the two things in the next point. Of the corrections, all but the Gregorian title are done.
  Everything about releases is **planned and not implemented**.
- **The 1.0.0 reference and the migration tests (test code only):**
  `ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs` is the source of 1.0.0, frozen.
  **Never edit it**; a test compares it byte for byte with the original. The tests in
  `ConsoleCalendarGenerator.Tests/Migration/` run the library and that source side by side and require the
  library to do what 1.0.0 did, apart from the corrections that have been implemented.
- **Public documents:** `README.md` (also the readme of the package), `CHANGELOG.md`, `CONTRIBUTING.md` and
  `SECURITY.md`, all at the root, and one image in `docs/images/`. Every public type and member has XML
  documentation, the legacy class included, and no warning is suppressed. The code in the README is copied from
  `ConsoleCalendarGeneratorExample/Examples.cs` and its output from `expected-output.txt` beside it; the tests in
  `ConsoleCalendarGenerator.Tests/Documentation/` compare them, so change them together.
- **Weekday labels and operating systems (decided, D-54):** labels taken from a culture are the day names .NET
  has on the machine, and they differ: for `en-US`, two letters on Linux and macOS, one letter on Windows. That
  stays as it is. Labels supplied with `WithWeekdayLabels` are the same everywhere, and the README's quick
  start and the example supply theirs, so they print the same text on all three systems. One example keeps the
  culture's own labels on purpose. Do not add a label mode or change `WeekdayLabelStyle` for this.
- **Publication (decided, D-55 and D-69):** nothing is published before 2.0, and nothing before its final
  approval. No 1.x release, no prerelease, no tag and no release is authorized.
- **Next phase:** the Gregorian title (D-61), the last of the five corrections. See
  [roadmap.md](docs/ai-context/roadmap.md). Start a phase only when Shaun has scoped it.

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
   line (done); the title names the Gregorian month. Every other output, console call, color, exception and side effect
   stays as in 1.0.0. See [compatibility.md](docs/ai-context/compatibility.md).
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
   settings. See [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md).
6. **Campertron is never modified from this repository or this workflow.** Adopting this package in Campertron is
   done in Shaun's separate Campertron project and chat, after 2.0 is published and verified. The parity
   fixtures and what the modern API renders for them do not change.
7. **No Python.** Do not propose, build or document a Python version or interoperability layer.
8. **One focused phase per pull request,** scoped by Shaun beforehand. Do not start the next phase unprompted.
9. **Keep these documents true.** A change that makes any of them wrong updates them in the same pull request.
   The same goes for the public documents: a change users can see is added to `CHANGELOG.md` under
   "Unreleased", and the README never describes a version as published before it is.

## What to read first

| Before working on | Read |
|---|---|
| Anything | [roadmap.md](docs/ai-context/roadmap.md), [decisions.md](docs/ai-context/decisions.md) |
| The library's design, new layout or rendering code | [architecture.md](docs/ai-context/architecture.md), [open-questions.md](docs/ai-context/open-questions.md) |
| Anything that touches `ConsoleCalendar` or its public API | [compatibility.md](docs/ai-context/compatibility.md) |
| Tests | [testing.md](docs/ai-context/testing.md) |
| The README, the example, the screenshot, XML comments | [development.md](docs/ai-context/development.md#the-readme-the-example-and-the-screenshot), [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md#the-readme-on-nugetorg) |
| Building, CI, SDK or framework upgrades | [development.md](docs/ai-context/development.md) |
| The package, versions, releases | [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md) |
