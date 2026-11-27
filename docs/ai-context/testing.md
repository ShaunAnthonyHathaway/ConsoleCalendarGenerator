# Testing

## Current tests

`ConsoleCalendarGenerator.Tests` holds **823 tests**: 153 characterization tests of the legacy API, 184 tests of
the layout model, 275 tests of text rendering, 98 tests of day states and markers, 52 tests of the theme, 58
tests of the writers, 2 tests of the modern public surface and 1 test about the build setup. It targets
`net11.0` and uses NUnit 5.0.0 with NUnit3TestAdapter 6.3.1 and Microsoft.NET.Test.Sdk 18.12.0.

### Purpose

The characterization tests record what `CalendarGenerator.ConsoleCalendar` does in published 1.0.0, before any
of it is refactored. They describe the behavior that exists, **including behavior that is wrong**. They are the
definition of "unchanged" for the whole 1.x line. See [compatibility.md](compatibility.md).

A test whose name contains `Characterized1xBehavior` pins down something that is not the intended design. Do not
correct it.

The tests of the modern API (layout, rendering, states, themes, output) are ordinary specification tests: they
say what the modern API must do, and a failure there means the code is wrong.

### Organization

| Fixture | Tests | Covers |
|---|---|---|
| `Legacy/PublicApiTests` | 5 | The public surface of the legacy namespace by reflection, and source compatibility with named arguments |
| `Legacy/ConstructorColorTests` | 7 | Which color each constructor argument controls |
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
| `ModernApiTests` | 2 | The modern public surface by reflection, enum values included, and that the assembly exports nothing else |
| `LibraryBuildTests` | 1 | That the run uses the library build it was built for |

`Support/` holds the helpers. The ones to know:

- `ConsoleCapture` and `RecordingWriter`: run code with `Console.Out` replaced, record every write separately
  with the console colors in effect, and restore all global state afterwards.
- `LegacyReference`: the month layout written out from the rules, independently of the library, so the library
  can be compared with it for every month.
- `BehaviorDigest`: condenses a large amount of output into a hash, for the published-baseline tests.
- `ChildProcess`, `ChildScenario` and `Program.cs`: start the test assembly as a separate process that draws one
  calendar to its real console streams.
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

### What a run executes

| System | Normal run (`TestCategory!=Terminal`) | Not run there | Terminal run |
|---|---|---|---|
| Linux | 814 pass | 3 Windows-only | 6 pass |
| macOS | 814 pass | 3 Windows-only | not part of CI |
| Windows | 767 pass | 50 that need Linux or macOS | not part of CI |

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

- **Linux and macOS only (50):** 47 tests that read console colors back (25 legacy; 1 each that check the
  layout, the renderer and the plain writer leave the colors alone; 19 for the console writer's colors and
  their restoration), and 3 that assert line-feed-only output (2 legacy, 1 for the console writer).
  On those systems the runtime keeps the current colors in the process, so they can be read without a
  terminal. On Windows the colors live in the console, and a test host without one ignores changes. These tests
  are marked `[Platform(Exclude = "Win")]`.
- **Windows only (3):** the legacy class's real mixed line endings (2), and the console writer's uniform
  `\r\n` endings (1).
- What Windows does verify for the console writer: the text, every individual write, the line terminator, the
  redirected output of a real child process, failures passed on, and writes from several threads. What it
  cannot verify is which color a write was made in, or that colors are restored.
- Add a platform condition only where the behavior really differs by platform. Text, write-sequence, API and
  validation tests run everywhere.

### The Terminal category

Six tests, three for the legacy class and three for the modern console writer, need a terminal database entry
for `xterm-256color`, and four of those also need Linux and the util-linux `script` program to create a
pseudo-terminal. They are in the `Terminal` category, are excluded from
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

## Rules for adding tests

- **Never weaken a legacy test** to make a change pass. A failing characterization test means the behavior
  changed.
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
- **No test may need the network** or download a package.
- **Check that a new test can fail.** Break the code it covers once, in a scratch copy, and see it fail.

## Planned tests

Nothing in this section exists yet.

### Legacy facade

- When the legacy class becomes a facade, every test in `Legacy/` must pass without being edited, on both
  library builds, including the two digests recorded from the published 1.0.0 assembly. That is the acceptance
  test for that phase.
- The modern tests must keep passing unchanged too: the facade is a consumer of the modern API, not a reason to
  change it. If the facade needs something the modern API lacks, that is a design question for Shaun, not a
  quiet addition.
- `LayoutPicture` and `Rendered` are for reading results and must not turn into a second renderer.

### Campertron parity fixtures

- Purpose: show, inside this repository, that the package can reproduce the output of Campertron's live
  `Calendar.Lines` renderer.
- Fixtures are recorded from Campertron's own code at a stated commit, as text and styled spans, and checked in
  here as data. Record the Campertron commit with them.
- Recording them must not modify Campertron. Its `Calendar` and styled-line types are public, so a separate
  throwaway program can call them.
- The tests here read the fixtures. They must not need a Campertron checkout or the network.
- The older, unused value-stream calendar in Campertron is not a parity target; see
  [open-questions.md](open-questions.md#q-09-should-parity-fixtures-ever-cover-campertrons-unused-value-stream).
