# 06 Contract roles

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974356483)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4937>

## General requirements

## UI Customization keys

## API

### Data:

GET /api/v2/it-contract-role-types

GET /api/v2/organizations/{organizationUuid}/users/{userUuid}

GET /api/v2/organizations/{organizationUuid}/users (for the searchable dropdown of users)

PATCH  /api/v2/it-contracts/{contractUuid}

### Permissions:

## Component specific requirements

### Overview table #1 (read/write,...)

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Kontraktrolle | `APIIdentityNamePairResponseDTO`  (options:  `APIRoleOptionResponseDTO` from/api/v2/it-contract-role-types) | roles\[i\].role.name | roles\[i\].roleUuid |  |
| Navn | `APIIdentityNamePairResponseDTO` | roles\[i\].user.name | roles\[i\].userUuid |  |
| Email | string | email | N/A | Read-only, filled automatically from user when user is selected in the “navn” field |
