# IT-Contracts overview

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974225453)

> Screenshots remain in Confluence; follow the image links below to view them.

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4929>

## General requirements

[Linked page](../../shared-components/registration-overviews-grids.md)

## UI Customization keys



[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974225453)


## API

Data:
`GET:/odata/Organizations(1)/ItContractOverviewReadModels?$expand=RoleAssignments($select=RoleId,UserId,UserFullName,Email),DataProcessingAgreements($select=DataProcessingRegistrationId,DataProcessingRegistrationName),ItSystemUsages($select=ItSystemUsageId,ItSystemUsageName,ItSystemIsDisabled)&%24format=json&%24top=100&%24orderby=Name&%24count=true`

_(this is to get all of the data in the Old UI, not necessarily all of those will be needed for the new UI)_

## Column requirements (the order specifies the default order in the grid)

**TODO**

| **Title** | **Persistid** | **Datasource** | **Rendering** | **Excel rendering** | **Data type** | **Sortable** | **Filterable** | **Filter strategy** | **UI customization key** | **Default available** | **UI Only** | **Excel only** | **Excel-only lhs column** |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
|   |   |   |   |   |   |   |   |   |   |   |   |   |   |
|   |   |   |   |   |   |   |   |   |   |   |   |   |   |
