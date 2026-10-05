# 03 Dataprocessing (databehandling)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974094344)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4933>

<https://os2web.atlassian.net/browse/KITOSUDV-4934>

## UI Customization keys


## API

Data: `GET | PATCH /api/v2/it-contracts/{contractUuid}`

DPRs: **Missing dpr search endpoint**

Permissions: **Missing permissions endpoint**?

## Component specific requirements

### Data processing table

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Registering | IEnumerable<IdentityNamePairResponseDTO> | DataProcessingRegistrations | DataProcessingRegistrationsUuid |  |
