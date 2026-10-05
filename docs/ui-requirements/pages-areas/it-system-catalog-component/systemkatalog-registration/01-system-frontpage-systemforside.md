# 01 System frontpage (Systemforside)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/963936264)

## Jira Links



## General requirements

[Linked page](../../../shared-components/help-text-dialogs.md)

[Linked page](../../../shared-components/data-change-toast-messages-with-optional.md)

## UI Customization keys

None.

## API

### Data:

`[GET | PATCH | DELETE] /api/v2/it-systems/{uuid}`

### Permissions:

`GET /api/v2/it-systems/{systemUuid}/permissions`



### Options:

`GET /api/v2/business-types`

## Component specific requirements

### Overview table #1 (read/write,...)

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT System | String | name | name | None |
| Overordnet system | `IdentityNamePairResponseDTO` (optional) | `parentSystem` | `parentUuid` | None |
| Tidligere systemnavn | String (optional) | formerName | formerName | None |
| Rettighedshaver | ShallowOrganizationResponseDTO (optional) | `rightsHolder` | `rightsHolderUuid` | None |
| Synlighed | `ItSystemResponseDTO.ScopeEnum` | scope | scope |  |
| Beskrivelse | String (optional) | `description` | `description` | None |
| KLE ID + KLE Navn (two UI fields) \* | Array<`IdentityNamePairResponseDTO`> | kle | kleUuids |  |
| Forretningstype | `IdentityNamePairResponseDTO` (optional) Get valid options: `GET /api/v2/business-types` | `businessType` | `businessTypeUuid` | Unique options |
| Rigsarkivets vejledning til arkivering | `RecommendedArchiveDutyResponseDTO`.`IdEnum` | `recommendedArchiveDuty`.id | `recommendedArchiveDuty.id` | None |
| Bemærkning fra Rigsarkivet (text field) | String (optional) | `recommendedArchiveDuty`.comment | `recommendedArchiveDuty`.comment |  |
| UUID | String | uuid | Not applicable (read-only id field) |  |

* The KLE fields are read-only on this tab, so no options need to be fetched.
