# Architecture

Two parts: what exists today, and the direction that has been approved but, apart from its first layer, not
built. Do not treat anything in the second part as existing code unless it says so.

## Current

### Repository layout

| Path | What it is |
|---|---|
| `ConsoleCalendarGenerator/` | The library and the NuGet package: the legacy class in `ConsoleCalendar.cs`, and the modern API in `Layout/`, `Rendering/`, `States/`, `Themes/` and `Output/`. |
| `ConsoleCalendarGeneratorExample/` | The example application: a tour of the API that runs unattended. Its `Examples.cs` holds the code the README shows, and `expected-output.txt` what it prints. |
| `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `SECURITY.md` | The public documents. The README is also the readme of the package. |
| `docs/images/` | The images the README shows. One at present. |
| `docs/ai-context/` | These maintenance notes. Not user documentation. |
| `ConsoleCalendarGenerator.Tests/` | NUnit tests. See [testing.md](testing.md). Its `Historical/Published100/ConsoleCalendar.cs` is the frozen source of 1.0.0, which is never edited. |
| `eng/` | `verify-package.sh`, `consumer-smoke.sh` and the two small projects the smoke script builds. |
| `.github/workflows/ci.yml` | The only workflow. |
| `Directory.Build.props`, `.editorconfig`, `global.json` | Shared build settings, formatting rules, SDK pin. See [development.md](development.md). |
| `docs/ai-context/`, `CLAUDE.md` | This documentation. |

### The library: a legacy class and a modern API

The assembly has two public namespaces. `CalendarGenerator` holds the legacy class and nothing else.
`ConsoleCalendarGenerator`, which matches the assembly and the package, holds the modern API: the layout model,
text rendering, day states, the theme and output. The legacy class uses one piece of the modern API, the layout
model; the modern API uses nothing of the legacy class.

### The legacy class

`CalendarGenerator.ConsoleCalendar` is the whole of the published 1.0.0 API. The exact public surface, the
behavior that must be preserved and the five behaviors that 2.0 corrects are in
[compatibility.md](compatibility.md). Two of the five are implemented, the constructor colors and the exception
for a null list; apart from those, what follows describes 1.0.0 and `master` alike.

It is a compatibility facade. Its public members are the published ones; what is behind them was rebuilt so
that the library has a single place that decides where a day goes.

- **State.** Four `ConsoleColor` values: title, weekday row, dates, selected dates. Five constructors set them;
  four getter/setter pairs read and change them. Instances are mutable and not thread-safe.
- **`GenerateMonth(Month, Year[, dates])`.** Refuses a null list, reads `dates` to find the selected days, lets
  the runtime check the month and the year (`DateTime.DaysInMonth`), creates `MonthLayout.Create(Year, Month, DayOfWeek.Monday)`, and
  then writes directly to the console: the title line, the fixed weekday row, and one call per cell of the
  layout, walking its weeks and cells in order.
- **`GenerateYear(Year[, dates])`.** Calls `GenerateMonth` for months 1 through 12.
- **From the modern API:** `MonthLayout` only, for the column and the row of every day. The class no longer has
  a grid algorithm of its own: the loop that cycled through two-letter weekday names (`GetNextShortDay`) and
  its `Started` flag are gone.
- **Kept private in the class, deliberately:** the title (month name from the current culture's own calendar,
  three-step indent, the year appended), the weekday row, which console method is called for each cell and
  with which text, the color set before every call, the three `Console.ResetColor()` calls, the bare `\n`
  after the last day, and the order in which invalid arguments fail. The modern renderer, theme and writers
  are not used, because each of them deliberately behaves differently; see
  [compatibility.md](compatibility.md#how-the-legacy-class-is-built).

What the legacy API cannot do, and the modern API exists for:

- Nothing is returned. The only output is what reaches the live console.
- Text goes through `Console.Out`, but colors are set on the console itself, so replacing `Console.Out` captures
  the text only.
- Layout, text, color and output are one loop and cannot be used separately.
- A selected day differs from other days by color only.

### The layout model

Three types in `ConsoleCalendarGenerator/Layout/`, namespace `ConsoleCalendarGenerator`. They are the first layer
of the approved direction below.

```text
public sealed class MonthLayout
    static MonthLayout Create(int year, int month, DayOfWeek firstDayOfWeek)
    int Year                               1 through 9999
    int Month                              1 through 12
    DayOfWeek FirstDayOfWeek
    IReadOnlyList<DayOfWeek> DaysOfWeek    the weekday of each column; always 7, starting with FirstDayOfWeek
    IReadOnlyList<MonthLayoutWeek> Weeks   4, 5 or 6, the week that contains the 1st first

public sealed class MonthLayoutWeek
    IReadOnlyList<MonthLayoutCell> Cells   always 7, in column order

public sealed class MonthLayoutCell
    DayOfWeek DayOfWeek                    the weekday of the cell's column, also when the cell is empty
    DateTime? Date                         a day of the month, or null for an empty cell
    bool IsEmpty
```

What it answers: which dates belong to the month, which week and weekday column each is in, how many weeks are
needed, and which cells are empty. What it deliberately knows nothing about: text, cell width, gaps, weekday or
month names, culture, titles, indentation, alignment, colors, markers, day states, the console and line endings.
Those belong to the layers above it.

What every layout guarantees (the tests check the first five for every one of the 839,916 layouts there are,
and the last two on samples):

- Every week has exactly seven cells, and cell *i* of every week has the weekday `DaysOfWeek[i]`.
- Every day of the month appears exactly once, in increasing order. No other date appears.
- Empty cells occur only before the 1st and after the last day. The cells of a neighbouring month are empty, not
  filled with its dates.
- There are no more weeks than the month needs: the first and the last week each hold at least one date.
- A date is midnight with `DateTimeKind.Unspecified`, in the Gregorian calendar.
- The result depends only on the three arguments. It does not read the culture, the console, the environment or
  the clock, and it changes nothing outside itself.
- The objects are immutable. The collections are read-only wrappers, not arrays, and reject every change.

Invalid arguments throw `ArgumentOutOfRangeException` naming the parameter, checked in the order year, month,
first day of the week, before anything is built. Equality is reference equality; nothing overrides `Equals`. The
reasons for these choices are in [decisions.md](decisions.md#layout-model).

The legacy class is a consumer of the layout model: it asks for the layout with weeks from Monday and draws
each cell its own way.

### Text rendering

Nine types in `ConsoleCalendarGenerator/Rendering/`, namespace `ConsoleCalendarGenerator`. They are the second and
third layers of the approved direction: deterministic text, and segments that carry meaning instead of color.
Rendering performs no input or output and does not use the console.

```text
public static class MonthRenderer
    static RenderedMonth Render(int year, int month, MonthRenderOptions options)
    static RenderedMonth Render(int year, int month, MonthRenderOptions options,
                                IEnumerable<KeyValuePair<DateTime, DayState>> dayStates)
    static RenderedMonth Render(int year, int month, MonthRenderOptions options,
                                IEnumerable<DateTime> dates, DayState state)

public sealed class MonthRenderOptions            immutable; start from Default, change with the With methods
    static MonthRenderOptions Default
    CultureInfo Culture                           WithCulture(CultureInfo)
    DayOfWeek FirstDayOfWeek                      WithFirstDayOfWeek(DayOfWeek)
    WeekdayLabelStyle WeekdayLabelStyle           WithWeekdayLabelStyle(WeekdayLabelStyle)
    IReadOnlyList<string>? WeekdayLabels          WithWeekdayLabels(IEnumerable<string>)
    int CellWidth                                 WithCellWidth(int)
    int ColumnGap                                 WithColumnGap(int)
    int GridWidth                                 computed: 7 cells and 6 gaps
    string Indent                                 WithIndent(string)
    string TitleFormat                            WithTitleFormat(string)
    string TitleSuffix                            WithTitleSuffix(string)
    TitleAlignment TitleAlignment                 WithTitleAlignment(TitleAlignment)

public sealed class RenderedMonth
    MonthLayout Layout
    IReadOnlyList<RenderedLine> Lines             title, weekday labels, then one line per week
    string ToText()                               lines joined by "\n"
    string ToText(string newLine)                 "\n" or "\r\n"
    string ToString()                             the same as ToText()

public sealed class RenderedLine
    RenderedLineKind Kind
    IReadOnlyList<RenderedSegment> Segments
    string Text                                   the segments joined; no terminator
    string ToString()                             Text

public sealed class RenderedSegment
    RenderedSegmentKind Kind
    string Text                                   never empty, never a line break
    DayOfWeek? DayOfWeek                          set for the three kinds of cell
    DateTime? Date                                set for Day
    DayState? State                               set for a Day that has a state
    string ToString()                             Text

public enum RenderedLineKind      Title = 0, WeekdayLabels = 1, Week = 2
public enum RenderedSegmentKind   Indent = 0, Padding = 1, Title = 2, TitleSuffix = 3, WeekdayLabel = 4, Gap = 5, EmptyDay = 6, Day = 7
public enum WeekdayLabelStyle     Shortest = 0, Abbreviated = 1
public enum TitleAlignment        Left = 0, Center = 1
```

**Defaults** (`MonthRenderOptions.Default`), and what each `With` method accepts:

| Setting | Default | Accepted | Otherwise |
|---|---|---|---|
| `Culture` | invariant culture | any culture; a read-only copy is taken | null: `ArgumentNullException` |
| `FirstDayOfWeek` | Monday | the seven `DayOfWeek` values | `ArgumentOutOfRangeException` |
| `WeekdayLabelStyle` | `Shortest` | `Shortest`, `Abbreviated`; clears supplied labels | `ArgumentOutOfRangeException` |
| `WeekdayLabels` | null (labels come from the culture) | exactly seven, Sunday first, each non-empty with no line break; copied | null: `ArgumentNullException`; else `ArgumentException` |
| `CellWidth` | 2 | 2 through 100 | `ArgumentOutOfRangeException` |
| `ColumnGap` | 1 | 0 through 100 | `ArgumentOutOfRangeException` |
| `Indent` | empty | any text with no line break | null: `ArgumentNullException`; else `ArgumentException` |
| `TitleFormat` | `"Y"` | a valid .NET date format string, not empty, no line break | null: `ArgumentNullException`; empty or line break: `ArgumentException`; invalid: `FormatException` |
| `TitleSuffix` | empty | any text with no line break | null: `ArgumentNullException`; else `ArgumentException` |
| `TitleAlignment` | `Center` | `Left`, `Center` | `ArgumentOutOfRangeException` |

`Render` checks `year`, then `month` (`ArgumentOutOfRangeException`, as in the layout), then `options`
(`ArgumentNullException`).

**Culture.** The culture supplies language only: month names, day names and the year-and-month pattern. The
grid is always Gregorian and the title always names the Gregorian month and year shown, so for a culture whose
own calendar is another one (Persian, Thai Buddhist) the culture's Gregorian names and patterns are used:
`th-TH` says 2023, not 2566. The culture never decides the first day of the week, and the days are always
written with the digits 0 to 9. The options keep a read-only copy of the culture, so later changes to the
caller's object do nothing. `CurrentCulture` and `CurrentUICulture` are never read.

**Title.** `TitleFormat` is applied to the first day of the month, as a value with a zero offset from UTC so
that no format can bring in the machine's time zone. The default `"Y"` gives `2023 May` under the invariant
culture and `May 2023` under `en-US`. Text in quotation marks is copied unchanged, so a caller can produce any
exact title. `TitleSuffix` follows directly, as its own segment. Centered, the title and suffix together get
half of the spare width before them, rounded down; a title as wide as the grid or wider starts at the left edge
and is never cut. A format that produces no text, with no suffix, leaves an empty title line with no segments.

**Weekday labels.** From the culture (`Shortest` or `Abbreviated` day names) or supplied by the caller. Supplied
labels are indexed by `DayOfWeek`, Sunday first, and are shown starting from the first day of the week. The
culture's names are whatever .NET reports on the machine: for `en-US` the shortest names are two letters on
Linux and macOS and one letter on Windows
([D-53](decisions.md#d-53-names-a-culture-supplies-are-documented-as-depending-on-the-system)).

**Cells and spacing.** Every label and day cell is exactly `CellWidth` wide. Text shorter than the cell is
right-aligned; a label longer than the cell is cut to the cell width. The shape of the grid therefore depends on
the options alone, never on the culture's names: `GridWidth` is seven cells and six gaps. Widths are counted in
text elements (`StringInfo`), so a surrogate pair or a letter with combining marks counts as one and is never
split; no claim is made about how many columns a terminal uses for a text element. The cells after the last day
of the month are not rendered, so no label or week line ends with padding. The indent starts every line that
has content and is not part of the grid width.

**Lines and segments.** A segment is the unit a later layer styles. For every line, the segment texts joined in
order are exactly the line's text; no segment is empty; none contains a line break. The cells of a week are one
segment each, padding included: `Day` (with its date and weekday) or `EmptyDay` (weekday only), with `Gap`
segments between. A day's date is in `Date`; nothing has to be parsed from text. Lines have no terminators.
`ToText()` joins them with `"\n"` on every platform, with nothing after the last line.

Reasons: [decisions.md](decisions.md#rendering).

### Day states and markers

One type in `ConsoleCalendarGenerator/States/`, and two overloads of `MonthRenderer.Render`. A state says what a
day is; its marker makes that visible in plain text, so state never depends on color alone.

```text
public sealed class DayState
    DayState(string name, string marker)
    string Name          identifies the state; compared character by character
    string Marker        placed directly before the day number
    string ToString()    Name
```

**The state.** The name and the marker must each contain a character that is not white space and no line
break; null throws `ArgumentNullException`, anything else invalid `ArgumentException`, the name first. Nothing
else is required of a name. States are immutable. Equality is reference equality; two states are the same kind
of state when their names are equal. A state has no color, no label and no other data.

**Giving days a state.** `Render(year, month, options, dayStates)` takes dates with their states, for example a
`Dictionary<DateTime, DayState>`. `Render(year, month, options, dates, state)` gives one state to many dates.
The overload without states is unchanged and still renders an unmarked month.

- A date selects the day with the same year, month and day. Its time of day and its `Kind` are ignored.
- Dates outside the rendered month are ignored.
- **A day has at most one state. When several entries select the same day, the first one decides** and the
  others are ignored.
- The collection is read once, from beginning to end, and is not changed.
- Every entry is checked, also those for dates outside the month, so the same input is accepted or refused
  whichever month is rendered: an entry with a null state, a marker that does not fit, or two states with the
  same name and different markers throw `ArgumentException`. A null collection or a null state argument throws
  `ArgumentNullException`. Arguments are checked in declaration order, after `year`, `month` and `options`.

**The marker in the cell.** A day with a state is still one `Day` segment, exactly `CellWidth` wide. Its text
is the marker directly before the day number, right-aligned together (` *5`, `*15` in a cell three wide); its
`State` is the state, the same instance that was passed. A marker must fit beside a two-digit day: its width in
text elements plus two must not exceed `CellWidth`, or nothing is rendered. A marker is never cut, dropped or
given extra room, so the default cell, two wide, holds none, and a caller who wants markers sets a wider cell.
Unmarked cells, the title, the labels, the number of segments and the width of every line are the same as
without states.

There is no legend. A caller explains markers in `TitleSuffix` or around the calendar.

### The theme

One type in `ConsoleCalendarGenerator/Themes/`. A theme maps what a segment is to an optional console color. It
does not change text, writes nothing and knows nothing about the console.

```text
public sealed class CalendarTheme        immutable; start from None, change with the With methods
    static CalendarTheme None            no colors at all
    ConsoleColor? TitleColor             WithTitleColor(ConsoleColor?)
    ConsoleColor? TitleSuffixColor       WithTitleSuffixColor(ConsoleColor?)
    ConsoleColor? WeekdayLabelColor      WithWeekdayLabelColor(ConsoleColor?)
    ConsoleColor? DayColor               WithDayColor(ConsoleColor?)
    IReadOnlyDictionary<string, ConsoleColor> StateColors      WithStateColor(string stateName, ConsoleColor?)
    ConsoleColor? GetColor(RenderedSegment segment)
```

- A null color means the theme has none: the text is written in whatever color the destination already has.
- Colors are foreground colors only. Indents, padding, gaps and empty cells never have one.
- A day with a state gets that state's color, looked up by the state's name, character by character and
  case-sensitively. A state with no color in the theme falls back to `DayColor`. A color for a state that a
  month does not use makes no difference.
- `WithStateColor` sets the color for a name, replaces one set earlier, or removes it when the color is null.
- A value that is not a defined `ConsoleColor` throws `ArgumentOutOfRangeException` when the theme is built, not
  later when it is used. A state name is held to the same rules as in `DayState`.
- `GetColor` is the whole of what a theme does, and is what a consumer calls to turn segments into its own
  styled output.

### Output

Two static classes in `ConsoleCalendarGenerator/Output/`.

```text
public static class MonthWriter
    static void Write(RenderedMonth month, TextWriter writer)

public static class ConsoleMonthWriter
    static void Write(RenderedMonth month, CalendarTheme theme)
```

**`MonthWriter`** writes to any `TextWriter` and never uses the console or a color. It writes each line's text
followed by the writer's own `NewLine`, the last line included (unlike `ToText`, which puts a terminator only
between lines). The writer is not flushed, closed, disposed or changed. If the writer throws, the exception is
passed on and what was written stays written.

**`ConsoleMonthWriter`** is the only modern type that uses the console. Text goes to `Console.Out`; colors are
set through `Console.ForegroundColor`; the library writes no escape sequences.

- Every segment the theme has a color for is written in it. Every other segment, and every line end, is
  written in the foreground the console had when the call began. The background is never changed.
- With `CalendarTheme.None` no color is read or changed. That is the way to write to the console without
  colors; there is no color mode and no capability detection.
- When the call returns or fails, the foreground and background are what they were when it began. A foreground
  that had been set is set again. One that had never been set, as is usual on Linux and macOS, has no value to
  set, so the colors are reset and a background the caller had set is set again.
- Whether a color reaches the screen is the runtime's and the terminal's business; the package interprets
  neither `NO_COLOR` nor `TERM` nor redirection. On Linux the runtime was observed not to emit color sequences
  when output is redirected and to honor `NO_COLOR`. macOS uses the same runtime code, but the terminal tests
  run on Linux only. On Windows colors are console attributes and were never part of the output stream.
- Calls from several threads do not mix: a private lock makes each month arrive whole. It cannot coordinate
  with other code writing to the console at the same time.

Reasons: [decisions.md](decisions.md#states-themes-and-output).

### Public documents and the example

- **`README.md`** is written for someone who has never seen the package, is the readme inside the NuGet
  package, and leads with the modern API. Its sections: status, features, installation, quick start, culture
  and first day of week, marked dates, colors, output, what each piece of text is, layout only, more options,
  legacy API, supported frameworks, limitations, building and testing, project, license.
- **`CHANGELOG.md`** has an "Unreleased" section for everything on `master` and one entry for 1.0.0.
  **`CONTRIBUTING.md`** has the commands and what a change must keep. **`SECURITY.md`** says which version gets
  fixes and how to report a problem.
- **The example application** has two files. `Examples.cs` has one method per capability, each containing one
  marked example. `Program.cs` calls them in order under headings. It needs no input, uses fixed dates and
  explicit cultures, and prints plain text when redirected. The quick start supplies its weekday labels and
  the later examples build on its options, so the program prints the same text on Linux, Windows and macOS,
  except for the one weekday row of the example that shows the culture's own day names.
- **The link between them** is one-directional: `Examples.cs` and `expected-output.txt` are the sources, and
  the README copies from them. Tests compare the copies
  ([D-49](decisions.md#d-49-the-readmes-code-and-output-are-the-example-programs)).
- **The sample that accompanied 1.0.0** is no longer the example. A byte-for-byte copy is frozen at
  `eng/consumer-smoke/OriginalSample/Program.cs` and must keep compiling and running against every later
  package, 2.0 included.
  It waits for key presses and writes terminal reset sequences, which is why it runs only in the smoke script,
  on a pseudo-terminal, in one defined scenario
  ([packaging-and-releases.md](packaging-and-releases.md#the-original-sample)).
- **XML documentation** on every public type and member is the API reference. There is no generated site.

Nothing in the public documents names a consumer of the package, and nothing in `docs/ai-context/` is written
for users.

### Tests

One NUnit project on `net11.0`. The legacy tests replace `Console.Out` with a recording writer, read console
colors back where the platform allows, start the test assembly as a child process to observe the real console
streams, and compare against digests recorded from the published 1.0.0 assembly. The layout tests need none of
that: they compare layouts with hand-written grids and with an independent reference for every month from
January 0001 through December 9999. The rendering tests compare rendered months with hand-written text and
segment lists, and with the layout for every month from 1900 through 2100. The state, theme and writer tests
do the same for marked months and colors; the console writer is observed through a recording `Console.Out`,
through a child process with redirected output, and on a pseudo-terminal. The facade tests compare every call
the legacy class makes with a record of the original implementation, and its grid with the layout. The
migration tests run the legacy class and the frozen source of 1.0.0 through the same scenarios and compare
everything a caller can observe. The documentation tests read the repository's own files: they compare the README with the example program, run
that program, check links and images against the checkout, and check the built XML documentation file against
the public API. The project can be built against either library build. Details: [testing.md](testing.md).

### Build, package and CI

- The library builds for `net10.0` and `netstandard2.1` from the same source, with no conditional compilation.
- `dotnet pack` produces `ConsoleCalendarGenerator.<version>.nupkg` and `.snupkg` and then runs package
  validation against published 1.0.0. Details: [packaging-and-releases.md](packaging-and-releases.md).
- `ci.yml` runs on pull requests and on pushes to `master`, on Linux, Windows and macOS: restore, Release build
  with every warning as an error, tests, terminal tests (Linux), tests against the `netstandard2.1` build, pack,
  package verification, consumer smoke tests, and upload of the two packages as a seven-day CI artifact (Linux).
  Its only permission is `contents: read`. It publishes nothing.

### Consumer smoke infrastructure

`eng/consumer-smoke.sh` copies the packed package to a local feed in a temporary folder, restores into a
temporary package cache with nuget.org forbidden for this package, and builds and runs small applications from
`eng/consumer-smoke/` against it. Each application uses the layout model, the renderer, a day state, a theme,
both writers and the legacy class. It proves which library build each kind of consumer receives. Details:
[packaging-and-releases.md](packaging-and-releases.md#consumer-smoke-tests).

## Approved direction

Every layer in the table exists, for a single month: see the sections above. The legacy class has been rebuilt
as a facade ([The legacy class](#the-legacy-class)). What remains is in [roadmap.md](roadmap.md) and
[open-questions.md](open-questions.md).

New code goes in the `ConsoleCalendarGenerator` namespace, and for 2.0 there is none: the modern API is
unchanged ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).
`CalendarGenerator.ConsoleCalendar` stays as the compatibility API. It shares the layout with the modern API
and nothing else, because everything else it does is a published behavior that the modern layers deliberately
do not have.

| Layer | Responsibility | Touches the live console | Status |
|---|---|---|---|
| Calendar layout | A month as weeks and days for a given first weekday. No text. | No | Implemented: `MonthLayout` |
| Text rendering | Layout, day states and options in; lines of text out, the same every time for the same input. | No | Implemented: `MonthRenderer` |
| Styled segments | Each piece of rendered text carries what it is (title, weekday label, day, a day in some state), not a color. | No | Implemented: `RenderedSegment` |
| State markers | A day's state can be shown with a text marker, so state is never conveyed by color alone. Optional. | No | Implemented: `DayState` |
| Theme | Maps segment roles and states to console colors. | No | Implemented: `CalendarTheme` |
| Output | Writes rendered lines to a `TextWriter`, or to the console in the theme's colors. | Only `ConsoleMonthWriter` | Implemented: `MonthWriter`, `ConsoleMonthWriter` |
| Consumer mapping | Turning segments into a consumer's own types. Lives in the consumer, not in the package. | n/a | n/a |

Constraints:

- Layout and rendering must not need a live console.
- Options and results are immutable where practical.
- The renderer is pure and safe to call from several threads.
- Line endings are explicit in the rendering result, not inherited by accident from a console.
- The new API is additive: it must not change anything `CalendarGenerator.ConsoleCalendar` does. The five
  corrections of 2.0 are made inside that class and add nothing to the modern API.
- Titles in the new renderer name the month that the (Gregorian) grid shows. There is no compatibility
  profile ([D-45](decisions.md#d-45-no-public-compatibility-profile)).

**ANSI.** An ANSI escape-sequence renderer is not planned. `Console.ForegroundColor` already produces the right
thing on each platform, and segments let a consumer do its own. Revisit only if real evidence of need appears.

### What Campertron needs from the package

Campertron is Shaun's campsite-availability application, in a separate repository. It currently draws its own
console calendar. The package must offer enough to replace Campertron's **live** renderer, `Calendar.Lines` in
`CampertronLibrary/function/Base/Calendar.cs`. Campertron itself is changed elsewhere; see
[decisions.md](decisions.md#d-19-campertron-adoption-happens-elsewhere).

As observed in Campertron at commit `3ded294` (2026-11-27; the file is unchanged since the first audit at
`b23a6b9`), that renderer:

- returns lines of styled spans (text plus an optional `ConsoleColor`) and never writes to the console;
- takes the year, month, the dates to mark, an explicit `CultureInfo`, an indent string and a mark color;
- titles the month with the culture's year-month pattern, followed by the legend ` (* = open)`, at the left;
- uses a fixed English weekday row, Monday first;
- right-aligns each day in a three-character cell with one space between cells;
- marks a day by prefixing `*` inside its cell and coloring the whole cell;
- builds each week as one span for the indent and any empty cells, then one span per day and one per gap.

Its two call sites, in `ConsoleResults.cs`, pass an indent of two spaces and `ConsoleColor.Cyan`.

**The package does offer enough, and this is tested.** `ConsoleCalendarGenerator.Tests/Parity/Campertron/` holds
47 calls recorded from that commit, with the lines and spans each returned, and tests that reproduce them with
the public modern API alone:

| Campertron | Package |
|---|---|
| The culture argument | `WithCulture` |
| Monday first, the fixed weekday row | `WithFirstDayOfWeek(Monday)`, `WithWeekdayLabels(Su ... Sa)` |
| Three-character cells, one space between | `WithCellWidth(3)`, `WithColumnGap(1)` |
| The indent argument | `WithIndent` |
| Year-month pattern, legend, at the left | `WithTitleFormat("Y")`, `WithTitleSuffix(" (* = open)")`, `WithTitleAlignment(Left)` |
| `*` before the number of an open day | a `DayState` with the marker `*`, given to the dates |
| The mark color on the whole cell | `CalendarTheme.None.WithStateColor(name, color)` and `GetColor(segment)` |
| Lines of spans | a short adapter over `RenderedLine.Segments`, in the tests |

The text of every line and the text, order and color of every span are identical, for every recorded case
under the invariant culture and under cultures whose calendar is Gregorian. One difference is intended: under
`th-TH` and `fa-IR` Campertron titles the month in the culture's own calendar and the package titles the
Gregorian month it draws; everything below the title is still identical. The full list of what must match, what
is the consumer's own business and what differs on purpose is in the README beside the fixtures.

The same file also contains an older value-stream calendar (`GenerateCalendar`). Campertron still produces it,
but nothing in Campertron displays it, and there are no fixtures for it
([D-47](decisions.md#d-47-campertron-parity-is-shown-with-recorded-fixtures)). The list above is a snapshot of
one commit, not a contract: if Campertron's renderer changes, the fixtures say what it was, not what it is.
