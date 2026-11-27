# Open questions

Questions that are genuinely undecided. When one is settled, move it to [decisions.md](decisions.md) with a new
decision identifier and delete it here; do not renumber the rest.

Recommendations are starting points from the Phase 0 audit, not decisions.

Q-01 (the modern API's public types and signatures) is settled for every planned layer: layout in D-25 to
D-28, rendering in D-30 to D-37, and states, the theme and output in
[D-38](decisions.md#d-38-a-day-state-is-a-name-and-a-visible-marker) to
[D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes). What is
left about the modern API is Q-02.

## Q-02: Does the modern API render a whole year?

- **Why it matters:** The legacy `GenerateYear` only stacks twelve months. A modern year view invites layout
  choices (stacked, or several months side by side) that add real complexity.
- **Evidence needed:** Whether any consumer wants more than twelve stacked months. Campertron does not render
  years.
- **Decide in:** later, if a consumer asks. The modern API renders single months only. The legacy facade draws a
  year as twelve months without any public year API, so nothing so far needs one.
- **Recommendation:** Keep to months.

Q-03 (how the title is expressed) is settled:
[D-34](decisions.md#d-34-the-title-is-a-date-format-string-a-suffix-and-an-alignment).

Q-04 (more than one state on a date) is settled: one state per day, and the first entry wins
([D-39](decisions.md#d-39-one-state-per-day-and-the-first-entry-wins)).

## Q-05: When do legacy members become obsolete?

- **Why it matters:** `[Obsolete]` produces a warning in every consumer's build, and consumers that treat
  warnings as errors would fail. It also needs a replacement to point to.
- **Evidence needed:** A finished replacement API, and a view on whether warning existing users during 1.x is
  acceptable.
- **Decide in:** the 1.x release decision. The facade phase added none, because the replacement's user
  documentation and migration guidance do not exist yet.
- **Recommendation:** None yet. Until it is decided, add no `[Obsolete]` attributes.

Q-06 (a compatibility profile for titles) is settled: there is none, and none is needed
([D-45](decisions.md#d-45-no-public-compatibility-profile)).

## Q-07: What ships in 1.x, and when?

- **Why it matters:** Development builds are `1.1.0-dev`, and 2.0 is the version planned for NuGet and for
  Campertron. Whether any 1.x version or prerelease is published in between, and what it contains, is not
  decided.
- **Evidence needed:** The state of the new API when the facade phase is done, and whether anyone needs it
  before 2.0.
- **Decide in:** the 1.x release decision in [roadmap.md](roadmap.md).
- **Recommendation:** None. Any publication needs explicit approval regardless.

## Q-08: What exactly changes in 2.0?

- **Why it matters:** 2.0 is the one chance to make breaking corrections, and each one costs existing users
  something.
- **Evidence needed:** A reviewed list, with the reason and the migration path for each item.
- **Decide in:** the 2.0 breaking-corrections phase.
- **Recommendation:** Candidates identified so far, none of them decided: the constructor color assignments; the
  exception type for a null list; the extra blank line after months ending on Sunday; the mixed line endings;
  the title under non-Gregorian cultures in the legacy class; whether the legacy class and its namespace remain;
  whether `netstandard2.1` remains a target.

Q-09 (parity fixtures for Campertron's unused value stream) is settled: there are none
([D-47](decisions.md#d-47-campertron-parity-is-shown-with-recorded-fixtures)).
