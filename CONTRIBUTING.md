# Contributing

Bug reports, questions and pull requests are welcome. For anything larger than a small fix, please open an
[issue](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues) first, so the change can be
agreed on before you spend time on it.

## What you need

- The .NET SDK named in [global.json](global.json). It builds every project, including the library's
  `net10.0` and `netstandard2.1` builds.
- `bash` for the two scripts in [eng](eng). On Windows, Git Bash works.
- Access to nuget.org when packing: the pack step downloads the published 1.0.0 package to compare against.

## Build and test

Run these from the repository root.

```text
dotnet restore ConsoleCalendarGenerator.sln
dotnet build ConsoleCalendarGenerator.sln --configuration Release --no-restore -warnaserror
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory!=Terminal"
```

The build must finish without warnings.

Two more test runs cover what the first one cannot:

```text
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory=Terminal"
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-restore -p:LibraryTargetFramework=netstandard2.1 -warnaserror --filter "TestCategory!=Terminal"
```

- **The terminal tests** run the library on a pseudo-terminal and look at the color sequences that reach it.
  They need Linux and the `script` program from util-linux. Elsewhere, leave them to CI.
- **The second command** runs the tests against the library's `netstandard2.1` build. The first run uses the
  `net10.0` build.

CI runs all of this on Linux, Windows and macOS for every pull request.

## Pack and check the package

```text
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/verify-package.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package
```

- Packing compares the public API with the published 1.0.0 package and fails on a breaking change.
- `verify-package.sh` checks the contents and the metadata of the package.
- `consumer-smoke.sh` installs the package into small separate applications and runs them. Add `--native-aot`
  where a native toolchain is installed.

None of these commands publishes anything.

## What a change must keep

- **The legacy class does not change.** `CalendarGenerator.ConsoleCalendar` keeps the public API, the output
  and the console behavior of version 1.0.0, including behavior that looks wrong, such as the constructor
  arguments that color a different element than their name says. Tests named `Characterized1xBehavior` record
  it. Please do not fix the code or those tests; corrections that break existing programs wait for a new major
  version. The details are in [docs/ai-context/compatibility.md](docs/ai-context/compatibility.md).
- **The public API is listed in tests.** A new or changed public member fails `ModernApiTests` or
  `PublicApiTests` until the list is updated, which makes every API change a visible decision. Removing or
  changing an existing member is a breaking change.
- **Every public type and member has an XML comment** that says what it does, including what it throws. A
  missing comment fails the build.
- **Both builds of the library behave the same.**
- **The README stays true.** Its examples are the code between the `// <Name>` comments in
  [Examples.cs](ConsoleCalendarGeneratorExample/Examples.cs), and its output blocks are in
  [expected-output.txt](ConsoleCalendarGeneratorExample/expected-output.txt). The tests fail when they drift
  apart, so change them together.
- **No new package dependencies** without discussing it first. The library has none.

## Pull requests

- Keep a pull request to one subject.
- Add tests for what you change.
- Say in the description what changed and how you checked it.

## Releases

Only the maintainer publishes packages, creates tags and makes releases. Pull requests must not add workflows
or scripts that publish.

## Maintenance notes

[docs/ai-context](docs/ai-context) holds the project's design decisions, compatibility rules, test
organization and roadmap. It is written as working context for coding agents, and it is just as useful to
people. You do not need any AI tool to contribute.
