# Computer Vitals Fork Router

This repository is a fork of Libre Hardware Monitor and the source foundation
for a small, local-first Windows diagnostics application. Keep its governance
smaller than the product.

## Authority and ownership

- The user's current request controls the task.
- `docs/PROJECT.md` owns accepted product direction, enduring safety and fork
  boundaries, current scope, and intentionally deferred decisions.
- Current source and Git state override plans, prompts, earlier reviews, and
  the non-normative governance derivation record.
- The inherited `LibreHardwareMonitorLib` and Windows Forms source are the
  current sensor-engine and prototype baseline, not permanent UI authority.
- Computer Vitals is a working technical name, not accepted branding.
- Do not silently settle a deferred decision when it materially changes
  licensing, privilege, packaging, architecture, data handling, or behavior.

## Fork and license discipline

- `origin` is the product fork. `upstream` is the official Libre Hardware
  Monitor repository and is read-only for this project.
- Preserve upstream Git history, `LICENSE`, third-party notices, attribution,
  and source-file obligations. Do not imply that fork-specific behavior is an
  upstream feature.
- Keep sensor-engine fixes separable from product-specific UI and alert work
  when practical so upstream intake and possible contribution remain clear.
- Review upstream changes before integration. Never discard product changes or
  resolve a behavioral conflict by automatically preferring upstream.
- Before copying or adapting source from anywhere else, record its origin,
  revision, license, local changes, and update strategy next to the import.

## Safety and truthful diagnostics

- Keep product hardware interaction read-only unless the current request
  explicitly authorizes a named control feature and its failure behavior.
- Treat every sensor value as fallible input. Missing, unsupported, stale,
  contradictory, or failed readings must remain visible; never replace them
  with invented healthy values.
- Do not claim that a threshold is universally safe. Prefer device-reported
  limits and document any fallback.
- Require persistence and hysteresis for ordinary alerts. Learned baselines may
  add context but must never suppress an absolute critical condition.
- Use least privilege. Administrator access, a service, or a low-level driver
  must be justified by the exact sensor capability that needs it.
- Never commit secrets, signing material, raw machine identifiers, personal
  sensor history, crash dumps, or unrelated private data. Tests use synthetic
  fixtures.

## Changes and external actions

- Inspect the owning source and current Git state before editing. Preserve
  unrelated work.
- Diagnosis authorizes investigation and reporting, not implementation, unless
  the request also asks for a fix.
- Adding telemetry, accounts, cloud storage, remote access, automatic hardware
  control, publishing, signing, package release, or another external mutation
  requires explicit current authority.
- Prefer adapters at product boundaries over invasive sensor-library changes.
  Modify the library when the capability or correctness fix genuinely belongs
  there.

## Proportional proof

Use the smallest evidence that can establish the claimed result:

1. Documentation or repository metadata: inspect the owning documents and run
   whitespace, link, or equivalent focused checks.
2. Pure alert or presentation logic: run focused unit tests for the changed
   owner.
3. Sensor or operating-system adapter: run contract tests with synthetic input,
   then a named hardware check when the claim depends on real hardware.
4. Privilege, persistence, installer, alert delivery, or cross-owner change:
   run affected integration checks.
5. Release, upstream integration, or broad shared-boundary change: run the full
   repository build and test gate.

Report unavailable hardware evidence honestly. Do not add a validator merely
to prove that prose rules exist.

## Rule maintenance and completion

- Change repository rules only for a concrete miss, ambiguity, unsafe default,
  changed enduring boundary, or explicit user decision.
- Put the rule in one normative owner, choose the smallest behavior that
  prevents the problem, and remove superseded wording.
- Ordinary work stays in one task and one coherent change. Add coordination or
  durable process machinery only after a demonstrated need.
- A completed change reports its result, checks, delivery state, blockers, and
  residual uncertainty. Do not overstate hardware coverage, visual acceptance,
  verification, commit, publication, or upstream compatibility.
- Commit and push only when the current request includes that delivery step.
