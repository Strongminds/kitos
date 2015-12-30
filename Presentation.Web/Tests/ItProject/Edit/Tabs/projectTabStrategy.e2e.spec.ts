﻿import mock = require("protractor-http-mock");
import Helper = require("../../../helper");
import PageObject = require("../../../../app/components/it-project/tabs/it-project-tab-strategy.po");

describe("project edit tab strategy", () => {
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

        it("should disable joint municipal strategy", () => {
            // arrange

            // act

            // assert
            expect(pageObject.jointMunicipalSelector.element).toBeSelect2Disabled();
        });

        it("should disable common public strategy", () => {
            // arrange

            // act

            // assert
            expect(pageObject.commonPublicSelector.element).toBeSelect2Disabled();
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

        it("should save when joint municipal strategy is clicked", () => {
            // arrange

            // act
            pageObject.jointMunicipalSelector.selectFirst();

            // assert
            expect(mockHelper.lastRequest()).toMatchRequest({ method: "PATCH", url: "api/itproject/1" });
        });

        it("should save when common public strategy is clicked", () => {
            // arrange

            // act
            pageObject.commonPublicSelector.selectFirst();

            // assert
            expect(mockHelper.lastRequest()).toMatchRequest({ method: "PATCH", url: "api/itproject/1" });
        });
    });
});
