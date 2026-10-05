# 03 Data processing

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881819684)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4194>

# API

Data:

### `GET /api/v2/data-processing-registrations?systemUsageUuid={usage.uuid}`

# Component specific requirements

## List of references including “valid” marking



| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Databehandling | Internal link to the dpr | `name` |  | Read only |
| Gyldig-markering | Boolean | `general.valid` |  | Read only |
