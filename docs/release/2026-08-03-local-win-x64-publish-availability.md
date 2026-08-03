# Local win-x64 publish availability — 2026-08-03

## Scope

This dated internal developer-preview record applies to source commit
`66f3587943849473f55990d07de8c9c0004b940f`. It records local availability of
the existing `LocalDotNetSelfContained` profile only. The profile declares
`Release`, `win-x64`, `SelfContained=true`,
`WindowsAppSDKSelfContained=false`, and single-file `false`; the WPF project
targets `net10.0-windows10.0.19041.0` with `WindowsPackageType=None`.

## Checked availability

- Core Release x64 tests passed 75 of 75. This is Core-only evidence.
- The no-restore WPF publish gate was unavailable because the locally restored
  assets did not include the profile's target. No restore or download was
  attempted.
- No existing output was used as publish evidence. No launch, installation,
  signing, signature inspection, manifest, package, archive, publication, or
  upload occurred.

Future pre-provisioned and separately approved assets must rerun the full
local gate and publish before any publish-output fact can be recorded.

## Not evidenced or claimed

This record does not evidence or claim clean-machine, Windows App Runtime,
deployment, runtime, launch, notification delivery, hardware, installation,
upgrade, accessibility, crash, restart, recovery, dump, reporting, signing,
release, or support results. It does not establish reproducibility, trust,
compatibility, release suitability, or a public release.

The Phase 1 notification and CPU-access gaps remain open: real notification
delivery and CPU access through an intentionally approved low-level capability
remain unobserved, and PawnIO was not installed or elevated. The Phase 4 vendor
availability result remains `Unknown`; no vendor checker, query, download, or
installer operation is established here.
