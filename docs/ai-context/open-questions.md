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
- **Decide in:** the legacy facade phase, or later. The modern API renders single months only so far.
- **Recommendation:** Keep to months. The legacy facade can produce a year from months without a public year
  API.

Q-03 (how the title is expressed) is settled:
[D-34](decisions.md#d-34-the-title-is-a-date-format-string-a-suffix-and-an-alignment).

Q-04 (more than one state on a date) is settled: one state per day, and the first entry wins
([D-39](decisions.md#d-39-one-state-per-day-and-the-first-entry-wins)).

## Q-05: When do legacy members become obsolete?

- **Why it matters:** `[Obsolete]` produces a warning in every consumer's build, and consumers that treat
  warnings as errors would fail. It also needs a replacement to point to.
- **Evidence needed:** A finished replacement API, and a view on whether warning existing users during 1.x is
  acceptable.
- **Decide in:** the legacy facade phase, or the 1.x release decision.
- **Recommendation:** None yet. Until it is decided, add no `[Obsolete]` attributes.

## Q-06: How is the compatibility profile for titles expressed?

- **Settled so far:** the correct default. The modern renderer always titles the Gregorian month
  ([D-33](decisions.md#d-33-the-culture-supplies-language-only)), and no compatibility profile was added.
- **Why it matters:** Exact parity with existing output is still wanted in two places. The legacy class names
  the month from the culture's own calendar, and Campertron's current title follows the culture's own calendar
  too. The mechanism for reproducing either is not decided.
- **Evidence needed:** Whether a dedicated option is needed at all. A caller can already produce any exact
  title, because quoted text in `TitleFormat` is copied unchanged
  ([D-34](decisions.md#d-34-the-title-is-a-date-format-string-a-suffix-and-an-alignment)); the facade and the
  parity fixtures will show whether computing the title outside the renderer is enough, including under cultures
  with a non-Gregorian calendar.
- **Decide in:** the legacy facade phase, and confirmed by the parity fixtures.
- **Recommendation:** Add no profile unless those phases show the quoted-title route is not enough.

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

## Q-09: Should parity fixtures ever cover Campertron's unused value stream?

- **Why it matters:** Campertron still contains an older calendar whose output its console no longer shows but
  whose text its own tests pin down. Parity work targets the live renderer only
  ([D-16](decisions.md#d-16-campertron-parity-targets-its-live-renderer)).
- **Evidence needed:** Whether the Campertron project wants to replace that code with the package, which is a
  question for that project.
- **Decide in:** the parity fixtures phase.
- **Recommendation:** No, unless the Campertron project asks for it.
