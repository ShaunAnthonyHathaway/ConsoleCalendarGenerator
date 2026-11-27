# Architecture

Two parts: what exists today, and the direction that has been approved but, apart from its first layer, not
built. Do not treat anything in the second part as existing code unless it says so.

## Current

### Repository layout

| Path | What it is |
|---|---|
| `ConsoleCalendarGenerator/` | The library and the NuGet package: the legacy class in `ConsoleCalendar.cs` and the layout model in `Layout/`. |
| `ConsoleCalendarGenerator/docs/` | A stale duplicate of the README and the four README screenshots. Not packed. To be replaced in the user-facing material phase. |
| `ConsoleCalendarGeneratorExample/` | A small interactive console program that uses the library. |
| `ConsoleCalendarGenerator.Tests/` | NUnit tests. See [testing.md](testing.md). |
| `eng/` | `verify-package.sh`, `consumer-smoke.sh` and the two small projects the smoke script builds. |
| `.github/workflows/ci.yml` | The only workflow. |
| `Directory.Build.props`, `.editorconfig`, `global.json` | Shared build settings, formatting rules, SDK pin. See [development.md](development.md). |
| `docs/ai-context/`, `CLAUDE.md` | This documentation. |

### The library: a legacy class and a layout model

The assembly has two public namespaces. `CalendarGenerator` holds the legacy class and nothing else.
`ConsoleCalendarGenerator`, which matches the assembly and the package, holds the modern API; so far that is the
layout model. Neither uses the other.

### The legacy class

`CalendarGenerator.ConsoleCalendar` is the whole of the published 1.0.0 API. The exact public surface and the
behavior that must be preserved are in [compatibility.md](compatibility.md).

How it works:

- **State.** Four `ConsoleColor` values: title, weekday row, dates, selected dates. Five constructors set them;
  four getter/setter pairs read and change them. Instances are mutable and not thread-safe.
- **`GenerateMonth(Month, Year[, dates])`.** Collects the days of that month that appear in `dates`, then writes
  directly to the console: the title line (the month name from `CultureInfo.CurrentCulture`, then the year), the
  fixed weekday row `Mo Tu We Th Fr Sa Su`, and one `Console.Write` per day cell. Before each write it sets
  `Console.ForegroundColor`; it calls `Console.ResetColor()` after the title, after the weekday row and at the
  end.
- **`GenerateYear(Year[, dates])`.** Calls `GenerateMonth` for months 1 through 12.
- **Private helpers.** `SetColor`, `GetSpaces` (title indent), `GetNextShortDay` (weekday cycling).

Consequences of this design that the planned work exists to remove:

- Nothing is returned. The only output is what reaches the live console.
- Text goes through `Console.Out`, but colors are set on the console itself, so replacing `Console.Out` captures
  the text only.
- Layout, text, color and output are one loop and cannot be used separately.
- A selected day differs from other days by color only.

### The layout model

Three types in `ConsoleCalendarGenerator/Layout/`, namespace `ConsoleCalendarGenerator`. They are the first layer
of the approved direction below and the only part of it that exists.

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
Those belong to the layers that are still planned.

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

The legacy class always starts weeks on Monday and does not use the layout model. It will only when it becomes a
facade, after the rendering layers exist and can be shown to reproduce its output.

### The example

`ConsoleCalendarGeneratorExample/Program.cs` is the sample that accompanied 1.0.0. It waits for key presses and
writes terminal reset sequences, so it cannot run unattended. A byte-for-byte copy is frozen at
`eng/consumer-smoke/OriginalSample/Program.cs` and must keep compiling against every 1.x package; that copy stays
as it is even when the example is rewritten.

### Tests

One NUnit project on `net11.0`. The legacy tests replace `Console.Out` with a recording writer, read console
colors back where the platform allows, start the test assembly as a child process to observe the real console
streams, and compare against digests recorded from the published 1.0.0 assembly. The layout tests need none of
that: they compare layouts with hand-written grids and with an independent reference for every month from
January 0001 through December 9999. The project can be built against either library build. Details:
[testing.md](testing.md).

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
`eng/consumer-smoke/` against it. Each application uses the layout model and the legacy class. It proves which
library build each kind of consumer receives. Details:
[packaging-and-releases.md](packaging-and-releases.md#consumer-smoke-tests).

## Approved direction

The first layer, calendar layout, exists; see [The layout model](#the-layout-model). None of the other types
exist. Their names and signatures are **not decided**; see [open-questions.md](open-questions.md). What is
decided is the separation and the constraints.

New code goes in the `ConsoleCalendarGenerator` namespace. `CalendarGenerator.ConsoleCalendar` stays and becomes
a facade over the new code.

| Layer | Responsibility | Touches the live console | Status |
|---|---|---|---|
| Calendar layout | A month as weeks and days for a given first weekday. No text. | No | Implemented: `MonthLayout` |
| Text rendering | Layout, day states and options in; lines of text out, the same every time for the same input. | No | Planned |
| Styled segments | Each piece of rendered text carries what it is (title, weekday label, day, a day in some state), not a color. | No | Planned |
| State markers | A day's state can be shown with a text marker, so state is never conveyed by color alone. Optional. | No | Planned |
| Theme | Maps segment roles and states to console colors. | No | Planned |
| Output | Writes rendered lines to a `TextWriter`, applying colors through something the caller can replace. | Only this layer | Planned |
| Consumer mapping | Turning segments into a consumer's own types. Lives in the consumer, not in the package. | n/a | n/a |

Constraints:

- Layout and rendering must not need a live console.
- Options and results are immutable where practical.
- The renderer is pure and safe to call from several threads.
- Line endings are explicit in the rendering result, not inherited by accident from a console.
- The new API is additive: it must not change anything `CalendarGenerator.ConsoleCalendar` does today.
- Titles in the new renderer should name the month that the (Gregorian) grid shows. A compatibility profile is
  allowed where exact parity with existing output is needed.

**ANSI.** An ANSI escape-sequence renderer is not planned. `Console.ForegroundColor` already produces the right
thing on each platform, and segments let a consumer do its own. Revisit only if real evidence of need appears.

### What Campertron needs from the package

Campertron is Shaun's campsite-availability application, in a separate repository. It currently draws its own
console calendar. The package must eventually offer enough to replace Campertron's **live** renderer,
`Calendar.Lines` in `CampertronLibrary/function/Base/Calendar.cs`. Campertron itself is changed elsewhere; see
[decisions.md](decisions.md#d-19-campertron-adoption-happens-elsewhere).

As observed in Campertron at commit `b23a6b9` (2026-11-27), that renderer:

- returns lines of styled spans (text plus an optional `ConsoleColor`) and never writes to the console;
- takes the year, month, the dates to mark, an explicit `CultureInfo`, an indent string and a mark color;
- titles the month with the culture's year-month pattern, followed by the legend ` (* = open)`;
- uses a fixed English weekday row, Monday first;
- right-aligns each day in a three-character cell with one space between cells;
- marks a day by prefixing `*` inside its cell and coloring the whole cell.

The same file also contains an older value-stream calendar that Campertron's console no longer shows. Parity
work targets `Calendar.Lines`, not that. Re-read Campertron's current code before building parity fixtures; this
list is a snapshot, not a contract.
