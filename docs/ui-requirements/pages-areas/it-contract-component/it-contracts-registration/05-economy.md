# 05 Economy

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974618626)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4936>

## General requirements

## UI Customization keys

## API

### Data:

GET /api/v2/it-contracts/{contractUuid}

GET /api/v2/it-contract-payment-frequency-types

GET /api/v2/it-contract-payment-model-types

GET /api/v2/it-contract-price-regulation-types

GET /api/v2/organizations/{organizationUuid}/organization-units

PATCH /api/v2/it-contracts/{contractUuid}

### Permissions:

## Component specific requirements

### Overview table #1 (read/write,...)

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| **Driftsvederlag påbegyndt** | DateTime (optional) | `paymentModel.operationsRemunerationStartedAt` | `paymentModel.operationsRemunerationStartedAt` |  |
| **Betalingsfrekvens** | `APIRegularOptionResponseDTO`  (optional) | `paymentModel.paymentFrequency.name` | `paymentModel.paymentFrequencyUuid` |  |
| **Betalingsmodel** | `APIRegularOptionResponseDTO` (optional) | `paymentModel.paymentModel.name` | `paymentModel.paymentModelUuid` |  |
| **Prisregulering** | `APIRegularOptionResponseDTO` (optional) | `paymentModel.priceRegulation.name` | `paymentModel.priceRegulationUuid` |  |
| **Organisationsenhed** | `APIRegularOptionResponseDTO` (optional), options from /api/v2/organizations/{organizationUuid}/organization-units | `payments[i].organizationUnit.name` | `payments[i].organizationUnitUuid` |  |
| **Anskaffelse** | int (optional) | `payments[i].acquisition` | `payments[i].acquisition` |  |
| **Drift/år** | int (optional) | `payments[i].operation` | `payments[i].operation` |  |
| **Andet** | int (optional) | `payments[i].other` | `payments[i].other` |  |
| **Kontering** | string (optional) | `payments[i].accountingEntry` | `payments[i].accountingEntry` |  |
| **Audit** | `APIPaymentResponseDTO.AuditStatusEnum` (optional) | `payments[i].auditStatus` | `payments[i].auditStatus` | Shown as the colours for 0-3: white, red, yellow, green |
| **Dato** | DateTime (optional) | `payments[i].auditDate` | `payments[i].auditDate` |  |
| **Note** | string (optional) | payments\[i\].note | payments\[i\].note |  |
