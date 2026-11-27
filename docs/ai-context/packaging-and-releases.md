# Packaging and releases

## Current state

- **Published on nuget.org:** `ConsoleCalendarGenerator` 1.0.0 only (verified 2026-11-27). It contains a single
  `netstandard2.1` assembly, placeholder metadata, and no readme, license, symbols or documentation.
- **Built from `master`:** `ConsoleCalendarGenerator` `1.1.0-dev`. It is produced locally and in CI for
  validation and has never been published.
- **There is no publication workflow.** Nothing in the repository can push a package, create a tag or create a
  release.

All package settings are in `ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj`.

## The package

| | |
|---|---|
| Package ID | `ConsoleCalendarGenerator`, exactly this casing. It must not change. |
| Version | `VersionPrefix` 1.1.0 with `VersionSuffix` dev, giving `1.1.0-dev` |
| Assembly version | `1.0.0.0` for all of 1.x; see [compatibility.md](compatibility.md#versions) |
| File version | follows the package version (`1.1.0.0`) |
| Library builds | `lib/net10.0/` and `lib/netstandard2.1/`, each with `ConsoleCalendarGenerator.dll` and `.xml` |
| Dependencies | none |
| Metadata | author Shaun Hathaway, description, MIT license expression, project and repository URLs, repository commit, tags, copyright |
| Readme | the repository's root `README.md`, packed at the package root |
| Symbols | a separate `ConsoleCalendarGenerator.<version>.snupkg` with one portable PDB per build |

Points that are easy to get wrong:

- **Never build a package numbered 1.0.0.** That version is published. `eng/verify-package.sh` and
  `eng/consumer-smoke.sh` both refuse it.
- **The version suffix is removed only as part of an approved release.**
- **The XML documentation files cover every public type and member,** the legacy class included. A missing
  comment fails the build, and nothing suppresses that. See
  [D-29](decisions.md#d-29-modern-public-api-is-documented-when-it-is-added) and
  [D-51](decisions.md#d-51-the-legacy-class-is-documented-as-it-behaves).
- **The packed readme says what is published.** It is the repository's README, which states that 1.0.0 is the
  only published version and that the modern API is not published yet. That is true of `master` and would be
  false inside a released package: the README is updated in the change that prepares a release. See
  [roadmap.md](roadmap.md#next-action).
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
`CP0009` (sealed) or `CP0017` (parameter renamed). Do not add a suppression file to make a break pass; a real
break is a 2.0 decision. Package validation checks API shape only; behavior is what the tests check.

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
be the compatibility baseline. Within 1.x, 1.0.0 remains the floor everything must stay compatible with.

## Package verification

```bash
bash eng/verify-package.sh artifacts/package
```

Checks the packed files without building anything: exactly one `.nupkg` and one `.snupkg` and nothing else in
the folder; the exact list of files inside each; the nuspec metadata; a version that is not 1.0.0 and matches the
file names; that the readme's images are files of this repository served from an allowed host and that it has
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
```

Installs the packed package into small applications the way a consumer would, in a temporary folder outside the
repository that is deleted afterwards:

- The package is copied to a local feed. NuGet source mapping allows `ConsoleCalendarGenerator` from that feed
  only, never from nuget.org.
- The NuGet package cache is a temporary folder as well, so the development package never enters the machine's
  real cache. This matters: a cached `1.1.0-dev` would hide later rebuilds of the same version.

| Consumer | Must receive | Check |
|---|---|---|
| `net10.0` application | the `net10.0` build | builds, runs, expected output |
| `net8.0` application | the `netstandard2.1` build | builds, runs, expected output |
| `net11.0` application | the `net10.0` build | builds, runs, expected output |
| The sample program that accompanied 1.0.0, unchanged | | compiles |
| Native AOT application (`--native-aot`) | the `net10.0` build | publishes without warnings, runs, expected output |

Each application creates a `MonthLayout`, builds `MonthRenderOptions`, renders a month, counts its segments by
kind and prints its text; defines a `DayState`, renders a marked month, finds the state on its segment, asks a
`CalendarTheme` for colors, writes the month with `MonthWriter` to a `StringWriter` and with
`ConsoleMonthWriter` without colors; and then draws a month with the legacy class. Both APIs are therefore exercised from the
package in every kind of consumer, Native AOT included. The `net8.0` consumer stands for every
existing consumer of the `netstandard2.1` build. The `net11.0` consumer
stands for an application such as Campertron. The applications roll forward to whatever runtime is installed;
that does not affect which library build they are given. Templates are in `eng/consumer-smoke/`; they are not in
the solution and are not meant to be built in place.

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

1. Complete the implementation.
2. Complete the user documentation and examples.
3. Run a release-readiness audit.
4. Validate the final `.nupkg` and `.snupkg` contents.
5. Test clean consumers from a local feed.
6. Verify migration from 1.0.0.
7. Obtain separate approval for the tag.
8. Obtain separate approval for the GitHub release.
9. Obtain separate approval for NuGet publication.
10. Verify the package from nuget.org in a clean consumer.
11. Prepare a factual handoff for the separate Campertron project and chat.

For 2.0, `AssemblyVersion` becomes `2.0.0.0` and the package-validation baseline has to be reconsidered, because
2.0 breaks compatibility with 1.0.0 on purpose. No 1.x version and no prerelease is published before 2.0
([D-55](decisions.md#d-55-the-next-published-version-is-20)).
