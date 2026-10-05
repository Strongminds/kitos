# 12 Notifications (Advis)

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/963674115)

> Screenshots remain in Confluence; follow the image links below to view them.

## Jira Links

<https://os2web.atlassian.net/browse/KITOSUDV-4204>

## General requirements

## UI Customization keys

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/963674115)
## API

### Data:

GET notifications list:

* `/api/v2/internal/notifications/{ownerResourceType}?organizationUuid={orgUuid}&ownerResourceUuid={usageUuid}`

GET single:

* `/api/v2/internal/notifications/{ownerResourceType}/{ownerResourceUuid}/{notificationUuid}`

POST (immediate):

* `/api/v2/internal/notifications/{ownerResourceType}/{ownerResourceUuid}/immediate`

POST (scheduled):

* `/api/v2/internal/notifications/{ownerResourceType}/{ownerResourceUuid}/scheduled`

PUT:

* `/api/v2/internal/notifications/{ownerResourceType}/{ownerResourceUuid}/scheduled/{notificationUuid}`

PATCH (deactivate notification):

* `/api/v2/internal/notifications/{ownerResourceType}/{ownerResourceUuid}/scheduled/deactivate/{notificationUuid}`

Choice type:

* `GET /api/v2/it-system-usage-roles`

### Permissions:

`GET /api/v2/internal/it-system-usages/{systemUsageUuid}/permissions`

## Component specific requirements

### Notifications

* List of notifications

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Aktiv | boolean | active | - | Appears as a checkmark or an 'x' |
| Navn | string | name | - | - |
| Sidst sendt | DateTime | lastSent | - | - |
| Fra dato | DateTime | fromDate | - | - |
| Til dato | DateTime | toDate | - | - |
| Modtager | RecipientResponseDTO | receivers | - | - |
| CC | RecipientResponseDTO | cCs | - | - |
| Emne | string | subject | - | - |

### Notification dialog

| **UI name** | **Field type** | **READ path** | **WRITE path** | **Constraints** |
| --- | --- | --- | --- | --- |
| Til modtager via roller | RoleRecipientResponseDTO\[\] ChoiceType: `/api/v2/it-system-usage-roles` | receivers.roleRecipients | receivers.roleRecipients |  |
| Til modtager via email | EmailRecipientResponseDTO\[\] | receivers.emailRecipients | receivers.emailRecipients |  |
| CC modtager via rolle | RoleRecipientResponseDTO\[\] ChoiceType: `/api/v2/it-system-usage-roles` | cCs.roleRecipients | cCs.roleRecipients |  |
| CC modtager via email | EmailRecipientResponseDTO\[\] | cCs.emailRecipients | cCs.emailRecipients |  |
| Emne | string | subject | subject | **required** |
| Email tekst | string | body | body | TextArea |
| Afsendelsestype | NotificationSendType | notificationType | notificationType | **required** |
| Navn | string | name | name | Show only when `Repeated` notification type is selected |
| Gentagelse | RepetitionFrequencyOptionsType | repetitionFrequency | repetitionFrequency | Show only when `Repeated` notification type is selected, **required** |
| Fra dato | DateTime | fromDate | fromDate | Show only when `Repeated` notification type is selected, **required** |
| Til dato | DateTime | toDate | toDate | Show only when `Repeated` notification type is selected |
