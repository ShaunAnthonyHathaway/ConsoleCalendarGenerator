# Decisions

Decisions Shaun has made or confirmed. Each has an identifier so that other documents and pull requests can
refer to it. Add new decisions at the end; do not renumber. If a decision is replaced, mark it superseded and
point to its replacement rather than deleting it.

Only decisions belong here. Things that are still undecided are in [open-questions.md](open-questions.md).

All decisions below date from 2026-11-27, the day the modernization was planned and its first phases, up to and
including the layout model, were merged.

## Scope and compatibility

### D-01: .NET only

- **Status:** Approved. Standing rule.
- **Decision:** The project is a .NET library. A Python version or a Python interoperability layer is out of
  scope and is not to be proposed, designed, built, documented, tested or published.
- **Reason:** The goal is a polished .NET package that Campertron, a .NET application, can consume.
- **Consequences:** No Python tooling, packaging or documentation appears in this repository.

### D-02: 1.x is additive; breaking corrections wait for 2.0

- **Status:** Approved. Standing rule.
- **Decision:** Every 1.x release stays compatible with published 1.0.0. Intentional breaking corrections are
  made in 2.0.
- **Reason:** 1.0.0 has existing users, and its behavior, defects included, is what they built on.
- **Consequences:** New capability is new API. Defects are characterized by tests, not fixed. See
  [compatibility.md](compatibility.md).

### D-03: The constructor color defect stays in 1.x

- **Status:** Approved. Implemented as tests (`ConstructorColorTests`).
- **Decision:** The parameterized constructors keep assigning `TitleColor` to the weekday row, and
  `DayOfWeekColor` to the title in the four-argument form, throughout 1.x. Correcting or removing them is 2.0
  work.
- **Reason:** A silent fix would recolor the output of existing programs. The README screenshots show the
  current behavior.
- **Consequences:** Tests named `Characterized1xBehavior` must not be "corrected".

### D-04: The legacy API stays; new API gets its own namespace

- **Status:** Approved. In use since the layout model ([D-25](#d-25-the-layout-model-is-three-small-sealed-classes)).
- **Decision:** `CalendarGenerator.ConsoleCalendar` remains the compatibility API. The modern API will live in
  the `ConsoleCalendarGenerator` namespace, which matches the package ID and the assembly name.
- **Reason:** The published namespace cannot change in 1.x, and the new API should not inherit the mismatch.
- **Consequences:** Two namespaces in one assembly. The legacy class is planned to become a facade over the new
  code.

### D-05: Assembly version is the major version only

- **Status:** Approved. Implemented (`AssemblyVersion` in the library project).
- **Decision:** `AssemblyVersion` is `1.0.0.0` for the whole 1.x line and becomes `2.0.0.0` at 2.0. Assembly
  major versions stay aligned with package major versions; it must not remain `1.0.0.0` after 1.x. The package
  version and file version reflect the actual release, and the Source Link informational version is kept.
- **Reason:** A stable assembly identity within a major version, as published in 1.0.0, with the package version
  carrying the release identity.
- **Consequences:** Raising the major version requires changing `AssemblyVersion` in the same change.

## Frameworks and tooling

### D-06: The library targets net10.0 and netstandard2.1

- **Status:** Approved. Implemented.
- **Decision:** `net10.0;netstandard2.1`. No `netstandard2.0` target and no separate `net11.0` library target.
- **Reason:** `netstandard2.1` is what 1.0.0 shipped, so every existing consumer can upgrade. `net10.0` is the
  current long-term-support framework, enables the trim and Native AOT analyzers, and is the build a `net11.0`
  application such as Campertron receives. There is no evidence of .NET Framework consumers.
- **Consequences:** One source, two builds, no conditional compilation so far. Dropping `netstandard2.1` would be
  a breaking change.

### D-07: The example targets net10.0

- **Status:** Approved. Implemented.
- **Decision:** The example application targets `net10.0`.
- **Reason:** Its previous target, `net7.0`, is out of support and produced a build warning.
- **Consequences:** The build has no warnings, and CI treats every warning as an error.

### D-08: The tests target net11.0 and use NUnit

- **Status:** Approved. Implemented.
- **Decision:** One NUnit test project on `net11.0`, matching the pinned SDK and Campertron's test stack.
- **Reason:** One current runtime for tests; the library build under test is chosen separately.
- **Consequences:** The tests run against both library builds through `-p:LibraryTargetFramework`. See
  [testing.md](testing.md).

### D-09: Development builds are versioned 1.1.0-dev

- **Status:** Approved. Implemented.
- **Decision:** Packages built from the repository carry a development version, currently `1.1.0-dev`. Nothing
  built here may be numbered 1.0.0.
- **Reason:** 1.0.0 is published; a local or CI package must never be mistaken for it or overwrite it in a cache.
- **Consequences:** The version suffix is removed only as part of an approved release. This does not decide
  which version is released next; see [open-questions.md](open-questions.md#q-07-what-ships-in-1x-and-when).

### D-10: Package validation against 1.0.0 on every pack

- **Status:** Approved. Implemented.
- **Decision:** `dotnet pack` validates the package's public API against published 1.0.0, including parameter
  names, and fails on a break.
- **Reason:** A second, independent check beside the tests, on the artifact that would actually ship.
- **Consequences:** The baseline version is raised only when a new version has been published and becomes the
  compatibility baseline.

### D-11: Public XML documentation is deferred

- **Status:** Approved. In place for the legacy class only; narrowed by
  [D-29](#d-29-modern-public-api-is-documented-when-it-is-added).
- **Decision:** The XML documentation file is generated and packed now. Writing the comments for the legacy
  class is part of the user-facing material phase. Until then `CS1591` is silenced for
  `ConsoleCalendarGenerator/ConsoleCalendar.cs`, in `.editorconfig`.
- **Reason:** Placeholder comments written only to silence a warning would be worse than none.
- **Consequences:** Remove the `CS1591` entry from `.editorconfig` in the same change that documents the legacy
  class.

## Architecture

### D-12: Layout and rendering are separated from console output

- **Status:** Approved direction. The layout layer is implemented (D-25 to D-28); the other layers are not.
- **Decision:** The modern API separates calendar layout, deterministic text rendering, semantic styling, color
  mapping and output. Layout and rendering must not need a live console.
- **Reason:** Testability, reuse, and Campertron's need for rendered lines it writes itself.
- **Consequences:** See [architecture.md](architecture.md#approved-direction).

### D-13: State can be shown with markers, not only color

- **Status:** Approved direction. Not implemented.
- **Decision:** The modern API can mark a day's state with text, so that state is never conveyed by color alone.
- **Reason:** Accessibility, redirected output and terminals without color. Campertron marks open days with `*`.
- **Consequences:** Markers are part of rendering, not of the theme.

### D-14: No ANSI subsystem without evidence

- **Status:** Approved.
- **Decision:** No ANSI escape-sequence renderer is planned.
- **Reason:** `Console.ForegroundColor` already does the right thing per platform, Campertron uses
  `ConsoleColor`, and styled segments let a consumer produce ANSI itself.
- **Consequences:** Revisit only if a real need is demonstrated.

### D-15: Modern titles match the Gregorian grid

- **Status:** Approved direction. Not implemented.
- **Decision:** The modern renderer should produce titles consistent with the Gregorian grid it draws, while
  allowing a compatibility profile where exact parity with existing output is required.
- **Reason:** 1.0.0 names the wrong month under cultures with a non-Gregorian calendar.
- **Consequences:** The legacy class keeps its 1.0.0 behavior in 1.x. The mechanism is an open question.

### D-16: Campertron parity targets its live renderer

- **Status:** Approved direction. Not implemented.
- **Decision:** Compatibility work targets Campertron's live `Calendar.Lines` renderer. The package is not
  designed around Campertron's older, unused value-stream calendar.
- **Reason:** `Calendar.Lines` is what Campertron's console actually shows.
- **Consequences:** See [architecture.md](architecture.md#what-campertron-needs-from-the-package).

## Repository, publication and process

### D-17: GitHub Pages is retired

- **Status:** Approved. Implemented.
- **Decision:** The Pages deploy workflow was removed and the repository's Pages setting was disabled. No
  replacement site is wanted.
- **Reason:** The configuration was broken, published nothing and failed on every push to `master`.
- **Consequences:** `docs/` at the repository root is ordinary documentation, not a site source. Do not
  re-enable Pages.

### D-18: Publication requires explicit approval, every time

- **Status:** Approved. Standing rule.
- **Decision:** Tags, GitHub releases, NuGet publication, publication-capable manual workflow dispatches or
  reruns, publication recovery and repository-setting changes each need Shaun's separate, explicit approval.
- **Reason:** These are outward-facing and hard or impossible to undo.
- **Consequences:** An approval for one does not cover another, and approval of a phase does not cover any of
  them. There is no publication workflow in the repository. See
  [packaging-and-releases.md](packaging-and-releases.md#release-boundaries).

### D-19: Campertron adoption happens elsewhere

- **Status:** Approved. Standing rule.
- **Decision:** Campertron is never modified from this repository or this workflow. After the package has been
  published and verified, Campertron's adoption of it is handled only in Shaun's separate Campertron project and
  chat.
- **Reason:** Campertron has its own rules, review and release process.
- **Consequences:** Work here ends with a factual handoff. Reading Campertron's code to understand what it needs
  is fine; changing it is not.

### D-20: 2.0 is to be published and verified on NuGet

- **Status:** Planned. **Not authorized.**
- **Decision:** `ConsoleCalendarGenerator` 2.0 must eventually be published on NuGet and verified there.
- **Reason:** It is the version Campertron is expected to adopt.
- **Consequences:** Every publication step still needs its own approval under D-18. See the sequence in
  [packaging-and-releases.md](packaging-and-releases.md#planned-20-publication-sequence).

### D-21: User-facing material is replaced later, in one phase

- **Status:** Approved. Not started.
- **Decision:** The README, the example, the duplicated docs and the screenshots will be replaced in a later
  documentation and polish phase, to the same general standard as Campertron's.
- **Reason:** They describe the 1.0.0 API only and show the constructor defect; rewriting them before the new API
  exists would mean writing them twice.
- **Consequences:** Until then, change them only as far as a valid package requires.

### D-22: Behavior is characterized before it is changed

- **Status:** Approved. Implemented.
- **Decision:** Tests record what the published code does, including what it does wrong, before any refactoring.
- **Reason:** A refactor can only be shown safe against a recorded baseline.
- **Consequences:** See [testing.md](testing.md).

### D-23: AI-maintenance documentation precedes architecture work

- **Status:** Approved. Implemented by this documentation.
- **Decision:** `CLAUDE.md` and `docs/ai-context/` are written before the layout and rendering work starts.
- **Reason:** The remaining phases span many separate sessions and need a shared, verified starting point.
- **Consequences:** These documents are maintained with the code.

### D-24: Order of priorities

- **Status:** Approved.
- **Decision:** In order: repository and test foundations; protection of published 1.0.0 behavior; modern layout
  and rendering; Campertron compatibility and parity; README, examples, screenshots and presentation; release
  readiness.
- **Reason:** Stabilize and protect before building, and build before polishing.
- **Consequences:** When two pieces of work compete, the earlier item wins.

## Layout model

Settled in the layout-model phase, within the authority Shaun gave that phase to choose names and shapes from
the evidence. The API itself is listed in [architecture.md](architecture.md#the-layout-model).

### D-25: The layout model is three small sealed classes

- **Status:** Approved. Implemented.
- **Decision:** `MonthLayout`, `MonthLayoutWeek` and `MonthLayoutCell`, in the `ConsoleCalendarGenerator`
  namespace. A layout is created with the static method `MonthLayout.Create(year, month, firstDayOfWeek)`; none
  of the three has a public constructor. All three are sealed, immutable classes with reference equality.
  `firstDayOfWeek` has no default and no overload supplies one.
- **Reason:** One entry point on the result type is the smallest surface that works, and a static method leaves
  room to change how layouts are built. The names share a prefix so that they cannot be mistaken for types of
  the rendering layers still to come. The argument order is the one `DateTime` uses. A cell is a class rather
  than a struct because it is not a single primitive-like value and is not under 16 bytes, which are .NET's
  criteria for a struct, and because a struct would need hand-written equality to avoid reflection. Equality is
  left alone because no consumer needs it yet and a layout is fully identified by its three inputs. A default
  first day of the week is a product choice that belongs with the rendering options.
- **Consequences:** Adding members later is compatible; renaming or removing these is not, once published. No
  interface, base class or options type exists for the layout. Which first day of the week is the default
  remains open in [Q-01](open-questions.md#q-01-what-are-the-modern-apis-public-types-and-signatures).

### D-26: Layout collections are read-only lists with no package dependency

- **Status:** Approved. Implemented.
- **Decision:** `Weeks`, `Cells` and `DaysOfWeek` are declared as `IReadOnlyList<T>` and are read-only wrappers
  over private arrays (`Array.AsReadOnly`). Arrays are never handed out.
- **Reason:** An array typed as a read-only interface can be cast back and changed. `ImmutableArray<T>` would
  need a package reference on `netstandard2.1`, and the package has no dependencies. Declaring the interface
  rather than `ReadOnlyCollection<T>` keeps the implementation free to change.
- **Consequences:** A caller that casts to `IList<T>` gets `NotSupportedException` on every change. The same
  approach is the default for later layers unless there is a reason to differ.

### D-27: Layout dates are DateTime values of the requested month only

- **Status:** Approved. Implemented.
- **Decision:** A cell's date is a `DateTime?`: midnight, `DateTimeKind.Unspecified`, Gregorian calendar, or null
  for an empty cell. Cells that fall in a neighbouring month are empty; they never carry that month's dates.
  The cell has no separate day-number property.
- **Reason:** `DateTime` behaves identically on both targets and `DateOnly` does not exist on `netstandard2.1`.
  `DateTime`, `DateTime.DaysInMonth` and `GregorianCalendar` were checked against plain arithmetic for all
  119,988 months of the years 1 through 9999, under Gregorian, Persian and Thai Buddhist cultures, and agree.
  Leaving neighbouring cells empty means January 0001 and December 9999 need no special case, and it is what
  the legacy output shows. The day number is `Date.Value.Day`.
- **Consequences:** The supported range is every month a `DateTime` can represent. Showing the days of
  neighbouring months, if ever wanted, would be new API.

### D-28: Layout arguments are validated in order with ArgumentOutOfRangeException

- **Status:** Approved. Implemented.
- **Decision:** `MonthLayout.Create` checks `year` (1 through 9999), then `month` (1 through 12), then
  `firstDayOfWeek` (one of the seven `DayOfWeek` values), and throws `ArgumentOutOfRangeException` with the
  parameter name and the offending value for the first that fails. Nothing is built before the checks pass. The
  message text is not part of the contract.
- **Reason:** It is what the runtime does for the same kinds of argument, including an undefined `DayOfWeek`.
  The library checks for itself because `new DateTime(...)` reports no parameter name and `DateTime.DaysInMonth`
  checks the month before the year.
- **Consequences:** The order and the parameter names are tested and are part of the contract.

### D-29: Modern public API is documented when it is added

- **Status:** Approved. Implemented.
- **Decision:** Every public type and member in the `ConsoleCalendarGenerator` namespace has XML documentation
  in the change that adds it. The compiler enforces it: `CS1591` is a warning, and so a build failure, for
  everything except the legacy `ConsoleCalendar.cs`.
- **Reason:** Shaun required documented modern API with no `CS1591` suppression for it. The project-wide
  suppression from D-11 would have hidden a missing comment on a modern type.
- **Consequences:** D-11 now covers the legacy class only. `eng/verify-package.sh` checks that the modern
  documentation is in both builds of the package.
