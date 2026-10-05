# 06 Organization (Organisation)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/882442241)

# Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4197>

# API

Data:

### `GET /api/v2/it-system-usages/{systemUsageUuid}`

# Component specific requirements

## UI customization

![Confluence screenshot](./06-organization-organisation.assets/image-001.png)

## Selection of relevant and responsible units

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| **Relevante organisationsenheder** | Selection of unit (perhaps tree-selector??) | `organizationUsage.usingOrganizationUnits` | `organizationUsage.usingOrganizationUnitUuids` | Unique |
| **Ansvarlig organisationsenhed** | Marking one of the relevant units to be the “responsible”.. could be a radio button, selector or whatever works | `organizationUsage.responsibleOrganizationUnit` | `organizationUsage.responsibleOrganizationUnitUuid` | Must also be in `usingOrganizationUnitUuids` |
