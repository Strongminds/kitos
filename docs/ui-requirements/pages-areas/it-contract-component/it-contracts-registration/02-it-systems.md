# 02 IT systems

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974323749)

> Screenshots remain in Confluence; follow the image links below to view them.

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4931>

<https://os2web.atlassian.net/browse/KITOSUDV-4932>

## General requirements

[Choice types in kitos](../../../shared-components/choice-types-in-kitos.md)

## UI Customization keys



[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974323749)
## API

**Data**: `GET | PATCH | DELETE /api/v2/it-contracts/{contractUuid}`

**System-usages:** `GET /api/v2/internal/it-system-usages/search`

**Permissions: Missing permissions endpoint?**

## Component specific requirements

### Used systems

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT Kontrakten gælder flg. IT Systemer | IEnumerable<IdentityNamePairResponseDTO> | SystemUsages | SystemUsages |  |

### Agreement elements

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT Kontrakten gælder flg. IT Systemer | IEnumerable<IdentityNamePairResponseDTO> | General.AgreementElements | General.AgreementElementUuids |  |

### Contracts applied to It system usage

**MISSING ENDPOINT TO GET RELATIONS**

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT System (anvender) |  |  | -- |  |
| IT System (udstiller) |  |  |  |  |
| Snitflade |  |  |  |  |
| Beskrivelse |  |  |  |  |
| Reference |  |  |  |  |
| Frekvens |  |  |  |  |
