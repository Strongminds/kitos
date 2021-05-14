﻿
import login = require("../../Helpers/LoginHelper");
import ItSystemReferenceHelper = require("../../PageObjects/it-system/tabs/ItSystemReference.po");
import TestFixtureWrapper = require("../../Utility/TestFixtureWrapper");
import ItSystemHelper = require("../../Helpers/SystemCatalogHelper");
import AdviceHelper = require("../../Helpers/AdviceHelper");
import CssLocatorHelper = require("../../Object-wrappers/CSSLocatorHelper");
import WaitTimers = require("../../Utility/WaitTimers");

var adviceHelper = new AdviceHelper();
var waitUpTo = new WaitTimers();
var testFixture = new TestFixtureWrapper();


describe("Is able to create and edit advice",
    () => {
        var loginHelper = new login();
        var itSystemName = createItSystemName();
        var startDate = getDateNow();
        var endDate = getDateNow();
        var email = getRandomEmail();
        var subjectText1 = getRandomText();
        var subjectText2 = getRandomText();

        beforeAll(() => {
            loginHelper.loginAsGlobalAdmin()
                .then(() => ItSystemHelper.createSystemAndActivateLocally(itSystemName));
        });

        beforeEach(() => {
            testFixture.enableLongRunningTest();
        });

        afterAll(() => {
            testFixture.disableLongRunningTest();
            testFixture.cleanupState();
        });

        it("Is able to create a new advice",
            () => {

                adviceHelper.goToSpecificItSystemAdvice(itSystemName)
                    .then(() => adviceHelper.createNewRepetitionAdvice(email, startDate, endDate, subjectText1))
                    .then(() => verifyAdviceWasCreated(subjectText1))
                    .then(() => adviceHelper.createNewInstantAdvice(email,subjectText2))
                    .then(() => verifyAdviceWasCreated(subjectText2));
            });
    });
//browser.wait(ec.elementToBeClickable(element(cssHelper.byDataElementType(name))), waitUpTo.twentySeconds);

function createItSystemName() {
    return `ItSystemAdviceTest${new Date().getTime()}`;
}

function getDateNow() {
    return new Date().getDay() + "-" + new Date().getMonth() + "-" + new Date().getFullYear();
}

function getRandomEmail() {
    return `ItSystemAdviceTest@${new Date().getTime()}.com`;
}

function getRandomText() {
    return `ItSystemAdviceText${new Date().getTime()}`;
}

function verifyAdviceWasCreated(subjectName: string) {
    console.log(`waiting for ${subjectName} to appear`);
    return browser.wait(element(by.xpath(`.//*[@id="mainGrid"]//span[text() = ${subjectName}]//text()`)).isPresent(),
        waitUpTo.twentySeconds);
}
