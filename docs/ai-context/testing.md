# Testing

## Current tests

`ConsoleCalendarGenerator.Tests` holds **154 tests**: 153 characterization tests of the legacy API and 1 test
about the build setup. It targets `net11.0` and uses NUnit 5.0.0 with NUnit3TestAdapter 6.3.1 and
Microsoft.NET.Test.Sdk 18.12.0.

### Purpose

The characterization tests record what `CalendarGenerator.ConsoleCalendar` does in published 1.0.0, before any
of it is refactored. They describe the behavior that exists, **including behavior that is wrong**. They are the
definition of "unchanged" for the whole 1.x line. See [compatibility.md](compatibility.md).

A test whose name contains `Characterized1xBehavior` pins down something that is not the intended design. Do not
correct it.

### Organization

| Fixture | Tests | Covers |
|---|---|---|
| `Legacy/PublicApiTests` | 5 | The public surface by reflection, and source compatibility with named arguments |
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

### What a run executes

| System | Normal run (`TestCategory!=Terminal`) | Not run there | Terminal run |
|---|---|---|---|
| Linux | 149 pass | 2 Windows-only | 3 pass |
| macOS | 149 pass | 2 Windows-only | not part of CI |
| Windows | 124 pass | 27 that need Linux or macOS | not part of CI |

The same numbers apply to each of the two library builds. CI runs the normal run against both.

### Isolation

The library writes to the process-wide console and reads the process-wide culture, so:

- The assembly is `[NonParallelizable]`. Never run these tests in parallel.
- Every test goes through `ConsoleCapture`, which restores `Console.Out`, both cultures and the console colors.
- `GlobalStateGuardAttribute` checks after each test that nothing leaked, reports it, and puts the state back so
  that one leaking test does not fail the ones after it.
- Tests never change environment variables of the test process. A child process gets its own.

### Platform-specific tests

- **Linux and macOS only (27):** 25 tests that read console colors back, and 2 that assert line-feed-only
  output. On those systems the runtime keeps the current colors in the process, so they can be read without a
  terminal. On Windows the colors live in the console, and a test host without one ignores changes. These tests
  are marked `[Platform(Exclude = "Win")]`.
- **Windows only (2):** the real mixed line endings.
- Add a platform condition only where the behavior really differs by platform. Text, write-sequence, API and
  validation tests run everywhere.

### The Terminal category

Three tests need a terminal database entry for `xterm-256color`, and two of those also need Linux and the
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

### Layout and renderer

- Because layout and rendering will be pure, their tests should need no console capture, no culture scoping
  beyond an explicit `CultureInfo` argument, and no platform conditions. They should be safe to run in parallel
  in principle, though the assembly stays non-parallel while it also holds the legacy tests.
- Layout: every month in a wide range against an independent model, and every first-weekday setting.
- Rendering: exact text, exact segment boundaries and roles, cell width, indent, alignment, no trailing spaces,
  explicit line endings.
- Both library builds, as now.
- When the legacy class becomes a facade, every test in `Legacy/` must pass without being edited. That is the
  acceptance test for that phase.

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
