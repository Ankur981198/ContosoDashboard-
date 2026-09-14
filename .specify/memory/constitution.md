<!--
Sync Impact Report
Version change: 0.0.0 -> 1.0.0
Modified principles: n/a (new constitution)
Added sections: Training Constraints, Development Workflow
Removed sections: none
Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security & Trust
All user-facing features must be designed for the training context: authentication and authorization must be explicit, explainable, and observable. Access control must be enforced in both UI and service layers, and every protected route or data operation must verify the current user, role, and project membership before returning information or performing changes. This protects the sample app from unsafe access patterns and teaches the boundaries required in real systems without hiding them behind implicit trust.

### II. Offline-First Architecture
The project must remain runnable without cloud dependencies and must prefer local-first implementations that can later be swapped to managed services. Database, file, and identity dependencies must be isolated behind clear abstractions so the business logic stays portable and training examples remain reproducible in disconnected environments. This makes the codebase dependable for learning while leaving a clear path to production migration.

### III. Test-First Delivery
No behavioral change is complete until the relevant scenario is described in a failing test or equivalent executable verification, then implemented, and finally validated. The team must verify the behavior that changed, not merely the code that was edited, and must prefer small, focused checks that prove security, data isolation, and UI behavior. This keeps the training app honest and prevents regressions from being mistaken for progress.

### IV. Clear Separation of Concerns
Models, services, data access, and UI concerns must remain distinct. Business rules must not be embedded in Razor or view logic, and cross-cutting concerns such as authorization, validation, and persistence must be centralized. This preserves readability, supports incremental learning, and makes future refactoring safe.

### V. Reviewable, Maintainable Change
Every change must be small, explainable, and easy to review. Names, comments, and structure must make intent obvious; complexity must be justified; and changes that affect security, contracts, or user-visible behavior require direct review before merge. The project is a teaching artifact, so clarity is a quality requirement equal to correctness.

## Training Constraints
The ContosoDashboard repository is intentionally a training-only implementation. It must not be treated as a production system, and any feature added to it must remain safe for learning, offline execution, and controlled experimentation. Mock authentication, local storage, and demo data are acceptable only when explicitly labeled as training patterns and kept isolated from real-world deployment assumptions. If a feature requires production-grade identity, persistence, or compliance controls, the implementation must document the gap and keep the training scope explicit.

## Development Workflow
All work must follow a small, traceable delivery loop: define the user-visible behavior, implement the minimal change, validate the relevant scenario, and document any training-specific limitations. Pull requests must inspect both the technical correctness and the teaching value of the change; security-sensitive updates must be reviewed with special attention to access control, data isolation, and configuration. The project must prefer reproducible setups and straightforward instructions over hidden assumptions or environment-specific shortcuts.

## Governance
This constitution governs all repository decisions, including architecture, security, and process. It supersedes informal preferences and ad hoc exceptions when the two conflict. Any amendment must be documented in the repository history, tied to a clear rationale, and reviewed for impact on security, onboarding, and training fidelity before adoption.

Changes are versioned using semantic versioning: MAJOR for incompatible governance or principle changes, MINOR for new principles or materially expanded guidance, and PATCH for wording or clarification updates that do not change policy intent. The version line in this document must reflect the latest approved revision, and the ratification and amendment dates must remain current when governance changes are adopted.

Compliance is reviewed by checking each change against the principle it affects, with special emphasis on authentication, authorization, offline execution, and clarity of design. When a rule is unclear or a gap is discovered, the team must resolve it in writing before continuing the work to avoid silent drift from the constitution.

**Version**: 1.0.0 | **Ratified**: 2026-09-14 | **Last Amended**: 2026-09-14
