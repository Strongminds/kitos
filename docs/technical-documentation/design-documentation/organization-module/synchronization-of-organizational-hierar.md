# Synchronization of organizational hierarchy from FK Organization

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/833683457)

# Introduction

The purpose of this document is to describe the design of the organizational import functionality from the FK Organisation system.

In order to do so, we start by identifying the high-level scenarios (use cases), which act as input to the design process.

# Useful Links

**EPIC:** <https://os2web.atlassian.net/browse/KITOSUDV-3087>

**Initial requirements from Ballerup:** <https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/828080133>

**FK Organisation:** <https://digitaliseringskataloget.dk/l%C3%B8sninger/organisation>

**FK Beskedfordeler:** <https://digitaliseringskataloget.dk/l%C3%B8sninger/beskedfordeler>

# Scenarios

## Prerequisites

* The organization must use FK Organisation locally
* The CVR number for the organizaiton in KITOS must match the CVR number in FK Organisation
* In order for FK Organisation integration to work, the municipality must have received and accepted a service agreement (through FK Administration) to grant access to FK Organisation as well as FK Beskedfordeler. For more info on this works, please see <https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/823787521/Connecting+to+services+exposed+through+Serviceplatformen#Getting-permissions-to-access-the-service>

    * The Service agreement must (at the time of writing) be for “Organisation V5 (Serviceplatformen)” and contain the read-rights “udstil” without any specific permissions for “se navn” or “se cpr”.


:exclamation: _**NOTE:** The initial release of the integration does not utilize FK Beskedfordeler._

## Definitions

### Synchronization depth

When we talk about “synchronization depth” we mean the levels in the organizational hierarchy, which will be imported From FK Organisation.

The following diagram illustrates this.

## Overview

The following diagram provides a high-level overview of the use cases the synchronization functionality must implement.

## Actors

### Local Admin

The Local Admin represents a user, within the context of an organization, who has the Local Admin role assigned.

The Local Admin has full administrative access within the organization in which the role has been assigned. There may be more than one Local Admin in an organization.

### Organization Admin

The Organization Admin represents a user, within the context of an organization, who has the Organization Admin role assigned.

The Organization Admin has scoped administrative access within the context of the organization module, in the organization in which the role has been assigned. There may be more than one Organization Admin in an organization.

### FK Beskedfordeler

The FK Beskedfordeler system allows KITOS to subscribe to changes in the organizational hierarchy of the organizations, which have the automatic synchronization functionality enabled.

**NOTE:** In the first iteration of the design, the “FK Beskedfordeler” actor will be replaced by a Hangfire job. Design based on actual FK Beskedfordeler is pending: <https://os2web.atlassian.net/browse/KITOSUDV-3641>

## Use case descriptions

The following sections will describe the different scenarios in the _brief format._ The purpose is not to provide an exhaustive set of requirements, but rather to serve as high-level design context for the user stories developed in [Jira](https://os2web.atlassian.net/secure/RapidBoard.jspa?rapidView=72&view=planning.nodetail).

### Use Case 1: Synchronize Organization Hierarchy

#### Description

The Local Admin initiates synchronization of the organizational hierarchy. Before starting synchronization the Local Admin must define the [synchronization depth](synchronization-of-organizational-hierar.md).

Optionally the user can preview the organizational hierarchy from FK Organisation filtered by the [synchronization depth](synchronization-of-organizational-hierar.md). In doing so, the Local Admin can see if the needed organizational units will be part of the import in the when filtered by the [synchronization depth](synchronization-of-organizational-hierar.md).

When the import has finishd, the Local Admin can see which of the organizational units that have been imported from FK Organisation and which organization units that have been created manually in KITOS (KITOS-only organization units).

This distinction is only visible to the Local Admin and only when managing the organizational structure.

#### Alternative flow 1

If this is the first time the Local Admin synchronizes from FK Organisation, the Local Admin may choose to subscribe to automatic updates (Link: [Use case 4: Toggle automatic syncronization](synchronization-of-organizational-hierar.md)).

#### Alternative flow 2

If the organizational data has already been imported into KITOS, the Local Admin must accept a list of consequences (Link: [Use Case 3: Accept import consequences](synchronization-of-organizational-hierar.md)) before proceeding.

#### Business Rules

Range of [synchronization depth](synchronization-of-organizational-hierar.md): `ALL` or `Any integer from greater than or equal to 1`

#### Post conditions

* The organizational hierarchy has been imported or updated based on the [synchronization depth](synchronization-of-organizational-hierar.md) set by the Local Admin.
* Automatic synchronization for the organization been enabled if that has been set by the Local Admin.
* An entry descibing the consequences of the import has been written in the synchronization log

### Use Case 2: Update syncronization depth

#### Pre conditions

Subscription to automatic updates is active

#### Description

The Local Admin can change the synchronization depth applied during background synchronization.

If the value is changed the user must complete a full import (Link: [Use Case 1: Synchronize Organization Hierarchy](synchronization-of-organizational-hierar.md)) to get a fresh import before any automatic updates occur.

#### Business Rules

Range of synchronization depth: `ALL` or `Any integer from greater than or equal to 1`

#### Post conditions

* The organizational hierarchy has been updated based on the [synchronization depth](synchronization-of-organizational-hierar.md) decided by the Local Admin.
* Future background synchronization will respect the updated [synchronization depth](synchronization-of-organizational-hierar.md)

### Use Case 3: Accept Import Consequences

#### Pre conditions

A pending import contains structural consequences to the existing organizational hierarchy in KITOS.

#### Description

During manual import of data from FK Organisation, the Local Admin may view and accept any organizational consequences of the operation. This includes:

* Organization units which have been renamed
* Organization units which will be introduced
* Organization units which will be removed, since they are unused in KITOS and have been removed in the _scope_ of the FK Organisation import.
* Organization units which will be converted to KITOS-only organization units, since they have been removed in the _scope_ of the FK Organisation import, but contains registrations which must be migrated.

Based on the consequences the user may choose to either accept or reject the consequences in order to proceed.

#### Post Conditions

The user has chosen to either accept or reject the consequences of the import.

### Use Case 4: Toggle automatic synchronization

#### Pre conditions

The Local Admin has performed at least one sucessful import from FK Organisation.

#### Description

The Local Admin can toggle whether or not automatic synchronization occurs.

If the Local Admin enables automatic synchronization, he/she is informed prompted to accept that consequences will be automatically accepted and a migration report will be sent to all Local Admins. Once the Local Admin accepts the prompt, KITOS will trigger an import (Link: [Use Case 1: Synchronize Organization Hierarchy](synchronization-of-organizational-hierar.md)) and then subscribe to updates for the organization in FK Organisation.

If the Local Admin disables the automatic synchronizaiton, then updates to the organization in FK Organisation will no longer affect the organization heirarchy in KITOS.

#### Post Conditions

The Local Admin has either enabled or disabled automatic synchronization from FK Organisation.

### Use Case 5: View Synchronization log

#### Description

In order to get a historical overview of the changes caused by FK Organisation import, the Local Admin may, at any time, export a document for latest five imports. Each document will contain an overview similar to the [consequences accepted](synchronization-of-organizational-hierar.md) during [manual import](synchronization-of-organizational-hierar.md) or the consequences included in the email sent to Local Admins during [background syncronization](synchronization-of-organizational-hierar.md).

Additionally the date of import is included as well as the initiator (name of Local Admin if imported manually or FK Beskedfordeler if the change occurred during background synchronization)

### Use Case 6: Migrate Organization Unit

#### Description

A Local Admin may choose to migrate some or all dependencies of an organization unit to another organization unit. Each dependency is categorized

* It is possible to migrate all data from all catagories to another organization unit.
* It is possible to migrate individual or a group of dependencies to another organization unit

### Use Case 7: Delete KITOS-Only organization unit

#### Pre conditions

The organization unit is not part of the hierarchy imported from FK Organisation

#### Description

For KITOS-Only organizaiton units (org units created inside KITOS by the Local or Organization Admin) the user has the option to delete the organization unit.

Before the user deletes the organization unit, the user is presented with a list of dependencies to the organization unit which the user can optionally migrate (Link: [Use Case 6: Migrate Organization unit](synchronization-of-organizational-hierar.md)).

If the user does not migrate the depenencies, they will be deleted as part of the organization unit deletion.

#### Post conditions

* If the user chose to do so, all or some of the dependencies to the deleted organization unit have been migrated to another organization unit.
* The Organization unit is deleted from KITOS.

### Use Case 8: Perform background synchronization

#### Pre conditions

Automatic updates are enabled for the organization.

#### Triggers

A change to the organizational hierarch occurs in FK Organisation.

#### Description

FK Beskedfordeler publishes an update to KITOS resulting in an import which automatically accepts consequences and writes them to the import log.

Following the completion of the import, an email is sent to all Local Admins containing an overview of the consequences of the import.

#### Post conditions

* The changes to the organizational hierarchy have been imported with respect to the [synchronization depth](synchronization-of-organizational-hierar.md) set by the Local Admin.
* An entry descibing the consequences of the import has been written in the synchronization log

### Use Case 9: Disconnect from FK Organisation

#### Pre conditions

The organization contains organization units which are synchronized from FK Organisation

#### Description

The Local Admin choses to disconnect the organization from FK Organisation. This converts all organization units to KITOS-only organization units and disables automatic synchronization (if that is active).

#### Post conditions

The entire organizational hierarchy has been converted to KITOS-only organization units and has lost any connection to FK Organisation.

Subscriptions in FK Beskedfordeler for FK Organisation have been removed for the organization.

### Use Case 10: Edit KITOS-only Organization units

#### Pre conditions

The organization contains organization units which are synchronized from FK Organisation

#### Description

The user is able to reorganize and edit properties of KITOS-only organization units. It is OK to add the organization unit to a hierarchy of organization units connected to FK Organization.

The user is not able to directly edit the names or structure of organization units connected to FK Organisation.

# Requirements

The detailed requirements are maintained in JIRA: <https://os2web.atlassian.net/browse/KITOSUDV-3087>

# Logical view

In this section we will describe the FK Organisation component design from a logical perspective uncovering the different services and components that provide the component’s functionality.

## Architectural footprint

The following class diagram illustrates some of the entities of the component in relation to the [application architecture](../../architectural-description.md).

This example is based on a request from the UI to display the current access status to FK Organisation _(does KITOS have access to the organization’s data in FK Organisation)_ and is not supposed to illustrate all operations and/or properties of different entities_._

:warning:  **NOTE on use of FK vs STS:** _From a user’s perspective the current name used for FK Organisation is just that. The systems behind it are prefixed with “STS” (støttesystem in danish), so for that reason, the STS prefix is used for anything but the user-facing components. The rationale behind this is that while the system remains the same over time, the external/business name has a tendency to change._

### Responsibilities

This section breaks down the responsibilities for each of the identified “groups of entities”, and should serve as a guideline for further development and maintenance of the component.

#### UI

In the UI the main component is the `FK Organization Import Config` AngularJS component, which is displayed in the “Local Admin” section. The component’s purpose is to:

* Render current access and synchronization state to the UI
* Allow the user to perform changes to the organization’s connection to FK Organisation.

All communication with the backend is handled through the `StsOrganizationSyncService`.

_**NOTE**: Based on an individual assessment some sub-functionality in the component might be implemented as components of their own. For more info see:_ <https://docs.angularjs.org/guide/component#component-based-application-architecture>

#### Internal API

The FK Organisation API is internal and not inteded to be exposed through API V2, and for that reason, only Cookie authentication is allowed on this endpoint collection.

#### Application services

In the application services layer, the synchronization use cases are orchestrated by `StsOrganizationSynchronizationService` which - as per the application architecture - must:

* Facilitate translation of Ids into domain entities which expose business logic.
* Authorize access to queries and commands before calling into the domain layer.

#### Domain Services

In this layer we define the abstractions used to decouple the complexity of FK Organisation from the KITOS Core. Implementations of these abstractions may exist in the infrastructure layer (as per onion architecture principles), but as much as possible is to be kept pure from external complexity/side effects.

Besides defining abstractions, we also place services here which deal with non-single-entity-isolated commands and/or queries which belong in the domain layer.

#### Domain model

In this layer we maintain the state of the organization as well as any entity-isolated business logic such as merging in organization units and validating state-specific operations.

#### Infrastructure

This is where the different integration implementations exist between KITOS and FK Organisation. By exposing the services defined in the core abstractions (domain services), none of the model/operation complexity from the SOAP contract with FK Organisation bleeds into KITOS, and hence any version bump in the integration should not affect the rest of KITOS, since they bind to implementation agnostic interfaces.

For more information about integrations between KITOS and Serviceplatformen see: <https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/823787521>

## State overview

**TODO - State diagram for organization hierarchies (with and without fk org binding) - DO it once we introduce states for both the organization as a whole and the individual units (wait until the Disconnect story)**

# Process view

In this section, we will add details to selected scenarios using diagramming which describe the component from at runtime perspective.

## Establishing connection to FK Organisation

The diagram above illustrates the translation of the responsibilities identified in <synchronization-of-organizational-hierar.md> into actual runtime behavior.

A few error cases have ben included, but the purpose of the diagram is not to be complete, but to serve as a responsibility segregation guide as well as a business logic placement guide (actual rules about the import are implemented and maintained in the domain model)

## Updating an existing connection

The following activity diagram describes the decision process provided for a user which is updating an existing connection to FK Organisation.
