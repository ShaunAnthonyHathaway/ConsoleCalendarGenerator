# ConsoleCalendarGenerator

Entry point for coding agents. This file is deliberately short; the detail is in [docs/ai-context](docs/ai-context/).

## What this is

A small .NET library, published on NuGet as `ConsoleCalendarGenerator`, that draws month and year calendars in
console applications. It is .NET only.

## Current state

- **Published:** only `ConsoleCalendarGenerator` 1.0.0 (June 2023). Nothing newer has been released.
- **`master`:** builds an unpublished development package, `1.1.0-dev`.
- **Production code:** two parts, in one assembly.
  - The legacy class `CalendarGenerator.ConsoleCalendar`, in `ConsoleCalendarGenerator/ConsoleCalendar.cs`. Its
    behavior is identical to published 1.0.0, which the tests verify.
  - The layout model, in `ConsoleCalendarGenerator/Layout/`: `MonthLayout`, `MonthLayoutWeek` and
    `MonthLayoutCell` in the `ConsoleCalendarGenerator` namespace. It places the days of a month in weeks and
    weekday columns and does nothing else. The legacy class does not use it.
- **Phase:** the foundations (CI, characterization tests, build and packaging, this documentation) and the
  layout model are complete. Text rendering and everything after it is **planned and not implemented**.
- **Next phase:** options and text rendering. See [roadmap.md](docs/ai-context/roadmap.md). Start it only when
  Shaun has scoped it.

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
2. **Published 1.0.0 behavior is preserved throughout 1.x.** The public API, the output and the side effects of
   `CalendarGenerator.ConsoleCalendar` must not change. See [compatibility.md](docs/ai-context/compatibility.md).
3. **Characterized defects are not quietly fixed in 1.x.** Some tested behavior is wrong on purpose, most visibly
   the constructor arguments that color the wrong element. Tests named `Characterized1xBehavior` record it. Do not
   "correct" the code or those tests.
4. **Breaking corrections belong in 2.0.**
5. **Publication needs Shaun's explicit approval, each time.** That covers tags, GitHub releases, NuGet
   publication, any manual workflow dispatch or rerun that could publish, publication recovery, and repository
   settings. See [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md).
6. **Campertron is never modified from this repository or this workflow.** Adopting this package in Campertron is
   done in Shaun's separate Campertron project and chat.
7. **No Python.** Do not propose, build or document a Python version or interoperability layer.
8. **One focused phase per pull request,** scoped by Shaun beforehand. Do not start the next phase unprompted.
9. **Keep these documents true.** A change that makes any of them wrong updates them in the same pull request.

## What to read first

| Before working on | Read |
|---|---|
| Anything | [roadmap.md](docs/ai-context/roadmap.md), [decisions.md](docs/ai-context/decisions.md) |
| The library's design, new layout or rendering code | [architecture.md](docs/ai-context/architecture.md), [open-questions.md](docs/ai-context/open-questions.md) |
| Anything that touches `ConsoleCalendar` or its public API | [compatibility.md](docs/ai-context/compatibility.md) |
| Tests | [testing.md](docs/ai-context/testing.md) |
| Building, CI, SDK or framework upgrades | [development.md](docs/ai-context/development.md) |
| The package, versions, releases | [packaging-and-releases.md](docs/ai-context/packaging-and-releases.md) |
