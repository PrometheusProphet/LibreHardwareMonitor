# Computer Vitals Fork Router

This is the Computer Vitals fork of Libre Hardware Monitor: a local-first Windows
diagnostics foundation. `docs/PROJECT.md` owns stable product direction and
enduring safety, fork, and deferred-decision boundaries. `docs/ROADMAP.md` owns
mutable implementation, milestone, roadmap, and current-prototype state.
Current source and Git state define implementation truth; Computer Vitals is a
working technical name, not accepted branding.

`origin` is the product fork and `upstream` is read-only official Libre Hardware
Monitor. Preserve history, `LICENSE`, notices, attribution, and source-file
obligations; keep upstream sensor fixes separable from product UI/alert work.
Record source revision, license, local changes, and update strategy before
adapting external code.

Hardware interaction is read-only unless an exact request authorizes a named
control feature. Sensor values remain fallible: show missing, unsupported, stale,
contradictory, and failed readings honestly. Prefer device limits, persistence,
and hysteresis; never let learned baselines suppress an absolute critical state.
Privilege, services, and drivers require a named capability justification.

Use the product proof ladder: docs get focused document checks; pure logic gets
unit proof; adapters get synthetic contract tests then named hardware evidence;
privilege/persistence/installer/cross-owner changes get affected integration;
release or upstream integration gets the full gate. Report absent hardware
evidence. Push only the existing `origin` product branch when the fork delivery
owner permits it; never push `upstream`.
