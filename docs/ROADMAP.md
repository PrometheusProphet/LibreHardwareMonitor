# Computer Vitals Roadmap

Status: canonical mutable roadmap and prototype-state owner

[PROJECT.md](PROJECT.md) owns the stable product, fork, safety, and deferred
product-decision contract. This document records implementation state and may
change as evidence is delivered. Detailed research artifacts remain
non-normative supporting evidence.

## Current prototype state

The current prototype includes the completed fork foundation plus:

- a WPF CPU/GPU temperature view with explicit unavailable and unsupported
  states, bounded session history, threshold persistence and hysteresis, local
  notification registration, a self-contained .NET publish profile, and a
  read-only AMD CPU-access diagnostic;
- local parent-child inventory, installed signed-driver evidence, explicit
  unknown firmware status, and an official Dell support link only for an exact
  `Dell Dock WD19S` display-name match;
- reusable selected-parent navigation with one-action return to the full
  Windows device tree;
- a session-only controlled-test and intervention timeline that cannot record a
  confirmed-root-cause claim; and
- session-only cautious recommendations and typed source claims that keep
  conflicts, unknowns, and scope limits visible.

It does not claim broad hardware compatibility, durable update availability,
release readiness, or successful notification delivery on every machine. Real
notification delivery and CPU temperature access with PawnIO installed remain
targeted hardware-evidence gaps. The exact WD19S match can open Dell's live
Drivers & Downloads catalog, but update status remains unknown because local
inventory does not expose comparable firmware-component versions and the
product does not query or compare vendor catalogs.

## Roadmap

This is an evidence-gated sequence, not a calendar or release promise. Unknown,
unsupported, contradictory, and failed states remain first-class results.

### Phase 0 — fork foundation and reproducible prototype

**Status: complete.**

The WPF .NET 10 shell, reusable sensor-library boundary, synthetic tests,
self-contained local publish profile, product documentation, reproducible
upstream baseline, and separable fork-specific work are delivered.

### Phase 1 — trustworthy temperature health

**Status: implemented; targeted hardware evidence remains open.**

Current/stale/unavailable/unsupported temperature states, source and freshness,
bounded session range, persistent threshold and hysteresis, explainable alert
content, and selectable diagnostics are implemented. Real Windows notification
delivery and named CPU-access evidence remain open; PawnIO is not installed or
elevated automatically to close them.

### Phase 2 — connected hardware investigation

**Status: selected-parent navigation complete; broader coverage remains
evidence-gated.**

The product preserves local device relationships and signed-driver evidence,
shows unknown firmware truthfully, and supports reusable selected-parent
navigation. Exact-match mappings expand only one vendor/model at a time with a
recorded source, owner, and update strategy.

### Phase 3 — incident evidence and controlled troubleshooting

**Status: session-only controlled-test assistant complete; retention decision
deferred.**

The assistant prepares one changed variable with its baseline, constant
conditions, user-performed action, observation stage, and optional explicit
device link, then records bounded session evidence. Persistence requires a
separate product decision about retention, location, deletion, and export.

### Phase 4 — evidence-gated update intelligence

**Status: WD19S evidence spike complete; update checker is a no-go on current
evidence.**

The non-normative
[WD19S vendor-evidence spike](research/2026-08-03-wd19s-vendor-evidence-spike.md)
found useful official Dell metadata surfaces but did not establish permitted
automated reuse or reliable least-privilege local component versions. Reconsider
an update checker only after obtaining:

- a stable source whose terms permit the intended local read-only use;
- reliable local component identity and comparable version evidence;
- licensed or synthetic applicability, normalization, prerequisite,
  supersedence, freshness, conflict, and failure fixtures; and
- named hardware evidence without retained raw identifiers or personal logs.

Until then, update availability remains unknown. Downloading, launching
installers, flashing, rollback, and automatic updates remain outside this phase.

### Phase 5 — local diagnostic recommendations

**Status: cautious recommendation and bounded session-only typed
reconciliation complete; raw, external, durable, and real-world reconciliation
remain gated or out of scope.**

The current session surface derives cautious recommendations from explicitly
scoped observations, inventory, vendor provenance, and temperature context.
Confidence remains `Insufficient` or `Limited`; no raw logs, external records,
durable state, causal verdict, status verdict, or automated action is implied.

### Phase 6 — product hardening and release decision

**Status: internal developer-preview decisions recorded; release and hardening
evidence remains open.**

The [developer-preview posture](release/DEVELOPER-PREVIEW.md) is an unsigned,
unpackaged, local `win-x64` self-contained .NET publish evaluation only. The
[dated publish evidence](release/2026-08-03-local-win-x64-publish-availability.md)
does not establish release readiness. Crash recovery, packaging and upgrade,
clean-machine install/uninstall, accessibility, and repeatable release checks
remain unverified or incomplete.

### Phase 7 — post-release capability decisions

**Status: intentionally unplanned.**

Additional sensors, longer history, export, optional privileged bridges, and
hardware control need focused proposals and safety/evidence plans. Fan control,
overclocking, voltage changes, shutdown actions, remote monitoring, accounts,
sync, telemetry, and cloud features are not default follow-ons.

## Completed immediate sequence

1. Synthetic notification-content evidence is in place; real notification and
   intentionally approved CPU-access evidence remain open.
2. Reusable selected-parent inventory navigation is delivered.
3. The one-variable controlled-test assistant is delivered with session-only
   data and user-performed actions.
4. The WD19S vendor-evidence spike is complete with a no-go disposition for an
   update checker; update availability remains unknown.
5. The internal developer-preview posture is recorded without settling
   retention, export, branding, vendor actions, hardware control, or release.
