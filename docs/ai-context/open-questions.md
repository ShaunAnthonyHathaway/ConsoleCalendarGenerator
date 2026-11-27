# Open questions

Questions that are genuinely undecided. When one is settled, move it to [decisions.md](decisions.md) with a new
decision identifier and delete it here; do not renumber the rest.

Recommendations are starting points, not decisions.

Three questions are open: Q-11, Q-14 and Q-15. All three belong to the release. The implementation and the
documentation of 2.0 are complete and depend on none of them.

Q-01 (the modern API's public types and signatures) is settled for every planned layer: layout in D-25 to
D-28, rendering in D-30 to D-37, and states, the theme and output in
[D-38](decisions.md#d-38-a-day-state-is-a-name-and-a-visible-marker) to
[D-43](decisions.md#d-43-the-console-writer-restores-the-callers-colors-and-serializes-its-own-writes).

Q-02 (whether the modern API renders a whole year) is closed as deferred: not in 2.0, and not until a consumer
provides evidence that it is wanted
([D-63](decisions.md#d-63-the-modern-api-is-unchanged-for-20-and-renders-single-months)).

Q-03 (how the title is expressed) is settled:
[D-34](decisions.md#d-34-the-title-is-a-date-format-string-a-suffix-and-an-alignment).

Q-04 (more than one state on a date) is settled: one state per day, and the first entry wins
([D-39](decisions.md#d-39-one-state-per-day-and-the-first-entry-wins)).

Q-05 (when legacy members become obsolete) is settled: not in 2.0
([D-62](decisions.md#d-62-the-legacy-class-keeps-every-signature-and-nothing-is-obsolete)).

Q-06 (a compatibility profile for titles) is settled: there is none, and none is needed
([D-45](decisions.md#d-45-no-public-compatibility-profile)).

Q-07 (what ships in 1.x) is settled: nothing is published before 2.0
([D-55](decisions.md#d-55-the-next-published-version-is-20)).

Q-08 (what exactly changes in 2.0) is settled: five corrections of the legacy class and nothing else
([D-56](decisions.md#d-56-20-corrects-exactly-five-legacy-behaviors-and-protects-everything-else), with the
five in D-57 to D-61). The legacy class and its namespace remain, and so does `netstandard2.1`.

Q-09 (parity fixtures for Campertron's unused value stream) is settled: there are none
([D-47](decisions.md#d-47-campertron-parity-is-shown-with-recorded-fixtures)).

Q-10 (what the links of a published readme point at) is settled: the README of the 2.0 package uses the
immutable tag `v2.0.0`, from the change that prepares the release
([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)).

## Q-11: Is private vulnerability reporting turned on?

- **Why it matters:** `SECURITY.md` has to tell people how to report a problem. GitHub's private vulnerability
  reporting is off for the repository, so the file says there is no private channel and asks for a public
  issue without details.
- **Evidence needed:** Shaun's choice. It is a repository setting, which needs his approval each time.
- **Decide in:** the repository-setting phase, which Shaun approves separately. It stays open until then; no
  setting is changed by any other phase.
- **Recommendation:** Turn it on before 2.0 is published and point `SECURITY.md` at it in the same change.
- **When it is settled:** `SECURITY.md` is changed in the same separately approved change that turns the
  setting on, never before, so that the file is true at every commit. The sentence to replace is the one that
  says the repository has no private channel; `PublicDocumentationTests` requires that sentence today and
  changes with it. It is an item of the
  [release preparation checklist](packaging-and-releases.md#release-preparation-checklist).

Q-12 (whether weekday labels should be the same on every operating system) is settled: culture-derived labels
may differ between systems, nothing in the library changes, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).

Q-13 (whether `netstandard2.1` stays a target in 2.0) is settled: it stays
([D-64](decisions.md#d-64-the-target-frameworks-stay-as-they-are-for-20)). The question was given its number
when the 2.0 scope was decided and was never a section of this file, where it had been one of the candidates
of Q-08.

## Q-14: How is the publication authenticated?

- **Why it matters:** 2.0 is to be published by a push of the exact validated files
  ([D-69](decisions.md#d-69-20-is-published-by-hand-from-the-validated-artifacts),
  [D-72](decisions.md#d-72-the-release-is-200-built-once-and-published-as-the-files-that-were-validated)), which
  Claude is expected to perform once Shaun has approved it and a secure credential is available. The push needs
  a credential for nuget.org, and which kind, how narrowly it is scoped, how long it lives and how it reaches
  the session decide what a leaked one could do.
- **Evidence needed:** What nuget.org offers at the time, how the package's account is set up, and that the
  credential works, which has not been verified.
- **Decide in:** the release phase. Nothing in the repository depends on it, and no secret is stored here.
- **Recommendation:** None yet.

## Q-15: Is 1.0.0 deprecated on nuget.org after 2.0?

- **Why it matters:** Deprecating 1.0.0 would show a warning to everyone who still restores it and point them
  at 2.0. It is an outward-facing action on a published version.
- **Evidence needed:** Shaun's choice, once 2.0 is published and verified.
- **Decide in:** the release phase, after publication. It needs its own approval.
- **Recommendation:** None yet.
