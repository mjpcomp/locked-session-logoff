<!-- Created: 2026-10-15 10:24 America/Los_Angeles | Path: /SERVER-PLAN.md | Repository: mjpcomp/locked-session-logoff | Owner: maintainers | Permissions: public read/maintainer write | Purpose: durable central-server, administrative security and reporting plan | Arguments: none | Usage: read with README.md, TASKS.md, IDLE-LOCK.md, INTEGRATION.md, SERVICE.md and BRANDING.md before changes -->
# Central server and administration plan

## Status and authority

Approved direction being recorded as requirements, not implemented capability. No central server, authentication, reporting, remote commands, admin UI, enrollment, or endpoint enforcement release exists yet. Latest known code increment is c6abed0c24af74de83f5bca44194bf623b7a5b40 (disabled service host and console dry-run); its build/device results are not established by this note. Earlier x64 probe console lock/unlock was maintainer-tested; 14 core synthetic tests passed previously. Do not transfer that evidence to new code.

These notes preserve the central administration requirements across sessions. Consult all linked planning files. Next README/TASKS consolidation must link these documents and reconcile current source status; do not treat earlier no-service-source paragraphs as current after the service increment.

## Hosting and components

Target a supported Ubuntu LTS host with Nginx, PHP-FPM, Laravel, PostgreSQL, a database-backed queue worker supervised by systemd, and Laravel scheduled processing. Use server-rendered Blade pages with lightweight refresh behavior initially; no separate SPA or Redis dependency is required by the proposal. Pin supported framework/runtime versions after compatibility review, not from a dated example. Windows service/session helper remain C#/.NET 10. Versioned authenticated HTTPS JSON connects them.

Provide documented install/update/rollback, migrations, service units, reverse-proxy configuration, request flow, log locations, health checks, backup/restore and troubleshooting. Exact server paths and production commands are to be documented when implemented. Public examples must be organization-neutral; credentials, real account/device names, tenant settings and domains remain deployment configuration outside Git.

## Local independence and reporting

Installation enables neither local enforcement nor network reporting. Administrator explicitly configures endpoint/enrollment and local monitoring. Reporting outages must not block local policy evaluation. Use outbound agent HTTPS only; no inbound workstation control listener. Reporting off remains a supported standalone deployment.

Proposed initial reporting cadence: heartbeat every 60 seconds plus significant events, configurable and bounded. Use server receipt time for freshness; retain observation time separately. Unique event IDs, boot/service identifiers, ordering, duplicate handling, bounded local buffering, backoff/jitter and payload limits are required. Old queued reports must not overwrite newer state or imply a fresh heartbeat. No full private logs uploaded by default.

Missing heartbeat means not reporting, not proven powered off. Show last-known observations with freshness. Distinguish logged off/no users, disconnected sessions, idle input, locked state, sleep, unknown and unreachable where evidence permits. Never fabricate idle measurements before the helper implements them.

## Dashboard data and privacy

Show computer/friendly name, stable enrolled ID, version, architecture, OS uptime separately from service uptime, last received heartbeat, agent health/errors, monitoring enabled/disabled, and dry-run/enforcement. Support authorized username reporting, configurable per deployment; never expose personal session details publicly. Show multiple sessions independently with state/age/idle/lock duration where measured.

Show retention policy and unused-day threshold, maintenance task next/last/result, pending commands, and action history. Differentiate configured/requested/delivered/installed/executed/completed/failed/unknown. Native profile cleanup is an eligibility evaluation at restart, not guaranteed deletion or inherently one-time cleanup. Distinguish pending restart from confirmed new boot and action request from successful result.

Configurable retention, audit access, deletion/export rules and backups are mandatory. Do not store input content, keystrokes, mouse coordinates, screen content, or window titles. Redact sensitive errors and never log credentials.

## Scoped authorization: core model

Separate identity, team membership, role/capability, and device scope. A grant consists of a principal (user or user group), a permission set, and a device group or explicit device scope. Model overlapping grants as the union of valid scoped grants, never as global role permissions combined with unrelated group membership. Define special deny behavior explicitly before implementing deny overrides; initial behavior is allow only through an applicable grant and deny otherwise.

Example: a user has view/restart/lock on Area A and view-only on Area B. They cannot restart Area B and cannot configure profile retention in either area. Empty scope never means all devices. No implicit rights from owning a device record. Device groups may overlap; sensitive group membership changes are authorization changes and must be audited.

Platform administrators manage global configuration, identity providers, enrollment, roles and grants; operator/viewer rights are scoped. Separate managing permissions from issuing commands. Scoped delegation, if implemented, cannot grant powers/scopes beyond an explicit delegator ceiling or allow self-escalation. Protect the last active platform administrator and define secure recovery.

## Capability examples

- devices.view, sessions.view, users.view, events.view, audit.view
- commands.lock, commands.logoff, commands.restart, commands.shutdown
- schedules.create, schedules.update, schedules.cancel
- profiles.view, profiles.retention.configure, profiles.delete
- policy.configure, devices.enroll, devices.revoke, groups.manage
- access.manage, identityProviders.manage, integrations.manage

These are proposed identifiers. Scheduling requires both schedule permission and the underlying action permission on each target. Combining retention configuration with reboot requires both permissions. Group-wide actions check every device; do not silently include unauthorized targets. Preview and report allowed/denied targets explicitly before authorizing execution. No privilege merely because a button is visible.

Enforce scopes on lists, detail pages, searches, counts, exports, audit views, API reads, websockets/refreshes and command endpoints. Test direct-object-reference attacks and cross-group access. Apply field-level user-name/history visibility where necessary. A third-party integration cannot exceed its own assigned permissions/scopes, even if its creator is more privileged.

## Authentication and external login

Plan local bootstrap/recovery plus external OIDC login (including Entra where configured), with MFA-capable administrator access. Validate provider issuer/audience/redirect/state/nonce and bind external identities by trusted issuer plus immutable subject, not email alone. No automatic account linking by matching email and no automatic access on first login.

Invitations/provisioning must assign explicit scopes. Provider-group mapping, if offered, is an explicit audited rule and must not implicitly promote external groups to platform administrators. Test disabled/deprovisioned users and permission revocation. Browser, integration and device authentication are separate credential classes. Integration tokens read-only by default; unique device credentials can report/fetch only that device's data/commands and can be rotated/revoked.

TLS, secure cookies/session expiration, CSRF protection for browser mutations, authorization on every operation, rate limits and fresh authorization for sensitive actions are required. An authentication proxy does not replace application permissions.

## Future remote command security

Remote execution is a later milestone, disabled locally by default. Server queues typed allowlisted actions; agent retrieves over outbound authenticated HTTPS. No arbitrary scripts, shell commands, executable paths or generic remote execution API. Initial types may include lock/logoff for a specified session, restart/shutdown now or scheduled, and retention-policy configuration. Profile deletion is distinct and requires separate guards.

Command records bind actor, exact device and stable logon identity where relevant, type, validated parameters, policy revision, unique ID, issue/not-before/expiry, and audit state. Reauthorize the actor/scope at creation and before dispatch; invalidate pending commands on revoked access or scope changes. Define revocation limits after delivery and keep command leases/expiry short. Agent independently validates authentication, target, expiration, supported schema, local allowed actions, and current session state. Server authorization cannot bypass local capability restrictions.

Use durable deduplication and action-specific reconciliation, not an exactly-once claim. Immediate actions expire rather than run unexpectedly after a long outage. Reboot outcomes require subsequent boot evidence; server acknowledgment is not completion. Define clock tolerance, ordering, retries, cancellations, and recovery after lost acknowledgments.

Scheduled actions carry explicit time zone/UTC semantics, local task ownership and installed acknowledgment, offline execution policy, cancellation/update rules and DST tests. A command to configure cleanup does not imply permission to restart. Expired schedules must not become immediate actions by accident.

Require clear target/action/data-loss preview, confirmation, and step-up authentication for sensitive actions. Bulk/profile actions need stricter limits and possibly dual approval as a later policy option. Audit actor/target/parameters/approval/delivery/result/rejection with protected server-side records; do not silently erase them when removing a device.

## Third-party API

Versioned read endpoints for devices, sessions, events, command status, and limited health, with authentication, pagination, freshness and scoped field filtering. Any future mutation API requires separate explicit permissions, same command validation/authorization/auditing, and idempotency rules. Read credentials cannot invoke commands. No public inventory or enrollment secrets.

## Uninstall and lifecycle

Local uninstall stops reporting and enforcement, removes protected local credentials and owned components, previews restored configuration, and retains recovery records on failures. Historical server records follow separate retention/deletion policy; uninstall cannot contact a server reliably or undo prior destructive actions. Define device retirement/revocation/re-enrollment and cloned-device identity handling.

## Implementation checklist

- [ ] Consolidate README/TASKS links/status for service, integration, server and branding notes.
- [ ] Finish local service/admin/helper security and functional validation.
- [ ] Define versioned report schema, device identity and enrollment/revocation.
- [ ] Laravel application skeleton, database/migrations, secure Ubuntu deployment guide.
- [ ] Local/external authentication, immutable identity binding, bootstrap/recovery.
- [ ] Scoped grants/policies and tests across browser/API/query/export surfaces.
- [ ] Ingestion, freshness/order/deduplication, privacy controls, retention/backups.
- [ ] Dashboard with authorized names, uptime, sessions, policies and action results.
- [ ] Read-only integration API and scoped credential management.
- [ ] Command threat model, durable queue/state machine, agent opt-in and validation.
- [ ] Permission revocation, replay, expiry, cross-device/session and bulk-action tests.
- [ ] Scheduling/offline/DST/cancel/reconciliation tests.
- [ ] Remote execution only after security gates and explicit release approval.

## Continuity and source references

These notes are the repository-backed administrative plan, not a claim that an assistant memory store has been modified. Maintain timestamp, paths, owner/permissions, purpose, arguments/usage, implementation evidence, blockers and next step in each increment. Use commit/PR diffs when connector reads omit text; a blob SHA is not content.

Framework authorization: https://laravel.com/framework/docs/authorization
Deployment: https://laravel.com/framework/docs/12.x/deployment
Queues: https://laravel.com/framework/docs/queues
Security design: https://cheatsheetseries.owasp.org/cheatsheets/Transaction_Authorization_Cheat_Sheet.html

No organization-specific examples, private support correspondence, tenant identities or real device/user details are included. Hosting infrastructure and credentials are not provisioned by this documentation commit.
