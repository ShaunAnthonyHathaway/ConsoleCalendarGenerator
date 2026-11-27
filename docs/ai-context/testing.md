# Testing

## Current tests

`ConsoleCalendarGenerator.Tests` holds **1,701 tests**: 153 characterization tests of the legacy API, 25 tests of
the legacy class as a facade, 184 tests of the layout model, 275 tests of text rendering, 98 tests of day states
and markers, 52 tests of the theme, 58 tests of the writers, 756 Campertron parity tests (most of them one test
per recorded case), 97 tests of the documentation and the example, 2 tests of the modern public surface and 1
test about the build setup. It targets `net11.0` and uses NUnit 5.0.0 with NUnit3TestAdapter 6.3.1 and
Microsoft.NET.Test.Sdk 18.12.0.

### Purpose

The characterization tests record what `CalendarGenerator.ConsoleCalendar` does in published 1.0.0, before any
of it is refactored. They describe the behavior that exists, **including behavior that is wrong**. They are the
definition of "unchanged" for the whole 1.x line. See [compatibility.md](compatibility.md).

A test whose name contains `Characterized1xBehavior` pins down something that is not the intended design. Do not
correct it.

The facade tests in `Facade/` are characterization tests as well. They were added when the class was rebuilt on
`MonthLayout`, they describe the same published behavior more closely, and every one of them also passes against
the original 1.0.0 implementation.

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
| `Facade/PreFacadeBaselineTests` | 2 | Digests of every call on the console's writer, and of the color of every call, recorded from the original implementation |
| `Facade/FacadeLayoutTests` | 6 | That the legacy class draws every day where `MonthLayout` puts it |
| `Facade/FacadeCompatibilityTests` | 17 | The places where the legacy class must stay different from the modern API: the runtime's own exception, colors reset and failing late, the bare line feed, no marker, the culture's own calendar, and no lock |
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
| `Documentation/PublicDocumentationTests` | 33 | Which documents exist; the frameworks, versions and commands they name; nothing machine-specific and nothing about a consumer |
| `Documentation/XmlDocumentationTests` | 10 | Every public type, member and parameter of the built documentation file has text; the legacy class's comments say what the constructors really color; no documentation warning is suppressed |
| `ModernApiTests` | 2 | The modern public surface by reflection, enum values included, and that the assembly exports nothing else |
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

### What a run executes

| System | Normal run (`TestCategory!=Terminal`) | Not run there | Terminal run |
|---|---|---|---|
| Linux | 1,689 pass | 3 Windows-only | 9 pass |
| macOS | 1,689 pass | 3 Windows-only | not part of CI |
| Windows | 1,640 pass | 52 that need Linux or macOS | not part of CI |

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

- **Linux and macOS only (52):** 49 tests that read console colors back (25 legacy; 2 for the facade; 1 each
  that check the layout, the renderer and the plain writer leave the colors alone; 19 for the console writer's
  colors and their restoration), and 3 that assert line-feed-only output (2 legacy, 1 for the console writer).
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
be recorded again from the current source.

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

## Rules for adding tests

- **Never weaken a legacy or facade test** to make a change pass. A failing characterization test means the behavior
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
- **Test what a document states, not how it says it.** A documentation test checks a fact against the
  repository (a file exists, an example compiles, a version is published). Do not add tests that pin prose.
- **No test may need the network** or download a package.
- **Check that a new test can fail.** Break the code it covers once, in a scratch copy, and see it fail.

## Planned tests

None at present.
