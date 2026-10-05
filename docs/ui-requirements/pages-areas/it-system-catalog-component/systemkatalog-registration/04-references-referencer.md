# 04 References (Referencer)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/963739688)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4877>

## General requirements

## API

### Data:

`GET /api/v2/it-systems/{uuid}`

`POST /api/v2/it-systems/{uuid}/external-references`

`DELETE /api/v2/it-systems/{uuid}/external-references/{externalReferenceUuid}`

`PUT /api/v2/it-systems/{uuid}/external-references/{externalReferenceUuid}`

Dialog options are already implemented in the ItSystemUsage Local references

## Component specific requirements

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Titel | string | externalReferences.title | title |  |
| url | string | externalReferences.url | url |  |
| Evt. DokumentID/sagsnr./Anden Reference | string | externalReferences.documentId | documentId |  |
| Vises i overblik | boolean | externalReferences.masterReference | masterReference | Only 1 reference can be masterReference |
