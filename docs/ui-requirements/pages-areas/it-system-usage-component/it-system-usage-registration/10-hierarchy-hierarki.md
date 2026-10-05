# 10 Hierarchy (Hierarki)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881819760)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4201>

# API

Data is read from the master data endpoint:

### `GET /api/v2/it-systems/{usage.systemContext.uuid}/hierarchy`

# Component specific requirements

## List of references including “valid” marking



| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| It-System block | Internal link to the system master data | `node.name` |  | Read only |
| Tilgængeligt/ikke tilgængeligt | boolean | `node.disabled` |  | Read only |
