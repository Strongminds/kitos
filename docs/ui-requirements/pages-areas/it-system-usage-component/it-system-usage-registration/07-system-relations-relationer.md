# 07 System relations (Relationer)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/882343949)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4198>

# Common requirements

[Linked page](../../../shared-components/external-links-in-kitos.md)

[Linked page](../../../shared-components/choice-types-in-kitos.md)

* Also note sections specifically around roles

[Linked page](../../../shared-components/help-text-dialogs.md)

[Linked page](../../../shared-components/data-change-toast-messages-with-optional.md)

# API

Data:

### `GET /api/v2/it-system-usages/{systemUsageUuid}`

Incoming relations

`GET /api/v2/it-system-usages/{systemUsageUuid}/incoming-system-relations`

Search for “systems which can be related to”

`GET /api/v2/it-system-usages?organizationUuid={currentOrg.uuid}`

* NOTE: Remove the “current system usage uuid” from the available choices

Search for contracts which can be used on relation

`GET /api/v2/it-contracts?organizationUuid={currentOrg.uuid}`

Get “exposed interfaces” available for selection in relation, through the:

`GET /api/v2/it-interfaces/?exposedBySystemUuid={targetUsage.systemContext.uuid}&includeDeactivated=true`

# Component specific requirements

## UI customization

![Confluence screenshot](./07-system-relations-relationer.assets/image-001.png)

## Administration of outgoing relations

* Creation and UPDATE goes through dialog
* Deletion prompts user before deleting

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT-System (udstiller) | Internal link | `toSystemUsage` | `toSystemUsageUuid` | Usage MUST be from the sane org and cannot be the “current system usage” |
| Snitflade | Internal link | `relationInterface` | `relationInterfaceUuid` | MUST be exposed by the system master data |
| Beskrivelse | text area | `description` | `description` |  |
| Reference | String (if not a link), if a link - render as link | `urlReference` | `urlReference` |  |
| Kontrakt | Internal link | `associatedContract` | `associatedContractUuid` |  |
| Frekvens | Rendered choice type `GET /api/v2/it-system-usage-relation-frequency-types` | `relationFrequency` | `relationFrequencyUuid` | Must apply choice type rendering |

## Overview of incoming relations

* `GET /api/v2/it-system-usages/{systemUsageUuid}/incoming-system-relations`

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT-System (anvender) | Internal link | `fromSystemUsage` |  |  |
| Snitflade | Internal link | `relationInterface` |  |  |
| Beskrivelse | text area | `description` |  |  |
| Reference | String (if not a link), if a link - render as link | `urlReference` |  |  |
| Kontrakt | Internal link | `associatedContract` |  |  |
| Frekvens | Rendered choice type `GET /api/v2/it-system-usage-relation-frequency-types` | `relationFrequency` |  |  |
