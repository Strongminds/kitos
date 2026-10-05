# 01 Interface details (Snitflade detaljer)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/964100158)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4928>

## General requirements

## UI Customization keys



## Backend requirements

* Update ItInterfaceResponseDTO or its base class to include the information for “rettighedshaver”.

## API

### Options

`GET /api/v2/it-interface-interface-types`

### Data:

`[GET | PATCH] /api/v2/it-interfaces/{uuid}`

### Permissions:

`GET /api/v2/it-interfaces/{interfaceUuid}/permissions`

## Component specific requirements

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Snitflade | string | name | name |  |
| Snitflade ID | string (optional) | interfaceId | `interfaceId` |  |
| Version | string (optional) | version | version |  |
| Udstillet af | `APIIdentityNamePairResponseDTO.name` (optional) | `exposedBySystem` | `exposedBySystemUuid` |  |
| UUID | string | uuid | Not applicable (read-only field) |  |
| Oprettet af | `APIIdentityNamePairResponseDTO`.name | `createdBy`.name | Not applicable (read-only field) |  |
| Rettighedshaver | `ShallowOrganizationResponseDTO` | OrganizationContext.Name | -- |  |
| Synlighed | `APIItInterfaceResponseDTO.ScopeEnum` | scope | scope |  |
| Grænseflade | `APIIdentityNamePairResponseDTO` | `APIItInterfaceResponseDTO.itInterfaceType` | `itInterfaceTypeUuid` |  |
| Beskrivelse | string (optional) | `description` | `description` |  |
| Note | string (optional) | notes | notes |  |
| Link til yderligere beskrivelse | string (optional) | `urlReference` | `urlReference` | Validly formed url |
| Data + Datatype (connected fields) | Array<`APIItInterfaceDataResponseDTO`> | data\[i\].description, data\[i\].dataType.name | data\[i\].description, data\[i\].dataTypeUuid |  |
