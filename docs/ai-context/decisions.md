# Decisions

Decisions Shaun has made or confirmed. Each has an identifier so that other documents and pull requests can
refer to it. Add new decisions at the end; do not renumber. If a decision is replaced, mark it superseded and
point to its replacement rather than deleting it.

Only decisions belong here. Things that are still undecided are in [open-questions.md](open-questions.md).

All decisions below date from 2026-11-27, the day the modernization was planned and its first phases, up to and
including the Campertron parity fixtures, were merged.

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
- **Reason:** A silent fix would recolor the output of existing programs. The README screenshots published
  with 1.0.0 showed the current behavior.
- **Consequences:** Tests named `Characterized1xBehavior` must not be "corrected".

### D-04: The legacy API stays; new API gets its own namespace

- **Status:** Approved. In use since the layout model ([D-25](#d-25-the-layout-model-is-three-small-sealed-classes)).
  The legacy class became a facade in [D-44](#d-44-the-legacy-class-shares-the-layout-and-nothing-else).
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

- **Status:** Completed and no longer in effect. The legacy class was documented and the suppression removed
  in the user-facing material phase ([D-51](#d-51-the-legacy-class-is-documented-as-it-behaves)).
- **Decision:** The XML documentation file was generated and packed from the start. Writing the comments for
  the legacy class was left to the user-facing material phase, and until then `CS1591` was silenced for
  `ConsoleCalendarGenerator/ConsoleCalendar.cs`, in `.editorconfig`.
- **Reason:** Placeholder comments written only to silence a warning would be worse than none.
- **Consequences:** None any more. The entry is gone from `.editorconfig`.

## Architecture

### D-12: Layout and rendering are separated from console output

- **Status:** Approved. Implemented for a single month: layout (D-25 to D-28), text rendering with segments
  (D-30 to D-37), and day states, the theme and output (D-38 to D-43).
- **Decision:** The modern API separates calendar layout, deterministic text rendering, semantic styling, color
  mapping and output. Layout and rendering must not need a live console.
- **Reason:** Testability, reuse, and Campertron's need for rendered lines it writes itself.
- **Consequences:** See [architecture.md](architecture.md#approved-direction).

### D-13: State can be shown with markers, not only color

- **Status:** Approved. Implemented ([D-38](#d-38-a-day-state-is-a-name-and-a-visible-marker),
  [D-40](#d-40-the-marker-goes-inside-the-cell-and-must-fit)). A state cannot exist without a visible marker.
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

- **Status:** Approved. Implemented in the modern renderer ([D-33](#d-33-the-culture-supplies-language-only)).
  How exact parity with existing output is expressed stays open in
  [Q-06](open-questions.md#q-06-how-is-the-compatibility-profile-for-titles-expressed).
- **Decision:** The modern renderer should produce titles consistent with the Gregorian grid it draws, while
  allowing a compatibility profile where exact parity with existing output is required.
- **Reason:** 1.0.0 names the wrong month under cultures with a non-Gregorian calendar.
- **Consequences:** The legacy class keeps its 1.0.0 behavior in 1.x. The mechanism is an open question.

### D-16: Campertron parity targets its live renderer

- **Status:** Approved. Implemented as fixtures and tests
  ([D-47](#d-47-campertron-parity-is-shown-with-recorded-fixtures)).
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

- **Status:** Approved. Done; what replaced them is in [User-facing material](#user-facing-material).
- **Decision:** The README, the example, the duplicated docs and the screenshots are replaced in one
  documentation and polish phase, after the new API exists.
- **Reason:** They described the 1.0.0 API only and showed the constructor defect; rewriting them before the new
  API existed would have meant writing them twice.
- **Consequences:** None any more.

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
the evidence, and accepted by him when he reviewed that phase. The API itself is listed in
[architecture.md](architecture.md#the-layout-model).

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
  interface, base class or options type exists for the layout. The default first day of the week was settled
  later, in [D-32](#d-32-rendering-defaults-are-fixed-and-read-nothing-from-the-process).

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
  in the change that adds it. The compiler enforces it: `CS1591` is a warning, and so a build failure. (Until
  [D-51](#d-51-the-legacy-class-is-documented-as-it-behaves) the legacy `ConsoleCalendar.cs` was excepted.)
- **Reason:** Shaun required documented modern API with no `CS1591` suppression for it. The project-wide
  suppression from D-11 would have hidden a missing comment on a modern type.
- **Consequences:** `eng/verify-package.sh` checks that the documentation is in both builds of the package, and
  `Documentation/XmlDocumentationTests` checks every public member of the built file.

## Rendering

Settled in the text-rendering phase, within the authority Shaun gave that phase, and accepted by him when he
reviewed it, including the defaults in D-32 and the limits in D-35. The API, the defaults and the contracts are
listed in [architecture.md](architecture.md#text-rendering).

### D-30: One rendering entry point that owns the layout

- **Status:** Approved. Implemented.
- **Decision:** `MonthRenderer.Render(year, month, options)` is the only way to render. It creates the
  `MonthLayout` itself from the options' first day of the week and returns it as `RenderedMonth.Layout`. There
  is no overload that takes a layout, and none that takes no options.
- **Reason:** The first day of the week is an option, so a layout passed in separately could disagree with it;
  with one entry point that cannot happen and nothing has to be validated. Creating a layout is cheap, so an
  overload that reuses one has no value. A caller who wants the defaults passes `MonthRenderOptions.Default`.
- **Consequences:** Later inputs, such as day states, arrive as further overloads of `Render`. A static class
  was chosen over a method on the result because later layers add siblings to it, not to the result.

### D-31: Options are an immutable class changed through With methods

- **Status:** Approved. Implemented.
- **Decision:** `MonthRenderOptions` has no public constructor. `Default` is a shared instance; each `WithX`
  method validates its argument and returns a new instance. Every property is read-only.
- **Reason:** It works identically on both targets. Init-only setters need compiler support that
  `netstandard2.1` consumers may lack, a constructor with many optional parameters cannot grow without breaking
  callers, and a mutable builder is a second type. Validation happens where the bad value is supplied, not later
  at render time.
- **Consequences:** A new setting is one property and one `With` method, which is a compatible addition. The
  options hold only what unmarked text rendering needs: nothing about colors, states, markers or output.

### D-32: Rendering defaults are fixed and read nothing from the process

- **Status:** Approved. Implemented.
- **Decision:** The default culture is the invariant culture and the default first day of the week is Monday.
  The first day of the week is never taken from a culture, including one set with `WithCulture`. Neither
  `CurrentCulture` nor `CurrentUICulture` is ever read.
- **Reason:** The renderer's promise is the same output for the same input on every machine (for the names a
  real culture supplies this later proved to hold per operating system only; see
  [D-53](#d-53-names-a-culture-supplies-are-documented-as-depending-on-the-system)), which a default
  taken from the current culture would break. Monday is ISO 8601 and what both existing renderers, the legacy
  class and Campertron's, draw. Keeping the first day independent of the culture means changing the language
  never moves the days. A caller who wants the culture's own first day passes
  `culture.DateTimeFormat.FirstDayOfWeek`.
- **Consequences:** The default title is the invariant culture's year-and-month pattern, for example
  `2023 May`. English month-first titles need `en-US` or the format `"MMMM yyyy"`.

### D-33: The culture supplies language only

- **Status:** Approved. Implemented. This implements [D-15](#d-15-modern-titles-match-the-gregorian-grid).
- **Decision:** The culture provides month names, day names and the year-and-month pattern, always those of
  the Gregorian calendar. For a culture whose own calendar is another one, the culture's Gregorian data is used.
  The options store a read-only copy of the culture (`CultureInfo.ReadOnly`). Day numbers are always written
  with the digits 0 to 9.
- **Reason:** The grid is Gregorian, so a title in another calendar names a different month or year than the
  one shown. On the runtime used here, 15 of 907 cultures default to another calendar (Persian and Thai
  Buddhist) and every one of them offers a Gregorian calendar to switch to. A copy makes the options immutable
  even when the caller's culture object is not.
- **Consequences:** `th-TH` titles say 2023, not 2566. Should a culture ever offer no Gregorian calendar, its
  names fall back to the invariant culture's rather than to another calendar's. A culture whose names would put
  a line break into the output is refused by `Render` with `ArgumentException`.

### D-34: The title is a date format string, a suffix and an alignment

- **Status:** Approved. Implemented. Resolves the former open question Q-03.
- **Decision:** `TitleFormat` is any standard or custom .NET date format string, `"Y"` by default, applied to
  the first day of the month as a `DateTimeOffset` with a zero offset. `TitleSuffix` is appended as its own
  segment with nothing in between. `TitleAlignment` is `Left` or `Center`; centering puts half the spare width,
  rounded down, before the title and suffix together, and never cuts or moves a title wider than the grid. An
  invalid format throws `FormatException` from `WithTitleFormat`. There is no title callback.
- **Reason:** A format string covers the three known needs: the modern default, Campertron's pattern plus
  legend, and, because quoted text is copied unchanged, any exact title such as the legacy one. A delegate would
  make options impossible to compare, serialize or reason about. A zero-offset value means no format specifier
  can read the machine's time zone, which a plain `DateTime` allows.
- **Consequences:** The standard format `"U"` is not supported. The legacy three-step title indent is not a
  modern alignment; the facade phase produces it itself.

### D-35: Geometry depends on the options alone, and widths are text elements

- **Status:** Approved. Implemented.
- **Decision:** Every cell is exactly `CellWidth` wide (2 through 100) with `ColumnGap` spaces between columns
  (0 through 100). Day numbers and labels are right-aligned. A label longer than the cell is cut to the cell
  width; nothing is ever widened. Width is counted in text elements (`StringInfo`). The cells after the last day
  are not rendered, so no line ends with padding.
- **Reason:** Culture day names vary in length (the "shortest" German names are three characters), so letting
  them set the width would make the grid's shape depend on operating-system data. Text elements are the unit the
  runtime can count exactly on both targets without splitting a surrogate pair or a combining sequence; terminal
  column width is not knowable from a string. Two is the narrowest cell that holds every day number.
- **Consequences:** `GridWidth` is always seven cells and six gaps. The limits of 100 can be raised later
  without breaking anyone. There is no option for alignment within a cell or for fixed-width lines; both would
  be additions.

### D-36: Weekday labels come from the culture or from the caller, Sunday first

- **Status:** Approved. Implemented.
- **Decision:** Two built-in styles, `Shortest` and `Abbreviated`, take the culture's day names. Otherwise the
  caller supplies exactly seven labels indexed by `DayOfWeek`, Sunday first, each non-empty and without a line
  break; they are copied. The last of `WithWeekdayLabelStyle` and `WithWeekdayLabels` to be called wins. There is
  no built-in fixed English style.
- **Reason:** Indexing by weekday, as `DateTimeFormatInfo.DayNames` does, ties a label to its day, so changing
  the first day of the week cannot put a label over the wrong column. The fixed English row that the legacy
  class and Campertron draw is what the invariant culture already gives, and seven supplied labels give it under
  any other culture, so a third style would add an enum value without adding a capability.
- **Consequences:** A label made only of spaces is allowed and is kept as text.

### D-37: Rendered text is segments with meaning, joined with an explicit line terminator

- **Status:** Approved. Implemented.
- **Decision:** A `RenderedMonth` is lines; a line is segments; a segment has a kind, text, and, for cells, a
  weekday and, for days, a date. A cell is one segment including its padding. No segment is empty or contains a
  line break. There are no colors, no state and no open-ended metadata. `ToText()` joins lines with `"\n"` on
  every platform; `ToText(newLine)` accepts only `"\n"` and `"\r\n"`; neither adds a terminator after the last
  line.
- **Reason:** Whole-cell segments match how both existing renderers color a day, and give the marker phase one
  place to put a marker: inside the cell, beside the number, described by the segment instead of parsed from its
  text. A fixed default terminator keeps the text identical across platforms, which the legacy class's mixed
  line endings show is easy to get wrong. Anything else can be joined from `Lines`.
- **Consequences:** The next phase adds state to `Day` segments and maps kinds to colors; neither changes how
  lines are built. New segment or line kinds are additions, so code that switches on a kind should have a
  default case.

## States, themes and output

Settled in the states, themes and output phase, within the authority Shaun gave that phase, and accepted by him
when he reviewed it, including that a repeated `WithStateColor` replaces (D-41) and that color restoration is
not verified on Windows (D-43). The API and the contracts are listed in
[architecture.md](architecture.md#day-states-and-markers).

### D-38: A day state is a name and a visible marker

- **Status:** Approved. Implemented.
- **Decision:** `DayState` has a name and a marker and nothing else. Both must contain a character that is not
  white space and no line break. There is no further grammar for a name. A state has no color, no display
  label and no metadata. Equality is reference equality; the name, compared character by character, is what
  identifies the kind of state.
- **Reason:** The name is what a theme and a consumer look a state up by, and an ordinal comparison never
  depends on a culture. Requiring a visible marker is what makes state accessible without color
  ([D-13](#d-13-state-can-be-shown-with-markers-not-only-color)). Color stays in the theme so that the same
  rendered month can be written plain, themed or mapped by a consumer. A label is only needed by a legend, and
  there is none.
- **Consequences:** There is no legend subsystem. A caller explains markers in `TitleSuffix` or around the
  calendar, as Campertron does today. Revisit only if real multi-state use shows a need.

### D-39: One state per day, and the first entry wins

- **Status:** Approved. Implemented. Resolves the former open question Q-04.
- **Decision:** States are attached through two overloads of `MonthRenderer.Render`: one takes dates with their
  states (`IEnumerable<KeyValuePair<DateTime, DayState>>`), one takes dates and a single state. A date selects a
  day by year, month and day; time of day and `Kind` are ignored; dates outside the month are ignored. A day has
  at most one state, and when several entries select it, the first in the order of the input decides. Every
  entry is validated, also those that are ignored. Within one call, two states with the same name must have the
  same marker.
- **Reason:** One state per day keeps the cell, the theme and the meaning of a marker simple, and both existing
  renderers have a single state. First-wins lets a caller express priority by ordering, and gives a dictionary
  and a list the same meaning. `KeyValuePair` lets a `Dictionary<DateTime, DayState>` be passed as it is with no
  new type; the single-state overload is the shape both the legacy class and Campertron have. Date matching is
  the legacy class's rule for selected dates. Validating every entry means the same input is accepted or refused
  whichever month is rendered.
- **Consequences:** The overload without states is unchanged, and a digest recorded before day states existed
  guards that. Several simultaneous states on one day would be new API, with new cell geometry.

### D-40: The marker goes inside the cell and must fit

- **Status:** Approved. Implemented.
- **Decision:** A marked day is one `Day` segment whose text is the marker directly before the day number,
  right-aligned together in the cell, and whose `State` is the state. The marker's width in text elements plus
  two must not exceed `CellWidth`; otherwise `Render` throws `ArgumentException` and returns nothing. The check
  is made for every state supplied, whatever the dates. No room is reserved for markers in unmarked output, and
  there is no option for the marker's position or a separator.
- **Reason:** Inside the cell, a marker never changes the width of a line or the position of a number, and the
  segment structure of PR #7 stays as it was. Cutting or dropping a marker that does not fit would silently
  remove the only non-color sign of a state. Checking against a two-digit day, and independently of the dates,
  means a month never fails because of which days happen to be marked.
- **Consequences:** The default cell, two wide, holds no marker; a one-character marker needs `CellWidth` 3.
  Rendering with states and too narrow a cell is an error a caller sees the first time, not an occasional one.

### D-41: A theme maps segments and state names to optional foreground colors

- **Status:** Approved. Implemented.
- **Decision:** `CalendarTheme` is immutable and built with `With` methods from `CalendarTheme.None`. It has an
  optional `ConsoleColor` for the title, the title suffix, the weekday labels and the days, and one per state
  name. Null means no color of the theme. A state without a color falls back to the day color. State names are
  matched character by character. `WithStateColor` replaces a color set earlier for the same name, and removes
  it when given null. An undefined `ConsoleColor` is refused when the theme is built. `GetColor(segment)` is
  public. There are no background colors, and indents, padding, gaps and empty cells have no color.
- **Reason:** Null-means-unchanged lets a theme color one thing and leave the rest to the terminal. Replacing
  rather than refusing a repeated name follows from the `With` pattern
  ([D-31](#d-31-options-are-an-immutable-class-changed-through-with-methods)) and is what makes a theme
  derivable from another; with no collection passed in, there is no duplicate input to reject. Refusing bad
  colors early keeps the failure away from the middle of a write, which is where the legacy class has it.
  `GetColor` is what a consumer needs to map segments to its own styled spans without the console writer.
- **Consequences:** No interface and no ANSI ([D-14](#d-14-no-ansi-subsystem-without-evidence)). Background
  colors, or colors for more roles, would be additions.

### D-42: Two writers, and no color mode

- **Status:** Approved. Implemented.
- **Decision:** `MonthWriter.Write(month, writer)` writes plain text to any `TextWriter`, ends every line,
  including the last, with the writer's own `NewLine`, and never touches the console or the writer's state.
  `ConsoleMonthWriter.Write(month, theme)` writes to `Console.Out` and sets `Console.ForegroundColor`. Writing
  to the console without colors is `CalendarTheme.None`. There is no color-mode enum, no `Auto`, and no
  interpretation of `NO_COLOR`, `TERM` or redirection in the package.
- **Reason:** Two types make it a fact of the API which code can touch the console. A line-oriented writer that
  ends the last line is what streams and consoles expect; `ToText` remains the form with no final terminator.
  An `Auto` mode could not be defined from public runtime facts without building capability detection, and the
  runtime already does the sensible thing: on Linux it was observed to emit no color sequences when output is
  redirected and to honor `NO_COLOR`.
- **Consequences:** A caller who wants colors forced into redirected output uses the runtime's own switch, not
  the package. A theme passed with a plain `TextWriter` is not a combination the API offers.

### D-43: The console writer restores the caller's colors and serializes its own writes

- **Status:** Approved. Implemented.
- **Decision:** `ConsoleMonthWriter` reads the console's colors only when it is about to change one, sets only
  the foreground, writes every uncolored segment and every line end in the caller's foreground, and puts the
  caller's foreground and background back in a `finally` block. A foreground that was never set cannot be
  assigned back, so in that case it calls `Console.ResetColor()` and then sets the caller's background again if
  there was one. A private lock makes one call write its month completely before the next begins.
- **Reason:** The legacy class resets the caller's colors and leaves them reset; a library should hand the
  console back as it found it. `ResetColor` followed by the background is the route that uses only documented
  behavior. The lock stops two package writes from interleaving, which separate calls to a synchronized
  `Console.Out` do not.
- **Consequences:** The lock covers this writer only; it cannot coordinate with other code that writes to the
  console. Colors cannot be read back on a Windows test host without a console, so restoration is asserted on
  Linux and macOS only. Nothing is transactional: text written before a failure stays written.

## Legacy facade

Settled in the legacy facade phase, within the authority Shaun gave that phase, and accepted by him when he
reviewed it. How each published behavior is kept is listed in
[compatibility.md](compatibility.md#how-the-legacy-class-is-built).

### D-44: The legacy class shares the layout and nothing else

- **Status:** Approved. Implemented.
- **Decision:** `CalendarGenerator.ConsoleCalendar` takes the column and the row of every day from
  `MonthLayout.Create(Year, Month, DayOfWeek.Monday)`. Its own grid algorithm is removed. Everything else it
  does stays in private code inside the class: the title, the weekday row, each console call and its color, the
  resets, the line endings and the order of failures. It does not use `MonthRenderer`, `DayState`,
  `CalendarTheme`, `MonthWriter` or `ConsoleMonthWriter`.
- **Reason:** The grid is the one part where the legacy class and the modern API must agree and where two
  implementations could drift. Every other legacy behavior is something the modern layers deliberately do
  differently: a Gregorian title, visible markers, early rejection of undefined colors, restored colors, whole
  segments, one line terminator, a lock. Routing the legacy output through them would mean either changing
  published behavior or adding legacy switches to the modern API. The month and the year are still checked by
  `DateTime.DaysInMonth`, so that the exception a caller receives is the runtime's own, as in 1.0.0.
- **Consequences:** A defect in `MonthLayout` now shows in the legacy output as well, and both the modern and
  the legacy tests catch it. A change to how the modern API renders or writes cannot change the legacy class.
  The class stays the place to read when a legacy behavior is in doubt.

### D-45: No public compatibility profile

- **Status:** Approved. Implemented by adding nothing. Resolves the former open question Q-06.
- **Decision:** The modern API has no option, overload or type whose purpose is to reproduce legacy output. The
  legacy title, with the month name from the culture's own calendar, exists only inside the facade.
- **Reason:** The facade reproduces 1.0.0 exactly without any such option, so the only known user of one would
  have been the facade itself. A profile would have put a published defect into the modern surface for good.
- **Consequences:** The modern title stays Gregorian in every case
  ([D-33](#d-33-the-culture-supplies-language-only)). Campertron's current title also follows the culture's own
  calendar, so under a culture such as `fa-IR` or `th-TH` the package's title differs from Campertron's today,
  on purpose. Whether that matters to Campertron is for the parity fixtures to show and for the Campertron
  project to decide; a caller that wants a particular title can already supply it as quoted text
  ([D-34](#d-34-the-title-is-a-date-format-string-a-suffix-and-an-alignment)).

### D-46: The original implementation is recorded call by call

- **Status:** Approved. Implemented as tests (`Facade/PreFacadeBaselineTests`).
- **Decision:** Two digests recorded from the class at commit `64995b6`, the last with the original 1.0.0
  implementation, are part of the suite: every call on the console's writer with its method and text, and the
  color of every call. They are not to be recorded again from current code.
- **Reason:** The digests recorded from the published assembly do not distinguish `Write(int)` from
  `Write(string)`, which a caller's own writer can, and cover fewer cases. Rebuilding the class needed a record
  that would notice such a change, and later changes to the class need the same.
- **Consequences:** These digests, like the published ones, define "unchanged" for the legacy class.

## Campertron parity

Settled in the parity fixtures phase, within the authority Shaun gave that phase. What was recorded and how it
is compared is described in `ConsoleCalendarGenerator.Tests/Parity/Campertron/README.md`.

### D-47: Campertron parity is shown with recorded fixtures

- **Status:** Approved. Implemented as tests. Resolves the former open question Q-09.
- **Decision:** What Campertron's live renderer returns is recorded as data: 47 calls of `Calendar.Lines` at
  Campertron commit `3ded294`, each with its input and the lines and spans it returned, in JSON files that are
  embedded in the test assembly. The tests reproduce every one with the public modern API and a test-only
  adapter. Nothing in this repository references, builds against or runs Campertron, and nothing named after
  Campertron exists in the package. The program that recorded the fixtures is not kept here. There are no
  fixtures for Campertron's older calendar value stream (`GenerateCalendar`).
- **Reason:** Recorded data makes the target exact, reviewable and usable offline, and keeps the two
  repositories independent ([D-19](#d-19-campertron-adoption-happens-elsewhere)). The value stream is still
  produced in Campertron and pinned by its own tests, but no renderer there consumes it: the console skips it
  and the results page has no case for it. Matching it would mean shaping the package after plumbing that
  nothing displays; what becomes of it is for the Campertron project.
- **Consequences:** The public API proved sufficient: no production code changed, and no profile, option or type
  was added for a consumer. The fixtures describe one commit. They are recorded again only when Campertron's
  renderer has changed and the new behavior is to become the target, from a copy outside this repository. Under
  cultures whose calendar is not Gregorian the titles differ on purpose
  ([D-45](#d-45-no-public-compatibility-profile)); those two fixtures compare everything except the title.

## User-facing material

Settled in the user-facing material phase, within the authority Shaun gave that phase.

### D-48: Four public documents at the root, and one README for GitHub and NuGet

- **Status:** Approved. Implemented.
- **Decision:** The documents for users and contributors are `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md` and
  `SECURITY.md`, at the repository root. The README is also the package readme, unchanged. Support is a line in
  the README, not a file. There is no second guide, no generated API reference and no site. `docs/` holds
  `ai-context/` (maintenance notes) and `images/` (the README's images) and nothing else; the copy of the README
  and the screenshots under `ConsoleCalendarGenerator/docs/` are deleted.
- **Reason:** The API is small enough for one page plus IntelliSense. Two readmes had already drifted apart
  once. Policy files are kept to what is true of a one-maintainer package.
- **Consequences:** The README has to satisfy nuget.org, which is stricter than GitHub: absolute links only
  (links to its own headings are fine), no raw HTML, images from an allowed host. See
  [packaging-and-releases.md](packaging-and-releases.md#the-readme-on-nugetorg). `PublicDocumentationTests`
  lists every Markdown file outside `docs/ai-context/`, so adding one is a visible decision.

### D-49: The README's code and output are the example program's

- **Status:** Approved. Implemented.
- **Decision:** `ConsoleCalendarGeneratorExample/Examples.cs` is the source of truth for every C# block in the
  README, and `expected-output.txt` beside it for every output block. Each example is the code between a
  `// <Name>` comment and its `// </Name>` comment; the README shows it character for character. The tests
  compare them, run the program and compare what it prints with the file. `Examples.cs` is also compiled into
  the test assembly, so every example compiles against both builds of the library.
- **Reason:** A README example that does not compile, or output that the library does not produce, is the most
  common way for documentation to go wrong. Copying with a check is simpler and more readable than generating
  the README or parsing C# out of Markdown.
- **Consequences:** To change an example, change `Examples.cs`, run the program, update `expected-output.txt`
  and copy both into the README; the tests say what is still different. The example project has
  `RollForward=LatestMajor` so that `dotnet run` works with the pinned SDK, whose runtime is newer than the
  `net10.0` the example targets. The example replaces the sample that accompanied 1.0.0; the frozen copy of
  that sample in `eng/consumer-smoke/OriginalSample/` is untouched and still must compile and run.

### D-50: One screenshot, drawn from the example's real terminal output

- **Status:** Approved. Implemented.
- **Decision:** The README has one image, `docs/images/month-with-theme.png`: the themed month of the example.
  Everything else is shown as text. The image is made from the bytes the example writes to a pseudo-terminal,
  with the colors of a fixed palette; the steps are in
  [development.md](development.md#the-readme-the-example-and-the-screenshot). It is not a photograph of a
  terminal window.
- **Reason:** Text blocks show plain output better than pictures do, can be tested, and can be read by a screen
  reader. Color is the one thing text cannot show. Drawing from captured output keeps the picture honest and
  free of window decoration, prompts, user names and paths.
- **Consequences:** The marked days in the image also carry their markers, so the image does not teach that
  color alone marks a day. `DocumentationLinkTests` fails if an image exists that the README does not show, or
  the reverse.

### D-51: The legacy class is documented as it behaves

- **Status:** Approved. Implemented. Replaces [D-11](#d-11-public-xml-documentation-is-deferred).
- **Decision:** `CalendarGenerator.ConsoleCalendar` and its 17 public members have XML documentation that
  describes what 1.0.0 does, including what a new design would not do: the constructor arguments that color
  another element than their name says, the month name from the current culture's own calendar, the colors
  that are reset and not restored, the mixed line ends, and the exceptions. The comments recommend the
  parameterless constructor and the setters, and point to the modern API for new code. `CS1591` is no longer
  suppressed anywhere. No `[Obsolete]` attribute is added
  ([Q-05](open-questions.md#q-05-when-do-legacy-members-become-obsolete)), and the comments promise nothing
  about 2.0.
- **Reason:** A user of the legacy class sees these comments in the editor at the moment of calling it, which
  is where a wrongly named argument does its damage. Documenting the defect is not correcting it.
- **Consequences:** The file changed in comments only; the test suite and the digests are the proof. The
  comments must stay true to [compatibility.md](compatibility.md). `XmlDocumentationTests` checks that every
  public member of both builds is documented and that the constructor comments say what the arguments color.

### D-52: Security reports go through a public issue until a private channel exists

- **Status:** Approved as the accurate statement of the present. To be revisited
  ([Q-11](open-questions.md#q-11-is-private-vulnerability-reporting-turned-on)).
- **Decision:** `SECURITY.md` says that the latest published version receives fixes, that there is no private
  channel for reports, and that a reporter should open an issue that says a problem exists without the details.
  It gives no e-mail address.
- **Reason:** GitHub's private vulnerability reporting is off for the repository (checked through the API on
  2026-11-27), turning it on is a repository setting that needs Shaun, and no address has been approved for
  publication. A policy that promised confidentiality would be false.
- **Consequences:** When private reporting is turned on, change `SECURITY.md` in the same change.

### D-53: Names a culture supplies are documented as depending on the system

- **Status:** Approved as documentation of what exists. Whether the behavior should change is open
  ([Q-12](open-questions.md#q-12-should-weekday-labels-be-the-same-on-every-operating-system)).
- **Decision:** The README, the XML comments of `MonthRenderOptions` and `WeekdayLabelStyle.Shortest`, and the
  example say that month and day names come from the culture data of the runtime and the operating system and
  can differ between systems, with the observed case: single-letter shortest day names on Windows. The README
  shows how to supply labels that are the same everywhere (`WithWeekdayLabels`), as a tenth example. No
  behavior changed.
- **Reason:** The README promised "the same on every machine", and the Windows CI job showed its first
  example printing a different weekday row there. The promise that holds is narrower: nothing is read from the
  process, so the same options give the same text on the same system
  ([D-32](#d-32-rendering-defaults-are-fixed-and-read-nothing-from-the-process)), and the invariant culture,
  the layout and supplied labels are the same everywhere.
- **Consequences:** `expected-output.txt` is the output of Linux and macOS, and the complete comparison with it
  is inconclusive on Windows; see [testing.md](testing.md#the-documentation-tests). Do not describe output
  under a real culture as identical on every machine.
