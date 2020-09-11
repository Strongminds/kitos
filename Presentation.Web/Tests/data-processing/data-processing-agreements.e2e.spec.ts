﻿"use strict";
import Login = require("../Helpers/LoginHelper");
import TestFixtureWrapper = require("../Utility/TestFixtureWrapper");
import DataProcessingAgreementOverviewPageObject =
require("../PageObjects/Data-Processing/data-processing-agreement.overview.po");
import WaitTimers = require("../Utility/WaitTimers");
import dpaHelper = require("../Helpers/DataProcessingAgreementHelper")

describe("Data processing agreement tests", () => {

    const loginHelper = new Login();
    const pageObject = new DataProcessingAgreementOverviewPageObject();
    const testFixture = new TestFixtureWrapper();
    const waitUpTo = new WaitTimers();
    const ec = protractor.ExpectedConditions;

    const createName = (index: number) => {
        return `Dpa${new Date().getTime()}_${index}`;
    }

    beforeAll(() => {
        loginHelper.loginAsLocalAdmin();
        testFixture.enableLongRunningTest();
        dpaHelper.checkAndEnableDpaModule();
    });

    afterAll(() => {
        testFixture.cleanupState();
        testFixture.disableLongRunningTest();
    });

    it("It is possible to create new data processing agreements", () => {
        const name = createName(1);
            //Verify correct creation of dpa and following update of kendo
        dpaHelper.createDataProcessingAgreement(name)
            .then(() => {
                console.log(`expecting to find new dpa with name ${name}`);
                expect(pageObject.findSpecificDpaInNameColumn(name).isPresent()).toBeTruthy();
            })
            //Verify that dialog acts correctly on attempt to create duplicate dpa name
            .then(() => dpaHelper.openNewDpaDialog())
            .then(() => {
                console.log(`Validating UI error when entering the same name again.`);
                return dpaHelper.enterDpaName(name);
            })
            .then(() => {
                console.log("Waiting for UI error to appear");
                browser.wait(ec.not(ec.elementToBeClickable(pageObject.getNewDpaSubmitButton())), waitUpTo.twentySeconds);
                validateSaveDpaClickable(false);
            });
    });

    function validateSaveDpaClickable(isClickable: boolean) {
        console.log(`Expecting 'save' have clickable state equal ${isClickable}`);
        const expectation = expect(pageObject.getNewDpaSubmitButton().isEnabled());
        return isClickable ? expectation.toBeTruthy() : expectation.toBeFalsy();
    }
});