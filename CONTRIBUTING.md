# Contributing

Bug reports, questions and pull requests are welcome. For anything larger than a small fix, please open an
[issue](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues) first, so the change can be
agreed on before you spend time on it. To report a security problem, see [SECURITY.md](SECURITY.md).

## What you need

- The .NET SDK named in [global.json](global.json). It builds every project, including the library's
  `net10.0` and `netstandard2.1` builds.
- `bash` for the scripts in [eng](eng). On Windows, Git Bash works.
- Access to nuget.org when packing: the pack step downloads version 1.0.0 of the package to compare against.

## Build and test

Run these from the repository root.

```text
dotnet restore ConsoleCalendarGenerator.sln
dotnet build ConsoleCalendarGenerator.sln --configuration Release --no-restore -warnaserror
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory!=Terminal"
```

The build must finish without warnings.

Two more test runs complete the picture:

```text
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory=Terminal"
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-restore -p:LibraryTargetFramework=netstandard2.1 -warnaserror --filter "TestCategory!=Terminal"
```

- **The terminal tests** run the library on a pseudo-terminal and check the color sequences that reach it.
  They need Linux and the `script` program from util-linux. Elsewhere, leave them to CI.
- **The second command** runs the tests against the library's `netstandard2.1` build. The first run uses the
  `net10.0` build.

CI runs all of this on Linux, Windows and macOS for every pull request, and a pull request is merged when all
three pass.

## Pack and check the package

```text
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/verify-package.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package
```

- Packing compares the public API with version 1.0.0 and fails on a breaking change, a renamed parameter
  included.
- `verify-package.sh` checks the contents and the metadata of the package.
- `consumer-smoke.sh` installs the package into small separate applications and runs them. Add `--native-aot`
  where a native toolchain is installed.

None of these commands publishes anything.

## Compatibility

- **`CalendarGenerator.ConsoleCalendar` is stable.** Its public API is the API of version 1.0.0, and its
  behavior is covered by tests that compare it with that version. The [changelog](CHANGELOG.md) lists the five
  behaviors version 2 updated. Please open an issue before proposing a change to what the class draws or
  throws.
- **The reference copy of version 1.0.0 stays as published.**
  [ConsoleCalendarGenerator.Tests/Historical](ConsoleCalendarGenerator.Tests/Historical) holds the source of
  1.0.0 and tests of what it does. It is a fixed reference: please do not format or edit it.
- **Recorded values stay as recorded.** Some tests hold hashes of recorded output. Where the library
  intentionally differs from the recording, a test states both behaviors side by side.
- **Public API changes are deliberate.** The public API is listed in `ModernApiTests` and `PublicApiTests`,
  so a new or changed public member shows up as a test to update. Removing or changing an existing member is
  a breaking change.
- **Every public type and member has an XML comment** that says what it does, including what it throws. A
  missing comment fails the build.
- **Both builds of the library behave the same.**
- **The README's examples come from the example program.** They are the code between the `// <Name>` comments
  in [Examples.cs](ConsoleCalendarGeneratorExample/Examples.cs), and the output blocks are lines of
  [expected-output.txt](ConsoleCalendarGeneratorExample/expected-output.txt). Tests compare them, so change
  them together.
- **New package dependencies** are discussed first. The library has none.

## Pull requests

- Keep a pull request to one subject.
- Add tests for what you change.
- Say in the description what changed and how you checked it.

## Releases

The maintainer publishes packages, creates tags and makes releases. A released version is never changed; a
correction is a new version. Pull requests keep the workflow at read access, reference actions by the full SHA
of a commit, and add nothing that publishes.

## Project notes

[docs/ai-context](docs/ai-context) holds the project's design decisions, compatibility rules, test
organization and release process.
