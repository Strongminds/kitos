# Front page

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/876576817)

> Screenshots remain in Confluence; follow the image links below to view them.

# Jira links

<https://os2web.atlassian.net/browse/KITOSUDV-4022>

<https://os2web.atlassian.net/browse/KITOSUDV-4039>

<https://os2web.atlassian.net/browse/KITOSUDV-3895>

<https://os2web.atlassian.net/browse/KITOSUDV-4040>

<https://os2web.atlassian.net/browse/KITOSUDV-4046>

<https://os2web.atlassian.net/browse/KITOSUDV-4044>

<https://os2web.atlassian.net/browse/KITOSUDV-4045>

<https://os2web.atlassian.net/browse/KITOSUDV-4049>

# Description

Forsiden på KITOS fungerer forskelligt afhængigt af login-tilstanden.

## Not signed in state

Five text blocks are shown with messages and status from OS2KITOS.

Besides that, the users are presented with functionality to:

* Log in

    * With optional “remember login” checkmark

* Login with FK Adgangsstyring (SSO)
* “Forgot password”/Reset functionality

## Signing-in state

If the authenticated user has access to more than one organization, they should be presented with a prompt to select which organization to work in. This organization will be referred to as the “active organization”.

## Signed-in state

When signed in, the front page texts will still be shown.

If the user has a personal start page preference different to the front page, then KITOS automatically navigates to that.

### Authorization

Regular users just see the front page texts.

Users with the “global admin” role have the option to edit the texts using a rich text editor.

### Navbar

* All enabled components become visible
* The “user menu” becomes visible with the “name” and “active org name”

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/876576817)
## API endpoints

### Front page texts

`/api/v2/internal/public-messages`

### Authentication/change org

Login with credentials: \`

* GET XSRF token: `api/authorize/antiforgery`
* Login: `api/authorize`

    * In the response the `defaultUserStartPreference` contains the user’s preferred start module

* Get available organizations: `/api/v2/organizations`
* SSO (will change once we begin supporting return url): `Login.ashx?forceAuthn=true`
