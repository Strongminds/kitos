# 09 References

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974684164)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4941>

## General requirements

## UI Customization keys

## API

Data:
`GET /api/v2/it-contracts/{contractUuid}`

Missing `POST | PUT | DELETE` methods for external-references

### Permissions:

## Component specific requirements

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Titel | string | externalReferences.title | title |   |
| url | string | externalReferences.url | url |   |
| Evt. DokumentID/sagsnr./Anden Reference | string | externalReferences.documentId | documentId |   |
| Vises i overblik | boolean | externalReferences.masterReference | masterReference | Only 1 reference can be masterReference |
