# Project Vision

## Vision

ResilienceOps is a SaaS application that helps organizations create a structured overview of operational risks, incidents and mitigation actions.

The application should make it easier to identify critical risks, assign responsibility and follow up on actions before problems develop into serious incidents.

## Problem

Organizations often manage risks and incidents through spreadsheets, emails and disconnected documents.

This can create several problems:

- Information becomes fragmented.
- Ownership is unclear.
- Deadlines are missed.
- Risk assessments are inconsistent.
- Management lacks a current overview.
- Changes are difficult to trace.
- Important follow-up actions may be forgotten.

## Proposed solution

ResilienceOps provides a shared web application where users can:

- Register operational risks
- Assess probability and impact
- Assign an owner
- Create mitigation actions
- Register incidents
- Monitor deadlines
- View dashboards
- Review historical changes

## Target users

### Administrator

Responsible for configuring the organization, users, roles and shared settings.

### Risk manager

Responsible for maintaining the overall risk overview and following up on critical risks.

### Team lead

Responsible for risks, incidents and actions within a specific team or business area.

### Employee

Can report incidents, view assigned actions and contribute relevant information.

## Initial product scope

The first version should include:

1. Dashboard
2. Risk overview
3. Risk creation
4. Risk editing
5. Incident overview
6. Incident registration
7. Mitigation actions
8. Ownership
9. Deadlines
10. Audit history

## Out of scope for the first version

The following areas are intentionally excluded from the first version:

- Native mobile applications
- Microservices
- Machine learning
- Real-time collaboration
- Complex multi-tenant billing
- External customer integrations
- Advanced reporting
- Offline support

These features may be considered later if a concrete requirement justifies the additional complexity.

## Architectural direction

The application will initially be implemented as a modular monolith.

A modular monolith is selected because:

- The application is developed by one developer.
- The initial domain is limited.
- Frontend and backend can be deployed together or independently.
- Operational complexity should remain low.
- Business boundaries can still be kept clear.
- Modules can potentially be extracted later if necessary.

Microservices are not selected initially because they would introduce additional deployment, networking, monitoring and data-consistency complexity without solving a current problem.

## Quality goals

The application should be:

- Understandable
- Testable
- Maintainable
- Observable
- Secure
- Deployable
- Documented

## Portfolio goal

The repository should demonstrate the ability to work across:

- Frontend development
- Backend development
- Database design
- API design
- Cloud deployment
- Monitoring
- Automated testing
- Technical documentation
- Architectural decision-making
