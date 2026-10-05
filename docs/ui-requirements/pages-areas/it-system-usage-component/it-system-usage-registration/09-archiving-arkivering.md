# 09 Archiving (Arkivering)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881721475)

> Screenshots remain in Confluence; follow the image links below to view them.

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4200>

# General requirements

[Linked page](../../../shared-components/choice-types-in-kitos.md)

[Linked page](../../../shared-components/help-text-dialogs.md)

[Linked page](../../../shared-components/data-change-toast-messages-with-optional.md)

# UI Customization keys

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881721475)
# API

`[GET | PATCH] /api/v2/it-system-usages/{systemUsageUuid}`

Search endpoint for organization

`GET /api/v2/organizations`

Choice types

* `GET /api/v2/it-system-usage-archive-types`
* `GET /api/v2/it-system-usage-archive-location-types`
* `GET /api/v2/it-system-usage-archive-test-location-types`

Convenience endpoints for journal periods

* `POST /api/v2/it-system-usages/{systemUsageUuid}/journal-periods`
* `DELETE /api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`
* `GET /api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`
* `/api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`

# Component specific requirements

## Individual data fields

| **UI name** | **Inputtype** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| **Arkiveringspligt** | Enum `ArchiveDutyChoice` | `archiving.archiveDuty` | `archiving.archiveDuty` |  |
| Tooltip content below the main field | Enum `RecommendedArchiveDutyChoice` | Fetched from the it-system masterdata: `recommendedArchiveDutyid` `recommendedArchiveDutycomment` |  | Read only |
| **Arkivtype** | Choice type `GET /api/v2/it-system-usage-archive-types` | `archiving.type` | `archiving.typeUuid` | Choice type rendering |
| **Arkiveringssted** | Choice type  `GET /api/v2/it-system-usage-archive-location-types` | `archiving.location` | `archiving.locationUuid` | Choice type rendering |
| **Arkiveringsleverandør** | Organization reference | `archiving.supplier` | `archiving.supplierOrganizationUuid` |  |
| **Er der arkiveret fra systemet?** | boolean | `archiving.active` | `archiving.active` |  |
| **Arkiveringsteststed** | Choice type  `GET /api/v2/it-system-usage-archive-test-location-types` | `archiving.testLocation` | `archiving.testLocation` | Choice type rendering |
| **Arkiveringsbemærkninger** | text area | `archiving.notes` | `archiving.notes` |  |
| **Arkiveringsfrekvens (antal år)** | postitive number | `archiving.frequencyInMonths` | `archiving.frequencyInMonths` |  |
| **Dokumentbærende** | boolean | `archiving.documentBearing` | `archiving.documentBearing` |  |

## Journal periods

* Add/edit go through a dialog
* delete from the table with confirmation dialog

Convenience endpoints for journal periods

* `POST /api/v2/it-system-usages/{systemUsageUuid}/journal-periods`
* `DELETE /api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`
* `GET /api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`
* `/api/v2/it-system-usages/{systemUsageUuid}/journal-periods/{journalPeriodUuid}`

| **UI name** | **Inputtype** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| **Startdato** | Date | `archiving.journalPeriods[*].startDate` | `startDate` | Both must be defined and start <= end |
| **Slutdato** | Date | `archiving.journalPeriods[*].endDate` | `endDate` | Both must be defined and start <= end |
| **Unikt arkiv-id** | String | `archiving.journalPeriods[*].archiveId` | `archiveId` |  |
| **Godkendt** | Boolean | `archiving.journalPeriods[*].approved` | `approved` |  |
