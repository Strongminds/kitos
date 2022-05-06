﻿class NavigationHelper {

    public getPage(destUrl: string): webdriver.promise.Promise<void> {
        console.log("Almost there...");
        return browser.getCurrentUrl()
            .then(url => {
                console.log("Before navigateToUrl");
                const navigateToUrl = browser.baseUrl[1] + destUrl;
                console.log("URL...");
                if (navigateToUrl !== url) {
                    console.log("Not at " + navigateToUrl + " but at:" + url + ". Navigating to:" + navigateToUrl);
                    return browser.get(navigateToUrl)
                        .then(() => browser.waitForAngular());
                } else {
                    console.log("Already at " + navigateToUrl + ". Ignoring command");
                }
            });;
    }

    public refreshPage(): webdriver.promise.Promise<void> {
        return browser.refresh()
            .then(() => browser.waitForAngular());
    }

    public acceptAlertBox(): webdriver.promise.Promise<void> {
        return browser.switchTo().alert().accept();
    }

    public goToSubMenuElement(srefName: string) {
        console.log("Navigating to sub menu ", srefName);
        return element(by.css(`[data-ui-sref="${srefName}"`)).click()
            .then(() => browser.waitForAngular());
    }
}
export = NavigationHelper;