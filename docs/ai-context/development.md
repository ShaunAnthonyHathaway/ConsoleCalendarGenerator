# Development

How to build, test and change this repository today. Every command here is run from the repository root.

## Prerequisites

- **.NET SDK 11.0.100,** or a later 11.0 feature band. `global.json` pins it (`rollForward: latestFeature`, no
  prereleases). The SDK also builds the `net10.0` and `netstandard2.1` targets; no other SDK is needed.
- **A .NET 11 runtime** to run the tests, which the SDK includes.
- **bash** for the two scripts in `eng/`. On Windows, Git Bash works; `.gitattributes` keeps `*.sh` files on LF
  line endings for that reason.
- **unzip** and **git,** used by `eng/verify-package.sh` and the release scripts.
- Optional: a native toolchain (for example clang on Linux) for the Native AOT part of the consumer smoke tests,
  and the util-linux `script` program for the terminal tests and for running the original sample in the
  consumer smoke tests.
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
build there. Do not add warning suppressions to get past it; fix the cause. Nothing is suppressed at present,
and `XmlDocumentationTests` fails if a suppression of the documentation warnings appears.

One file is exempt from "fix the cause": `ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs`,
the frozen source of 1.0.0, is never edited ([testing.md](testing.md#the-frozen-source-of-100)). It builds
without warnings today. Should a later SDK warn about it, silence that warning for that one file, in its
section of `.editorconfig`, and nowhere else.

A public type or member without an XML documentation comment is such a warning (`CS1591`) everywhere in the
library, the legacy class included. New public API is documented in the change that adds it.

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
bash eng/consumer-smoke.sh artifacts/package --native-aot --published-sample     # Linux: also the 1.0.0 sample with the published 1.0.0
bash eng/upgrade-smoke.sh artifacts/package                    # a program upgraded from the published 1.0.0
bash eng/release-state.sh                                      # the repository agrees with itself about its version
```

The release bundle needs a build that names no folder of the machine, and a checkout with nothing uncommitted:

```bash
dotnet build ConsoleCalendarGenerator.sln --configuration Release --no-restore -warnaserror -p:ContinuousIntegrationBuild=true
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/release-bundle.sh artifacts/package artifacts/release-bundle
bash eng/release-bundle-tamper-test.sh artifacts/release-bundle
bash eng/direct-reference-smoke.sh artifacts/release-bundle
```

Packing is explicit; a build never creates a package. Packing also runs package validation against published
1.0.0. What each step checks: [packaging-and-releases.md](packaging-and-releases.md). The package and the
bundle these commands make call themselves `2.0.0`, like every build of this source, and are **not** the
release: the release is the one candidate, which is never built again
([packaging-and-releases.md](packaging-and-releases.md#the-candidate)). Do not publish, attach or hand on what
these commands make. None of them publishes anything, and no command that needs a credential belongs in this
file.

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
- **`.editorconfig`** holds basic formatting rules for editors and `dotnet format`. They are not enforced in CI.
  `ConsoleCalendar.cs` keeps its byte order mark and its block-scoped namespace from 1.0.0. One section marks
  the frozen source of 1.0.0 as generated code, which is how tools are told to leave a file alone: `dotnet
  format` skips it and the analyzers do not report on it. Nothing else in the file affects the build.
- **Implicit usings** are off in the library, so its dependencies stay visible across both targets, and on in the
  example and the tests.

## The README, the example and the screenshot

The public documents are described in [architecture.md](architecture.md#public-documents-and-the-example).
What to do when one of them changes:

- **An example in the README.** Change it in `ConsoleCalendarGeneratorExample/Examples.cs`, between its
  `// <Name>` and `// </Name>` comments, and copy the lines into the README unchanged apart from the
  indentation. The first code block of the README also shows the using directives of `Examples.cs`. A new
  example needs a method named like its marker, a call in `Program.cs`, and a block in the README; the tests
  ask for all three.
- **Output in the README.** Run the program with its output redirected, and replace
  `ConsoleCalendarGeneratorExample/expected-output.txt` with what it printed, after reading the difference:

  ```bash
  dotnet run --project ConsoleCalendarGeneratorExample --configuration Release > ConsoleCalendarGeneratorExample/expected-output.txt
  ```

  Then copy the changed lines into the README's output block. Every output block must be consecutive lines of
  that file. Record it on Linux or macOS: one line, the weekday row of the culture-derived example, is
  different on Windows, and the file and the README show the Linux and macOS form
  ([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
  An example that should print the same text everywhere supplies its weekday labels, or builds on the
  `options` of the quick start, which do.
- **A link.** In the README every link is an absolute `https` address or `#a-heading-of-the-page`, because the
  same file is shown on nuget.org. The other three documents may use relative links. A new link from the
  README into this repository also has to be added to the list in `DocumentationLinkTests`, with whether it
  moves to the release tag when a version is released, and to the table of the
  [release preparation checklist](packaging-and-releases.md#release-preparation-checklist). In the list every
  address is written as it is on `master`; in the README of 2.0.0 the ones that are part of the version are at
  `v2.0.0`.
- **What a document says about the state of the project.** The repository is the source of 2.0.0, and 2.0.0 is
  not published. The public documents name the version and say nothing that publication would make false
  ([D-78](decisions.md#d-78-the-documents-of-the-release-say-nothing-that-publication-would-make-false)): do
  not write that 2.0.0 is published, tagged or released, and do not write that it is unpublished either, in a
  file that is packed into the package. Tests hold the documents to it.
- **The screenshot,** `docs/images/month-with-theme.png`. It is drawn from what the example really writes to a
  terminal, and only needs redoing when the "Colors" example changes what it draws. The tools are not part of
  the build: util-linux `script` and ImageMagick with Pango.
  1. Run the built example on a pseudo-terminal and keep the bytes:
     `TERM=xterm-256color script -qec "dotnet ConsoleCalendarGeneratorExample/bin/Release/net10.0/ConsoleCalendarGeneratorExample.dll" /dev/null`.
  2. Take the lines between the "Colors" heading and the next heading. Remove the carriage returns.
  3. Turn each color sequence into Pango markup: the sequence for a bright color opens a
     `<span foreground="...">`, the reset sequence closes it. The palette used is `#F9F1A5` for bright yellow
     (code 93), `#61D6D6` for bright cyan (96), `#16C60C` for bright green (92), `#CCCCCC` for the default
     foreground and `#0C0C0C` for the background. Wrap everything in one span with a monospaced font.
  4. Render and crop: `magick -background '#0C0C0C' -density 120 pango:@month.pango -trim +repage -bordercolor '#0C0C0C' -border 28x24 -strip PNG24:month-with-theme.png`.

  The text of the image must be exactly what the program wrote. Do not type it in, and do not edit the picture.
  Keep the alternative text in the README true to what the image shows.

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
9. `eng/consumer-smoke.sh` (with `--native-aot --published-sample` on Linux, which also runs the sample
   program of 1.0.0 with the published 1.0.0 and checks binary compatibility)
10. `eng/release-bundle.sh`: the four files of a release, made and checked
11. `eng/release-bundle-tamper-test.sh`: the check refuses a damaged bundle
12. `eng/direct-reference-smoke.sh`: a program that references an assembly of the archive directly
13. `eng/upgrade-smoke.sh`: a program upgraded from the published 1.0.0
14. `eng/verify-published-package-selftest.sh`: the comparison that follows publication, rehearsed with the
    signed 1.0.0 that nuget.org serves
15. `eng/release-state.sh`: the repository agrees with itself about the version it holds
16. Upload the `.nupkg` and `.snupkg` as the artifact `nuget-packages`, kept seven days, and the bundle as the
    artifact `release-bundle`, kept 90 days (Linux only)

**Never run a workflow run again whose bundle is, or may become, the candidate of a release.** A rerun builds
again and leaves the run with a second attempt, after which its artifact can no longer be designated
([D-79](decisions.md#d-79-the-candidate-is-the-linux-bundle-of-the-first-push-run-for-the-release-commit)).

It has read-only repository permission, uses no secrets, has no manual trigger and publishes nothing. Keep it
that way; see [packaging-and-releases.md](packaging-and-releases.md#release-boundaries). `WorkflowTests` and
`ReleaseToolingTests` fail on a change that would give it more.

### Updating a pinned action

Every action is pinned to the full SHA of a commit, with its version in a comment on the same line
([D-75](decisions.md#d-75-actions-are-pinned-to-the-full-sha-of-a-commit)):

```yaml
uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
```

A pin never moves by itself. To move one, deliberately and as its own small change:

1. **Decide why.** A fixed vulnerability, a deprecation notice in a CI log, or a runner that the pinned
   version no longer supports. A newer release existing is not a reason by itself, and a new major version is
   a change of behavior to be read about first.
2. **Find the commit in the action's own repository,** never in a fork, and never from a search result:

   ```bash
   gh api repos/actions/checkout/git/ref/tags/v7.0.1 --jq '.object.type + " " + .object.sha'
   ```

   If the type is `commit`, that is the SHA. If it is `tag`, the tag is annotated and the SHA shown is the
   tag object, not the commit: ask for the commit behind it with
   `gh api repos/actions/checkout/git/tags/<that sha> --jq .object.sha`. `git ls-remote
   https://github.com/actions/checkout 'refs/tags/v7.0.1^{}'` gives the commit of an annotated tag directly.
3. **Read what changed** between the two releases: the release notes, and the comparison of the two commits.
4. **Change the SHA and the comment together,** in every place the action is used.
5. **Let CI run on all three systems,** and check in the log's "Download action repository" line that the SHA
   it fetched is the one written.

The three pins were made this way. Each is the latest release of its major version at the time, and the
commit that CI was already running under the major tag, so pinning changed nothing about what runs.

## Repository settings

Settings of the repository on GitHub. They are not files, so no checkout shows them; this is their record.
They were made on 2026-11-27 with Shaun's approval
([D-82](decisions.md#d-82-repository-settings-protect-master-release-tags-and-published-releases)). **Changing one, changing a
ruleset or using a bypass needs Shaun's separate approval**
([Release boundaries](packaging-and-releases.md#release-boundaries)).

| Setting | State | Read it back with |
|---|---|---|
| Immutable releases | on | `gh api repos/{owner}/{repo}/immutable-releases` |
| Private vulnerability reporting | on | `gh api repos/{owner}/{repo}/private-vulnerability-reporting` |
| Secret scanning | on | `gh api repos/{owner}/{repo} --jq .security_and_analysis` |
| Push protection | on | the same |
| Actions pinned to a full commit SHA | required | `gh api repos/{owner}/{repo}/actions/permissions` |
| Permission of the workflow's token | read, as before | `gh api repos/{owner}/{repo}/actions/permissions/workflow` |
| Rulesets | two, both active | `gh api repos/{owner}/{repo}/rulesets`, then `.../rulesets/<id>` |

Left as they were: the secret-scanning options for patterns that are not a provider's and for validity checks
(off), Dependabot (off), the merge methods, the description and the topics.

**Immutable releases.** The setting protects a release published after it was turned on; none existed. Once a
release is published, its files cannot be changed or removed and its tag cannot be moved, or deleted while the
release exists. The title and the notes stay editable. So a release is created as a draft, gets all of its
files, is compared with the checksums, and only then is published.

**The ruleset for `master`** ("master: required checks, no force push, no deletion", id 28270949) applies to
`refs/heads/master` and to nothing else:

- Three status checks are required, reported by GitHub Actions:

  ```text
  Build (ubuntu-latest)
  Build (windows-latest)
  Build (macos-latest)
  ```

- Force pushes are blocked, and so is deleting the branch.
- No review, no linear history and no signed commit is required, and the merge methods are not restricted.
  The branch need not be up to date with `master` before a merge.
- Bypass: the repository administrator, for a pull request only. It lets the administrator merge a pull
  request whose checks did not pass. It is there for recovery, for instance when the ruleset and the workflow
  disagree about a name, and it is not a way to merge faster.

**The names of the checks are part of the ruleset.** They come from the workflow: the job is named
`Build (${{ matrix.os }})` and the matrix holds the three systems. Rename the job, change the matrix or a
runner's label, and the checks the ruleset waits for never arrive: no pull request can be merged until the
ruleset is changed, which is a setting. Plan such a change with Shaun and change both together.
`CurrentStateTests` fails when the names above and the workflow differ.

**The ruleset for tags** ("v* tags: no update, no deletion", id 28270959) applies to `refs/tags/v*`:

- A tag cannot be moved to another commit, by a push or a forced push, and cannot be deleted.
- Creating a tag is not restricted by the ruleset. Who may create one is who may write to the repository,
  which is its owner alone; the workflow's token is read-only and cannot.
- Bypass: the repository administrator. Before the release is published it allows a tag on the wrong commit
  to be corrected. After the GitHub release of a tag is published, immutability holds the tag whatever the
  bypass says.

**Secret scanning and push protection.** GitHub looks for credentials of known providers in the repository
and refuses a push that contains one. Nothing in this repository is a credential and nothing may become one
([Release boundaries](packaging-and-releases.md#release-boundaries)).

**Private vulnerability reporting.** `SECURITY.md` sends reports to it
([D-81](decisions.md#d-81-security-reports-go-through-githubs-private-vulnerability-reporting)). A report
arrives under the repository's **Security** tab, visible to the reporter and the maintainer.

**What was not tried.** No tag was created to see the tag ruleset work, and no force push was tried on
`master`: creating a tag needs its own approval, and a test of a protection on the real branch is a risk the
protection is meant to remove. Both are known from what the API returns for the rulesets. The required
checks are seen at work on every pull request.

## Working practice

- One dedicated branch per phase, created from current `master`, and one focused pull request.
- A pull request says what changed, how it was validated and what did not change.
- All three CI jobs must pass. Do not weaken a check to get there. GitHub enforces it: `master` takes a
  change only through a pull request whose three checks passed ([Repository settings](#repository-settings)).
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
