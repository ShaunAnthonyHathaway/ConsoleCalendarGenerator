# Open questions

Questions that are genuinely undecided. When one is settled, move it to [decisions.md](decisions.md) with a new
decision identifier and delete it here; do not renumber the rest.

Recommendations are starting points, not decisions.

One question is open: Q-15. The release of 2.0.1 is complete, on GitHub and on NuGet, and does not depend on
it. The version the question speaks of as 2.0 is 2.0.1, the 2.x version that is on NuGet
([D-85](decisions.md#d-85-200-stays-on-github-only-and-201-replaces-it-for-nuget),
[D-91](decisions.md#d-91-201-is-published-on-nuget-and-100-stays-listed-and-supported)).

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

Q-10 (what the links of a published readme point at) is settled: the README of a released package uses the
immutable tag of its release, `v2.0.1` for 2.0.1, from the change that prepares the release
([D-68](decisions.md#d-68-the-published-readme-of-20-links-to-the-tag-v200)).

Q-11 (whether private vulnerability reporting is turned on) is settled: it is on, and `SECURITY.md` sends
reports there ([D-81](decisions.md#d-81-security-reports-go-through-githubs-private-vulnerability-reporting)).

Q-12 (whether weekday labels should be the same on every operating system) is settled: culture-derived labels
may differ between systems, nothing in the library changes, and supplied labels are the stable option
([D-54](decisions.md#d-54-culture-derived-labels-may-differ-between-systems-and-supplied-labels-are-the-stable-option)).

Q-13 (whether `netstandard2.1` stays a target in 2.0) is settled: it stays
([D-64](decisions.md#d-64-the-target-frameworks-stay-as-they-are-for-20)). The question was given its number
when the 2.0 scope was decided and was never a section of this file, where it had been one of the candidates
of Q-08.

Q-14 (how the publication is authenticated) is settled, and was carried out for 2.0.1: by NuGet Trusted
Publishing, from a workflow that is started by hand. No API key was created and no secret is stored
([D-88](decisions.md#d-88-201-goes-to-nuget-through-trusted-publishing-from-a-workflow-that-is-started-by-hand)).

## Q-15: Is 1.0.0 deprecated on nuget.org after 2.0?

- **Why it matters:** Deprecating 1.0.0 would show a warning to everyone who still restores it and point them
  at 2.0. It is an outward-facing action on a published version.
- **Where it stands:** 2.0.1 is published and verified, so the condition for asking the question holds. 1.0.0
  is listed, supported and not deprecated, and `SECURITY.md` says so
  ([D-91](decisions.md#d-91-201-is-published-on-nuget-and-100-stays-listed-and-supported)).
- **Evidence needed:** Shaun's choice.
- **Decide in:** a step of its own. It needs his separate, explicit approval, and it is made on nuget.org:
  nothing in the repository can do it.
- **Recommendation:** None yet.

Q-16 (where the candidate is built) is settled: it is the Linux `release-bundle` artifact of the first push run
for the release commit, in its first attempt, and nothing else
([D-79](decisions.md#d-79-the-candidate-is-the-linux-bundle-of-the-first-push-run-for-the-release-commit)).
