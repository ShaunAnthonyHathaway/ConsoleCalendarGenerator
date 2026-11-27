# Packaging and releases

## Current state

- **Published on nuget.org:** `ConsoleCalendarGenerator` 1.0.0 only (verified 2026-11-27). It contains a single
  `netstandard2.1` assembly, placeholder metadata, and no readme, license, symbols or documentation.
- **Built from `master`:** `ConsoleCalendarGenerator` `2.0.0-dev`, a development version of 2.0 with assembly
  and file version `2.0.0.0`
  ([D-65](decisions.md#d-65-the-repository-builds-200-dev-with-assembly-and-file-version-2000)). It is produced
  locally and in CI for validation and has never been published. No prerelease is published.
- **The release machinery exists and runs on every build, on a development version.** CI makes the four
  files a release consists of (the package, the symbol package, an archive of the assemblies, a file of
  checksums), checks them, uses the assemblies directly, upgrades a program from the published 1.0.0, and
  rehearses release preparation. See [The release bundle](#the-release-bundle). What it makes is a
  development bundle: **no candidate for a release has been built or named, and no checksum of a release
  exists.**
- **There is no publication workflow.** Nothing in the repository can push a package, create a tag or create a
  release, and a test fails on a script or a workflow that could.

All package settings are in `ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj`.

## The package

| | |
|---|---|
| Package ID | `ConsoleCalendarGenerator`, exactly this casing. It must not change. |
| Version | `VersionPrefix` 2.0.0 with `VersionSuffix` dev, giving `2.0.0-dev` |
| Assembly version | `2.0.0.0`, the major version alone, for all of 2.x; see [compatibility.md](compatibility.md#versions) |
| File version | follows the version prefix (`2.0.0.0`) |
| Informational version | the package version and the commit, such as `2.0.0-dev+<commit>` |
| Library builds | `lib/net10.0/` and `lib/netstandard2.1/`, each with `ConsoleCalendarGenerator.dll` and `.xml` |
| Dependencies | none |
| Metadata | author Shaun Hathaway, a description of the modern API that also names the class of 1.0, MIT license expression, project and repository URLs, repository commit, tags, copyright, and release notes that link to the changelog ([D-74](decisions.md#d-74-the-package-describes-the-modern-api-and-its-release-notes-are-the-changelog)) |
| Readme | the repository's root `README.md`, packed at the package root |
| Symbols | a separate `ConsoleCalendarGenerator.<version>.snupkg` with one portable PDB per build |

Points that are easy to get wrong:

- **Never build a package numbered 1.0.0.** That version is published. `eng/verify-package.sh` and
  `eng/consumer-smoke.sh` both refuse it.
- **The version suffix is removed only as part of an approved release.** Until then the version stays
  `2.0.0-dev`. The release will be 2.0.0, and the suffix goes in the change that prepares it, not earlier.
- **The version is stated in four places that change together:** the project file, `VersionIdentityTests`,
  `eng/verify-package.sh` and `eng/consumer-smoke/AssetProbe/expected-output.txt`. Each of the last three fails
  when the project file says something else.
- **The XML documentation files cover every public type and member,** the legacy class included. A missing
  comment fails the build, and nothing suppresses that. See
  [D-29](decisions.md#d-29-modern-public-api-is-documented-when-it-is-added) and
  [D-51](decisions.md#d-51-the-legacy-class-is-documented-as-it-behaves).
- **The packed readme says what is published.** It is the repository's README, which states that 1.0.0 is the
  only published version and that the modern API is not in the published package. That is true of `master`
  and would be false inside a released package: the README is updated in the change that prepares a release;
  see the [release preparation checklist](#release-preparation-checklist). It does not say that 2.0 is
  published, and must not before it is. It names 2.0 as the version in development, and says "not published
  yet" where it first does; a test requires that.
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
be the compatibility baseline. Until 2.0 is published, 1.0.0 is the only version there is to compare with, and
it remains the baseline for the 2.0 package itself.

## Package verification

```bash
bash eng/verify-package.sh artifacts/package
```

Checks the packed files without building anything: exactly one `.nupkg` and one `.snupkg` and nothing else in
the folder; the exact list of files inside each; the nuspec metadata, including release notes that are the
changelog of this repository; a version that is not 1.0.0, is the
development version `2.0.0-dev` and matches the file names; in both library builds, the assembly version and the
file version `2.0.0.0` and an informational version made of the package version and the commit the nuspec
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
  real cache. This matters: a cached `2.0.0-dev` would hide later rebuilds of the same version.

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
is. The version has to be the version of the package file that was installed, with assembly and file version
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
bash eng/release-rehearsal.sh                                             # what release preparation would do; changes nothing
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

**The rehearsal** (`eng/release-rehearsal.sh`) asks, on every build, whether release preparation would work
from this commit, and changes nothing. It never builds a package or an archive of the release version: it
evaluates the project with the version suffix removed and requires package version 2.0.0, assembly and file
version `2.0.0.0` and an informational version of `2.0.0` and the commit; it rehearses the substitution of the
README's links; it checks that the changelog's "Unreleased" section can be given the version and a date; and
it finds every place that states the development version by number.

**The README's links** (`eng/release-links.sh`). `eng/release-links.txt` lists every link of the README into
this repository with `tag` or `master`
([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)); the tests read the same file.
`eng/release-links.sh v2.0.0` shows the substitution and checks its result on a copy, without a tag having to
exist. With `--apply` it writes `README.md`, which is the one edit release preparation makes there.
`eng/verify-package.sh` requires the image of a packed readme on `master` for a development version and at the
version's own tag for a released one, so the check of the package needs no change for the release.

**In CI** all of this runs on Linux, macOS and Windows after the consumer smoke tests. The Linux job uploads
the bundle as the artifact `release-bundle`, kept 90 days, and prints its checksums. Until the version suffix
is removed, those are the checksums of a development bundle and of nothing else.

### Artifact attestations: not now

GitHub can attest that a file was built by a workflow run of this repository at a commit
(`gh attestation verify <file> -R <repository>`). It was considered for the four files and **deferred**
([D-77](decisions.md#d-77-no-artifact-attestations-for-now)):

- Creating one needs `id-token: write` and `attestations: write` in the job that does it. The only workflow
  runs on every pull request and every push with `contents: read` and nothing else, and that is worth more
  here than what an attestation adds.
- An attestation is a public, permanent record. A job that attests on every push to `master` would start
  making them for development bundles the day it is merged.
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
- changing a repository setting

Approval of a phase, of a pull request or of one of these actions does not cover another. When in doubt, stop
and ask. Routine work (branches, commits, pull requests, CI runs triggered by pushes) is not in this list.

Adding a publication workflow would itself be a release-mechanism change. Do not add one unless a phase is
explicitly scoped to do so.

## Release preparation checklist

What the change that prepares the release of 2.0 has to do to the repository. **Nothing here is done, and none
of it is authorized.** It is a list of what is known to go stale at that moment, so that the change is not
put together from memory. It starts from the release-readiness audit and is one of the steps of the
[sequence](#planned-20-publication-sequence) below.

**Before anything else,** run `bash eng/release-rehearsal.sh` on the commit to be prepared. It lists, with
line numbers, the places named below, and fails if one of them moved.

**The version.** Remove `VersionSuffix` from the project file, so that the package is 2.0.0. Three places
state the version and change together: the project file, `expected_version` in `eng/verify-package.sh`, and
`VersionIdentityTests`. The smoke tests take the version from the package they are given.

**The README's links** ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)). The README
is packed into the package and shown on nuget.org for as long as the version exists. A link whose target is
part of the version moves to the tag `v2.0.0`; a link to a document of current policy stays on `master`.

| Link in the README today | In the README of the released package |
|---|---|
| `https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/master/docs/images/month-with-theme.png` | `https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/v2.0.0/docs/images/month-with-theme.png` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/global.json` | `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/v2.0.0/global.json` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/tree/master/ConsoleCalendarGeneratorExample` | `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/tree/v2.0.0/ConsoleCalendarGeneratorExample` |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CHANGELOG.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CONTRIBUTING.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/SECURITY.md` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/LICENSE` | unchanged |
| `https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues` | unchanged |

The same list is the file `eng/release-links.txt`, which `DocumentationLinkTests` and `eng/release-links.sh`
both read, and which a test compares with this table. **The edit is one command:**
`bash eng/release-links.sh v2.0.0 --apply`. Run without `--apply` first; it shows the three lines. The tag does
not exist until it is created, which needs its own approval, so the links are changed in the release commit
and lead nowhere until the tag is pushed: check them after the tag exists and before the package is
published. One test, `WhileTheVersionIsADevelopmentVersion_NoLinkPointsAtTheReleaseTag`, fails on purpose as
soon as the suffix is removed; it is replaced in the same change by one that requires the tag form, and
`EveryLink_PointsAtSomethingThatExists` learns the tag form with it. `eng/verify-package.sh` already requires
the image at the version's own tag for a version without a suffix.

**The README's text.** The "Status" section (what is published, what is not), the "Installation" section (what
the command installs) and the first sentences of "Legacy API" and of "Migrating from 1.0.0 to 2.0" speak of
2.0 as the next version. `PublicDocumentationTests` requires today's statements and changes with them.

**The changelog.** The "Unreleased" section becomes the section of 2.0.0 with its date, and its first
paragraph, which says nothing is published, goes. The link definitions at the end of the file gain the
version. `TheChangelog_HasOnePublishedVersion_AndAnUnreleasedSection` and
`TheChangelog_KeepsTheNextVersionUnreleased_AndUndated` change with it.

**`SECURITY.md`.** The supported version becomes 2.0. If private vulnerability reporting has been turned on by
then ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)), the section on reporting
was already changed in the change that turned it on; if not, it still says there is no private channel, which
is then still true.

**`CONTRIBUTING.md`** says where the project is, including the development version by number.

**The release notes of the package** ([D-74](decisions.md#d-74-the-package-describes-the-modern-api-and-its-release-notes-are-the-changelog))
link to the changelog on `master`. Decide then whether they stay so or point at the tag;
`eng/verify-package.sh` and the rehearsal accept either, and `PackageMetadataTests` requires `master` until
then.

**The release's other files.** The changelog, and the notes of the GitHub release, are where a user learns
that a release also has an archive of the assemblies and a file of checksums. Nothing public says so yet,
because no release has them yet.

**These notes.** `CLAUDE.md`, [roadmap.md](roadmap.md), this file and
[compatibility.md](compatibility.md#versions) say that 2.0 is unpublished and that the version is `2.0.0-dev`.

**After 2.0 is published,** not before: raise `PackageValidationBaselineVersion` when 2.0 is meant to become
the baseline ([When the baseline changes](#when-the-baseline-changes)), and give `master` the next development
version.

## Planned 2.0 publication sequence

Planned, **not authorized**. See [decisions.md](decisions.md#d-20-20-is-to-be-published-and-verified-on-nuget)
and [D-72](decisions.md#d-72-the-release-is-200-built-once-and-published-as-the-files-that-were-validated).
Every step is a phase or an action that Shaun scopes and approves by itself; finishing one does not start the
next.

Done:

- The implementation: the modern API and the five corrections.
- The user documentation and the migration guidance, for the repository as it is while 2.0 is unpublished.
- The release infrastructure: actions pinned to commits, the [release bundle](#the-release-bundle) made and
  checked on every build, direct use and the upgrade from 1.0.0 tested, release preparation rehearsed.

To come, in this order:

2. **Release-readiness audit.** A read-only review of the merged result, made right after the release
   infrastructure is merged and reported to Shaun outside the repository. It changes nothing here.
3. **Release preparation.** The [checklist](#release-preparation-checklist) above, in one change: the final
   version, the README's links and text, the changelog. Its result is the release commit.
4. **The candidate.** Built once, from the release commit Shaun approves, as a
   [release bundle](#the-release-bundle): four files, with their checksums in `SHA256SUMS.txt`. These files,
   and no later build, are what everything after this step is about. Where it is built is open
   ([Q-16](open-questions.md#q-16-where-is-the-candidate-built)).
5. **Validation of the candidate:** `eng/verify-release-bundle.sh`, `eng/direct-reference-smoke.sh` and the
   upgrade from 1.0.0 on those exact files, clean consumers from a local feed, and the readme as nuget.org
   will render it.
6. **Repository settings,** as approved separately: private vulnerability reporting
   ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)) and whatever the audit names.
7. **The tag** `v2.0.0`, on the release commit. Separate approval.
8. **The GitHub release,** created as a draft, with the candidate's four files attached, unchanged, and then
   published. With release immutability turned on for the repository, which is a setting of step 6, the tag
   and the files can no longer be changed once it is published, and no file can be added: everything has to
   be attached while it is a draft. Separate approval.
9. **NuGet publication:** a push of the candidate's package and symbol package, the same bytes, not a rebuild
   ([D-69](decisions.md#d-69-20-is-published-by-hand-from-the-validated-artifacts)). Separate approval. Claude
   is expected to perform it once that approval is given and a secure credential is available; how it is
   authenticated is open ([Q-14](open-questions.md#q-14-how-is-the-publication-authenticated)).
10. **Verification:** the package from nuget.org in a clean consumer, its hash against the recorded one, the
    readme and the links on its page.
11. **Deprecation of 1.0.0,** if Shaun decides it
    ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)). Separate approval, after
    verification.
12. A factual handoff for the separate Campertron project and chat.

**An immutable release.** A published version is never changed or replaced. If anything is wrong after step
9, the correction is a new version, such as 2.0.1, made and approved the same way. A partial publication, for
example the package pushed and the symbols refused, is recovered only with Shaun's approval
([Release boundaries](#release-boundaries)).

`AssemblyVersion` is already `2.0.0.0`. The package-validation baseline stays 1.0.0: 2.0 differs from 1.0.0 in
behavior only ([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)). No 1.x
version and no prerelease is published before 2.0
([D-55](decisions.md#d-55-the-next-published-version-is-20)).
