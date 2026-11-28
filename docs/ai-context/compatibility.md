# Compatibility

The baseline is the package that is actually on nuget.org: **`ConsoleCalendarGenerator` 1.0.0**, published
2023-06-25, containing one `netstandard2.1` assembly. The next published version is 2.0
([D-55](decisions.md#d-55-the-next-published-version-is-20)), and this page says exactly how 2.0 may differ
from 1.0.0 and how everything else is kept.

## Policy

This is the 2.0 migration policy ([D-56](decisions.md#d-56-20-corrects-exactly-five-legacy-behaviors-and-protects-everything-else)).
It replaced an earlier rule under which the faults of 1.0.0 were to stay through a line of 1.x versions. No
such version was ever published, so that rule protected nothing and is gone.

- **What 1.0.0 does stays on record and stays testable.** The source of 1.0.0 is frozen in the tests, and tests
  say what it did, the defects included. See [The 1.0.0 reference](#the-100-reference).
- **2.0 corrects exactly five behaviors of the legacy class,** listed below, and nothing else.
- **Every approved difference needs all four of these,** and is not made without them:
  1. a decision in [decisions.md](decisions.md) with an identifier;
  2. a focused pull request that makes that one difference;
  3. a migration test that fails if the library and 1.0.0 differ in any other way;
  4. an entry in `CHANGELOG.md` and the migration documentation for users.
- **Everything outside the five corrections is protected** exactly as before: text, every single call on the
  console, colors, exceptions and side effects.
- **No public signature and no existing parameter name changes.** That includes the four setters, whose
  parameter is named `TitleColor`, and the namespace `CalendarGenerator`. Nothing is removed and nothing is
  marked obsolete ([D-62](decisions.md#d-62-the-legacy-class-keeps-every-signature-and-nothing-is-obsolete)).
- **The modern API is unchanged for 2.0:** nothing is added to it, removed from it or changed in it
  ([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).
- **Package validation against published 1.0.0 stays on,** with no suppression
  ([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)).
- **A breaking change that is not on the list is forbidden.** A new one needs Shaun's decision first, recorded
  like the five.
- **What the modern API renders for Campertron does not change.** The parity fixtures stay as they are and keep
  passing. Campertron itself is out of scope
  ([D-19](decisions.md#d-19-campertron-adoption-happens-elsewhere)).
- **Nothing is published before the final approval of 2.0.**

## The five approved corrections

| | Decision | 1.0.0 | 2.0 | In the library |
|---|---|---|---|---|
| 1 | [D-57](decisions.md#d-57-constructor-arguments-color-the-element-they-are-named-for) | In the constructors that take colors, `TitleColor` colors the weekday row, the title stays Yellow, and with four arguments `DayOfWeekColor` colors the title. | Each argument controls the element it is named for. | **Yes** (pull request #14) |
| 2 | [D-58](decisions.md#d-58-a-null-list-throws-argumentnullexception-for-dates) | A null list throws `NullReferenceException`. | A null list throws `ArgumentNullException` with the parameter name `dates`, still before the month and the year are checked. | **Yes** (pull request #15) |
| 3 | [D-59](decisions.md#d-59-the-last-row-of-a-month-ends-with-the-consoles-line-terminator) | The last row of days of a month ends with a literal `\n`. | It ends with the console's line terminator, like every other line. | **Yes** (pull request #16) |
| 4 | [D-60](decisions.md#d-60-every-month-is-followed-by-exactly-one-blank-line) | A month is followed by one blank line, or by two when its last day is a Sunday. | Every month is followed by exactly one blank line. | **Yes** (pull request #16) |
| 5 | [D-61](decisions.md#d-61-legacy-titles-name-the-gregorian-month) | The title names the month of that number in the current culture's own calendar. | The title names the Gregorian month that the grid shows, also under a culture whose calendar is not Gregorian. | **Yes** (pull request #17) |

**All five are implemented: the constructor colors, the null list, the line terminator, the blank line and
the title.** In everything else `master` behaves exactly as 1.0.0 does, and the tests require that. There is no
sixth correction, and none may be added without a decision by Shaun.

### What did not change

For a caller of the legacy class, everything outside the five rows above is as in 1.0.0. The list a migration
has to be able to rely on:

- every public signature and parameter name, the namespace `CalendarGenerator` and the type name
  `ConsoleCalendar`; nothing removed, nothing obsolete;
- `GenerateYear`, and the four getters and four setters;
- which dates are selected: by year, month and day;
- the fixed English weekday row and weeks from Monday;
- writing directly to the console, piece by piece, with the same calls;
- resetting the console's colors, not restoring the caller's;
- no lock;
- the modern API, which is new in 2.0 and whose behavior no correction touched;
- the Campertron parity fixtures and what the modern API renders for them.

The README tells users what they need of this in its section on moving from 1.0.0, in one sentence and
without the last item: the public documents name no consumer of the package, and they describe the five as
updated behaviors, not as corrections
([D-84](decisions.md#d-84-public-text-is-positive-direct-and-about-the-library))
([D-73](decisions.md#d-73-the-migration-guidance-is-one-section-of-the-readme-and-the-changelog)).

**Why package validation passes although behavior changed.** Package validation compares the shape of the
public API with published 1.0.0: types, members, signatures, parameter names. All five corrections change
what a member does and none changes its shape, so the strict comparison has nothing to report and needs no
suppression ([D-66](decisions.md#d-66-package-validation-stays-against-100-with-no-suppression)). Behavior
is what the migration tests compare.

### How a correction is made

All five were made this way, and a sixth, should Shaun ever decide one, would be made the same way: one
correction per pull request, scoped by Shaun (the line terminator and the blank line were made together, as he
approved). The pull request for the constructor colors is the worked example. In that pull request:

1. Change the legacy class for that one behavior.
2. In `ConsoleCalendarGenerator.Tests/Migration/ApprovedCorrection.cs`, say what the correction changes, in the
   one of two forms that states it exactly: `EquivalentIn100`, the use of 1.0.0 that does what the library now
   does (the constructor colors are stated this way: a constructor now does what the parameterless
   constructor and the setters did), or `Corrected`, what the library now does given what 1.0.0 did.
   `LegacyMigrationTests` then requires exactly that, in every scenario and every aspect, so the other
   behaviors and everything else stay compared with 1.0.0 at full strength.
3. Add tests that state the corrected behavior outright.
4. Change the tests in `Legacy/` and `Facade/` that pin the 1.0.0 behavior being corrected, and only those. A
   corrected library is not required to satisfy an assertion that pins a defect it no longer has. The recorded
   digests are handled the same way: see [testing.md](testing.md#when-a-correction-changes-a-recorded-digest).
5. Leave `Historical/` alone. It describes 1.0.0, which does not change.
6. Update the XML comments of the class, the table above, the sections below, `CHANGELOG.md` and the README
   where they describe the old behavior.

Nothing about a correction is implemented, tested as done or documented as done before its pull request.

## The published API

```text
namespace CalendarGenerator
public class ConsoleCalendar                     (not sealed, no interfaces, no virtual or static members)

ConsoleCalendar()
ConsoleCalendar(ConsoleColor TitleColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor, ConsoleColor DayOfWeekColor)

void GenerateMonth(int Month, int Year)
void GenerateMonth(int Month, int Year, List<DateTime> dates)
void GenerateYear(int Year)
void GenerateYear(int Year, List<DateTime> dates)

ConsoleColor GetTitleColor()            void SetTitleColor(ConsoleColor TitleColor)
ConsoleColor GetDayOfWeekColor()        void SetDayOfWeekColor(ConsoleColor TitleColor)
ConsoleColor GetDateColor()             void SetDateColor(ConsoleColor TitleColor)
ConsoleColor GetSelectedDateColor()     void SetSelectedDateColor(ConsoleColor TitleColor)
```

This is the API of 1.0.0 and of 2.0. Details that are part of the contract:

- The namespace is `CalendarGenerator`, which is not the package ID. That mismatch is published and stays.
- The date parameters are the concrete `List<DateTime>`, not an interface.
- Parameter names are part of the contract, because callers can use named arguments: `Month`, `Year`, `dates`,
  the constructor names above, and `TitleColor` on all four setters. The setter parameters are not renamed in
  2.0.
- The class is not sealed. Consumers may have derived from it.
- The assembly is named `ConsoleCalendarGenerator` and is not strong-named. Giving it a strong name would change
  its identity; do not do that without real consumer evidence and Shaun's approval.

## Behavior of the legacy class

What the class does on `master` today, which is what 2.0 does. Each line is covered by tests in
`ConsoleCalendarGenerator.Tests/Legacy/`. A line marked **(corrected, D-nn)** is one of the five corrections
and says what 1.0.0 did instead. Every other line is what 1.0.0 does as well, is protected and does not
change.

The XML comments of the class describe the same behavior to users
([D-51](decisions.md#d-51-the-legacy-class-is-documented-as-it-behaves)); those of the constructors describe
the corrected behavior and say what 1.0.0 did. They describe the contract; the tests define it. A comment that
disagrees with a test is the thing to fix.

**Colors and constructors**

- Defaults: title Yellow, weekday row Cyan, dates White, selected dates Blue.
- **Constructors (corrected, D-57).** Each argument colors the element it is named for, and an element with no
  argument keeps its default:

  | Constructor | Title | Weekday row | Dates | Selected dates |
  |---|---|---|---|---|
  | `()` | Yellow | Cyan | White | Blue |
  | `(TitleColor)` | `TitleColor` | Cyan | White | Blue |
  | `(TitleColor, DateColor)` | `TitleColor` | Cyan | `DateColor` | Blue |
  | `(TitleColor, DateColor, SelectedDateColor)` | `TitleColor` | Cyan | `DateColor` | `SelectedDateColor` |
  | `(TitleColor, DateColor, SelectedDateColor, DayOfWeekColor)` | `TitleColor` | `DayOfWeekColor` | `DateColor` | `SelectedDateColor` |

  In 1.0.0 the title was Yellow and the weekday row `TitleColor` in the second, third and fourth row, and in
  the last row the title was `DayOfWeekColor` and the weekday row `TitleColor`. The dates and the selected
  dates were as above. A constructor now does exactly what the parameterless constructor followed by the
  setters of the same names does, which is also what it does in 1.0.0 with those setters.
- The setters are correct in 1.0.0 and unchanged.
- Setters and constructors accept values that are not real `ConsoleColor`s, and check nothing. The failure
  (`ArgumentException`) comes later, when the console is asked to use the color, and whatever was written
  before it stays written. The title is drawn first, then the weekday row, then the cells; the empty cells
  before the 1st and the days that are not selected use the date color, and the selected-date color is first
  used at the first selected day.
- **Where an undefined constructor argument fails (a consequence of D-57).** Because the first and the fourth
  argument color other elements than in 1.0.0, a value that is not a color fails at another point when it is
  passed as one of them. Nothing validates earlier than before, and the exception type is the same:

  | Undefined argument | 1.0.0 | 2.0 |
  |---|---|---|
  | `TitleColor`, in any of the four constructors | at the weekday row, after the title is written | at the title, before anything is written |
  | `DayOfWeekColor` | at the title, before anything is written | at the weekday row, after the title is written |
  | `DateColor` | at the first cell, after the weekday row | the same |
  | `SelectedDateColor` | at the first selected day; never, if no day of the month is selected | the same |

  With several undefined arguments, the element drawn first fails and the others are never reached.

**Layout and text**

- Weeks start on Monday. The weekday row is always the English `Mo Tu We Th Fr Sa Su`, whatever the culture.
- Day cells are two characters wide with one space between them. No line has trailing spaces.
- The title is indented five spaces for month names of up to 4 characters, four for up to 7, three otherwise,
  followed by the name, a space and the year as a plain number (year 1 is `1`).
- **Every month is followed by exactly one blank line (corrected, D-60).** In 1.0.0 a month whose last day is
  a Sunday was followed by two, because its last row was ended twice: once as a full week and once as the last
  row.
- `GenerateYear` is exactly twelve `GenerateMonth` calls. There is no year heading. So a year has one blank
  line after each month, December included, and nothing more between the months or at the end.

**Line endings**

- **Every line ends with `Console.WriteLine`, so with the console writer's newline (corrected, D-59):** the
  title, the weekday row, every row of days, the last one included, and the blank line. No line feed is ever
  written as text. In 1.0.0 the last row of days ended with a literal `\n` instead, so where the writer's
  newline is `\r\n`, as on Windows, the output mixed both; it no longer does. Where the newline is `\n` the
  text of a month that does not end on a Sunday is byte for byte what 1.0.0 wrote.
- The newline is the writer's, whatever it is: `Console.Out.NewLine` when the month is drawn, not
  `Environment.NewLine`.

**Culture**

- The month name comes from `CultureInfo.CurrentCulture` (not `CurrentUICulture`). Nothing else is localized.
- **The title names the Gregorian month, the one the grid shows (corrected in 2.0, D-61).** The name is the
  current culture's name for that month of the Gregorian calendar. The exact rule, which is the contract:
  1. `CultureInfo.CurrentCulture` and its `DateTimeFormat` are read once for each month drawn, after the title
     color is set and before the title is written, where 1.0.0 read them.
  2. If the calendar of those formats is a `GregorianCalendar`, of any kind, the name is
     `DateTimeFormat.GetMonthName(Month)`. That is the call 1.0.0 made, so under every such culture the title is
     identical to 1.0.0, month names that a program set on the culture included.
  3. Otherwise the name is taken from a Gregorian calendar among `CultureInfo.OptionalCalendars`: the first one
     of the kind `GregorianCalendarTypes.Localized`, or failing that the first of another kind. The calendar is
     set on a copy of the formats (`Clone()`), and the name is `GetMonthName(Month)` of the copy. A calendar the
     formats refuse (`ArgumentException`, possible only when the formats are not the culture's own) is skipped,
     so the lookup adds no exception that 1.0.0 did not throw.
  4. If no Gregorian calendar is offered or taken, the name is the invariant culture's. No culture of any
     system is known to reach this; it names the right month in English and never a month of another calendar.
  5. Nothing is changed and nothing is kept: not the culture, not its formats, not their calendar, and there is
     no cache. Month names a program set on a culture whose calendar is not Gregorian are names of that
     calendar's months and are not used.
- In 1.0.0 the name was `DateTimeFormat.GetMonthName(Month)` whatever the calendar of the formats was. Under a
  culture whose calendar is not Gregorian and whose months are not the Gregorian ones (for example `fa-IR`
  with the Persian calendar), the title therefore named a different month than the grid shows. Under a culture
  whose other calendar has the Gregorian months (Thai Buddhist, Japanese, Korean, Taiwan), both versions write
  the same name.
- The indent of the title still has its three steps by `string.Length` of the name that is written (five
  spaces up to 4, four up to 7, three beyond). Where the Gregorian name falls on another step than the name
  1.0.0 wrote, the indent follows the name written. It is not centering and counts UTF-16 code units.
- The year is the argument, never converted to the culture's calendar.

**Selected dates**

- A date selects a day when its year, month and day match. Time of day and `DateTimeKind` are ignored.
- Duplicates, order and dates outside the rendered month or year make no difference.
- The caller's list is not modified.
- Selection changes color only. The text is identical with and without it.
- The space before a day belongs to that day's write and takes its color. The empty cells before the 1st are
  written in the color of the 1st.

**Exceptions**

- Month outside 1 to 12, or year outside 1 to 9999: `ArgumentOutOfRangeException`, before anything is written.
- **A null list (corrected, D-58):** `ArgumentNullException` with the parameter name `dates`, from
  `GenerateMonth(Month, Year, dates)` and from `GenerateYear(Year, dates)`. The message is the runtime's for a
  null argument of that name; the library passes none of its own, and only the type and the parameter name are
  the contract. In 1.0.0 it was `NullReferenceException`, which names no parameter, thrown by the runtime when
  the class began to read the list.
- **The order did not change.** The list comes first, in 1.0.0 because reading it was the first thing done, in
  2.0 because the check is the first statement: before the month and the year are looked at, so a null list
  wins over an invalid month, an invalid year or both; before the culture is asked for a month name; and before
  any color is used, so it also wins over a color that is not defined.
- **Nothing is touched before it.** No text is written, no call is made on the console's writer, no color is
  set and none is reset: the caller's foreground and background are as they were. Nothing in the process
  changes, and the calendar keeps its colors.
- A list that is not null is never refused, also when it is empty, and everything about it is as in 1.0.0.

**Console side effects**

- Output goes straight to the console: text through `Console.Out`, colors through `Console.ForegroundColor`.
- `Console.ResetColor()` is called after the title, after the weekday row and at the end. The caller's
  foreground and background colors are reset, not restored.
- Colors are changed even when `Console.Out` has been replaced.
- The library writes no escape sequences of its own.
- Which method of the console's writer is called is observable to a program that replaces `Console.Out` with
  its own writer. The title and the weekday row are one `WriteLine(string)` each. Every cell is one
  `Write(string)`, except a two-digit day in the Monday column, which is `Write(int)`. A row ends with one
  `WriteLine()`, after its Sunday or, for the last row, after the last day, and one more `WriteLine()` is the
  blank line **(corrected, D-59 and D-60)**. So every month ends: last day, `WriteLine()`, `WriteLine()`. In
  1.0.0 it ended: last day, `Write("\n")`, `WriteLine()`, with a `WriteLine()` before those when the last day
  was a Sunday. A program that replaces `Console.Out` therefore sees one call made another way for every
  month, and one call fewer for a month that ends on a Sunday.
- The calls that end a month are made in the color of the last day, as the calls that ended it in 1.0.0 were;
  no color is set or reset for them, and the final `Console.ResetColor()` still comes after them.
- The space between two columns is part of the write of the day after it. An empty cell before the 1st is
  written three wide, so the 1st itself is written two wide.
- Nothing is locked: the class does not keep draws from several threads apart.

## How the legacy class is built

Since the facade phase the class takes one thing from the modern API and keeps the rest to itself. This table is
the map from each behavior to where it lives. A change to the class has to keep every row true, apart from the
one behavior a correction is about.

| Behavior | Where it lives |
|---|---|
| Public members, parameter names, unsealed class | The same declarations as 1.0.0 |
| Constructor color assignments, each argument to the element it is named for (D-57); getters and setters | Four private fields |
| A null list fails first, with `ArgumentNullException` for `dates` (D-58) | A null check as the first statement of the two methods that take a list, where 1.0.0 had none and failed on reading the list |
| An invalid month or year fails with the runtime's own `ArgumentOutOfRangeException` | The same `DateTime.DaysInMonth(Year, Month)` call, in the same position |
| Which column and which row each day is in; four, five or six rows; Monday first | `MonthLayout.Create(Year, Month, DayOfWeek.Monday)` |
| A date selects a day by year, month and day; the list is not changed | A private lookup by day of the month |
| Title: the current culture's name for the Gregorian month (D-61), three-step indent, the year appended | Private. Two small private methods choose the name, where 1.0.0 asked the culture's formats for the month number whatever their calendar was. The modern renderer is not used for it |
| The fixed English weekday row | Private constant |
| One console call per cell, the method and text of each, empty cells in the color of the 1st | Private, walking the cells of the layout in order |
| Row end after Sunday and after the last day, once; one more line end for the blank line (D-59, D-60) | Private. Two lines of the loop that walks the cells, where 1.0.0 wrote a literal `\n` |
| Color set before every call; three `ResetColor` calls; caller's colors not restored; an undefined color fails when it is first used | Private |
| No lock, no restoration, no early validation | By not using `ConsoleMonthWriter`, `CalendarTheme` or `MonthRenderer` |

What the class does not use, and why:

- **`MonthRenderer`.** Its title is formatted from a pattern; its cells and gaps are separate segments. The
  legacy title and the legacy write boundaries would have to be reassembled from them.
- **`DayState`.** A state must have a visible marker, and a selected date in the legacy class changes color
  only.
- **`CalendarTheme` and `ConsoleMonthWriter`.** They refuse undefined colors early, restore the caller's colors,
  write whole segments and take a lock. The legacy class does none of those, in 1.0.0 or in 2.0.

No option, overload or type was added to the modern API for the facade, and none is needed
([D-45](decisions.md#d-45-no-public-compatibility-profile)). The five corrections do not change that: each is
made inside the class.

## The 1.0.0 reference

`ConsoleCalendarGenerator.Tests/Historical/Published100/ConsoleCalendar.cs` is the source of the class as it
was when 1.0.0 was published, compiled into the test assembly under a namespace of its own
([D-67](decisions.md#d-67-100-is-kept-as-a-frozen-source-and-differences-are-shown-by-migration-tests)).

- **It is never edited.** Not formatted, not modernized, not corrected. Its defects are the point. A test
  undoes the four documented differences from the original file (a comment in front, the byte order mark, the
  namespace, a final line feed) and compares the result with the SHA-256 of the original, so any other change
  fails.
- **It is test code only.** It is not in the library, not in the package, and not public API of anything.
- **Where it comes from** is recorded in the file's header and in `Historical/Published100Source.cs`: commit
  `93f2d5d`, Git blob `02b3c1c`. That is the source the published assembly was built from. The class at commit
  `64995b6`, just before the facade, is seven weeks newer and differs in one private helper with the same
  result; see [testing.md](testing.md#the-frozen-source-of-100).
- **It reproduces every value recorded from 1.0.0:** the two digests recorded from the published assembly and
  the two recorded before the facade.

## What protects it

Four independent layers. A change has to pass all of them.

1. **Migration tests** (`ConsoleCalendarGenerator.Tests/Migration/`), which run the library and the frozen
   source of 1.0.0 through the same scenarios and require the library to do exactly what 1.0.0 did, apart from
   the five corrections, each of which is stated there as an exact change.
2. **Characterization tests** (`ConsoleCalendarGenerator.Tests/Legacy/`), including digests recorded from the
   published 1.0.0 assembly, and `ConsoleCalendarGenerator.Tests/Facade/`, with digests of every console call
   recorded from the original implementation before it was rebuilt, and a comparison of the drawn grid with
   `MonthLayout`. They run against both library builds. See [testing.md](testing.md).
3. **`PublicApiTests`,** which compares the public surface of the `CalendarGenerator` namespace, by reflection,
   with the list above. The modern API is listed the same way in `ModernApiTests`, which also fails if the
   assembly exports a type outside the two namespaces.
4. **Package validation** on every `dotnet pack`: the `netstandard2.1` build is compared with the one in the
   1.0.0 package, the `net10.0` build with the `netstandard2.1` build, and renamed parameters count as breaks.
   See [packaging-and-releases.md](packaging-and-releases.md#package-validation).

If one of these fails, the default assumption is that the change is wrong, not the test. The one exception is
the pull request for an approved correction, which changes the tests that pin the behavior it corrects and no
others.

## Versions

- **`AssemblyVersion` is the major version only,** always matching the package's major version: it was `1.0.0.0`
  in 1.0.0 and is `2.0.0.0` now, for 2.0 and every later 2.x version
  ([D-65](decisions.md#d-65-the-repository-builds-200-dev-with-assembly-and-file-version-2000)).
- **The package version** is SemVer and is what identifies a release to consumers. The repository builds
  `2.0.1`, with no suffix: it is the source of the release
  ([D-72](decisions.md#d-72-the-release-is-200-built-once-and-published-as-the-files-that-were-validated),
  [D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget)).
  Every build carries that number; only the candidate is the release. 2.0.0 is released on GitHub and 1.0.0 on
  NuGet, and neither number is ever built again.
- **The file version** follows the version prefix: `2.0.1.0`. It is the one version number of the assembly
  that tells 2.0.1 from 2.0.0.
- **The informational version** carries the package version and the commit, added by the SDK's Source Link
  support. Keep it.

`VersionIdentityTests` checks all of this on both library builds, and that the major versions of the assembly
and of the package agree.

## Changing the API

- **For 2.0, nothing public changes.** No legacy signature or parameter name, and nothing in the modern API:
  no addition, no removal, no change. Package validation and the two API tests fail on any of them.
- After 2.0, adding a type or a member is the compatible kind of change. Removing, renaming or changing one is
  breaking.
- Do not add an overload that makes an existing call ambiguous. Example: adding
  `GenerateMonth(int, int, IEnumerable<DateTime>)` beside the `List<DateTime>` overload makes
  `GenerateMonth(1, 2023, null)` fail to compile. Analyze `null` arguments and implicit conversions before adding
  any overload to an existing method group. New capability normally belongs on new types instead.
- Do not add `[Obsolete]` to legacy members. None is added in 2.0
  ([D-62](decisions.md#d-62-the-legacy-class-keeps-every-signature-and-nothing-is-obsolete)).
- Both library builds must expose the same API. Package validation enforces this.
