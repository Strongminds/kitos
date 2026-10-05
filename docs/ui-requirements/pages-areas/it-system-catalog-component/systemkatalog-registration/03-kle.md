# 03 KLE

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/964231224)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4876>

## General requirements

## API

### Data:

`GET /api/v2/kle-options`

`GET /api/v2/it-systems/{uuid}`

`PATCH /api/v2/it-systems/{uuid}`

Dialog options are already implemented in the existing KLE component

### Permissions:

`GET /api/v2/it-systems/{systemUuid}/permissions`

## Component specific requirements

| **UI Name** | **Field Type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| KLE ID | string | payload.kleNumber | kle.uuid | Should get KLE from the options, and update it on the specific ItSystem |
| KLE Navn | string | kle.name | kle.name |  |
