# The "Default Organization"

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/818872321)

> Screenshots remain in Confluence; follow the image links below to view them.

# Background

When KITOS was initially created, it spawned an initial “default configuration” called “Fælles Kommune”.

This organization was meant to be used by the system administrators (global admins) to create global objects shared between the different member organizations.

# Problem description

During the resolution of <https://os2web.atlassian.net/browse/KITOSUDV-2468> we identified a need to “promote objects to the default organization” since they were used across organizations, but were tied to an organization which was about to be deleted.

There was no reasonable way of finding the “logical default organization” (Fælles kommune) without searching for it by name (which can be changed), we needed another way of identifying the “default organization”.

# Solution

In order to provide an in-system answer to the question “which organization is the default?”, we introduced an optional boolean property on the Organiztion object, which marks the organization as “Default”.

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/818872321)
As part of introduction of this boolean, we added a custom migration script to promote the “Fælles kommune” organization to the “default”.

## Resolving the default organization

Whenever a software component needs to resolve the default organization, they should use the `Resolve()` method on  `IDefaultOrganizationResolver` which is available for dependency injection across the application.

## Changing which organization is default

Changing which organization is default is not exposed on the API or the UI and is a developer-only setting.
