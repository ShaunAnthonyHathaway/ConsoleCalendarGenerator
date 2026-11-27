# Development

How to build, test and change this repository today. Every command here is run from the repository root.

## Prerequisites

- **.NET SDK 11.0.100,** or a later 11.0 feature band. `global.json` pins it (`rollForward: latestFeature`, no
  prereleases). The SDK also builds the `net10.0` and `netstandard2.1` targets; no other SDK is needed.
- **A .NET 11 runtime** to run the tests, which the SDK includes.
- **bash** for the two scripts in `eng/`. On Windows, Git Bash works; `.gitattributes` keeps `*.sh` files on LF
  line endings for that reason.
- **unzip,** used by `eng/verify-package.sh`.
- Optional: a native toolchain (for example clang on Linux) for the Native AOT part of the consumer smoke tests,
  and the util-linux `script` program for two of the terminal tests.
- Network access to nuget.org on the first restore. See
  [packaging-and-releases.md](packaging-and-releases.md#network-and-cache-behavior).

CI runs on `ubuntu-latest`, `windows-latest` and `macos-latest`. Those are the systems the project is verified
on.

## Build

```bash
dotnet restore ConsoleCalendarGenerator.sln
dotnet build ConsoleCalendarGenerator.sln --configuration Debug --no-restore
dotnet build ConsoleCalendarGenerator.sln --configuration Release --no-restore -warnaserror
```

The expected result is **0 warnings and 0 errors**. CI builds Release with `-warnaserror`, so a warning fails the
build there. Do not add warning suppressions to get past it; fix the cause, or record a narrow, explained
exception the way `CS1591` is handled in the library project.

To build one library target on its own:

```bash
dotnet build ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-restore -f net10.0
dotnet build ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-restore -f netstandard2.1
```

## Test

```bash
# Everything except the terminal tests, against the library's net10.0 build.
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory!=Terminal"

# The terminal tests (they ignore themselves where the machine cannot run them).
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-build --filter "TestCategory=Terminal"

# The same tests against the library's netstandard2.1 build. This one builds, into its own output folder.
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --no-restore -p:LibraryTargetFramework=netstandard2.1 -warnaserror --filter "TestCategory!=Terminal"
```

A `net11.0` test project would normally only ever see the library's `net10.0` build. The
`LibraryTargetFramework` property in the test project selects the build instead; `LibraryBuildTests` fails if a
run is not using the build it asked for. What the tests cover and how to add to them: [testing.md](testing.md).

## Pack and check the package

```bash
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/verify-package.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package --native-aot     # also publishes and runs a Native AOT application
```

Packing is explicit; a build never creates a package. Packing also runs package validation against published
1.0.0. What each step checks: [packaging-and-releases.md](packaging-and-releases.md).

## Generated output and cleaning

- Build output goes to each project's `bin/` and `obj/`. The `netstandard2.1` test run uses
  `bin/<configuration>/net11.0-with-netstandard2.1/` and the matching `obj/` folder.
- Packages go where `--output` says; the convention is `artifacts/package/`.
- The consumer smoke script works in a temporary folder outside the repository and deletes it.
- All of this is ignored by Git. To remove it, preview first and then delete:

```bash
git clean -Xdn     # lists the ignored files that would be removed
git clean -Xdf     # removes them
```

`-X` removes ignored files only, so it also removes ignored editor state such as `.vs/`. It never touches
tracked or new unignored files.

## Shared settings

- **`Directory.Build.props`** applies to every project: C# 14, nullable reference types, the .NET analyzers at
  their default level for every target (including `netstandard2.1`, where they are off by default),
  `IsPackable` off (the library turns it on), and `ContinuousIntegrationBuild` on GitHub Actions. Put a setting
  here only if every project should have it; anything else belongs in a project file.
- **`.editorconfig`** holds basic formatting rules for editors and `dotnet format`. It is not enforced in CI, and
  the two legacy source files (`ConsoleCalendar.cs`, the example's `Program.cs`) do not fully conform. Do not
  reformat them as a side effect of other work.
- **Implicit usings** are off in the library, so its dependencies stay visible across both targets, and on in the
  example and the tests.

## Deterministic builds and Source Link

- Compilation is deterministic by default in the SDK.
- On GitHub Actions `ContinuousIntegrationBuild` also replaces local source paths in the symbols, so the same
  commit gives the same binaries on any machine. To reproduce that locally, build with
  `-p:ContinuousIntegrationBuild=true`.
- Source Link is part of the SDK; no package reference is needed. The symbols point at the GitHub repository and
  the built commit, and the commit is appended to the assembly's informational version. It needs a Git checkout
  with the `origin` remote.

## CI

`.github/workflows/ci.yml` is the only workflow. It runs on every pull request and on every push to `master`,
once per operating system:

1. Check out, install the SDK from `global.json`
2. Restore
3. Release build, every warning an error
4. Tests
5. Terminal tests (Linux only)
6. Tests against the `netstandard2.1` library build
7. Pack, which includes package validation against 1.0.0
8. `eng/verify-package.sh`
9. `eng/consumer-smoke.sh` (with `--native-aot` on Linux)
10. Upload the `.nupkg` and `.snupkg` as a CI artifact, kept seven days (Linux only)

It has read-only repository permission, uses no secrets, has no manual trigger and publishes nothing. Keep it
that way; see [packaging-and-releases.md](packaging-and-releases.md#release-boundaries).

## Working practice

- One dedicated branch per phase, created from current `master`, and one focused pull request.
- A pull request says what changed, how it was validated and what did not change.
- All three CI jobs must pass. Do not weaken a check to get there.
- Pull requests so far were merged with a merge commit, and the branch was deleted locally and remotely once it
  had no unmerged commits.
- Whether a pull request may be merged without further review is stated in the brief for that phase. Do not
  assume it.
- Nothing in the working practice overrides the publication boundaries.

Two things that have gone wrong before:

- `dotnet sln add` rewrites the solution with extra platform configurations. Add a project to
  `ConsoleCalendarGenerator.sln` by hand instead, with `Debug|Any CPU` and `Release|Any CPU` only.
- Scripts must tolerate Git Bash on Windows and the older bash on macOS.

## Upgrading the SDK or the target frameworks

Treat an upgrade as its own phase, not as a side effect.

1. **Gather current evidence.** Check which .NET versions are supported and until when, from Microsoft's
   published support policy or release metadata. Do not rely on memory or on this document for dates.
2. **Change `global.json` deliberately,** in its own commit, and say why.
3. **Verify every target framework** still builds with 0 warnings: both library targets, the example, the tests.
4. **Run the full test matrix:** both library builds, the terminal tests, all three operating systems in CI.
5. **Run the package matrix:** pack with validation, `eng/verify-package.sh`, `eng/consumer-smoke.sh` including
   Native AOT.
6. **Check consumer selection.** If a library target is added or changed, update the frameworks the smoke script
   uses so that each library build is still selected by at least one consumer, and update the expected file list
   in `eng/verify-package.sh`.
7. **Update the documentation:** these files, and the user-facing statement of supported frameworks.

Removing a library target is a breaking change for consumers on it and belongs in a major version. Do not weaken
or delete tests or validation to make an upgrade pass; if something fails, that is the finding.
