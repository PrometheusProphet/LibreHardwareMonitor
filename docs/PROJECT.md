# Computer Vitals Project

Status: canonical product, fork, and safety owner

## Objective

Evolve this Libre Hardware Monitor fork into a friendly Windows desktop
diagnostics application that combines useful Task Manager-style context with
physical health readings and understandable local warnings.

The product should reduce interpretation work. It should show what changed,
whether it is unusual, how confident the application is, and what the user can
safely check next.

## Source and fork boundary

- This repository preserves the official Libre Hardware Monitor history and
  uses its library and existing Windows Forms application as the prototype
  foundation.
- `origin` is `PrometheusProphet/LibreHardwareMonitor`; `upstream` is
  `LibreHardwareMonitor/LibreHardwareMonitor`.
- Existing upstream code remains governed by MPL 2.0 and the repository's
  third-party terms. License and attribution files are enduring source
  boundaries, not branding artifacts.
- The sensor library should remain usable independently of the product UI.
  Product-specific alerting, history, explanation, and presentation behavior
  should sit outside hardware-specific implementations when practical.
- Upstream fixes are reviewed and integrated deliberately. Product changes
  should remain understandable across upstream synchronization.

## Accepted implementation choices

- The first product shell uses WPF on .NET 10.
- New fork-specific product modules use MPL 2.0, consistent with the inherited
  repository obligations.

## Product boundaries

- **Windows first and local first.** The core experience works without an
  account, hosted service, remote connection, or telemetry.
- **Read-only first.** The first product release observes hardware. Fan curves,
  overclocking, voltage changes, shutdown actions, and other automatic control
  are later decisions with separate failure and recovery requirements.
- **Truth before completeness.** Unsupported and unreliable readings are
  explicit states. The application does not fabricate normal values or hide a
  sensor failure behind a green status.
- **Layered alerts.** Device limits and persistent absolute thresholds protect
  safety. Learned baselines provide supplemental anomaly context. Both use
  cooldowns and hysteresis to avoid alarm fatigue.
- **Local data minimization.** Store only the history needed for user-visible
  trends and alert evaluation. Hardware identifiers are normalized or omitted
  unless a feature genuinely needs them.
- **Least privilege.** Most of the application should run as the user. Any
  privileged sensor bridge must be narrow, optional where possible, and unable
  to turn a failed read into a hardware write.

## First vertical slice

The first product milestone is one end-to-end, read-only path:

1. discover available CPU and GPU temperature sensors through the inherited
   library;
2. classify each sample as current, stale, unavailable, or unsupported;
3. display current value, recent range, source, and freshness;
4. evaluate a configurable persistent threshold with hysteresis;
5. issue a local Windows notification that explains the trigger; and
6. retain enough local history to show why the notification fired.

The slice passes only with synthetic contract tests and a clearly identified
real-hardware observation. Passing on one machine does not establish broad
hardware compatibility.

## Future use case: update drift and intervention evidence

A real incident established a useful future diagnostic path. With a Dell WD19S
dock attached, repeated Windows restarts completed shutdown but stalled at the
ASRock logo during POST; shutdown followed by a cold start worked. The first
successful restart occurred with the dock disconnected, strongly associating
the dock and host interaction with the failure condition before Windows
started.

The AMD chipset package was updated from 8.03.25.247 to 8.07.16.1035. Four
outdated WD19S components were also updated: the MST controller, two USB hubs,
and embedded controller. After the dock was power-cycled and reconnected, a
restart with the dock attached succeeded. This is an observed successful
remediation, not proof that any one update was the root cause, because the
chipset and dock changes were applied before the final test. A future naturally
occurring successful restart would increase confidence that the result is
durable.

The incident also exposed contradictory evidence. An AMD summary labeled two
components as failed while the detailed Windows Installer log reported success
status codes and Device Manager showed no AMD device errors. Product conclusions
must reconcile source scope, detail, and recency instead of treating one summary
as authoritative. Composite and hidden devices also matter: Windows exposed the
dock as several child USB components, including a Realtek USB Ethernet device
with no Ethernet cable connected.

The eventual product should be able to:

- inventory installed driver and firmware versions and group composite or
  hidden child devices under their parent hardware;
- explain detected components in plain language and link to official
  manufacturer guidance;
- show evidence source, observation time, update age, importance, confidence,
  and conflicting or missing evidence;
- retain a bounded before-and-after intervention timeline;
- distinguish an available update, a recommended update, a change potentially
  relevant to a symptom, an observed remediation, and a confirmed root cause;
- guide controlled one-variable-at-a-time troubleshooting; and
- identify relevant restart or boot patterns while stating when a pre-Windows
  failure leaves Windows without decisive evidence.

The smallest useful implementation slice is read-only local inventory, not an
updater. It should enumerate local device instances, parent-child relationships,
hardware identifiers needed for matching, installed driver provider, version,
and date, plus locally exposed firmware versions. It should present one parent
device with understandable child roles, record the source and freshness of each
field, and provide an official manufacturer support link only when the
manufacturer and model match is reliable. Until authoritative current-version
evidence exists, the product should say that current-version and update status
are unknown.

Reliable update-availability detection requires more evidence before
implementation:

- stable official vendor catalogs, APIs, or signed metadata that may be queried
  under acceptable terms;
- exact applicability keys such as model, hardware and compatible IDs,
  revision, operating system, and architecture;
- a defined way to compare vendor version formats across driver, firmware, and
  composite-device components;
- vendor release date, importance, prerequisites, release notes, supersedence,
  and source retrieval time;
- provenance and conflict rules for local inventory, installer results,
  detailed logs, Device Manager state, and vendor evidence; and
- synthetic fixtures plus hardware observations that cover current, outdated,
  unsupported, unknown, contradictory, and failed states.

Firmware drift may be detected and explained, but Computer Vitals must never
flash firmware automatically. An eventual update workflow requires explicit
user initiation and must show power, restart, encryption, compatibility, and
interruption risks before any external tool or installer runs. An update must
not be claimed to fix a symptom without evidence, and a multi-change remediation
must not be attributed to one component.

## Recommended product roadmap

This is an evidence-gated sequence, not a calendar or release promise. A phase
becomes implementation authority when the user starts it; a later phase does
not silently settle a deferred decision. At every phase, unknown, unsupported,
contradictory, and failed states remain first-class results.

### Phase 0 — fork foundation and reproducible prototype

**Status: complete.**

The fork has a WPF .NET 10 product shell, a reusable sensor-library boundary,
synthetic tests for fork-specific behavior, a self-contained local publish
profile, product documentation, and a small local-first governance boundary.

The practical exit condition is already met: the unchanged upstream baseline
was built reproducibly, fork-specific work is separable, and the prototype can
be published locally without an installer or release channel.

### Phase 1 — trustworthy temperature health

**Status: implemented; targeted hardware evidence remains open.**

Deliver the narrow daily-use view of CPU and GPU temperature health:

- current, stale, unavailable, and unsupported samples with source, freshness,
  and bounded session range;
- persistent threshold and hysteresis behavior with an explainable alert;
- session-only history sufficient to explain an alert; and
- readable, selectable diagnostic text rather than invented normal values.

Before expanding sensors or alerts, close the remaining focused evidence gaps:
exercise real notification delivery with a synthetic alert, and capture a
named hardware result for CPU access when an intentionally user-approved
low-level capability is available. PawnIO must not be installed or elevated
automatically merely to close that gap.

### Phase 2 — connected hardware investigation

**Status: selected-parent navigation complete; broader coverage remains
evidence-gated.**

Turn the local Windows device tree into an understandable diagnostic surface:

- preserve parent-child groupings, hidden or composite components, installed
  signed-driver evidence, and unknown firmware fields;
- make a recognized parent device and its reported children easy to reach
  without hiding the full tree;
- explain the role of a selected component in plain language; and
- provide official support or catalog guidance only for an exact, reliable
  manufacturer-and-model match.

Reusable local navigation now lets any selected parent with reported children
show its complete subtree, with one action restoring the unchanged full
inventory. Broaden exact-match mappings only one vendor/model at a time, with a
source, ownership, and update strategy recorded beside each mapping. Do not
represent the current inventory as a universal driver or firmware database.

### Phase 3 — incident evidence and controlled troubleshooting

**Status: session-only controlled-test assistant complete; retention decision
deferred.**

Make an incident understandable across conditions, interventions, outcomes,
and evidence:

- record conditions such as dock attached or detached, a restart or cold-start
  action, and succeeded, stalled, failed, or unknown outcomes;
- identify whether the observation was pre-Windows or within Windows so missing
  Windows evidence is visible;
- explicitly link a recorded device only when the user says the observation
  involved it; and
- distinguish potentially relevant, observed remediation, and confirmed root
  cause without inferring the last category.

The controlled-test assistant now prepares exactly one variable with its
baseline, constant conditions, user-performed action, observation stage, and
optional explicit device link, then records the evidence and result in the
existing bounded session timeline. It rejects a multi-variable attestation,
performs no restart, cable change, hardware control, or vendor-tool action, and
cannot record a confirmed-root-cause claim. Before persisting incidents across
sessions, make a separate user decision on retention duration, location,
deletion, and export; session-only data remains the default until then.

### Phase 4 — evidence-gated update intelligence

**Status: WD19S evidence spike complete; update checker is a no-go on current
evidence.**

The product may point an exact match to a manufacturer’s live Drivers &
Downloads page, but it reports update availability as unknown. The dated
[WD19S vendor-evidence spike](research/2026-08-03-wd19s-vendor-evidence-spike.md)
found useful official Dell metadata surfaces but did not establish either
permission for the proposed automated reuse or a reliable, least-privilege
local source of comparable dock component versions. The following evidence is
still required before an update checker can be proposed:

- a stable catalog, API, or signed metadata source with terms that permit the
  intended local read-only use;
- reliable local component identity and firmware-version evidence for each
  compared package component, not just the dock display name;
- licensed or synthetic fixtures covering applicability, version
  normalization, prerequisites, release notes, supersedence, freshness,
  conflicts, and failures; and
- a named hardware observation that reports identity and version capability
  without committing raw identifiers or personal logs.

Only then can a product state such as **update available** be implemented. A
vendor's own importance label is not yet a Computer Vitals recommendation; a
release is only **potentially relevant** to a symptom when supporting evidence
is displayed. Downloading, launching installers, flashing, rollback, and
automatic updates remain out of scope for this phase.

### Phase 5 — local diagnostic recommendations

**Status: cautious recommendation and bounded session-only typed
reconciliation complete; raw, external, durable, and real-world
reconciliation remain gated or out of scope.**

Combine temperature, inventory, incident, and vendor evidence into clear local
recommendations:

- explain what changed, why it might matter, confidence, evidence scope, and
  the safest next observation or controlled test;
- reconcile installer summaries, detailed logs, Device Manager state, and
  user observations rather than selecting a convenient source; and
- keep a recommendation distinct from a claim that a change will fix a
  symptom.

Incident Timeline now derives a selectable, session-only cautious
recommendation from the selected observation plus explicitly scoped local
inventory, vendor-provenance, and temperature context. Confidence is limited to
`Insufficient` or `Limited`; conflicts and unknowns remain visible, and the
card proposes only a user-performed next observation.

The same bounded session surface now retains at most 30 timeline entries and
30 typed source claims. A retained entry receives a stable bounded observation
key; a claim contains only a typed source, disposition, stage, observation
time, and that key. Reconciliation considers only the selected entry's key and
copies only the selected entry's already explicit device reference exactly, or
no device reference. Typed `Unknown`, conflicts, and scope limits remain visible. The
surface does not import raw logs or files, infer an identity from inventory
presence or selection, retain data beyond the session, operate a vendor path,
or return a causal, status, or action verdict. It does not claim that actual
installer, Device Manager, or other external records were reconciled.

This phase remains read-only. Any link to a vendor tool opens only on explicit
user action and must show relevant restart, power, encryption, compatibility,
and interruption cautions first.

### Phase 6 — product hardening and release decision

**Status: internal developer-preview decisions recorded; release and hardening
evidence remains open.**

The selected [developer-preview posture](release/DEVELOPER-PREVIEW.md) is an
unsigned, unpackaged, local `win-x64` self-contained .NET publish evaluation
only. The dated [local win-x64 publish availability and evidence](release/2026-08-03-local-win-x64-publish-availability.md)
records the existing profile, authorized local restore, full Release x64
build/test, fresh ignored-output manifest, and unsigned primary executable
status. Expected startup, sensor-refresh, notification, and inventory-operation
failures now have fixed local presentation that keeps unavailable data and
partial driver evidence explicit. It is not a release candidate or a
release-completion claim. Unexpected process crashes, restart and recovery,
dumps or reporting, packaging and upgrade tests, clean-machine install and
uninstall evidence, accessibility checks, and a repeatable release checklist
remain unverified or unperformed.

No package publication, signing, auto-update, telemetry, account, cloud, or
remote-support feature is implied by this roadmap. Each requires separate
current authorization.

### Phase 7 — post-release capability decisions

**Status: intentionally unplanned.**

Possible future directions include additional sensor families, longer local
trend history, export, selective optional privileged sensor bridges, and
eventually hardware control. Each needs its own focused proposal, safety model,
failure behavior, retention impact, and evidence plan. Fan control,
overclocking, voltage changes, shutdown actions, remote monitoring, accounts,
sync, telemetry, and cloud features are not default follow-ons.

### Recommended immediate sequence

The bounded immediate implementation sequence is complete:

1. Synthetic notification-content evidence is in place. Real Windows
   notification delivery and CPU access with an intentionally approved
   low-level capability remain unobserved; PawnIO was not installed or
   elevated.
2. Reusable selected-parent inventory navigation is delivered, preserving a
   one-action return to the unchanged full Windows tree.
3. The one-variable-at-a-time controlled-test assistant is delivered with
   session-only incident data and user-performed actions.
4. The WD19S vendor-evidence spike is complete with a no-go recommendation for
   an update checker; update availability remains unknown.

The internal developer-preview posture is recorded in
[DEVELOPER-PREVIEW.md](release/DEVELOPER-PREVIEW.md). It does not settle
retention or export, final public branding, vendor actions, hardware control,
or a future release reversal, and it does not activate Phase 7.

## Deferred decisions

These remain user-owned until a focused spike or product decision settles them:

- final public product name, branding, trademark posture, and support promise;
- installer, code-signing, update, release channel, and public deployment;
- vendor catalog integrations, update-availability policy, downloads,
  installation, rollback, and firmware execution;
- retention duration and export formats;
- process-level attribution depth;
- remote monitoring, accounts, synchronization, or telemetry; and
- any automatic fan, power, shutdown, or other hardware-control behavior.

## Current prototype state

The current prototype includes the completed fork
foundation plus a WPF CPU/GPU temperature view, explicit unavailable and
unsupported states, bounded session history, threshold persistence and
hysteresis, local-notification registration, a self-contained .NET publish
profile, and a read-only AMD CPU-access diagnostic. The active connected-
hardware features add local parent-child inventory, installed signed-driver
evidence, explicit unknown firmware status, and an official Dell support link
only for an exact `Dell Dock WD19S` display-name match. It does not claim broad
hardware compatibility, durable update availability, release readiness, or
successful notification delivery on every machine. Real notification delivery
and CPU temperature access with PawnIO installed remain targeted hardware
evidence gaps. The intervention timeline is session-only: it records
concise local conditions, actions, outcomes, evidence sources, and qualified
conclusions, then discards them when closed. It cannot record a confirmed root
cause claim. When that exact Dell dock is recognized, its reported subtree is
the initial inventory view; the full Windows tree remains one click away. A
timeline observation may explicitly link that recognized device and identify a
pre-Windows stage, but the record never infers a relationship from device
presence or treats Windows logs as decisive for POST-stage failures.
The exact WD19S match also provides a manual link to Dell's live Drivers &
Downloads catalog. It still reports update status as unknown, because the local
inventory does not expose comparable firmware component versions and the
product does not query or compare vendor catalogs.
