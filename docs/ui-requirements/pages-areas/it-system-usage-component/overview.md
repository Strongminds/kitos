# Overview

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/876445745)

# Jira links

TODO

# Common requirements

[Linked page](../../shared-components/registration-overviews-grids.md)

# Module-specific requirements

* **TODO**
* “Role selector” (changes role columns order and visibility)
* ~~Active/inactive~~

    * According to the new design this option is gone and replaced by filtering where the active state is enriched with reasoning

* GDPR Export “tab” with additional in-memory grid with the GDPR export data
* Selected org unit (not a simple query param)

## Endpoints (ODATA)

`GET /odata/Organizations({organizationUuid})/ItSystemUsageOverviewReadModels`

Query params:

* `responsibleOrganizationUnitUuid` Optionally the uuid of the org unit selected in “Ansv. organisationsenhed”. Is not equivalent to just using a regular odata query on the object field.

## Column requirements (the order specifies the default order in the grid)

**TODO**

| **Title** | **Persistid** | **Datasource**  | **Rendering** | **Excel rendering** | **Data type** | **Sortable** | **Filterable** | **Filter strategy** | **UI customization key** | **Default available** | **UI Only** | **Excel only** | **Excel-only lhs column** |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |  |  |  |  |  |  |  |  |
|  |  |  |  |  |  |  |  |  |  |  |  |  |  |
