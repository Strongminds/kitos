# 07 Hierarchy

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974225470)

## Backend requirements

The API seems to be missing a contracts/{uuid}/hierarchy endpoint like the one for it-systems.

The backend logic for creating hierarchies seems to be generic and able to handle contract hierarchies as well as the current one(s).

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4938>

<https://os2web.atlassian.net/browse/KITOSUDV-4939>

## General requirements

## UI Customization keys

## API

### Data:

GET /api/v2/it-contracts (with organization Id)

### Permissions:

## Component specific requirements

### Overview table #1 (read/write,...)

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| **Overordnet kontrakt** | string | name | N/A |  |
|  |  |  |  |  |

### Overview table #2 (read/write,...)
