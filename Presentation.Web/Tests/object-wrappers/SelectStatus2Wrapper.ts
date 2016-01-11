﻿// handle selectStatus2 directive dropdowns
class SelectStatus2Wrapper {
    cssSelector: string;
    dropdownElement: protractor.ElementFinder;
    options: protractor.ElementArrayFinder;

    /**
     * instantiate selectStatus2 directive wrapper.
     *
     * @param {string} [cssLocator] CSS locator for directive element.
     */
    constructor(cssLocator: string) {
        this.cssSelector = cssLocator;

        this.dropdownElement = $(this.cssSelector + " a.select-status");
        this.options = element.all(by.css(cssLocator + " .traffic-light li a"));
    }

    /**
     * select first element.
     */
    selectFirst(): webdriver.promise.Promise<void> {
        this.dropdownElement.click().then(null, () => {
            throw Error(`No active selectStatus2 directive found with selector '${this.cssSelector}'`);
        });

        browser.driver.wait(() => this.options.count().then(count => count > 0), 2000)
            .then(null, err => this.createError(`No options found.\n  ${err}`));

        return this.options.first().click();
    }

    /**
     * select element by index.
     *
     * @param {number} [index] Options index on dropdown starting on 0.
     */
    select(index: number): webdriver.promise.Promise<void> {
        if (index < 0) {
            this.createError(`Index must be positive: ${index}`);
        }

        this.dropdownElement.click().then(null, () => {
            throw Error(`No active selectStatus2 directive found with selector '${this.cssSelector}'`);
        });

        browser.driver.wait(() => this.options.count().then(count => count > 0), 2000)
            .then(() =>
                this.options.count(),
            err =>
                this.createError(`No options found.\n  ${err}`))
            .then(count => {
                if (index >= count) {
                    this.createError(`Index out of range: ${index} Range: [0 - ${count}]`);
                }
            });

        return this.options.get(index).click();
    }

    /**
     * is directive present
     *
     * @return Promise that resolves to a boolean indicating if the element is present.
     */
    isPresent(): webdriver.promise.Promise<boolean> {
        return this.dropdownElement.isPresent();
    }

    /**
     * is directive disabled
     *
     * @return Promise that resolves to a boolean indicating if the element is disabled
     */
    isDisabled(): webdriver.promise.Promise<boolean> {
        return $(this.cssSelector + " span").isPresent();
    }

    /**
     * throw error message
     */
    private createError(message: string) {
        throw Error(`selectStatus2 selector: ${this.cssSelector}\n  ${message}\n`);
    }
}

export = SelectStatus2Wrapper;
