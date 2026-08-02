# Governance Derivation

Status: non-normative design evidence

This fork uses Workflow Optimizer as a framework source to compile down, not as
a policy bundle to copy. Normative rules live only in `AGENTS.md` and
`docs/PROJECT.md`.

## Sources inspected

- Workflow Optimizer: `main` at
  `f5b8a77a797f74a3e5684f895e392e4a6256dd1a`
- Workflow sources: `AGENTS.md`,
  `docs/governance/RULE_CHANGE_WORKFLOW.md`, and
  `docs/program/WORKFLOW_OPTIMIZER_PROGRAM.md`
- Libre Hardware Monitor upstream baseline:
  `d6cb2604646287da7a21e414b6539055cefb6a81`
- Compilation date: 2026-08-02

No Workflow Optimizer prose, product source, test, schema, or automation was
imported. The rules here are target-written expressions of the smaller
Computer Vitals boundary.

## Principles retained

| Framework principle | Compiled Computer Vitals behavior |
| --- | --- |
| Current authority outranks historical artifacts | Current request, then `docs/PROJECT.md`, then current source and Git state |
| Truthful missing and failed states | Failed or unsupported sensors remain visible and never become fabricated healthy values |
| Explicit gates for risky expansion | Hardware writes, privileged services, telemetry, cloud features, and publication require current authority |
| Proportional execution and proof | A five-step evidence ladder scales from docs to release without making every change run every check |
| One owner per enduring rule | Product boundaries live in `docs/PROJECT.md`; execution defaults live in `AGENTS.md` |
| Rules need a demonstrated reason | Rule changes require a concrete miss, ambiguity, unsafe default, changed boundary, or explicit decision |
| Protect secrets and private data | Machine identifiers, personal sensor history, dumps, and signing material are excluded from source |
| Do not overstate evidence | Hardware coverage, validation, delivery, and acceptance claims must name what was actually proved |

## Fork-specific additions

- preserve upstream history, license, notices, and attribution;
- keep official upstream separate from the product fork;
- review rather than automatically absorb upstream changes; and
- keep product behavior separable from sensor-engine fixes where practical.

## Machinery intentionally compiled out

- phase registries and transfer contracts;
- coordinator/executor task protocols and independent audit roles;
- mandatory full-verification batching checkpoints;
- durable closeout receipts and personal notification adapters;
- donor import registries and bespoke prose-policy validators;
- deployment-provider exceptions and hosted identity boundaries;
- tenant, workspace, capability, schema, and migration governance; and
- governance indexes that only point at other governance documents.

Those mechanisms address Workflow Optimizer's scale and risks, not this
project's current needs. They should not be introduced speculatively. If a
future failure demonstrates the need for one, add the smallest targeted control
to the existing owner and record why.
