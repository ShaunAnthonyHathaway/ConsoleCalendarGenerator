# Open questions

Questions that are genuinely undecided. When one is settled, move it to [decisions.md](decisions.md) with a new
decision identifier and delete it here; do not renumber the rest.

Recommendations are starting points from the Phase 0 audit, not decisions.

## Q-01: What are the modern API's public types and signatures?

- **Why it matters:** Public names are permanent once published. Everything in
  [architecture.md](architecture.md#approved-direction-planned-not-implemented) is still unnamed.
- **Evidence needed:** A concrete design per layer, reviewed against what Campertron's `Calendar.Lines` needs
  and against what a general consumer needs. Includes the defaults for culture and first weekday, and how dates
  are passed (`DateTime` works on both targets; `DateOnly` does not exist on `netstandard2.1`).
- **Decide in:** each implementation phase, for the types that phase adds, before they are merged.
- **Recommendation:** Decide layer by layer rather than up front, and keep each phase's public surface as small
  as it can be.

## Q-02: Does the modern API render a whole year?

- **Why it matters:** The legacy `GenerateYear` only stacks twelve months. A modern year view invites layout
  choices (stacked, or several months side by side) that add real complexity.
- **Evidence needed:** Whether any consumer wants more than twelve stacked months. Campertron does not render
  years.
- **Decide in:** the rendering phase, or later.
- **Recommendation:** Start with months only. The legacy facade can produce a year from months without a public
  year API.

## Q-03: How is the title expressed?

- **Why it matters:** Three needs meet here: the legacy title with its three-step indent, Campertron's
  year-month pattern followed by a legend, and a sensible modern default.
- **Evidence needed:** Whether a format string plus fixed text covers all three, or whether callers need to
  supply the title themselves, and how alignment (left, centered, legacy) is chosen.
- **Decide in:** the rendering phase.
- **Recommendation:** Prefer declarative options over a callback if they are enough.

## Q-04: What happens when one date has more than one state?

- **Why it matters:** A cell shows one marker and one color.
- **Evidence needed:** Whether any real consumer has overlapping states. Campertron has a single state.
- **Decide in:** the states and output phase.
- **Recommendation:** One state per day, with a simple, documented rule for duplicates.

## Q-05: When do legacy members become obsolete?

- **Why it matters:** `[Obsolete]` produces a warning in every consumer's build, and consumers that treat
  warnings as errors would fail. It also needs a replacement to point to.
- **Evidence needed:** A finished replacement API, and a view on whether warning existing users during 1.x is
  acceptable.
- **Decide in:** the legacy facade phase, or the 1.x release decision.
- **Recommendation:** None yet. Until it is decided, add no `[Obsolete]` attributes.

## Q-06: How is the compatibility profile for titles expressed?

- **Why it matters:** The direction is decided ([D-15](decisions.md#d-15-modern-titles-match-the-gregorian-grid)):
  modern titles match the Gregorian grid, with a compatibility profile for exact parity. The mechanism is not.
  Campertron's current title follows the culture's own calendar.
- **Evidence needed:** The title design from Q-03, and how parity fixtures behave under cultures with a
  non-Gregorian calendar.
- **Decide in:** the rendering phase, and confirmed by the parity fixtures.
- **Recommendation:** Correct by default, exact parity as an explicit opt-in.

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
