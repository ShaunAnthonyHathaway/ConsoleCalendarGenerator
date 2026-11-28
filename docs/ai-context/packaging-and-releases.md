# Packaging and releases

## Current state

- **On nuget.org:** `ConsoleCalendarGenerator` 1.0.0 and 2.0.1, both listed, neither deprecated (verified
  2026-11-28).
  - **2.0.1,** published on 2026-11-28, is the candidate's package and symbol package
    ([The publication of 2.0.1](#the-publication-of-201)):
    <https://www.nuget.org/packages/ConsoleCalendarGenerator/2.0.1>.
  - **1.0.0,** published in 2023, contains a single `netstandard2.1` assembly, placeholder metadata, and no
    readme, license, symbols or documentation. It stays listed and supported; deprecating it is a decision
    that has not been made ([D-91](decisions.md#d-91-201-is-published-on-nuget-and-100-stays-listed-and-supported)).
- **On GitHub Releases:** 2.0.0 and 2.0.1, both published on 2026-11-27 and both immutable.
  - **2.0.1,** for the tag `v2.0.1` on commit `bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd`, is marked Latest and
    holds the four files of the candidate of 2.0.1. It is the version that is on NuGet.
  - **2.0.0,** for the tag `v2.0.0` on commit `117df06e5b6d684da6891cd3ac0310e5aa81ec98`, holds the four files
    of the candidate of 2.0.0.
  - **Neither release is ever edited, replaced, deleted or recreated, and neither are their files or their
    tags.**
- **2.0.0 is not on NuGet and is never pushed there**
  ([D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget)). Its public
  documentation was rejected after the GitHub release was published. The package cannot be changed and a
  version cannot be reused with other bytes, so **2.0.1 replaces it as the 2.x version for NuGet.** No runtime
  or API defect was involved: 2.0.1 has the runtime behavior and the public API of 2.0.0. The files and the
  records of 2.0.0 are kept as they are.
- **`master` is the source of the release `2.0.1`.** The version has no suffix, and every build of it, local
  or in CI, identifies itself as `2.0.1` with assembly version `2.0.0.0` and file version `2.0.1.0`. **No build
  is the release.** `master` has moved past the release commit, and a commit after it changes nothing of the
  release.
- **The release is the candidate, and it is on GitHub.** The candidate is the bundle the Linux job of the
  first push run for the release commit uploaded, and nothing else ([The candidate](#the-candidate)). It was
  designated, and its four files are the files of the GitHub release. The release commit of 2.0.1 is the merge
  commit of the last pull request that changed what the package contains: the wording changes that followed
  the final editorial review
  ([D-87](decisions.md#d-87-the-release-commit-of-201-is-the-merge-of-the-final-wording-changes)). An
  earlier candidate, built from `097eace`, was superseded before any tag. The candidate's record is **not in
  the repository.** The SHA-256 of its four files are, in `eng/publication.sh`, where the publication checks
  them ([D-89](decisions.md#d-89-the-workflow-publishes-one-approved-release-and-its-facts-are-in-the-repository)). Read
  no checksum in a CI log as the release's: every run prints the checksums of its own bundle.
- **The repository's settings for a release are made**
  ([D-82](decisions.md#d-82-repository-settings-protect-master-release-tags-and-published-releases)): a published
  release is immutable, rulesets protect `master` and the tags `v*`, and the environment `nuget-production` is
  what a publication must be let into. See [development.md](development.md#repository-settings).
- **The release machinery runs on every build.** CI makes the four files a release consists of (the package,
  the symbol package, an archive of the assemblies, a file of checksums), checks them, uses the assemblies
  directly, upgrades a program from the published 1.0.0, rehearses the check that follows publication, and
  checks that the repository agrees with itself about its version. See
  [The release bundle](#the-release-bundle). A bundle is not a release, and none but the candidate is ever
  used for one.
- **There is one publication workflow, and it ran once.** `.github/workflows/publish-nuget.yml` published
  2.0.1 to nuget.org on 2026-11-28, started by hand; it runs at no other time, publishes nothing but 2.0.1,
  and is not started again for that version
  ([Publication to nuget.org](#publication-to-nugetorg)). It is the one file of the repository that can
  publish. No script can push a package, create a tag or create a release, and a test fails on one that could.

All package settings are in `ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj`.

## The package

| | |
|---|---|
| Package ID | `ConsoleCalendarGenerator`, exactly this casing. It must not change. |
| Version | `VersionPrefix` 2.0.1 and no suffix, giving `2.0.1` |
| Assembly version | `2.0.0.0`, the major version alone, for all of 2.x; see [compatibility.md](compatibility.md#versions) |
| File version | follows the version prefix: `2.0.1.0` |
| Informational version | the package version and the commit: `2.0.1+<commit>` |
| Library builds | `lib/net10.0/` and `lib/netstandard2.1/`, each with `ConsoleCalendarGenerator.dll` and `.xml` |
| Dependencies | none |
| Metadata | author Shaun Hathaway, a description of what the package does that names the original `ConsoleCalendar` API, MIT license expression, project and repository URLs, repository commit, tags, copyright, and release notes that link to the changelog ([D-74](decisions.md#d-74-the-package-describes-the-modern-api-and-its-release-notes-are-the-changelog)) |
| Readme | the repository's root `README.md`, packed at the package root |
| Symbols | a separate `ConsoleCalendarGenerator.<version>.snupkg` with one portable PDB per build |

Points that are easy to get wrong:

- **Never build a package numbered 1.0.0 or 2.0.0.** Both versions are released: 1.0.0 on NuGet, 2.0.0 on
  GitHub. `eng/verify-package.sh` and `eng/consumer-smoke.sh` refuse 1.0.0, and the package check expects the
  version of the project file.
- **A build that says it is 2.0.1 is not thereby the release.** Do not publish, attach or hand on a package
  built here. The candidate is designated once; see [The candidate](#the-candidate).
- **The version is stated in three places that change together:** the project file, `VersionIdentityTests` and
  `expected_version` in `eng/verify-package.sh`. `eng/release-state.sh` fails when they disagree. The smoke
  tests take the version from the package they are given.
- **After 2.0.1 is released, the version here changes only to a new one,** with a development suffix until
  that one is released. A released version is never changed, and its number is never used again: that is why
  the version after 2.0.0 is 2.0.1 although 2.0.0 never reached NuGet.
- **The XML documentation files cover every public type and member,** the legacy class included. A missing
  comment fails the build, and nothing suppresses that. See
  [D-29](decisions.md#d-29-modern-public-api-is-documented-when-it-is-added) and
  [D-51](decisions.md#d-51-the-legacy-class-is-documented-as-it-behaves).
- **The packed readme can never be edited, so it says nothing that publication would make false,** and
  nothing about whether the version is published at all. It is the repository's README. It says which version
  it documents and gives the installation command with that version
  ([D-86](decisions.md#d-86-a-release-has-written-notes-and-the-readme-names-its-version)). Its wording
  follows [D-84](decisions.md#d-84-public-text-is-positive-direct-and-about-the-library).
- **The readme must satisfy nuget.org,** which is stricter than GitHub; see
  [The readme on nuget.org](#the-readme-on-nugetorg).
- **The `net10.0` build is marked trim- and AOT-compatible** (`IsAotCompatible`), which also turns on the
  corresponding analyzers. The Native AOT smoke test is what backs that claim; keep it passing.
- **Source Link and the repository commit** come from the Git checkout. Pack from a clone with the `origin`
  remote, not from an exported copy.

## The readme on nuget.org

nuget.org shows the `README.md` inside the package, and renders it differently from GitHub. Checked on
2026-11-27 against Microsoft's page "Package readme on NuGet.org" and against the gallery's own renderer
(`MarkdownService.cs` in the NuGetGallery repository):

| What | On nuget.org | So the README |
|---|---|---|
| Images | Shown only from a list of allowed hosts, which includes `raw.githubusercontent.com`. An image with a relative path or from another host is not shown, and only the package owner sees a warning. | uses `https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/master/docs/images/...` |
| Links | A link that is not an absolute address is emptied, except a link that starts with `#`. | uses absolute `https` links, and `#heading` links within the page |
| Headings | Get GitHub-style identifiers, so `#heading` links work. Every heading is shown one level lower. | has one `#` title and `##` sections |
| Raw HTML | Disabled. HTML comments are removed. | has none |
| Supported | Headers, images, emphasis, lists, links, block quotes, code spans and fenced code with highlighting, task lists, pipe tables, emoji, auto-links. | uses fenced code, lists, one image and pipe tables |

`eng/verify-package.sh` checks the image and link rules on the packed file, and `DocumentationLinkTests` checks
them, the HTML rule and that every linked file exists, on the source. Neither renders the file. When the README
was written it was rendered once, outside the repository, with Markdig and the pipeline of the gallery's
renderer: every heading link found its heading, and the tables, code blocks and the image came out as
intended. The preview that nuget.org shows on its upload page is the first real rendering, and looking at it
is a step of any release.

## Building the package

```bash
dotnet build ConsoleCalendarGenerator.sln --configuration Release -warnaserror
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
```

Packing is always an explicit `dotnet pack`; building never creates a package. Only the library is packable.
The result is two files in `artifacts/package/`: the `.nupkg` and the `.snupkg`.

## Package validation

At the end of every `dotnet pack` of the library, the SDK's package validation runs and fails the pack on any of:

- a public API difference between the `netstandard2.1` build and the one in published **1.0.0**
  (`PackageValidationBaselineVersion`);
- a public API difference between the `net10.0` build and the `netstandard2.1` build;
- a renamed parameter (`ApiCompatEnableRuleCannotChangeParameterName`), because that breaks named arguments.

Adding API passes. Removing, renaming or changing it fails with a `CP` error such as `CP0002` (member missing),
`CP0009` (sealed) or `CP0017` (parameter renamed). Package validation checks API shape only; behavior is what
the tests check.

**This stays exactly so for 2.0**
([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)). The five corrections
change behavior and no signature, so 2.0 passes the comparison with 1.0.0 as it is. Do not add a suppression
file, turn a rule off or move the baseline to make something pass; no API break is approved.
`VersionIdentityTests` fails if any of that appears in the project.

Checked in a scratch copy during the 2.0 groundwork, with nothing of it committed: the package with the 2.0
identity passes; removing a public member fails with `CP0002`; renaming a parameter fails with `CP0017`; and
the library with all five corrections applied passes.

### Network and cache behavior

- The baseline is not in the repository. Restoring the library downloads `ConsoleCalendarGenerator` 1.0.0 from
  nuget.org once, into the normal NuGet package cache, exactly like any other dependency. After that, restore
  and pack work offline as far as this package is concerned.
- Building never runs validation. Only packing does.
- To pack without validation, for example with no network and an empty cache:

```bash
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --output artifacts/package -p:EnablePackageValidation=false
```

  That also removes the baseline download from restore. Never use it for a package that is going to be published.

### When the baseline changes

Raise `PackageValidationBaselineVersion` only after a newer version has actually been published and is meant to
be the compatibility baseline. Until 2.0 was published, 1.0.0 was the only version there was to compare with,
and it was the baseline for the 2.0 package itself. 2.0.1 is published now. The baseline is still 1.0.0, and
nothing changed it for the publication: whether a later version is compared with 2.0.1 instead is decided
when work on that version begins, with the version's own development suffix.

## Package verification

```bash
bash eng/verify-package.sh artifacts/package
```

Checks the packed files without building anything: exactly one `.nupkg` and one `.snupkg` and nothing else in
the folder; the exact list of files inside each; the nuspec metadata, including release notes that are the
changelog of this repository; a version that is not 1.0.0, is the
version the repository builds, `2.0.1`, and matches the file names; in both library builds, the assembly
version `2.0.0.0`, the file version `2.0.1.0` and an informational version made of the package version and the commit the nuspec
names; that neither build, its documentation nor its symbols contains anything of the test project, the frozen
source of 1.0.0 included; that the readme's images are files of this repository served from an allowed host and that it has
no relative links; the documentation files, including that both document every public type of the modern
namespace and the legacy class with each of its constructors and methods; and Source Link in both PDBs.

The script holds the list of modern public types. A new public type is added to that list in the change that
adds it; `ModernApiTests` is what fails first when a public type appears.

The file list is exact on purpose. Adding a library target, a packed file or a dependency means updating the
script in the same change.

## Consumer smoke tests

```bash
bash eng/consumer-smoke.sh artifacts/package
bash eng/consumer-smoke.sh artifacts/package --native-aot
bash eng/consumer-smoke.sh artifacts/package --native-aot --published-sample     # what CI runs on Linux
```

Installs the packed package into small applications the way a consumer would, in a temporary folder outside the
repository that is deleted afterwards:

- The package is copied to a local feed. NuGet source mapping allows `ConsoleCalendarGenerator` from that feed
  only, never from nuget.org.
- The NuGet package cache is a temporary folder as well, so the development package never enters the machine's
  real cache. This matters: a cached package would hide later builds that carry the same version, and every
  build of this source carries `2.0.1`.

| Consumer | Must receive | Check |
|---|---|---|
| `net10.0` application | the `net10.0` build | builds, runs, expected output |
| `net8.0` application | the `netstandard2.1` build | builds, runs, expected output |
| `net11.0` application | the `net10.0` build | builds, runs, expected output |
| The sample program that accompanied 1.0.0, unchanged | the `net10.0` build | compiles; on Linux with the `script` program it is also run on a pseudo-terminal and writes the text expected of 2.0 |
| The same program with the published 1.0.0 (`--published-sample`) | published 1.0.0 from nuget.org | builds, runs, writes the text recorded for 1.0.0; its raw terminal bytes differ from the package's in the blank line after a Sunday and in color sequences only |
| That program, as compiled against 1.0.0 and not rebuilt, with the library under test in place of the published assembly (`--published-sample`) | the `netstandard2.1` build, substituted | runs, and writes byte for byte what the rebuilt program writes |
| Native AOT application (`--native-aot`) | the `net10.0` build | publishes without warnings, runs, expected output |
| The `net10.0` and the `net8.0` application and the Native AOT one, run with `culture-audit` | both builds | under every culture the system has, and under cultures given another of their calendars, every legacy title is the culture's name for the Gregorian month (D-61) |

Each application first prints which build of the library it was given and which version that build says it
is. The version has to be the version of the package file that was installed, with assembly version
`2.0.0.0`, so an application that was somehow given another package fails. Each application then creates a
`MonthLayout`, builds `MonthRenderOptions`, renders a month, counts its segments by
kind and prints its text; defines a `DayState`, renders a marked month, finds the state on its segment, asks a
`CalendarTheme` for colors, writes the month with `MonthWriter` to a `StringWriter` and with
`ConsoleMonthWriter` without colors; and then draws a month with the legacy class. Both APIs are therefore exercised from the
package in every kind of consumer, Native AOT included. The `net8.0` consumer stands for every
existing consumer of the `netstandard2.1` build. The `net11.0` consumer
stands for an application such as Campertron. The applications roll forward to whatever runtime is installed;
that does not affect which library build they are given. Templates are in `eng/consumer-smoke/`; they are not in
the solution and are not meant to be built in place.

**The culture audit.** Started with the argument `culture-audit`, the same application
(`AssetProbe/CultureAudit.cs`) draws every month of a year with the legacy class under every culture the system
has and checks each title against the culture's Gregorian name, which it works out itself. It fails on the
first title that is another one. It then prints what the system's cultures are: how many, how many count their
dates in a calendar that is not Gregorian, under how many the title therefore differs from 1.0.0, and how many
offer no Gregorian calendar. That text differs between systems and is printed, not compared; it is the record,
in every CI log, of which cultures the title correction touches there.

### The original sample

`eng/consumer-smoke/OriginalSample/Program.cs` is the sample program that accompanied 1.0.0, byte for byte. It
uses the legacy class through the parameterless constructor, the one-argument constructor, the four-argument
constructor and the setters, draws 34 months, and waits four times for a key. Because it reads keys it needs a
terminal, and what a terminal receives depends on details that have to be fixed before two runs can be
compared ([D-70](decisions.md#d-70-the-original-sample-is-compared-in-one-defined-scenario-by-its-text)).

**The scenario,** as `eng/consumer-smoke.sh` runs it:

| | |
|---|---|
| Program | the sample, built for `net10.0` in Release, started as `dotnet OriginalSample.dll` |
| Runtime | `RollForward` `LatestMajor`: the newest runtime installed |
| Operating system | Linux. The script skips the run elsewhere |
| Terminal | a pseudo-terminal from util-linux `script -qec "<command>" /dev/null`. Standard input, output and error of the program are that terminal; nothing is redirected or piped |
| Settings | `TERM=xterm-256color`; `LC_ALL=C` and `LANG=C`, so the current culture is the invariant culture; `NO_COLOR` unset; terminal echo turned off with `stty -echo` before the program starts |
| Newlines | the program writes `\n`; the terminal turns each into `\r\n` |
| Input | four presses of the letter `x`, one for each `Console.ReadKey`, written to the terminal once echo is off; nothing else |
| Captured, "raw" | every byte the terminal received: the text, the color and keypad escape sequences the .NET runtime writes, the form feed and the two escape sequences the program writes to clear the screen, and one `x` after each prompt, which the runtime echoes when it reads a key. The terminal echoes nothing itself, and `script -q` adds nothing |
| Captured, "text" | the raw bytes without escape sequences and without carriage returns |

**What is compared.** The text, exactly, with one file for each version, because 2.0 corrects what 1.0.0
wrote:

| File in `eng/consumer-smoke/OriginalSample/` | What it is | Compared with |
|---|---|---|
| `expected-text-1.0.0.txt` | What the program writes with the published 1.0.0, recorded from a run of that package. Historical: it never changes, and a test holds its SHA-256 | the run with the published 1.0.0 (`--published-sample`) |
| `expected-text-2.0.txt` | What the program writes with 2.0 | the run with the package under test, and the run of the program compiled against 1.0.0 with the new library put in its place |

The two files differ in one way: 1.0.0 wrote two blank lines after a month whose last day is a Sunday, and 2.0
writes one (D-60). The program draws six such months, April 2023 four times and December 2023 twice, so the
2.0 text is six lines shorter. The script checks that this is the whole difference between the two files, and
`Migration/OriginalSampleTextTests` checks it again without a terminal, together with two more things: that
the frozen source of 1.0.0 writes exactly the text recorded from the published package, and that the library
writes exactly the 2.0 text. The other corrections do not show in the text: the colors of D-57 are escape
sequences, the program passes no null list (D-58), and the last row of a month ending with the console's line
terminator instead of a literal line feed (D-59) is the same character on this terminal, whose terminator is
a line feed.

**The console calls** of the program's calendars are compared in the tests, not here: the twelve calendars it
draws are scenarios of the migration matrix (`MigrationMatrix.OriginalSample`), where the library is compared
with the frozen source call by call, with the colors of every call.

**The raw bytes** depend on the runtime and the terminal database, so they are compared only between two runs
on the same machine. With `--published-sample`, the run with the published 1.0.0 and the run with the package
must be the same bytes once two things are done to what 1.0.0 wrote: the second blank line after a month that
ends on a Sunday is taken out (D-60), and then the sequences that set a color are removed from both (D-57).
The run of the program compiled against 1.0.0 with the new library must be byte for byte the run of the
rebuilt program.

**Measured on 2026-11-27** (Linux, .NET runtime 11.0.0, util-linux 2.42.4, ncurses 6.6; the Linux CI runner has
given the same hashes), for the record and not as a criterion:

| Run | Raw bytes | SHA-256 of the raw bytes | Text |
|---|---|---|---|
| Published 1.0.0 | 21,738 | `cb4664cd9417416a6de600359ce49f2a09705679282e41776a2e681a15f8c43b` | 4,869 bytes, SHA-256 `ad83e16e54f45f7de0a48bf885379c085178e815b444329638fab8b368d9eb6d` |
| 2.0 after the constructor colors (D-57) and the null list (D-58) | 21,738 | `1ef9b52c3ca0094a0511983bdf7133864bf1d1ce44233242ac55a33784e3ff6e` | the same |
| 2.0 after the end of a month as well (D-59, D-60) | 21,726 | `c1b563dd108771435f35a7e76ac5f5a250ec7ff52c10f70b4cab1cf13bf24822` | 4,863 bytes, SHA-256 `260f0c0aafcd4781af8ebe09a8ef3730dad192786713c5913db4720dfa441d18` |

What the differences are:

- **D-57.** The raw streams of 1.0.0 and of 2.0 have the same 2,522 color sequences in the same order and differ
  in 46 bytes, each the color digit of one sequence: the five titles of the one-argument calendar are red where
  they were yellow (5), its seventeen weekday rows cyan where they were red (17), and the twelve titles and
  twelve weekday rows of the four-argument calendar have traded red and blue (24). The sequences are the same
  length, so the byte count did not change.
- **D-60.** Six line ends are gone, one after each of the six months that end on a Sunday. On the terminal a
  line end is a carriage return and a line feed, so the raw stream is 12 bytes shorter and the text 6.
- **D-59** changes no byte here. The program writes `\n` either way, as text in 1.0.0 and as the writer's
  terminator in 2.0, and the terminal turns both into `\r\n`. It shows where the terminator is not a line
  feed, which is Windows, and there the redirected-output tests cover it.

**The two numbers reported before.** Earlier reports gave 21,742 bytes and 21,738 bytes for this program with
the published assembly. Both are reproduced exactly, and they differ in one thing. When the four keys are
already waiting in the terminal as the program starts, the terminal still has echo on and echoes them itself:
four bytes, `xxxx`, at the very start of the output, and the rest is byte for byte the 21,738 of the scenario
above. When the keys arrive after the program is waiting for them, or echo is off as in the scenario, the
terminal echoes nothing. The run behind the 21,738 sent its keys after a delay. What the run behind the 21,742
sent, and when, was not recorded, so that it was this case is a reproduction of the number and not a record
of the run. Neither number was wrong, and neither is a criterion now.

## The release bundle

A release of this package is four files, and `eng/release-bundle.sh` makes them
([D-76](decisions.md#d-76-a-release-is-four-files-made-and-checked-by-scripts)):

| File | What it is |
|---|---|
| `ConsoleCalendarGenerator.<version>.nupkg` | The package, exactly as `dotnet pack` made it |
| `ConsoleCalendarGenerator.<version>.snupkg` | The symbol package, exactly as `dotnet pack` made it |
| `ConsoleCalendarGenerator-<version>-binaries.zip` | The assemblies for use without NuGet. A library, not an application |
| `SHA256SUMS.txt` | The SHA-256 of the other three |

```bash
dotnet build ConsoleCalendarGenerator.sln --configuration Release -warnaserror -p:ContinuousIntegrationBuild=true
# ... the tests ...
dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package -warnaserror
bash eng/release-bundle.sh artifacts/package artifacts/release-bundle     # makes the bundle, then checks it
bash eng/verify-release-bundle.sh artifacts/release-bundle                # the check alone
bash eng/release-bundle-tamper-test.sh artifacts/release-bundle           # the check refuses a damaged bundle
bash eng/direct-reference-smoke.sh artifacts/release-bundle               # a program that references an assembly directly
bash eng/upgrade-smoke.sh artifacts/package                               # a program upgraded from the published 1.0.0
bash eng/verify-published-package-selftest.sh artifacts/release-bundle    # the check that follows publication, rehearsed
bash eng/release-state.sh                                                 # the repository agrees with itself about its version
```

None of these publishes, uploads, tags or pushes, and none changes a file of the repository. They need `bash`,
`unzip`, `git` and the .NET SDK, and work on Linux, macOS and Windows (Git Bash). **Linux is the authoritative
place to make a bundle:** it is where CI uploads one, and where line ends in the readme and the documentation
files are the repository's. A bundle made on Windows passes the same checks but is not the same bytes.

**The archive.**

```text
ConsoleCalendarGenerator-<version>/
  LICENSE
  README.md
  net10.0/ConsoleCalendarGenerator.dll, .pdb, .xml
  netstandard2.1/ConsoleCalendarGenerator.dll, .pdb, .xml
```

- **Nothing in it is built for it.** The assemblies, their documentation files and the readme are taken out of
  the package, the symbols out of the symbol package, and the license from the commit the package names. The
  script also requires them to be this checkout's Release build, byte for byte, which is the build the tests
  ran against. So the package, the symbol package, the archive and the tested build cannot differ.
- **It is reproducible.** `eng/tools/StoredZip` writes it: entries in ordinal order of their paths, stored and
  not compressed, each with the same fixed time, with no attributes and no name of an operating system. The
  same files give the same archive on any system and any runtime. The class library's own ZIP writer records
  the operating system and its file permissions, which is why the tool writes the format directly. The check
  makes the archive again from its own contents and requires the same bytes.
- **No name of a machine.** A build for a bundle is made with `ContinuousIntegrationBuild`, which CI sets by
  itself, so the symbols map their sources from `/_/` to the commit on GitHub. The check refuses symbols or an
  assembly that name a folder of the machine that built them.
- It holds no test assembly, no example and nothing else: the list of entries is exact.

**The checksums.** One line for each of the other three files, in the order of their names:
`<64 lowercase hexadecimal digits><two spaces><file name>`, ending with a line feed. That is what `sha256sum`
writes, so anyone can check a downloaded release without this repository: `sha256sum -c SHA256SUMS.txt` on
Linux and in Git Bash, `shasum -a 256 -c SHA256SUMS.txt` on macOS. The check in this repository is stricter:
exactly three lines, exactly these names, in order, lower case, every checksum right.

**What the bundle script refuses:** a package built from another commit than the checkout is at; a checkout
with uncommitted changes; a package whose assemblies are not this checkout's Release build; an output folder
that is not empty; and, through the check, everything the tamper test tries. The tamper test damages a copy of
a good bundle in sixteen ways, one at a time: a byte changed or added in each file, a file missing or too
many, checksums in upper case, out of order, short of a line or with one too many, and an archive put together
again with another file, another assembly or another license and given checksums that match.

**Using the assemblies directly** (`eng/direct-reference-smoke.sh`). A `net10.0` program references
`net10.0/ConsoleCalendarGenerator.dll` by its path, and a `net8.0` program the `netstandard2.1` one. Neither
has a package reference, and NuGet is offered no source for the library. Each must run with exactly the
assembly from the archive, find the symbols and the documentation beside it, use both APIs, and report the
behavior of 2.0. Native AOT stays tested through the package.

**Upgrading from 1.0.0** (`eng/upgrade-smoke.sh`). A `net8.0` and a `net10.0` program reference
`ConsoleCalendarGenerator` 1.0.0 from nuget.org, build, run and report the behavior of 1.0.0. Code that uses
the modern API does not compile there. The reference is then upgraded with `dotnet add package` to the package
under test, from a local feed that is the only source allowed for it; the source of the program is not
touched. It builds again, now with the modern API, runs with the build of the package meant for its framework,
byte for byte, and reports the behavior of 2.0. NuGet's cache is a temporary folder, so no cached package can
stand in for either version. This is a real change of a package reference, and is separate from running the
sample of 1.0.0 and from putting the new assembly under a program that is not rebuilt, which
`eng/consumer-smoke.sh` does.

**The program both use** is `eng/consumer-smoke/MigrationProbe`. Every line it prints is one fact about the
legacy class. `expected-1.0.0.txt` and `expected-2.0.txt` are what it prints with each version: the lines
under `[unchanged]` are the same in both, each of the five numbered lines under `[corrected in 2.0]` differs,
and only the second has the section of the modern API. A test and the script both require exactly that, so
the two files are the migration from 1.0.0 written as data. The published 1.0.0 produced the first file.

**The state of the release** (`eng/release-state.sh`) checks, on every build, that the repository agrees with
itself about what it holds, and changes nothing. With no version suffix, as now, that is a release: the
project evaluates to package version `2.0.1`, assembly version `2.0.0.0`, file version `2.0.1.0` and an
informational version of `2.0.1` and the commit; the package check and the version tests expect that version
and no public document states a development version; the README has the links of the version at the tag
(`eng/release-links.sh v2.0.1 --check`); the changelog's first section is the section of 2.0.1, whose heading
holds the date of publication or "Unreleased" and never a guessed date; and the notes of the release are
written (`eng/release-notes.sh 2.0.1`). With a suffix, for the development of a later version, the same script
is a rehearsal of release preparation.

**The README's links** (`eng/release-links.sh`). `eng/release-links.txt` lists every link of the README into
this repository with `tag` or `master`
([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)); the tests read the same file.
For 2.0.1 the README was written with the three links at the tag `v2.0.1`. `--check` verifies exactly that state. Without an option the script rehearses the substitution on a
README that still has the links on the default branch, which is its use for a later version.
`eng/verify-package.sh` requires the image of a packed readme on `master` for a version with a suffix and at
the version's own tag for one without.

**In CI** all of this runs on Linux, macOS and Windows after the consumer smoke tests. The Linux job uploads
the bundle as the artifact `release-bundle`, kept 90 days, and prints its checksums. Every run does, for every
commit; which one bundle is the candidate is decided by which run it came from
([The candidate](#the-candidate)), and no other run's checksums are those of the release.

What the first CI run with this machinery showed, for the record and not as a criterion: the Linux job and the
macOS job made the same archive from the same commit, byte for byte, so the compiler's output and the archive
are both independent of the system there. The Windows job's archive differs, because its readme and
documentation files have Windows line ends. The package and the symbol package differ between all three jobs:
`dotnet pack` does not write the same package file twice. That is why a release is built once and its files
are kept, and not built again and compared.

### Artifact attestations: not now

GitHub can attest that a file was built by a workflow run of this repository at a commit
(`gh attestation verify <file> -R <repository>`). It was considered for the four files and **deferred**
([D-77](decisions.md#d-77-no-artifact-attestations-for-now)):

- Creating one needs `id-token: write` and `attestations: write` in the job that does it. The workflow that
  builds runs on every pull request and every push with `contents: read` and nothing else, and that is worth
  more here than what an attestation adds.
- An attestation is a public, permanent record. A job that attests on every push to `master` would make
  them for every bundle of every commit, not for the one that is the release.
- An immutable GitHub release, which is a repository setting and part of the plan, makes GitHub record an
  attestation of the release's tag, commit and assets by itself, with no workflow permission; and
  `SHA256SUMS.txt` lets anyone compare a download with the release.

If it is wanted later, the clean way is a job of its own that runs only for the release commit, has those two
permissions and no others beyond reading, takes the bundle the build job uploaded and attests it, without
rebuilding. That is a change of the workflow's permissions and needs its own decision.

## Release boundaries

Each of the following needs Shaun's **separate, explicit approval**, every time:

- creating a tag
- creating a GitHub release
- publishing to NuGet
- a manual workflow dispatch or rerun that could publish or change external state
- recovering from a failed or partial publication
- changing a repository setting or a ruleset, or using a ruleset's bypass

Approval of a phase, of a pull request or of one of these actions does not cover another. When in doubt, stop
and ask. Routine work (branches, commits, pull requests, CI runs triggered by pushes) is not in this list.

The publication workflow exists ([Publication to nuget.org](#publication-to-nugetorg)). Starting it is
"publishing to NuGet" in the list above, and running one of its runs again is the rerun the list names.
Changing the workflow, the environment it uses or the policy on nuget.org changes the release mechanism: do
not change one unless a phase is explicitly scoped to do so.

## Release preparation checklist

What the change that prepares a release does to the repository. It was done for 2.0.0, and again for 2.0.1 in
the pull request that rewrote the public presentation. It is kept here as the list for the next version.

**The version.** The project file has no `VersionSuffix`, so every build is 2.0.1. The three places that
state the version changed together: the project file, `expected_version` in `eng/verify-package.sh`, and
`VersionIdentityTests`. The assembly version stays `2.0.0.0`; the file version follows the package version.

**The README's links** ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)). The README
is packed into the package and shown on nuget.org for as long as the version exists. A link whose target is
part of the version is at the tag `v2.0.1`; a link to a document of current policy stays on `master`.

| Link, as `eng/release-links.txt` lists it | In the README of the release |
|---|---|
| `https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/master/docs/images/month-with-theme.png` | `https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/v2.0.1/docs/images/month-with-theme.png` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/global.json` | `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/v2.0.1/global.json` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/tree/master/ConsoleCalendarGeneratorExample` | `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/tree/v2.0.1/ConsoleCalendarGeneratorExample` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CHANGELOG.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CONTRIBUTING.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/SECURITY.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/LICENSE` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues` | unchanged |

For a later version the edit is one command, `bash eng/release-links.sh <tag> --apply`.
**Until the tag exists, those three addresses answer "not found". That is expected,** and it ends when the tag
is put on the release commit, without any file changing. What would be a defect is an address of another
shape, or one that names something the release commit does not have: the tests and
`eng/release-links.sh v2.0.1 --check` fail on those, and neither uses the network. After the tag exists,
open the three addresses once, before the package is published. For 2.0.1 the tag exists, and the three
addresses were opened and answer with the release commit's content.

**The README's text.** It says which version it documents and gives the installation command with that
version ([D-86](decisions.md#d-86-a-release-has-written-notes-and-the-readme-names-its-version)). It says
nothing about whether the version is published, in either direction.

**The changelog.** It has a section for each version. While a release is being prepared its heading has the
word "Unreleased" where the date will be: the date of publication is not known then, and a date is never
guessed. Once the version is published the heading gets that date, as the section of 2.0.1 has
(`## [2.0.1] - 2026-11-28`). The section of 2.0.0 has its date and says that the version was distributed
through GitHub Releases and was not published to NuGet. Each version's link definition points at its release
on GitHub.

**The notes of the release.** `docs/release-notes/2.0.1.md` is the text the GitHub release carries, written
for the people who read the release page. `eng/release-notes.sh 2.0.1` prints it after checking that it names
the four files of that version and links to no other tag. It is a text of its own, not the changelog's
section.

**`SECURITY.md`** has a row for the 2.x line and a row for 1.0.0 that say where each is available. **`CONTRIBUTING.md`** says how to
contribute and nothing about the state of a release.

### What is finalized after publication

None of this changes a file of the candidate. The changelog, `SECURITY.md` and `CONTRIBUTING.md` are not in
the package and not in the archive, and everything links to them on `master`. For 2.0.1 it was done on
2026-11-28, in one pull request after the publication was verified; the list stays for the next version.

- **The date in the changelog.** The heading of the version gets the date it was published to NuGet, in
  Shaun's time zone like every date of the project: `## [2.0.1] - 2026-11-28`. A test requires exactly that
  heading, and that nothing in the changelog is "Unreleased".
- **The table in `SECURITY.md`.** It states the present, and names no patch version of 2.x, so it does not
  change with each release: the 2.x line is on GitHub Releases and on NuGet, 1.0.0 is on NuGet, and both are
  supported. What it says about 1.0.0 follows Shaun's decision on that version and changes only with it
  ([D-91](decisions.md#d-91-201-is-published-on-nuget-and-100-stays-listed-and-supported)). A test
  requires those two rows.
- **The test of what the documents say about availability**
  (`NoDocumentPutsTheGitHubOnlyReleaseOnNuGet_CallsTheFirstVersionDeprecated_OrAnnouncesThisRelease`): no
  document puts 2.0.0 on NuGet, none calls 1.0.0 deprecated or unsupported, and none announces the release.
  The README was not changed for the publication and did not need to be: it names its version and the command
  that installs it, which is as true after the publication as before.
- **The maintenance notes,** which state where every version is.
- **The package-validation baseline and the next development version,** when work on a later version begins
  ([When the baseline changes](#when-the-baseline-changes)). Not done, and not due.

## The candidate

**Two releases, two candidates.**

| | 2.0.0 | 2.0.1 |
|---|---|---|
| Release commit | `117df06e5b6d684da6891cd3ac0310e5aa81ec98`, the merge commit of its release preparation | `bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd`, the merge commit of the wording changes that followed the final editorial review ([D-87](decisions.md#d-87-the-release-commit-of-201-is-the-merge-of-the-final-wording-changes)) |
| Workflow run | 46083727520, first attempt, never run again | 46097347304, the first push run for that commit, first attempt, never run again |
| State | designated, tagged `v2.0.0`, and released on GitHub; not on NuGet, and never to be | designated, tagged `v2.0.1`, released on GitHub, and published on NuGet on 2026-11-28; verified in each place |
| Record and checksums | outside the repository, with the four files, read-only, kept unchanged | the record outside the repository, in a folder of its own, with the four files, read-only; the checksums also in `eng/publication.sh` |

The candidate of 2.0.0 is history that stays: its four files are the files of the immutable GitHub release,
and its package is not authorized for NuGet ([D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget)).
The candidate of 2.0.1 is on GitHub and on NuGet: its package and its symbol package are what was pushed.

**A candidate of 2.0.1 was superseded.** The first one was the bundle of the first push run (46094403379)
for `097eace95f90d668badf9684188f32c3db164f4b`, the merge commit of the pull request that set the version to
2.0.1. It was designated, and then Shaun read the public text once more and approved ten wording
improvements before any tag. The README is inside the package, so the improvements needed a new commit and
with it a new candidate. The superseded candidate is kept as it is, with a record that says so, and is never
tagged, released or published. Nothing was wrong with its code: the version stays 2.0.1, because no tag,
release or publication of 2.0.1 existed.

`master` may get commits after a release commit. They are not part of that release, and its tag does not go
on them. CI makes a bundle for every push, and each says the version of the project file: none of those is a
candidate. `eng/designate-candidate.sh` is run once for a release commit, while it is the head of `master`.
It was run for both release commits, and is not run again for either.

The rule ([D-79](decisions.md#d-79-the-candidate-is-the-linux-bundle-of-the-first-push-run-for-the-release-commit)):

> The candidate of a version is the artifact `release-bundle` of the Linux job, of the first run of the workflow
> that a push to `master` started for the release commit, in its first attempt.

- It is **never built again.** Not by running the workflow again, which builds again; not locally; not from a
  pull request; not by the Windows or the macOS job, whose bundles are never uploaded.
- It is **never replaced.** If that run failed, or its artifact does not pass every check, **there is no
  candidate for that commit.** The problem is corrected in a new reviewed commit, and the first push run for
  the new merge commit is the next one to look at.
- **Do not run the candidate's workflow run again, ever.** A rerun leaves the run with a second attempt, and
  the designation refuses a run that is not in its first attempt.

`eng/designate-candidate.sh <run id> <folder>` enforces the rule and does the work. Before it downloads
anything it requires: this checkout at the release commit, which is the head of `master` on GitHub, with
nothing uncommitted and no version suffix; the run a push run on `master` for exactly that commit, the first
such run, in attempt 1, with all three jobs successful; and exactly one unexpired artifact of that name for
that commit. Then it downloads the artifact once and runs, on those exact files: the bundle check with its
checksums, the package check, the tamper test, direct use of the assemblies, the upgrade from the published
1.0.0, the consumer smoke tests with Native AOT and the sample of 1.0.0, the rehearsal of the comparison that
follows publication, and the release-state check. Only if all pass does it write the record.

**The record** is `CANDIDATE-RECORD.txt`, beside the bundle and not inside it, with `RELEASE-NOTES.md`. It
names the repository, the version, the release commit, the run with its number, event, branch and attempt, the
Linux job, the artifact with its id, size, digest and the date GitHub keeps it until, the time of the
download, the SHA-256 of each of the four files, and the result of each check. The kept files are made
read-only.

**Where it lives.** Outside the repository, in a folder on the maintainer's machine, and in the report of
the phase that designated it. It is **not committed.** A commit after the release commit would be a commit
the candidate was not built from; the tag of the version belongs on the release commit, and the record must not
blur which commit that is. GitHub also keeps the artifact itself for 90 days from the run, so the files can
be downloaded again and compared with the recorded checksums. From the GitHub release on, the release holds
them.

## After publication: what is checked, and what cannot be

`eng/verify-published-package.sh <folder with the candidate>` runs after the version is on nuget.org. It needs
no credential ([D-80](decisions.md#d-80-after-publication-the-served-package-is-compared-by-its-entries-and-the-symbols-through-the-symbol-server)).
In the publication workflow it is the last job: `eng/publication-verify.sh` downloads the four files of the
GitHub release again, compares them with the approved checksums, and runs this check on them with
`--wait 45 --native-aot`. With `--wait`, the two things that depend on nuget.org's own pace, the version being
served and listed and the symbol server having the symbol files, are asked for again every half minute until
they hold or the minutes are over. Waiting only reads: nothing is pushed again.

**nuget.org changes the package file.** It signs every package it serves by adding one entry,
`.signature.p7s`. So the file downloaded from nuget.org is **not** the file that was pushed, its size is
another one, and its SHA-256 is **not** the one in `SHA256SUMS.txt`. That is expected, and the check says so
in its output; it does not pretend the two files are the same. What it requires instead:

1. **nuget.org's own record.** The version is listed, and the size and the SHA-512 that nuget.org's catalog
   records for the package are those of the file that was downloaded.
2. **The entries.** The served package has `.signature.p7s`; it has every entry of the candidate, byte for
   byte, the manifest included; and it has no other entry. A missing, changed or added entry is a failure.
3. **The served package as a package** (`eng/verify-package.sh --published`): identity, version, metadata,
   license, no dependency, both builds with their documentation, the readme, the commit, the versions the
   assemblies carry. Both assemblies must have exactly the public API of `eng/public-api.txt`; they are the
   candidate's bytes, and step 5 compiles and runs against them.
4. **The symbols.** See below.
5. **Installed from nuget.org.** A `net8.0` and a `net10.0` program install the version from nuget.org alone,
   build, run with the candidate's assembly for their framework, use both APIs and report the five
   corrections. The consumer smoke tests and the upgrade from 1.0.0 then run against the served package,
   with a Native AOT application when `--native-aot` is given.

**The symbol package cannot be downloaded back.** nuget.org offers no address for a `.snupkg`: the one a
package has returns "not found" for it. Nothing here claims otherwise. What is done:

- **Before publication:** every bundle check requires each symbol file to be the one its assembly was built
  with, by the identifier and the checksum the assembly records (`eng/tools/SymbolIdentity`). That is the
  validation nuget.org applies to a symbol package after the package itself is published and cannot be taken
  back, so it is made first. The exact bytes of the symbol package are in `SHA256SUMS.txt`, and the file is
  one of the four attached to the GitHub release.
- **At publication:** the output of the push is recorded. It says whether the symbol package was accepted.
- **After publication:** nuget.org's symbol server gives a symbol file to anyone who has the assembly, at
  `https://symbols.nuget.org/download/symbols/<name>/<identifier>FFFFFFFF/<name>`, with the checksum the
  assembly records in a `SymbolChecksum` header. The check fetches both symbol files that way and requires each
  to be the candidate's, byte for byte. Validation of a symbol package can lag behind the package; until it is
  done the server answers "not found", and the package's page on nuget.org shows the state.
- **Not verifiable:** the bytes of the symbol package as nuget.org stores it. Only its two symbol files can be
  fetched.

**What was tested before any publication.** `eng/verify-published-package-selftest.sh` runs on every build. It
downloads the published 1.0.0, which nuget.org signed in 2023; confirms the download has the size and the
SHA-512 nuget.org records and has the signature entry; takes it apart and puts it together without that entry,
which gives a stand-in for the file that was pushed; and requires the comparison to pass on that pair, the
only pair of "pushed" and "served" this project can have before it publishes again. It then gives the
bundle's own package a signature the same way and runs the whole offline check on it, and makes one difference
at a time, twelve in all, each of which must be refused. The symbol server's behavior was checked by hand on
two public packages that have symbols: with the identifier and the header the server returned the symbol file
of the assembly; without the header it refused; for a symbol file that was never published it answered "not
found". Steps 1, 4 and 5 could run against this package only once it was published. They ran for 2.0.1 on
2026-11-28, in the last job of the publication, and passed
([The publication of 2.0.1](#the-publication-of-201)).

## Publication to nuget.org

Set up on 2026-11-28 and **run once, the same day, for 2.0.1**
([D-88](decisions.md#d-88-201-goes-to-nuget-through-trusted-publishing-from-a-workflow-that-is-started-by-hand),
[D-89](decisions.md#d-89-the-workflow-publishes-one-approved-release-and-its-facts-are-in-the-repository),
[D-90](decisions.md#d-90-the-environment-allows-the-default-branch-asks-its-reviewer-and-holds-no-secret)).
Three things exist, and each names the others:

| | Where | What it is |
|---|---|---|
| The workflow | `.github/workflows/publish-nuget.yml` | Publishes the package and the symbol package of 2.0.1. Started by hand, and by nothing else |
| The environment | GitHub: `nuget-production` | What the job that publishes must be let into ([development.md](development.md#the-environment-nuget-production)) |
| The policy | nuget.org: Trusted Publishing, account `ShaunHathaway` | Gives that workflow, in that environment, a temporary key for new versions of this package |

**Starting the workflow is a publication.** It needs Shaun's separate, explicit approval, each time
([Release boundaries](#release-boundaries)). That the workflow exists is not that approval, and neither is the
review its environment asks for, which comes after the start. It is started for `master`, with its four
inputs as they are. **A run of it is never run again:** if a run fails, what happens next is decided first.
**For 2.0.1 it was started once and succeeded, and it is not started again for that version:** the checks
would refuse, because nuget.org lists the version, and there is nothing left to publish.

**There is no key.** nuget.org's Trusted Publishing gives the job a key that lasts an hour, in exchange for
the identity token GitHub issues to that job. nuget.org compares the token with the policy: the repository and
its owner, by their permanent ids, the workflow file and the environment. No API key was created on nuget.org
and nothing is stored on GitHub: the repository and the environment have no secret. The environment has one
variable, `NUGET_USER`, the nuget.org user name `ShaunHathaway`, which is public. The action that logs in
needs the user name of whoever created the policy; it is a user name, not an e-mail address.

**The policy on nuget.org,** as it was read back after it was created:

| Field | Value |
|---|---|
| Name | ConsoleCalendarGenerator publish-nuget.yml |
| Package owner | `ShaunHathaway`, the individual account that owns the package |
| Publisher | GitHub Actions |
| Repository owner | `ShaunAnthonyHathaway`, GitHub id 105829169 |
| Repository | `ConsoleCalendarGenerator`, GitHub id 650463678 |
| Workflow file | `publish-nuget.yml`: the file name, without its folder |
| Environment | `nuget-production` |
| Scope | Push only new package versions. Not new packages, not unlisting or relisting, not staging |
| Packages | `ConsoleCalendarGenerator`: the exact ID, with no pattern |
| State | Active |

It is the only policy of the account. nuget.org's documentation says a policy can start out "temporarily
active" for seven days, until a first publication supplies GitHub's ids; this one showed both ids and "Active"
as soon as it was created. It is read and changed on nuget.org, on the account's Trusted Publishing page, by
Shaun. The policy names the workflow file and the environment, so renaming either breaks the match. If
nuget.org ever shows the policy as inactive, **do not create a second one:** that is for Shaun to look at.

**What can be published** is one release, stated once, in `eng/publication.sh`: version 2.0.1, the tag
`v2.0.1` with its tag object, GitHub release 461246889, the release commit, and the SHA-256 of the four files.
The workflow's inputs (`version`, `tag`, `release_id`, `release_commit`) must repeat those values. They are
asked for so that whoever starts the publication says what is published; they select nothing, and any other
value is refused before anything else is looked at. There is no input that turns a check off and no dry-run
mode of the workflow.

**Three jobs,** each needing the one before it:

| Job | May | Does |
|---|---|---|
| `preflight` | read | Every check below, with `eng/publication-preflight.sh`. It shows whether everything holds before anyone is asked to approve |
| `publish` | read, and ask GitHub for an identity token. It is the only job in the environment `nuget-production` | The same checks again, on the files it downloads; then the login; then the push of exactly those files |
| `verify` | read | `eng/publication-verify.sh`: gives nuget.org up to 45 minutes, then checks what it serves ([After publication](#after-publication-what-is-checked-and-what-cannot-be)) |

**The checks before the push** (`eng/publication-preflight.sh`), in this order, each before the next:

1. The four values asked for are the approved version, tag, release id and release commit.
2. The run is a manual run of the publication workflow of this repository, for `master`, in its first
   attempt. In the job that publishes, the user for nuget.org is the approved one.
3. nuget.org lists 1.0.0 and nothing else, and has no package file of 2.0.1.
4. The tag `v2.0.1` is annotated, is the approved tag object, and peels to the release commit: on GitHub, and
   in the checkout, which must have the release commit in its history.
5. The GitHub release is the approved one: published, immutable, not a prerelease, the release GitHub has for
   the tag, with exactly four files of the right names, each uploaded, each recorded by GitHub with the
   approved checksum.
6. The four files, downloaded from the release by their ids, have the approved SHA-256, and `SHA256SUMS.txt`
   verifies the other three.
7. `eng/verify-release-bundle.sh` passes on them: the package and the symbol package as
   `eng/verify-package.sh` expects, the archive, and each symbol file the one its assembly records.
8. The package is `ConsoleCalendarGenerator` 2.0.1, names the release commit, has no dependency, and holds both
   builds, with assembly version `2.0.0.0`, file version `2.0.1.0` and the informational version of the
   release.
9. Both assemblies have exactly the public API of `eng/public-api.txt`.
10. nuget.org still does not have the version. The checksums of the four files are compared once more.

All ten pass before the login: the login is the next step of the same job and has no condition, and a step
runs only when every step before it succeeded. The checks build three small tools of their own (`eng/tools/`).
Nothing of the library is built, packed or changed.

**The push.** One command, the only one in the repository that publishes: `dotnet nuget push` of
`ConsoleCalendarGenerator.2.0.1.nupkg`, from the folder the checks downloaded into, to
`https://api.nuget.org/v3/index.json`, with the temporary key. The symbol package of the same name beside it
is pushed with it. In the same step, directly before it, `eng/publication-checksums.sh` compares the four
files with the approved checksums again. The key reaches that one step, as an environment variable. It is not
printed, not written to a file and not handed to another step or job, and the output of the push is kept
only after it was searched for the key. There is no `--skip-duplicate`: that the version is not on nuget.org
is checked, not assumed.

**When something fails.** The workflow stops; it never repairs.

- **Before the push:** nothing is published.
- **nuget.org already lists the version:** the checks say so and stop. No login is made and nothing is pushed.
- **The push fails,** as a whole or after the package and before the symbols: the job fails. Its summary has
  the output of the push, which says what nuget.org accepted, and what nuget.org lists. Nothing is tried
  again, and the verification does not run.
- **The verification fails:** the version is on nuget.org as it was pushed. Nothing is pushed again,
  replaced, unlisted or deprecated.
- **In each of these cases what happens next is Shaun's decision,** and starting the workflow again is not a
  repair. A run started after a push finds the version on nuget.org and stops before the login.

**Without publishing.** What can be shown before the day:

```bash
# The checks, against the real release: reads GitHub and nuget.org, downloads and compares. Needs gh and jq.
bash eng/publication-preflight.sh --version 2.0.1 --tag v2.0.1 --release-id 461246889 \
  --release-commit bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd
# The same, on files that are already there, such as the kept candidate:
bash eng/publication-preflight.sh --version 2.0.1 --tag v2.0.1 --release-id 461246889 \
  --release-commit bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd --bundle <folder with the four files>
# That the checks refuse what they must: recorded answers stand for GitHub and nuget.org. No network.
bash eng/publication-preflight-selftest.sh [<folder with the four files>]
```

Outside a workflow the first two are a dry run: they pass when everything is ready, and they have no way to
publish. They passed before the publication of 2.0.1. Since it, they stop at the third stage, as they must,
because nuget.org lists the version; that is the answer of a guard doing its work, not a defect to repair.
The self-test makes one thing wrong at a time, 45 things without the files and 46 with them, and
requires each to be refused for its own reason; with the files it also requires the right answers to pass. A
simulation is refused inside a workflow. The tests read the workflow itself: its one trigger, its
permissions, its pins, that it builds nothing, where the key goes, and which steps would run after a failure
of each step ([testing.md](testing.md#the-release-checks)).

**A later version.** The workflow is written for 2.0.1, on purpose, and was not made general after it ran.
After a later version's GitHub release exists: change the stated facts in `eng/publication.sh`, the defaults
of the workflow's inputs, the file name in the push and what nuget.org is expected to list beforehand, in one
reviewed pull request. The tests compare them with each other and with the project file. The policy on
nuget.org needs no change as long as the workflow file, the environment and the package keep their names. A
later version is published the same way: the immutable files of its GitHub release, Trusted Publishing, no
key.

### The publication of 2.0.1

Done on 2026-11-28, with Shaun's explicit approval for that one publication
([D-88](decisions.md#d-88-201-goes-to-nuget-through-trusted-publishing-from-a-workflow-that-is-started-by-hand),
[D-91](decisions.md#d-91-201-is-published-on-nuget-and-100-stays-listed-and-supported)).
The full record, with the times and the output of each step, is outside the repository, beside the candidate.

| | |
|---|---|
| Package | <https://www.nuget.org/packages/ConsoleCalendarGenerator/2.0.1>, listed |
| Run | 46137844158 of `publish-nuget.yml`: started by hand, for `master`, run number 1, attempt 1, all three jobs successful. Never run again |
| Deployment | one, to `nuget-production`, approved once, after the preflight job had passed |
| Authentication | Trusted Publishing: GitHub's identity token for the job, exchanged for a temporary key. No long-lived key was created and nothing is stored |
| Pushed | `ConsoleCalendarGenerator.2.0.1.nupkg`, SHA-256 `e9a2a67f4d302a966ec7d7fe5c8d270c5458481f6d2bd831c5db21811d04ac76`, and with it `ConsoleCalendarGenerator.2.0.1.snupkg`, SHA-256 `ee5e2789d1adba2a64b40f1728a5c5735c64bb05201f9e238c369b3716969502`: the files of the immutable GitHub release, downloaded by the job and compared with those checksums directly before the push. nuget.org accepted both |
| Listed | about five minutes after the push; the symbol files were on the symbol server about ten minutes after that |

What the last job found, and what was looked at again afterwards:

- **The package nuget.org serves is the candidate's package with one entry added.** nuget.org added
  `.signature.p7s`. All nine entries of the candidate's package are in the served one, byte for byte, and
  there is no other entry. The served file is therefore larger and has another checksum, as expected.
- **Its metadata is the release's:** the ID and the version, the commit `bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd`,
  the license, no dependency, both builds with their documentation, the readme. The public API of both
  assemblies is exactly `eng/public-api.txt`.
- **Installed from nuget.org alone,** a `net10.0` program gets the `net10.0` build and a `net8.0` program the
  `netstandard2.1` build, each the candidate's assembly. Both use the modern API and the legacy class and
  report the five corrections and nothing else changed.
- **The upgrade from 1.0.0 passes,** and so do the consumer smoke tests against the served package, with a
  Native AOT application.
- **Both symbol files on nuget.org's symbol server are the candidate's,** byte for byte.
- **The readme renders on the package's page** as intended: the image, which is served from the tag, the
  code blocks, the tables and the links.
- **Not verifiable,** as before: the bytes of the symbol package as nuget.org stores it.

Nothing else changed by it: 1.0.0 is listed and not deprecated, 2.0.0 is not on NuGet, both GitHub releases
and both tags are as they were, and the policy and the environment are as they were set up.

## Planned 2.0 publication sequence

The plan of the release, with what is done. See
[decisions.md](decisions.md#d-20-20-is-to-be-published-and-verified-on-nuget),
[D-72](decisions.md#d-72-the-release-is-200-built-once-and-published-as-the-files-that-were-validated),
[D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget) and
[D-88](decisions.md#d-88-201-goes-to-nuget-through-trusted-publishing-from-a-workflow-that-is-started-by-hand).
Every step was a phase or an action that Shaun scoped and approved by itself; finishing one did not start
the next. **The sequence is complete for 2.0.1.** What remains is not part of it.

Done:

- The implementation: the modern API and the five corrections.
- The user documentation and the migration guidance, the release infrastructure, and the release-readiness
  audit.
- **2.0.0:** release preparation, the candidate, the repository settings, the tag `v2.0.0` and the GitHub
  release, which is published and immutable. Its NuGet publication was stopped before it began and does not
  happen.
- **2.0.1:** the public presentation rewritten
  ([D-84](decisions.md#d-84-public-text-is-positive-direct-and-about-the-library)), release preparation, and
  a final editorial review of everything public, with ten wording improvements. Then the candidate, designated
  from the first push run for the merge commit of those improvements; the tag `v2.0.1` on that commit; and the
  GitHub release "ConsoleCalendarGenerator 2.0.1", created as a draft with the written notes and the
  candidate's four files, checked, and published. It is immutable and marked Latest in place of 2.0.0.
- **The setup of the NuGet publication:** the workflow, the environment and the policy on nuget.org
  ([Publication to nuget.org](#publication-to-nugetorg)).
- **The NuGet publication of 2.0.1:** one manual run of the publication workflow, which pushed the candidate's
  package and symbol package, the bytes that are attached to the GitHub release
  ([The publication of 2.0.1](#the-publication-of-201)).
- **Verification:** the last job of that run, `eng/publication-verify.sh` on the files of the release
  ([After publication](#after-publication-what-is-checked-and-what-cannot-be)).
- **Finalizing on `master`:** the date in the changelog, the table in `SECURITY.md` and these notes
  ([What is finalized after publication](#what-is-finalized-after-publication)).

Not part of the sequence, and not done:

1. **Deprecation of 1.0.0,** if Shaun decides it
   ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)). Separate, explicit approval. Until
   then 1.0.0 stays listed and supported. Nothing in the repository deprecates, unlists or deletes a version.
2. Campertron's adoption of the package, which belongs to the separate Campertron project and chat and is not
   planned from here.

**A released version is never changed or replaced.** 2.0.0 is the example: its documentation was rejected
after its GitHub release was published, the release stayed as it is, and the correction is a new version. If
anything is found wrong in 2.0.1, the correction is again a new version, made and approved the same
way. A partial publication, for example the package pushed and the symbols refused, is recovered only with
Shaun's approval ([Release boundaries](#release-boundaries)). nuget.org's own editing of a published package's
metadata is not part of this plan: nothing in the package needs editing after publication.

`AssemblyVersion` is `2.0.0.0`. The package-validation baseline stays 1.0.0: 2.x differs from 1.0.0 in
behavior only ([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)). No 1.x
version and no prerelease was published
([D-55](decisions.md#d-55-the-next-published-version-is-20)).
