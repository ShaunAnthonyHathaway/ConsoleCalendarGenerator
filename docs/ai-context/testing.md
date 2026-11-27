# Testing

## Current tests

`ConsoleCalendarGenerator.Tests` holds **1,973 tests**: 165 tests of the legacy API, 25 tests of the legacy class
as a facade, 38 tests of the frozen source of 1.0.0, 202 migration tests that compare the library with it, 184
tests of the layout model, 275 tests of text rendering, 98 tests of day states and markers, 52 tests of the
theme, 58 tests of the writers, 756 Campertron parity tests (most of them one test per recorded case), 101
tests of the documentation and the example, 2 tests of the modern public surface, 16 tests of the version
identity and 1 test about the build setup. It targets `net11.0` and uses NUnit 5.0.0 with NUnit3TestAdapter 6.3.1 and
Microsoft.NET.Test.Sdk 18.12.0.

### Purpose

The characterization tests record what `CalendarGenerator.ConsoleCalendar` does in published 1.0.0, before any
of it is refactored. They describe the behavior that exists, **including behavior that is wrong**. They define
"unchanged" for everything that 2.0 does not correct, which is everything except five listed behaviors. See
[compatibility.md](compatibility.md).

A test whose name contains `Characterized1xBehavior` pins down something that is not the intended design. It
is changed in the pull request that implements the correction of that behavior, and at no other time. Two
such tests remain; they pin the exception for a null list (D-58). The six that pinned the constructor colors
were replaced when D-57 was implemented: `Legacy/ConstructorColorTests` now states the corrected behavior under
names that say so, and what 1.0.0 did is in `Historical/Published100ConstructorTests`.

The tests in `Historical/` describe 1.0.0 itself, through its frozen source. They are never changed to match
the library: when the library is corrected they keep saying what 1.0.0 did. The tests in `Migration/` hold the
two side by side; see [The migration tests](#the-migration-tests).

The facade tests in `Facade/` are characterization tests as well. They were added when the class was rebuilt on
`MonthLayout`, they describe the same published behavior more closely, and every one of them also passes against
the original 1.0.0 implementation.

The tests of the modern API (layout, rendering, states, themes, output) are ordinary specification tests: they
say what the modern API must do, and a failure there means the code is wrong.

### Organization

| Fixture | Tests | Covers |
|---|---|---|
| `Legacy/PublicApiTests` | 5 | The public surface of the legacy namespace by reflection, and source compatibility with named arguments |
| `Legacy/ConstructorColorTests` | 19 | Which color each constructor argument controls in 2.0: the one it is named for. The colors held, the colors drawn, that a constructor does what the setters of the same names do, and where an argument that is not a color fails |
| `Legacy/ColorAccessorTests` | 28 | Getters, setters, defaults, undefined color values |
| `Legacy/MonthLayoutTests` | 26 | The text of a month, line by line, and the layout rules for every month from 1900 through 2100 |
| `Legacy/LineEndingTests` | 9 | Which line endings are written |
| `Legacy/GenerateYearTests` | 9 | `GenerateYear` against twelve `GenerateMonth` calls |
| `Legacy/SelectedDateTests` | 14 | Which dates are highlighted |
| `Legacy/CultureTests` | 18 | What follows the culture and what does not |
| `Legacy/ValidationTests` | 18 | Exceptions for invalid input |
| `Legacy/ConsoleBehaviorTests` | 10 | The individual writes, the color of each, and what is left behind |
| `Legacy/RedirectedOutputTests` | 4 | The real console streams, through a child process |
| `Legacy/TerminalTests` | 3 | Color on a terminal (`Terminal` category) |
| `Legacy/Published100BaselineTests` | 2 | Digests recorded from the published 1.0.0 assembly |
| `Facade/PreFacadeBaselineTests` | 2 | Digests of every call on the console's writer, and of the color of every call, recorded from the original implementation |
| `Facade/FacadeLayoutTests` | 6 | That the legacy class draws every day where `MonthLayout` puts it |
| `Facade/FacadeCompatibilityTests` | 17 | The places where the legacy class must stay different from the modern API: the runtime's own exception, colors reset and failing late, the bare line feed, no marker, the culture's own calendar, and no lock |
| `Historical/Published100SourceTests` | 8 | That the frozen source is the original file byte for byte, that its header records where it comes from, that its class is named apart from the library's and has the same public members, and that nothing of it is in the library or can be packed |
| `Historical/Published100DigestTests` | 4 | That the frozen source reproduces the four values recorded from 1.0.0: two from the published assembly, two from the class before the facade |
| `Historical/Published100ConstructorTests` | 15 | The constructors of 1.0.0, complete: the colors each one held, the colors drawn, and where an argument that is not a color failed |
| `Historical/Published100BehaviorTests` | 11 | What 1.0.0 did in the five places that 2.0 corrects, written out against the frozen source: constructor colors, the null list, the bare line feed, the two blank lines after a Sunday, the title under a culture with another calendar |
| `Migration/LegacyMigrationTests` | 141 | The library against the frozen source: 134 scenarios of the matrix, one test each; every month from 1900 through 2100 and of the years 1 and 9999; every culture whose calendar is not Gregorian; the selections the recorded digests use; 500 generated scenarios; and that the five corrections are the five decisions |
| `Migration/ConstructorColorMigrationTests` | 41 | The constructor correction (D-57) as the exact difference between 1.0.0 and the library: the colors held and drawn by each constructor in both; that with the same arguments only colors differ, and among them only the title's and the weekday row's; how a program keeps its appearance; 14 cases of where an undefined argument fails in each; and that both descriptions of the constructors match what they describe |
| `Migration/LegacyObservationTests` | 20 | The tools those tests use, tested with code written in the tests: what an observation records, that a comparison finds each kind of difference and names only the aspects that differ, and that a correction, in either of its two forms, changes the expectation only once it is implemented |
| `Layout/FirstDayOfWeekTests` | 53 | The weekday of each column and where the 1st and the last day land, for each of the seven first days of the week, with hand-written grids |
| `Layout/MonthShapeTests` | 77 | Month lengths, leap years, four, five and six weeks, January 0001 and December 9999, and the step from each month to the next |
| `Layout/ExhaustiveLayoutTests` | 20 | Every layout against an independent reference and against the layout rules: 1900 through 2100, and all 839,916 layouts there are |
| `Layout/LayoutValidationTests` | 20 | Exception type, parameter name, value and checking order for invalid arguments |
| `Layout/LayoutImmutabilityTests` | 6 | That the collections cannot be cast and changed, and that nothing public can be set |
| `Layout/LayoutDeterminismTests` | 8 | The same result every time, under any culture, from many threads, with no effect on the console or the process |
| `Rendering/DefaultRenderingTests` | 17 | The defaults, and the exact text for months of every length, every weekday of the 1st, and four, five and six weeks |
| `Rendering/FirstDayOfWeekRenderingTests` | 21 | The exact text and the label weekdays for each of the seven first days of the week |
| `Rendering/SegmentTests` | 14 | The exact segments of each kind of line, the rules every segment obeys, and that results cannot be changed |
| `Rendering/CultureRenderingTests` | 37 | What the culture decides and what it does not, Gregorian titles under Persian and Thai cultures, every culture of the runtime, and independence from the current cultures |
| `Rendering/WeekdayLabelTests` | 40 | The two label styles, supplied labels and their order, copying, fitting labels into cells without splitting characters, and invalid labels |
| `Rendering/TitleTests` | 46 | Title formats, the suffix, left and centered placement, the indent, a title with no text, and invalid formats |
| `Rendering/CellSizingTests` | 39 | The exact text for several cell widths and gaps, right alignment, the grid width, and the sizes refused |
| `Rendering/TextConversionTests` | 16 | The text with each line terminator, and the terminators refused |
| `Rendering/RenderOptionsTests` | 18 | That options are immutable, that every change renders differently, and that the same settings render the same |
| `Rendering/RenderValidationTests` | 16 | Exception type, parameter name, value and checking order for `Render` |
| `Rendering/RenderDeterminismTests` | 7 | The same result every time and from many threads with different cultures, with no effect on the console or the process |
| `Rendering/RenderedLayoutTests` | 4 | That every month from 1900 through 2100, for every first day of the week, shows exactly what its layout says |
| `States/DayStateTests` | 43 | What a state's name and marker may be, and that a state cannot be changed |
| `States/DayStateAssociationTests` | 27 | Which dates select which days, which segments carry the state, the input collections, and what is refused |
| `States/StatePrecedenceTests` | 8 | One state per day, and the first entry for a date wins |
| `States/MarkerRenderingTests` | 19 | The exact text and segments of marked months, markers measured in text elements, markers that do not fit, and that nothing else moves |
| `States/UnmarkedRenderingRegressionTests` | 1 | A digest of unmarked rendering recorded before day states existed |
| `Themes/CalendarThemeTests` | 52 | The color of every kind of segment, state colors by name, the fallback, colors and names refused, and immutability |
| `Output/MonthWriterTests` | 21 | The exact text for any writer and line terminator, that the writer is left alone, failures, and that the console is never touched |
| `Output/ConsoleMonthWriterTests` | 31 | The text and every single write to the console, the color of each, the caller's colors afterwards and after a failure, and calls from several threads |
| `Output/ConsoleMonthWriterProcessTests` | 3 | The real standard output of a child process, redirected |
| `Output/ConsoleMonthWriterTerminalTests` | 3 | Color on a terminal (`Terminal` category) |
| `Parity/Campertron/FixtureIntegrityTests` | 14 | The fixtures themselves: provenance, strict form, consistency, and that they cover what they are meant to |
| `Parity/Campertron/TextParityTests` | 141 | Every line of every recorded case, character for character |
| `Parity/Campertron/SpanParityTests` | 233 | Every span of every recorded case: text, order, boundaries and color; which spans are the marked days |
| `Parity/Campertron/MarkerAccessibilityTests` | 190 | That a marked day is recognizable without color, and that the title explains the marker |
| `Parity/Campertron/CultureParityTests` | 25 | Titles under Gregorian cultures, the intended difference under `th-TH` and `fa-IR`, and identical rows everywhere |
| `Parity/Campertron/ParityDeterminismTests` | 51 | The same spans every time and on every thread, whatever the order of the dates |
| `Parity/Campertron/WidthTests` | 96 | That no line exceeds the width Campertron writes to, and that the indent is part of the line |
| `Parity/Campertron/ApiSufficiencyTests` | 6 | That the adapter can only have used public API, and that nothing is built against Campertron |
| `Documentation/ExampleProgramTests` | 20 | The example program: it runs with redirected output and no input and prints `expected-output.txt` on every system; the primary example prints the same text everywhere; the one culture-derived weekday row is what the machine's culture data gives; the output does not depend on the culture the process starts with; its source follows the rules the README recommends |
| `Documentation/ExampleProgramTerminalTests` | 3 | The example on a pseudo-terminal: the same text with color added, nothing sent but color changes, default colors given back (Terminal category) |
| `Documentation/ReadmeExamplesTests` | 9 | Every C# block of the README is an example of `Examples.cs` character for character, and every output block is lines of `expected-output.txt` |
| `Documentation/DocumentationLinkTests` | 20 | Links and images of the four public documents point at files that exist; what nuget.org needs of the README; no image that nothing shows |
| `Documentation/PublicDocumentationTests` | 37 | Which documents exist; the frameworks, versions and commands they name; that the version in development is called unpublished; that the README and the changelog describe the constructor correction; nothing machine-specific and nothing about a consumer |
| `Documentation/XmlDocumentationTests` | 10 | Every public type, member and parameter of the built documentation file has text; the comments of the legacy constructors say that each argument colors what it is named for and that 1.0.0 did otherwise; no documentation warning is suppressed |
| `ModernApiTests` | 2 | The modern public surface by reflection, enum values included, and that the assembly exports nothing else |
| `VersionIdentityTests` | 16 | The assembly version, the file version and the informational version of the library build under test; that the major version of the assembly is that of the package; that nothing says it is 1.0.0; that the project file sets those versions; that package validation compares with 1.0.0 and nothing is suppressed; that the maintenance notes name the development version the repository builds |
| `LibraryBuildTests` | 1 | That the run uses the library build it was built for |

`Support/` holds the helpers. The ones to know:

- `ConsoleCapture` and `RecordingWriter`: run code with `Console.Out` replaced, record every write separately
  with the console colors in effect, and restore all global state afterwards.
- `LegacyReference`: the month layout written out from the rules, independently of the library, so the library
  can be compared with it for every month.
- `BehaviorDigest`: condenses a large amount of output into a hash, for the published-baseline tests.
- `ChildProcess`, `ChildScenario` and `Program.cs`: start the test assembly as a separate process that draws one
  calendar to its real console streams. `ChildProcess.RunProgram` starts another program of the repository the
  same way, which is how the example application is run.
- `Repository`: finds the repository root above the test assembly and reads its files with `\n` line ends,
  for the documentation tests. `Markdown`: the fenced blocks, links, images and heading anchors of a document.
  `ExampleProgram`: the marked examples of `Examples.cs`, the expected output and the built program.
- `GlobalStateGuardAttribute`: applied to the assembly; fails any test that leaves global state changed.
- `Ordinal`: substring constraints that compare character by character. NUnit's `Does.Contain` compares with the
  current culture, which ignores control characters and so "finds" an escape character in any text.
- `ReferenceCalendar`: the Gregorian calendar and a month's grid from whole-number arithmetic alone, with no
  `DateTime` and no calendar class, filled one day at a time. It shares nothing with `MonthLayout`, and it is
  itself tested against dates whose weekday is known and against `DateTime` for every month.
- `LayoutPicture`: writes a layout out as lines of text so that a test can spell out the grid it expects. It is
  a test aid, not the library's rendering.
- `Rendered`: the lines of a rendered month as strings, and its segments described as `Kind[text] weekday date`,
  so that a test can spell out the text and the segments it expects.
- `ProcessState`: a snapshot of the process-wide state that pure code must leave alone, and a way to run code
  under given current cultures and put them back.
- `ModernChildScenario`: the month, the state and the theme that the child process writes with
  `ConsoleMonthWriter`, and its plain text. `Program.cs` has one command for the legacy scenario and one for
  this one.
- `TerminalOutput`: recognizes and removes escape sequences, for the modern terminal tests.
- `CallRecordingWriter`: like `RecordingWriter`, but it also records which method was called for each write
  (`Write(string)`, `Write(int)`, `WriteLine()`, `WriteLine(string)`), which a caller's own writer can tell apart.
  It keeps the calls as text, for the digests, and one by one, for the migration tests.

The helpers of the migration tests are in `Migration/`, not in `Support/`:

- `LegacySubject` and `ILegacyCalendar`: the two implementations of the legacy calendar, the library's class
  and the frozen source of 1.0.0, behind one interface that passes every call straight on. `BehaviorDigest` and
  the scenarios of `PreFacadeBaselineTests` take a subject, so that the same code condenses the output of
  either. A third subject, `LibraryWithTheConstructorColorsOf100`, is the library's class given the colors
  that 1.0.0 gave a calendar made with the same constructor arguments; the recorded digests are computed with
  it.
- `ConstructorColorRoles`: which element each constructor argument colors in 1.0.0 and in 2.0, as the setter
  calls with the same effect. Both lists are tested against the implementation they describe.
- `RenderedRoleColors`: the color the title, the weekday row, the days and the selected days of a drawn month
  were written in, read from an observation.
- `LegacyScenario`: one use of the legacy calendar as data: constructor arguments, setters, one drawing call,
  the line terminator, the cultures and the caller's colors.
- `LegacyObservation`: everything a caller can observe about one use, and the comparison of two observations
  aspect by aspect (`LegacyAspects`).
- `MigrationMatrix`: the scenarios, in one group for each of the five corrections and one for everything else.
- `ApprovedCorrection`: the five corrections, each with its decision, its scenarios and, once it is
  implemented, what it changes, in one of two forms.

### What a run executes

| System | Normal run (`TestCategory!=Terminal`) | Not run there | Terminal run |
|---|---|---|---|
| Linux | 1,961 pass | 3 Windows-only | 9 pass |
| macOS | 1,961 pass | 3 Windows-only | not part of CI |
| Windows | 1,892 pass | 72 that need Linux or macOS | not part of CI |

The parity tests have no platform conditions. A few of them compare titles that come from the operating
system's culture data, and are reported as inconclusive, not failed, on a system whose data differs from the
system the fixtures were recorded on; the numbers above are for systems where it does not differ.

No test is inconclusive on any of the three systems. `dotnet test` lists an inconclusive test as "Skipped"
above its summary and leaves it out of the totals, without the reason, so a total lower than the one above is
the sign to look for.

The same numbers apply to each of the two library builds. CI runs the normal run against both.

### Isolation

The library writes to the process-wide console and reads the process-wide culture, so:

- The assembly is `[NonParallelizable]`. Never run these tests in parallel.
- Every test goes through `ConsoleCapture`, which restores `Console.Out`, both cultures and the console colors.
- `GlobalStateGuardAttribute` checks after each test that nothing leaked, reports it, and puts the state back so
  that one leaking test does not fail the ones after it.
- Tests never change environment variables of the test process. A child process gets its own.

The layout and rendering tests touch no global state and would be safe to run in parallel, but they live in the
same non-parallel assembly and run one after another like the rest. A few of them use `Parallel.For` inside the
test to create and read results from several threads. Tests that check independence from the current cultures
set them through `ProcessState.UnderCultures`, which puts them back. The two folders together take a few
seconds; the slowest test is the one that checks every layout there is.

Rendering tests that use a real culture (`en-US`, `de-DE`, `th-TH`, `fa-IR` and so on) compare with what the
runtime reports for that culture, and assert literally only what cannot reasonably change, such as the year in
a title. They use `Assume.That` for the culture's availability, so on a system without that culture's data they
are reported as inconclusive rather than failed. Exact text is tested with the invariant culture and with
cultures built in the test.

### Platform-specific tests

- **Linux and macOS only (72):** 69 tests that read console colors back (26 legacy; 2 for the facade; 8 for the
  frozen source of 1.0.0; 11 for the migration tests, 2 of them for their observations and 9 for the colors the
  constructors draw; 1 each that check the layout, the renderer and the plain writer leave the colors alone;
  19 for the console writer's colors and their restoration), and 3 that assert line-feed-only output (2 legacy,
  1 for the console writer).
  On those systems the runtime keeps the current colors in the process, so they can be read without a
  terminal. On Windows the colors live in the console, and a test host without one ignores changes. These tests
  are marked `[Platform(Exclude = "Win")]`.
- **Windows only (3):** the legacy class's real mixed line endings (2), and the console writer's uniform
  `\r\n` endings (1).
- The migration tests themselves run on all three systems and compare colors there too. On Windows every color
  reads the same, so there they can show a difference in text, calls and exceptions, and not one in colors.
- What Windows does verify for the console writer: the text, every individual write, the line terminator, the
  redirected output of a real child process, failures passed on, and writes from several threads. What it
  cannot verify is which color a write was made in, or that colors are restored.
- Add a platform condition only where the behavior really differs by platform. Text, write-sequence, API and
  validation tests run everywhere.

### The Terminal category

Nine tests, three for the legacy class, three for the modern console writer and three for the example
program, need a terminal database entry for `xterm-256color`, and seven of those also need Linux and the
util-linux `script` program to create a pseudo-terminal. They are in the `Terminal` category, are excluded from
the normal run by filter, run in their own CI step on Linux, and ignore themselves where they cannot run. They
check that color sequences are present or absent and that the text is unchanged; they do not compare escape
sequences byte for byte, because the .NET runtime writes those, not the library. Keep anything that depends on
the machine's terminal setup in this category, out of the normal run.

### The published-1.0.0 digests

`Published100BaselineTests` compares two SHA-256 values with ones recorded by running the same helper code
against the assembly from the real 1.0.0 package on nuget.org. One covers text and write sequence on every
platform; the other covers color sequence where colors can be read back. The package is not downloaded by the
tests and is not checked in. The test file says exactly how the values were recorded.

Do not regenerate these values from the repository's own source. If they stop matching, the library's behavior
changed.

`Historical/Published100DigestTests` compares the frozen source of 1.0.0 with the same two values, computed by
the same code.

Since the constructor colors were corrected (D-57), the color digest of the library is computed with
`LegacySubject.LibraryWithTheConstructorColorsOf100`; see
[When a correction changes a recorded digest](#when-a-correction-changes-a-recorded-digest). The text digest
uses the parameterless constructor only and is computed as before.

### The pre-facade digests

`Facade/PreFacadeBaselineTests` compares two more SHA-256 values, recorded by running the code in that file
against the legacy class at commit `64995b6`, the last one with the original 1.0.0 implementation. They look
closer than the published digests and cover more:

- For every write they record which method of the console's writer was called. The original writes a two-digit
  day in the Monday column with `Console.Write(int)` and everything else with strings; the published digests
  cannot tell those apart.
- They cover selected dates in many patterns, month names of every length, both line terminators, all five
  constructors, setters, undefined colors in each of the four places, and invalid months, years and lists in
  combination, with the type of each exception.
- One value covers the calls and is the same on every system. The other covers the color of every call and is
  only checked where colors can be read back.

They use the invariant culture and cultures built in the test only. Like the published digests, they are not to
be recorded again from the current source. `Historical/Published100DigestTests` compares the frozen source of
1.0.0 with these two values as well. Since D-57 both are computed for the library with
`LegacySubject.LibraryWithTheConstructorColorsOf100`, like the published color digest.

### The frozen source of 1.0.0

`Historical/Published100/ConsoleCalendar.cs` is the source of `CalendarGenerator.ConsoleCalendar` as published in
1.0.0, compiled into the test assembly as `ConsoleCalendarGenerator.Tests.Historical.Published100.ConsoleCalendar`
([D-67](decisions.md#d-67-100-is-kept-as-a-frozen-source-and-differences-are-shown-by-migration-tests)). It is
what the library is compared with once the library itself is no longer 1.0.0.

**Where it comes from.** The package on nuget.org has no symbols and names no commit, so the source had to be
identified:

- The repository has had two versions of the file with the original implementation. Git blob `02b3c1c` is the
  file from commit `93f2d5d` (2023-06-24) until commit `def8a3b` (2023-08-16). Git blob `83c7945` is the file
  from `def8a3b` until `64995b6`, the last commit before the facade.
- They differ in one thing: the first has a private method `ConvertToShortDay`, which the second replaces with
  `DayOfWeek.ToString().Substring(0, 2)`. Both give the same two letters.
- 1.0.0 was published on 2023-06-25, and the assembly in the package contains the name `ConvertToShortDay`. So
  the published assembly was built from the first, and that is the one frozen here. The digests of
  `PreFacadeBaselineTests` were recorded from the second.
- Both were put through all four recorded digests when the reference was added, and both reproduce all four.

The file's header and `Historical/Published100Source.cs` record the commit, the Git blob, the SHA-256 of the
original file and the SHA-256 of the package and of its assembly. The source is this repository's own, under
the same MIT license; the header carries the copyright line.

**What differs from the original file,** and nothing else: a comment in front of it; the byte order mark, which
could not stay in the middle of a file; the namespace; and a line feed at the end.
`Published100SourceTests` undoes those four and compares the result with the SHA-256 of the original, so the
file cannot change by a single character without a test failing.

The rules:

- **Never edit it.** Do not format it, rename anything in it, modernize it or correct it. Its defects are the
  record. If a test says it changed, restore it from Git.
- **Never record anything from the library into `Historical/`.** Those tests state what 1.0.0 did.
- It is test code. It must not move into the library, be referenced from it, or be packed.
  `eng/verify-package.sh` and `Published100SourceTests` both check that nothing of it is there.
- It builds with no warnings. `.editorconfig` marks it as generated code so that formatters and analyzers leave
  it alone; no warning is suppressed for it.
- It runs in the test process, with the runtime of the tests, like the library does. It needs no network, no
  package and no compilation at run time.

### The migration tests

`Migration/LegacyMigrationTests` runs the library's class and the frozen source of 1.0.0 through the same
scenarios and compares what a caller can observe. The rule it enforces: **the library does what 1.0.0 did, in
every scenario and every aspect, apart from the corrections that are implemented.** One is: the constructor
colors (D-57). In everything else the two must be identical.

What is compared (`LegacyAspects`):

| Aspect | What it is |
|---|---|
| `StoredColors` | What the four getters return after the constructor and the setters |
| `Text` | The complete text, with the writer's line terminator where the writer added one |
| `Calls` | How the text was divided into calls on the console's writer: the method and the text of each |
| `Colors` | The foreground and background color in effect for every call, and the colors left behind |
| `ExceptionType` | The type of the exception that ended the use, or that there was none |
| `ExceptionParameterName` | The parameter an `ArgumentException` names |
| `ExceptionMessage` | The message. It is the runtime's text, so it is only compared between two observations made in the same process under the same UI culture, which is how these tests make them |

A failure names the aspects that differ and the first place where each does.

The scenarios (`MigrationMatrix`) are grouped by what they are about: the constructors that take colors; a null
list; the line terminator; months that end on a Sunday; titles under cultures with another calendar; and
behavior that no correction is about (invalid months and years, colors that are not defined, selected dates,
month names of every length, the first and the last month there is). Every scenario is compared in every
aspect, whichever group it is in. Around the matrix, four tests compare in bulk: every month of 203 years,
every culture of the runtime whose calendar is not Gregorian, the selections that the recorded digests use,
and 500 scenarios from a generator with a fixed seed.

**How a correction is marked.** `ApprovedCorrection` lists the five, each with its decision. A correction has
two functions, both null until the library implements it. The pull request that implements one sets the one
that states the correction exactly:

- `EquivalentIn100` takes a scenario and returns the scenario in which 1.0.0 does what the corrected library
  does. The constructor colors are stated this way: a scenario that uses a constructor with colors becomes
  the same scenario with the parameterless constructor and the setter for each argument. The setters of 1.0.0
  were right, so the frozen source run in that scenario is exactly the corrected behavior, including where a
  color that is not defined fails.
- `Corrected` takes a scenario and what was expected of the library before, and returns what is expected now:
  for example the same observation with another exception type, for the scenarios that pass a null list, and
  the argument unchanged for every other scenario. It is for a result that no use of 1.0.0 produces.

From then on the library is compared with 1.0.0 plus that correction, still in every aspect and every
scenario. So:

- a correction made in the library without being marked fails the tests;
- a correction marked without being made fails the tests;
- anything else that changes along the way fails the tests, because nothing was excluded from the comparison
  to let the correction through.

The rules:

- **The function of a correction says what an approved decision says and no more.** It is written in the pull
  request for that decision. Do not set one ahead of time, and do not add a sixth correction without a decision by
  Shaun; `LegacyMigrationTests` checks the five identifiers and that each is a decision in `decisions.md`.
- **Do not remove a scenario or narrow a comparison** to make a change pass. Add scenarios freely.
- **Real cultures are named, never required.** A scenario that names a culture the machine lacks is
  inconclusive, not failed. Which cultures have a calendar that is not Gregorian depends on the system: on the
  Linux system the tests were written on, `fa-IR`, `ps-AF` and `th-TH` do, and `ar-SA` does not.
- The corrected behavior itself is stated by tests of its own, added with the correction. The migration tests
  say that nothing else changed. For D-57 those are `Legacy/ConstructorColorTests`, with the literal 2.0
  expectations, and `Migration/ConstructorColorMigrationTests`, with 1.0.0 and 2.0 side by side.

### When a correction changes a recorded digest

The four recorded values describe 1.0.0. Two tests compare the library with them (`Published100BaselineTests`,
`PreFacadeBaselineTests`), and each correction changes what at least one of those digests covers. In the pull
request whose correction does:

- **Never record a value again from corrected code** and call it the baseline. The values stay what they are.
- `Historical/Published100DigestTests` keeps comparing the frozen source with all four, unchanged, for good.
- The test that compares the *library* with a value it no longer reproduces is removed or narrowed in that pull
  request, which says so. It pins a defect the library no longer has. What it protected is carried by
  `LegacyMigrationTests`, which covers what the digests cover (every month from 1900 through 2100, the years 1
  and 9999, both line terminators, all five constructors, setters, the same selections, month names of every
  length, colors that are not defined in each place, invalid months, years and lists) and compares it call by
  call instead of as one hash.

**What was done for the constructor colors (D-57).** Three of the four library-side digests cover calendars
made with constructors that take colors: the published color digest and both pre-facade digests. None was
removed and none was recorded again. They were narrowed: the library's calendars are made through
`LegacySubject.LibraryWithTheConstructorColorsOf100`, which gives each the colors 1.0.0 gave it, and the three
values are reproduced exactly. The tests carry `GivenTheConstructorColorsOf100` in their names. What they no
longer say is which element a constructor argument colors; what they still say is everything else the digests
cover. `ConstructorColorMigrationTests` checks that this subject does what the frozen source does with the same
arguments. A later correction that changes text or calls cannot be handled this way and will have to remove
the library-side comparison.

### The Campertron parity fixtures

`Parity/Campertron/` shows that the public modern API reproduces Campertron's live calendar renderer. Everything
about the fixtures (where they come from, their format, how the package side is built, what must match and what
differs on purpose) is in `Parity/Campertron/README.md`. The rules:

- The fixtures are data recorded from Campertron at one commit, named in `Fixtures/manifest.json`. The tests
  never run Campertron, never reference it and never use the network; the fixture files are embedded in the
  test assembly.
- Do not edit a fixture to make a test pass. A failing parity test means the package no longer reproduces the
  recorded behavior.
- Record them again only when Campertron's renderer has changed and the new behavior is to become the target,
  from a copy of Campertron outside this repository. Campertron is never modified from here.
- `ParityProfile` and `SegmentAdapter` are test code that stands in for a consumer. They use public API only,
  which `ApiSufficiencyTests` pins, and they must not move into the package.
- The parity tests were checked like the others: five deliberate breaks of the package, three tampered fixtures
  and one wrong adapter in a scratch copy, each of which failed them.

### The documentation tests

`Documentation/` keeps the public documents, the example application and the XML documentation true. These
tests read the repository's own files, found by walking up from the test assembly to the solution file, so they
only run from a build inside a checkout. What they are built on:

- **`Examples.cs` and `expected-output.txt` are the sources; the README copies from them**
  ([D-49](decisions.md#d-49-the-readmes-code-and-output-are-the-example-programs)). A failing
  `ReadmeExamplesTests` test prints the block that has no counterpart. Fix the copy, not the test.
- **`Examples.cs` is compiled twice:** into the example program, and into the test assembly through a linked
  file, which compiles every README example against the library build under test (`net10.0`, or
  `netstandard2.1` in the second run). The test project also has a build-order-only reference to the example
  project, so the program exists before a test starts it.
- **The example is run as a real process** from `ConsoleCalendarGeneratorExample/bin/<configuration>/net10.0/`
  with its standard input closed, and its output is compared with `expected-output.txt` after line ends are
  made the same.
- **The complete output is compared on every system.** Every example but one supplies its weekday labels, so
  `expected-output.txt` is the same on Linux, macOS and Windows in every line but one
  ([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).
  That line is the weekday row of the example that takes its labels from the culture: the file has what
  Linux and macOS give (`lu ma me je ve sa di`), Windows gives ` L  M  M  J  V  S  D`, and the comparison puts
  in whatever the machine's culture data gives (`ExampleProgram.ExpectedOutputOnThisMachine`). A separate test
  builds that row from the culture's day names directly. Never write a culture-derived row into a test as the
  one right answer.
- **What is still assumed:** the English and the French name of March, which are in the titles and are the same
  on all three systems. On a machine with other names those comparisons are inconclusive, not failed. The
  test that the starting culture of the process changes nothing compares two runs on the same machine and
  assumes nothing.
- **Two structural tests keep the output deterministic:** exactly two examples start from
  `MonthRenderOptions.Default` (the quick start, which supplies labels, and the culture-derived one, which
  must not), and every weekday row of the expected output except the culture-derived one is made of labels
  that an example supplies.
- **Links are checked against the checkout, never the network.** A link to a file of this repository on GitHub
  or on `raw.githubusercontent.com` must name a file that exists; a `#` link must name a heading; other
  addresses must be `https`. That a file exists here says nothing about `master` until the change is merged,
  which matters for a new image: look at the README on GitHub after merging.
- **Facts are tested, wording is not.** The tests check which documents and images exist, which target
  frameworks, versions and commands the documents name, and a few phrases a release would have to change
  (`not published yet`, the changelog's section headings). Prose can be edited freely otherwise.
- **`XmlDocumentationTests` computes the documentation ID of every public type and member by reflection** and
  looks each one up in the XML file built beside the library. It handles the shapes the API has (generic and
  nullable parameter types, arrays); a new shape, such as a generic method or a nested type, shows up as a
  member "not in the documentation file" and means the test's ID code needs extending.

Each kind of drift was introduced once in a scratch copy and seen to fail: a changed character in a README
example, an output line the program does not print, a relative link and a link to a missing file in the
README, an image nothing shows, a missing legacy comment, a restored warning suppression, an example that
reads a key, and a parameterized legacy constructor in the example.

### Other compatibility layers

- **Package validation** compares the packed assemblies with published 1.0.0 on every `dotnet pack`. It checks
  API shape, not behavior. See [packaging-and-releases.md](packaging-and-releases.md#package-validation).
- **Consumer smoke tests** install the packed package into separate applications. See
  [packaging-and-releases.md](packaging-and-releases.md#consumer-smoke-tests).

Neither replaces the tests, and the tests do not replace them.

### Historical verification (not a CI stage)

When the suite was written, seven deliberate changes were applied to a scratch copy of the library, one at a
time: fixing the four-argument constructor, replacing the literal `\n`, throwing `ArgumentNullException`, taking
the month name from the invariant culture, ignoring the year when matching selected dates, dropping the extra
blank line, and changing the title indent. Each failed the suite. Deliberate API breaks were checked the same
way against package validation. This was a one-time check of the tests themselves. It is not automated; repeat
it by hand when a change raises doubt about whether the tests would notice something.

The layout tests were checked the same way when they were written. Twenty-seven deliberate changes were applied
to a scratch copy of the layout model, one at a time, and each failed the suite: reversed column arithmetic; a
first day of the week and a month length taken from the culture; one week too many and one too few; the Julian
leap-year rule; a dropped last day; columns that ignore the first day of the week; validation checks removed,
reordered, off by one, given the wrong parameter name, the wrong exception type or no value; each of the three
collections handed out as its array; an array shared between layouts; dates with a time of day or a kind; a
write to the console; a wrong weekday on empty cells; an inverted `IsEmpty`; and a settable property.

The rendering tests were checked the same way: 53 deliberate changes to a scratch copy of the renderer, the
options and the result types, each of which failed the suite. Among them: centering that rounds the other way,
ignores the suffix or counts UTF-16 code units; rendered trailing cells; a gap before the first column;
left-aligned cells; labels that are not cut, or cut by code unit; a title or labels taken from the current
culture or from the culture's own calendar; a title formatted from a plain date; supplied labels indexed by
column; segments missing their weekday or date; each validation check removed or off by one; a culture or labels
that are not copied; collections handed out as arrays or lists; a different default first day or alignment; a
final line terminator; any line terminator accepted; and a write to the console. One change cannot be seen on
Linux and is left to the Windows job: a default text that uses the platform's line terminator.

When the legacy class was rebuilt as a facade, the original and the new implementation were also compared side
by side, outside the suite. One scratch program was built against each and run over 12,986 scenarios: every
year from 1 through 9999; every culture of the runtime (907) as the current culture; 1,500 pseudo-random
selections with duplicates, times of day, kinds and dates elsewhere; every constructor and setter with every
color; undefined colors in every place; and every combination of invalid month, year and list through every
method. It recorded every call, the color of every call, the colors left behind, and each exception with its
type, parameter name and message. The two records were identical. The same program drew to a pseudo-terminal,
to a pipe, to a pipe with the runtime's color opt-in, to a dumb terminal and with `NO_COLOR`; the bytes that
arrived were identical each time, escape sequences included. The scratch program is not in the repository.

For the facade, 29 deliberate changes were then applied to a scratch copy, four of them to `MonthLayout`. The
four layout changes failed the legacy tests as well as the layout tests, which is the evidence that the legacy
class really takes its grid from the layout. Of the changes to the facade itself, one cannot be observed with
any real culture (reading the month name before setting the title color instead of after). Two were not noticed
at first and led to new tests: using the modern validation instead of the runtime's, which keeps the exception
type but changes its parameter name and message, and a lock around a month. Every other change failed the suite,
several of them only because of the new facade tests, among them a two-digit Monday written as a string.

The state, theme and writer tests were checked the same way: 52 deliberate changes to a scratch copy, each of
which failed the suite. Among them: the last entry winning instead of the first; a date matched without its year
or its month; a time of day that shifts the date; the marker after the number; the fit not checked, checked for
a one-digit day only, counted in UTF-16 code units, or checked only for dates in the month; an entry without a
state skipped silently; names compared without case; a state missing from its segment; invisible markers and
names accepted; state colors found without case, ignored, or not falling back to the day color; the suffix
taking the title's color; gaps taking the day's; undefined colors accepted; no terminator after the last line; a
fixed line feed instead of the writer's terminator; the writer flushed or disposed; colors not put back after a
failure; a line end written in a theme color; the background lost or set; an unset foreground not restored; no
lock around a month; and the theme ignored.

The frozen source of 1.0.0 and the migration tests were checked the same way when they were added, in a scratch
copy of which nothing was committed:

- **Each of the five corrections was made in the library, one at a time,** as a plausible implementation of its
  decision. Each failed `LegacyMigrationTests`: the constructor colors in 13 of its tests, the null list in 13,
  the line terminator in 74, the single blank line in 26 and the Gregorian title in 6. That is the evidence
  that none of the five can reach `master` unnoticed.
- **One correction was then marked as implemented:** with the null-list correction in the library and its
  `Corrected` function set, every migration test and every test of `Historical/` passed again, and in the whole
  suite exactly four tests still failed, the three in `Legacy/ValidationTests` that pin
  `NullReferenceException` and the call digest of `PreFacadeBaselineTests`. Those are the tests that pull
  request has to change.
- **The frozen source was changed in three ways.** One added space failed the byte-for-byte test. A corrected
  four-argument constructor failed that test, three of the four digests, two tests of `Published100BehaviorTests`
  and the migration tests. The source of commit `64995b6` in its place failed the byte-for-byte test and the
  list of private methods, and passed everything else, the four digests included, which is how it is known
  that both versions of the original source behave alike.
- **Package validation:** see [packaging-and-releases.md](packaging-and-releases.md#package-validation).

The tests of the constructor correction were checked the same way, with seven deliberately wrong versions in a
scratch copy, each of which failed the suite: the one-argument constructor left as in 1.0.0 (17 tests failed);
the four-argument constructor ignoring `DayOfWeekColor` (22); the weekday row yellow instead of cyan with
three arguments (9); a constructor that rejects a value that is not a color (7); `DateColor` given to the
selected dates (12); the correction described wrongly in `ConstructorColorRoles.In20` (11); and the 1.0.0
constructors described wrongly in `ConstructorColorRoles.In100` (5, the three narrowed digests among them).

## Rules for adding tests

- **Never weaken a legacy, facade, historical or migration test** to make a change pass. A failing
  characterization test means the behavior changed. The one exception is the pull request for an approved
  correction, which changes the tests that pin the behavior it corrects, names them, and changes no others; see
  [compatibility.md](compatibility.md#how-a-correction-is-made).
- **Never edit the frozen source of 1.0.0,** and never change a test in `Historical/` to match the library.
- **New behavior gets new tests** in a new folder beside `Legacy/`. Leave `Legacy/` for the legacy class.
- **Write expected output explicitly.** Use `Golden.Lines` or concatenated string literals with `\n`. Do not use
  verbatim or raw multi-line string literals: their line endings follow the source file, which is CRLF in a
  Windows checkout.
- **Use ordinal comparisons** for text (`Is.EqualTo`, `Ordinal.Contains`, or `.Using(StringComparison.Ordinal)`
  on `Does.StartWith` and `Does.EndWith`).
- **Set the culture explicitly.** `ConsoleCapture` uses the invariant culture unless told otherwise. No test may
  depend on the machine's culture.
- **Keep exact strings away from operating-system culture data.** Month and day names from ICU differ between
  versions and platforms. For exact text, build a culture in the test (`TestCultures.WithMonthNames`). For real
  cultures, compare with what the culture reports at run time, and use `Assume.That` for anything that might not
  be available or might differ.
- **Restore everything a test changes.** Prefer the existing helpers, which already do.
- **Test what a document states, not how it says it.** A documentation test checks a fact against the
  repository (a file exists, an example compiles, a version is published). Do not add tests that pin prose.
- **No test may need the network** or download a package.
- **Check that a new test can fail.** Break the code it covers once, in a scratch copy, and see it fail.

## Planned tests

With each of the four corrections still to be made, in its pull request: tests that state the corrected
behavior outright, and the function of that correction in `Migration/ApprovedCorrection.cs`. Nothing else is
planned.
