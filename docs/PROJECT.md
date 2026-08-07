# Computer Vitals Project

Status: canonical stable product, fork, and safety owner

Current implementation and roadmap state are owned by [ROADMAP.md](ROADMAP.md).
Detailed research remains non-normative evidence under [research/](research/).

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

## Durable diagnostic evidence contract

- Sensor health distinguishes current, stale, unavailable, and unsupported
  readings and identifies source, freshness, and bounded context.
- Device investigation preserves parent-child, hidden, and composite hardware
  relationships when the operating system exposes them. Manufacturer guidance
  is linked only after reliable manufacturer-and-model matching.
- Intervention evidence distinguishes observed hardware state, available or
  missing evidence, update availability, potential relevance, observed
  remediation, and confirmed root cause. A multi-change remediation is never
  attributed to one component without supporting evidence.
- Conflicting sources are reconciled by scope, detail, and recency rather than
  by treating one summary as automatically authoritative. A pre-Windows failure
  may leave Windows without decisive evidence, and that absence stays visible.
- Without machine-comparable, applicable vendor evidence, current-version and
  update status remain unknown. A support link or vendor importance label is
  not a verified update verdict or a Computer Vitals recommendation.
- Firmware may be detected and explained but is never flashed automatically.
  Any future updater or external-tool workflow requires explicit user
  initiation, applicable evidence, and visible power, restart, encryption,
  compatibility, interruption, and recovery boundaries.

The detailed Dell WD19S source analysis and its current no-go evidence are
owned by the non-normative
[WD19S vendor-evidence spike](research/2026-08-03-wd19s-vendor-evidence-spike.md).

## Deferred product decisions

These remain user-owned until a focused spike or product decision settles them:

- final public product name, branding, trademark posture, and support promise;
- installer, code-signing, update, release channel, and public deployment;
- vendor catalog integrations, update-availability policy, downloads,
  installation, rollback, and firmware execution;
- retention duration and export formats;
- process-level attribution depth;
- remote monitoring, accounts, synchronization, or telemetry; and
- any automatic fan, power, shutdown, or other hardware-control behavior.

Mutable milestone status, completed implementation results, remaining evidence
gaps, sequencing, and current prototype scope belong only in
[ROADMAP.md](ROADMAP.md).
