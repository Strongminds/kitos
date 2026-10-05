# 01 Contract frontpage

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974749697)

> Screenshots remain in Confluence; follow the image links below to view them.

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4930>

## General requirements

[Linked page](../../../shared-components/choice-types-in-kitos.md)

[Linked page](../../../shared-components/help-text-dialogs.md)

[Linked page](../../../shared-components/data-change-toast-messages-with-optional.md)

## UI Customization keys

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974749697)
## API

### Data: ` GET | PATCH /api/v2/it-contracts/{contractUuid}`

### Permissions: Missing permissions endpoint?

## Component specific requirements

### Frontpage

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| IT kontraktnavn | string | Name | Name |  |
| Kontrakt ID | string | General.ContractId | General.ContractId |  |
| Kontrakttype | IdentityNamePairResponseDTO | General.ContractType | General.ContractTypeUuid |  |
| Kontraktskabelon | IdentityNamePairResponseDTo | General.ContractTemplate | General.ContractTemplateUuid |  |
| Kritikalitet | IdentityNamePairResponseDTO | General.Criticality | General.CriticalityUuid |  |
| Indkøbsform | IdentityNamePairResponseDTO | Procurement.PurchaseType | Procurement.PurchaseTypeUuid |  |
| Status | bool | General.Validity.Valid | -- |  |
| Gyldig fra | DateTime | General.Validity.ValidFrom | General.Validity.ValidFrom |  |
| Gyldig til | DateTime | General.Validity.ValidTo | General.Validity.ValidTo |  |
| Gennemtving gyldighed | bool | General.Validity.EnforcedValid | General.Validity.EnforcedValid |  |
| Bemærkning | string | General.Notes | General.Notes |  |

### Responsible unit

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Ansvarlig enhed | IdentityNamePairResponseDTO | Responsible.OrganizationUnit | Responsbile.OrganizationUnitUuid |  |
| Kontraktunderskriver | string | Responsible.SignedBy | Responsible.SignedBy |  |
| Dato | DateTime | Responsible.SignedAt | Responsible.SignedAt |  |
| Underskrevet | bool | Responsible.Signed | Responsible.Signed |  |

### Supplier

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Leverandør | IdentityNamePairResponseDTO | Supplier.Organization | Supplier.OrganizationUuid |  |
| Leverandørens kontraktunderskriver | string | Supplier.SignedBy | Supplier.SignedBy |  |
| Dato | DateTime | Supplier.SignedAt | Supplier.SignedAt |  |
| Underskrevet | bool | Supplier.Signed | Supplier.Signed |  |

### Reacquisition

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Genanskaffelsesstrategi | IdentityNamePairResponseDTO | Procurement.ProcurementStrategy | Procurement.ProcurementStrategyUuid |  |
| Genanskaffelsesplan | ProcurementPlanDTO | Procurement.ProcurementPlan | Procurement.ProcurementPlan | Consists of 2 fields: QuarterOfYear and Year.  |
| Genanskaffelse igangsat | YesNoUndecidedChoice | Procurement.ProcurementInitiated | Procurement.ProcurementInitiated |  |

### History

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Oprettet af | IdentityNamePairResponseDTO | CreatedBy | -- |  |
| Sidst redigeret (bruger) | IdentityNamePairResponseDTO | LastModifiedBy | -- |  |
| Sidst redigeret (dato) | DateTime | LastModified | -- |  |
