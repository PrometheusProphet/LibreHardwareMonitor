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

**Status: first useful slice complete; usability and coverage are next.**

Turn the local Windows device tree into an understandable diagnostic surface:

- preserve parent-child groupings, hidden or composite components, installed
  signed-driver evidence, and unknown firmware fields;
- make a recognized parent device and its reported children easy to reach
  without hiding the full tree;
- explain the role of a selected component in plain language; and
- provide official support or catalog guidance only for an exact, reliable
  manufacturer-and-model match.

The next bounded implementation here is reusable local navigation: a compact
filter or focus mechanism for a selected parent device, with a clear return to
the whole inventory. Broaden exact-match mappings only one vendor/model at a
time, with a source, ownership, and update strategy recorded beside each
mapping. Do not represent the current inventory as a universal driver or
firmware database.

### Phase 3 — incident evidence and controlled troubleshooting

**Status: session-only foundation complete; retention decision deferred.**

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

The next step is a controlled-test assistant that proposes one variable at a
time and records what changed, while leaving the user in control of every
restart, cable change, or vendor tool. Before persisting incidents across
sessions, make a separate user decision on retention duration, location,
deletion, and export; session-only data remains the default until then.

### Phase 4 — evidence-gated update intelligence

**Status: discovery and policy work only.**

The product may point an exact match to a manufacturer’s live Drivers &
Downloads page, but it currently reports update availability as unknown. The
next work is a vendor-specific evidence spike, beginning with the WD19S:

- establish whether Dell provides a stable, permitted catalog, API, or signed
  metadata source suitable for local read-only lookup;
- establish a reliable local component identity and firmware-version source
  for each package component, not just the dock display name;
- define version normalization, operating-system and architecture
  applicability, prerequisites, importance, release notes, supersedence, and
  source freshness; and
- prove presentation with synthetic current, outdated, unknown, contradictory,
  and failed fixtures plus a named hardware observation.

Only then can a product state such as **update available** be implemented. A
vendor's own importance label is not yet a Computer Vitals recommendation; a
release is only **potentially relevant** to a symptom when supporting evidence
is displayed. Downloading, launching installers, flashing, rollback, and
automatic updates remain out of scope for this phase.

### Phase 5 — local diagnostic recommendations

**Status: follows successful evidence spikes.**

Combine temperature, inventory, incident, and vendor evidence into clear local
recommendations:

- explain what changed, why it might matter, confidence, evidence scope, and
  the safest next observation or controlled test;
- reconcile installer summaries, detailed logs, Device Manager state, and
  user observations rather than selecting a convenient source; and
- keep a recommendation distinct from a claim that a change will fix a
  symptom.

This phase remains read-only. Any link to a vendor tool opens only on explicit
user action and must show relevant restart, power, encryption, compatibility,
and interruption cautions first.

### Phase 6 — product hardening and release decision

**Status: blocked on explicit product decisions, not implementation effort.**

Before calling the product a release candidate, decide and document the
installer, code-signing, update and release channel, supported Windows
versions, privacy statement, crash-handling policy, accessibility target, and
support boundary. Then add packaging and upgrade tests, clean-machine install
and uninstall evidence, accessibility checks, failure recovery, and a
repeatable release checklist.

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

1. Complete the remaining Phase 1 hardware observations without adding a
   driver automatically.
2. Add the Phase 2 selected-parent navigation refinement, keeping the full
   Windows tree visible and reversible.
3. Build the Phase 3 one-variable-at-a-time controlled-test assistant while
   keeping incident entries session-only.
4. Run the Phase 4 Dell WD19S evidence spike before writing any update-
   availability checker.
5. Pause for the explicit Phase 6 release and data-retention decisions before
   packaging or retaining diagnostics beyond a session.

## Deferred decisions

These remain user-owned until a focused spike or product decision settles them:

- final product name and visual identity;
- installer, code-signing, update, and release channel;
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
