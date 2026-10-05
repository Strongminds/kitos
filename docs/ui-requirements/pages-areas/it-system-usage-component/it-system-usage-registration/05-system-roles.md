# 05 System roles

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881918021)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4196>

# General requirements

[Linked page](../../../shared-components/choice-types-in-kitos.md)

* Also note sections specifically around roles

[Linked page](../../../shared-components/help-text-dialogs.md)

[Linked page](../../../shared-components/data-change-toast-messages-with-optional.md)

# UI Customization keys

![Confluence screenshot](./05-system-roles.assets/image-001.png)

# API

Data:

`[GET | PATCH | DELETE] /api/v2/it-system-usages/{systemUsageUuid}`

Permissions:

`GET /api/v2/internal/it-system-usages/{systemUsageUuid}/permissions`

# Component specific requirements

## Roles overview table

Note: The internal endpoint for the “assigned roles” extends the regular user reference with the `email` field which is needed on that page.

`GET /api/v2/internal/it-system-usages/{systemUsageUuid}/roles`

NOTE: Add/remove roles easily:

* `PATCH /api/v2/it-system-usages/{systemUsageUuid}/roles/add`
* `PATCH /api/v2/it-system-usages/{systemUsageUuid}/roles/remove`

Search endpoints to be used in the dialog (for adding new roles)

* `/api/v2/organizations/{organizationUuid}/users?nameOrEmailQuery={....}`
* `GET /api/v2/it-system-usage-role-types`

    * Choice types can be queried in memory


| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Systemrolle | Choice type `GET /api/v2/it-system-usage-role-types` | NOTE: Lookup descriptions and write access info from the choice types response. Main name: `lookup(roles[*].uuid).name` Tooltip: `lookup(roles[*].uuid).description` |  | Read only |
| Skriv | Boolean | `lookup(roles[*].uuid).writeAccess` |  | Read only |
| Navn | String | `roles[*].name` |  | Read only |
| Email | String | `roles[*].email` |  | Read-only |
