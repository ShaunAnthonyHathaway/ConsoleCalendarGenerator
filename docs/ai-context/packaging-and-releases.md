# Packaging and releases

## Current state

- **Published on nuget.org:** `ConsoleCalendarGenerator` 1.0.0 only (verified 2026-11-27). It contains a single
  `netstandard2.1` assembly, placeholder metadata, and no readme, license, symbols or documentation.
- **Built from `master`:** `ConsoleCalendarGenerator` `2.0.0-dev`, a development version of 2.0 with assembly
  and file version `2.0.0.0`
  ([D-65](decisions.md#d-65-the-repository-builds-200-dev-with-assembly-and-file-version-2000)). It is produced
  locally and in CI for validation and has never been published. No prerelease is published.
- **There is no publication workflow.** Nothing in the repository can push a package, create a tag or create a
  release.

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
| Metadata | author Shaun Hathaway, description, MIT license expression, project and repository URLs, repository commit, tags, copyright |
| Readme | the repository's root `README.md`, packed at the package root |
| Symbols | a separate `ConsoleCalendarGenerator.<version>.snupkg` with one portable PDB per build |

Points that are easy to get wrong:

- **Never build a package numbered 1.0.0.** That version is published. `eng/verify-package.sh` and
  `eng/consumer-smoke.sh` both refuse it.
- **The version suffix is removed only as part of an approved release.** Until then the version stays
  `2.0.0-dev`; the final number is not set early.
- **The version is stated in four places that change together:** the project file, `VersionIdentityTests`,
  `eng/verify-package.sh` and `eng/consumer-smoke/AssetProbe/expected-output.txt`. Each of the last three fails
  when the project file says something else.
- **The XML documentation files cover every public type and member,** the legacy class included. A missing
  comment fails the build, and nothing suppresses that. See
  [D-29](decisions.md#d-29-modern-public-api-is-documented-when-it-is-added) and
  [D-51](decisions.md#d-51-the-legacy-class-is-documented-as-it-behaves).
- **The packed readme says what is published.** It is the repository's README, which states that 1.0.0 is the
  only published version and that the modern API is not published yet. That is true of `master` and would be
  false inside a released package: the README is updated in the change that prepares a release. See
  [roadmap.md](roadmap.md#next-action). It does not say that 2.0 is published, and must not before it is. It may
  name 2.0 as the version in development, and says "not published yet" where it first does; a test requires
  that.
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
the folder; the exact list of files inside each; the nuspec metadata; a version that is not 1.0.0, is the
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

## Planned 2.0 publication sequence

Planned, **not authorized**. See [decisions.md](decisions.md#d-20-20-is-to-be-published-and-verified-on-nuget).

1. Complete the implementation: the five corrections, one pull request each
   ([roadmap.md](roadmap.md#remaining-sequence)).
2. Complete the user documentation, the migration guidance and the examples. In the same change, point the
   README's image and file links at the tag `v2.0.0`
   ([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)) and set the final version.
3. Run a release-readiness audit.
4. Validate the final `.nupkg` and `.snupkg` contents.
5. Test clean consumers from a local feed.
6. Verify migration from 1.0.0.
7. Obtain separate approval for the tag.
8. Obtain separate approval for the GitHub release.
9. Obtain separate approval for NuGet publication, which is a manual push of the exact artifacts that were
   validated in step 4, not of a rebuild
   ([D-69](decisions.md#d-69-20-is-published-by-hand-from-the-validated-artifacts)).
10. Verify the package from nuget.org in a clean consumer.
11. Prepare a factual handoff for the separate Campertron project and chat.

`AssemblyVersion` is already `2.0.0.0`. The package-validation baseline stays 1.0.0: 2.0 differs from 1.0.0 in
behavior only ([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)). No 1.x
version and no prerelease is published before 2.0
([D-55](decisions.md#d-55-the-next-published-version-is-20)). Repository settings are a phase of their own,
approved separately, and are not changed by any of the steps above. Still open for the release:
how the push is authenticated ([Q-14](open-questions.md#q-14-how-is-the-publication-authenticated)) and whether
1.0.0 is deprecated afterwards ([Q-15](open-questions.md#q-15-is-100-deprecated-on-nugetorg-after-20)).
