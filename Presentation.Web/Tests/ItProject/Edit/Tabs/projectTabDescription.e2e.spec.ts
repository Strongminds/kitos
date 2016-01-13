﻿import mock = require("protractor-http-mock");
import Helper = require("../../../helper");
import PageObject = require("../../../../app/components/it-project/tabs/it-project-tab-description.po");

describe("project edit tab description", () => {
    var mockHelper: Helper.Mock;
    var pageObject: PageObject;
    var mockDependencies: Array<string> = ["itproject", "itprojecttype", "itprojectstatus"];

    beforeEach(() => {
        browser.driver.manage().window().maximize();

        mockHelper = new Helper.Mock();
        pageObject = new PageObject();
    });

    afterEach(() => {
        mock.teardown();
    });

    describe("with no write access", () => {
        beforeEach(() => {
            mock(["itProjectNoWriteAccess"].concat(mockDependencies));
            pageObject.getPage();

            // clear initial requests
            mock.clearRequests();
        });

        it("should disable description", () => {
            // arrange

            // act

            // assert
            expect(pageObject.descriptionElement).toBeDisabled();
        });
    });

    describe("with write access", () => {
        beforeEach(() => {
            mock(["itProjectWriteAccess"].concat(mockDependencies));
            pageObject.getPage();

            // clear initial requests
            // necessary hack to let protractor-http-mock clear all requests after page load
            browser.sleep(300);
            mock.clearRequests();
        });

        it("should save when description looses focus", () => {
            // arrange
            pageObject.descriptionInput("SomeDescription");

            // act
            pageObject.descriptionElement.sendKeys(protractor.Key.TAB);

            // assert
            expect(mockHelper.lastRequest()).toMatchRequest({ method: "PATCH", url: "api/itproject/1" });
        });
    });
});
