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

## Deferred decisions

These remain user-owned until a focused spike or product decision settles them:

- final product name and visual identity;
- retain Windows Forms, adopt WinUI, WPF, Avalonia, Tauri, or use another UI;
- license treatment for new fork-specific product modules, consistent with all
  inherited obligations;
- installer, code-signing, update, and release channel;
- retention duration and export formats;
- process-level attribution depth;
- remote monitoring, accounts, synchronization, or telemetry; and
- any automatic fan, power, shutdown, or other hardware-control behavior.

## Current milestone

The fork-foundation milestone is complete when the GitHub fork and local clone
are connected to explicit `origin` and `upstream` remotes, the compact
governance owners are committed to the fork, and the working tree is clean. It
does not claim product implementation, hardware compatibility, packaging, or
release readiness.
