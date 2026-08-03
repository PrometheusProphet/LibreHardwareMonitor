# Computer Vitals developer preview

Status: internal decision record for local evaluation only

## Decided now

Computer Vitals is a technical, developer-preview label only. It is not
accepted public branding, a trademark assertion, or a support promise.

The selected evaluation posture is an unsigned, unpackaged, local `win-x64`
self-contained .NET publish evaluation only. The tracked profile and WPF
project currently state:

- `net10.0-windows10.0.19041.0`;
- `win-x64`;
- `SelfContained=true`;
- `WindowsAppSDKSelfContained=false`;
- single-file `false`; and
- WPF `WindowsPackageType=None`.

Those settings describe an evaluation profile, not a public release artifact.
In particular, `WindowsAppSDKSelfContained=false` does not establish complete
Windows App Runtime bundling or deployment compatibility. Windows App Runtime
and deployment compatibility, plus clean-machine behavior, remain unverified
prerequisites.

The preview remains a local diagnostic evaluation. Its bounded Incident
Timeline and typed-source reconciliation data exist in session memory only and
are discarded when the window closes.

## Not evidenced / not claimed

This decision does not authorize or claim a release channel, installer,
package publication, signing, auto-update, upgrade behavior, or public
release. It does not add telemetry, accounts, cloud behavior, export, or
persistent diagnostic retention; retention and export remain deferred.

No crash, dump, remote-reporting, or recovery evidence is recorded. No
accessibility conformance, assistive-technology, or visual acceptance claim is
made; accessibility is evaluation intent only.

There is no public or remote support promise, repair, fix, or causal promise,
universal compatibility claim, notification-delivery guarantee, or privileged
hardware control. No clean-machine, deployment, upgrade, recovery, or
accessibility evidence has been collected.

## Evidence gaps retained

Phase 1 still lacks a real Windows notification-delivery observation and a
named CPU-access observation using an intentionally approved low-level
capability. PawnIO was not installed or elevated to close either gap.

Phase 4 still lacks comparable local component identity and version evidence,
permission for automated catalog reuse, and a least-privilege local component
source. Vendor update availability remains `Unknown`.

Future public branding, support, deployment, release, vendor-operation, and
hardware-control decisions remain separate decisions. This preview posture
does not activate Phase 7.
