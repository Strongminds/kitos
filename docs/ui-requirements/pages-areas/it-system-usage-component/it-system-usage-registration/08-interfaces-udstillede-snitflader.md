# 08 Interfaces (Udstillede snitflader)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/882147353)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4199>

# API

Data is fetched from the it-interfaces resource by providing the uuid of the master data system:

### `GET /api/v2/it-interfaces?exposedBySystemUuid={usage.systemContext.uuid}&includeDeactivated=true`

# UI customization

![Confluence screenshot](./08-interfaces-udstillede-snitflader.assets/image-001.png)

# Component specific requirements

## List of exposed interfaces

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Snitflade | Internal link to the interface | `name` |  |  |
| Aktivt/Ikke aktivt markering | Boolean | `general.deactivated` |  |  |
| Grænseflade | Rendered choice type | `interfaceType` |  | Render as choice type |
| Beskrivelse | Description | `description` |  |  |
| Reference | Link | `urlReference` |  | Render as [link](../../../shared-components/external-links-in-kitos.md) |
