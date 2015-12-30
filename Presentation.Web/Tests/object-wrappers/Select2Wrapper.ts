﻿// handle select2 wrapping for protractor
// http://stackoverflow.com/questions/26504623/protractor-how-to-test-select2?answertab=votes#tab-top
class Select2Wrapper {
    cssSelector: string;
    elementSelector: string;
    element: protractor.ElementFinder;
    options: protractor.ElementArrayFinder;

    constructor(cssLocator: string) {
        this.cssSelector = cssLocator;
        this.elementSelector = cssLocator + " a.select2-choice";

        this.element = $(this.cssSelector);
        this.options = element.all(by.css(".select2-results-dept-0"));
    }

    /**
     * Select first element in select2 dropdown.
     *
     * @param query An optional search query for the dropdown.
     */
    selectFirst(query?: string): webdriver.promise.Promise<void> {
        this.isPresent();

        browser.driver.executeScript("$(arguments[\"0\"]).mousedown();", (this.elementSelector));

        if (query) {
            browser.driver.switchTo().activeElement().sendKeys(query);
            browser.driver.wait(() => browser.driver.executeScript("return $.active === 0;"), 2000);
        }

        browser.driver.wait(() => this.options.count().then(count => count > 0), 2000)
            .thenCatch(err => {
                throw new Error("No options found for select2 selector '" + this.cssSelector + "'");
            });

        return this.options.first().click();
    }

    /**
     * Detect if select2 dropdown is disabled
     *
     * @return Promise that resovles to a boolean indicating if the dropdown is disabled or not.
     */
    isDisabled(): webdriver.promise.Promise<boolean> {
        return $(this.cssSelector + ".select2-container-disabled").isPresent();
    }

    /**
     * detect if element is present in the DOM
     *
     * @throws error is not present.
     */
    isPresent(): webdriver.promise.Promise<boolean> {
        return this.element.isPresent()
            .then(present => {
                if (!present) {
                    throw Error("select2 element not found using selector '" + this.cssSelector + "'");
                }
            })
            .then(() => $(this.elementSelector).isPresent())
            .then(present => {
                if (!present) {
                    throw Error("select2 element not found using selector '" + this.cssSelector + "'. Is the element select2 initialized?");
                }
            })
            .then(() => true);
    }
}

export = Select2Wrapper;
