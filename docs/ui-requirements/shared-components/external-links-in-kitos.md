# External links in KITOS

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/881819739)

In KITOS, we render external links as working `hrefs` based on the following conditions

* The link is a valid web url
* The link is a valid ESDH reference

If the link is not a valid link, it will be rendered as text

## Valid external links spec

```
export class Validation {

        static validateUrl(url: string): boolean {
            if (url == null || _.isUndefined(url)) {
                return false;
            }
            const regexp = /(^https?):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/])$)?/;
            return regexp.test(url.toLowerCase());
        }

        static isValidExternalReference(externalRef: string): boolean {
            if (externalRef == null || _.isUndefined(externalRef)) {
                return false;
            }
            if (this.validateUrl(externalRef)) {
                return true;
            } else {
                const regexp = /^(kmdsageraabn|kmdedhvis|sbsyslauncher):.*/;
                return regexp.test(externalRef.toLowerCase());
            }
        }
    }
```
