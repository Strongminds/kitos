# 04 Milestones (aftalefrister)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/974389284)

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4935>

## General requirements

[Choice types in kitos](../../../shared-components/choice-types-in-kitos.md)

## UI Customization keys


## API

Data: `GET | PATCH /api/v2/it-contracts/{contractUuid}`

Permissions: Missing

## Component specific requirements

### Agreement Period

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
|  Varighed \| År | int | AgreementPeriod.DurationYears | AgreementPeriod.DurationYears | Editable if IsContinuous = false |
| Varighed \| Måneder | int | AgreementPeriod.DurationMonths | AgreementPeriod.DurationMonths | Editable if IsContinuous = false |
| Løbende | bool | AgreementPeriod.IsContinuous | AgreementPeriod.IsContinuous |  |
| Option forlæng | IdentityNamePairResponseDTO | AgreementPeriod.ExtensionOptions | AgreementPeriod.ExtensionOptionsUuid |  |
| Antal brugte optioner | int | AgreementPeriod.ExtensionOptionsUsed | AgreementPeriod.ExtensionOptionsUsed |  |
| Uopsigelig til | DateTime | AgreementPeriod.IrrevocableUntil | AgreementPeriod.IrrevocableUntil |  |

### Termination

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Kontrakten opsagt | DateTime | Termination.TerminatedAt | Termination.TerminatedAt |  |
| Opsigelsesfrist (måneder) | IdentityNamePairResponseDTO | Termination.Terms.NoticePeriodMonths | Termination.Terms.NoticePeriodMonthsUuid |  |
| Løbende | YearSegmentChoice | Termination.Terms.NoticePeriodExtendsCurrent | Termination.Terms.NoticePeriodExtendsCurrent |  |
| Inden udgangen af | YearSegmentChoice | Termination.Terms.NoticeByEndOf | Termination.Terms.NoticeByEndOf |  |
