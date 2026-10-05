# 02 Exposed interfaces (Udstillede snitflader)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/964329495)

## Jira Links

## General requirements

None.

## UI Customization keys

None.

## API

### Data:

`GET /api/v2/it-interfaces?exposedBySystemUuid={systemUuid}`

### Permissions:

`GET /api/v2/it-interfaces/permissions`

## Component specific requirements

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Snitflade | String | name | Not applicable (read-only field) | None |
| Grænseflade | IdentityNamePairResponseDTO | `itInterfaceType` | --\|\|-- | None |
| Beskrivelse | String (optional) | `description` | --\|\|-- | None |
| Reference | String (optional) | `urlReference` | --\|\|-- | None |
