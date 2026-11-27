# Campertron parity fixtures

These fixtures record what Campertron's live calendar renderer returns, so that this repository can show,
without Campertron, that the modern API reproduces it. Campertron is a separate application and is never
modified from here; see `docs/ai-context/decisions.md` (D-16, D-19, D-47).

## Where the fixtures come from

| | |
|---|---|
| Repository | `https://github.com/ShaunAnthonyHathaway/Campertron` |
| Commit | `3ded294c37642243359bcee2563d7227295a7f40` |
| Symbol | `CampertronLibrary.function.Base.Calendar.Lines(int Year, int Month, IEnumerable<DateTime> Dates, CultureInfo Culture, string Indent, ConsoleColor MarkColor)` |
| File | `CampertronLibrary/function/Base/Calendar.cs` (unchanged since the Phase 0 audit at `b23a6b9`) |
| Recorded | 2026-11-27, on Linux with .NET 11.0 |

The same information is in `Fixtures/manifest.json`, which the tests read.

`Calendar.Lines` returns a list of lines, each a list of spans, and a span is some text with an optional
`ConsoleColor`. A fixture is the input of one call and exactly that result: for every line its text, and its
spans as `[text, color]` pairs in order, with `null` for no color.

## What is in a fixture file

```text
schemaVersion   1
about           what the file covers
cases[]
  id            unique within the file
  covers        what this case is for
  title         "exact", or "intentionally-different" where the package's title deliberately differs
  input
    year, month
    culture     a culture name; "" is the invariant culture
    indent      the text before every line
    markColor   the ConsoleColor given to marked days
    dates[]     date (yyyy-MM-ddTHH:mm:ss.fffffff, as written, never converted) and kind (DateTimeKind)
  lines[]
    text        the whole line
    spans       [text, color] pairs; color is a ConsoleColor name or null
```

Unknown fields are an error: the tests read the files strictly. The files are ASCII only, with anything else
written as `\uXXXX`. They contain no paths, no times of recording and nothing from a real search: every date is
made up.

## What the source fixes, and what a caller chooses

`Calendar.Lines` has no options besides its arguments. Weeks start on Monday; the weekday row is the fixed text
` Mo  Tu  We  Th  Fr  Sa  Su`; a cell is three wide with one space between cells; an open day is marked with
`*` directly before its number and its whole cell is given the mark color; the title is the culture's
year-and-month pattern (`"Y"`) followed by ` (* = open)`, at the left. The two places in Campertron that call it
pass an indent of two spaces and `ConsoleColor.Cyan`.

## How the package reproduces it

`ParityProfile` builds the package's side from a case, using public API only:

```text
MonthRenderOptions.Default
    .WithCulture(culture)                 .WithFirstDayOfWeek(Monday)
    .WithWeekdayLabels(Su Mo Tu We Th Fr Sa)
    .WithCellWidth(3)                     .WithColumnGap(1)
    .WithIndent(indent)
    .WithTitleFormat("Y")                 .WithTitleSuffix(" (* = open)")     .WithTitleAlignment(Left)
DayState("open", "*")                     on the dates of the case
CalendarTheme.None.WithStateColor("open", markColor)
```

`SegmentAdapter` then turns the rendered month into lines of spans the way a consumer would: the title and the
weekday row are one span each; a week starts with one span holding everything before its first day (the indent
and any empty cells), followed by one span per day cell, colored by the theme, and one per gap. It reads
`RenderedSegment.Kind`, `Text`, `Date` and `State` and asks `CalendarTheme.GetColor`; it never parses text. The
names `open` and `*`, the wording of the legend and the color are the caller's, not the package's.

## Exact, adapter and intentional

| | |
|---|---|
| **Exact parity** | The Gregorian grid from Monday; the cell width and the gap; the marker inside the cell before the number; the indent; the text of every line under the invariant culture and under cultures whose calendar is Gregorian; the order and text of every span; which spans are colored and in which color; that a marked day is visible without color. |
| **The adapter's or the caller's** | Turning segments into styled spans; the state's name; the color; the legend's wording; when a calendar is shown; terminal and width decisions; writing and locking. |
| **Intentionally different** | The title under `th-TH` and `fa-IR`. Campertron titles the month in the culture's own calendar (`2566`, `1402`), the package always titles the Gregorian month it draws. Those two cases are marked `intentionally-different`: every line except the title must still match exactly, and the titles must differ. |

Campertron also still produces an older stream of calendar values (`Calendar.GenerateCalendar`). Nothing in
Campertron displays it, so there are no fixtures for it (D-47).

## Culture data

The title of a real culture comes from the operating system's culture data, which can differ between systems
and over time. A test for such a case first checks that this system formats the month as the fixture's system
did; where it does not, the test is reported as inconclusive rather than failed, because the difference is in
the data, not in the package. Cases under the invariant culture have no such dependency and must always match.

## Recording them again

Only when Campertron's renderer has changed and the new behavior is to become the target. From a copy of the
chosen commit, outside this repository and without changing Campertron:

1. Build `CampertronLibrary`.
2. For every case in the fixture files, call `Calendar.Lines` with the case's input and write the result in the
   format above.
3. Update the commit in `Fixtures/manifest.json` and in this file.

```csharp
List<ConsoleUi.StyledLine> lines = Calendar.Lines(year, month, dates, CultureInfo.GetCultureInfo(culture), indent, markColor);
foreach (ConsoleUi.StyledLine line in lines)
{
    // line.Text, then for each span: span.Text and span.Color
}
```

The program that does this is not kept here, because nothing in this repository may reference Campertron. The
inputs are all in the fixture files, so the fixtures themselves are the list of calls to make.
