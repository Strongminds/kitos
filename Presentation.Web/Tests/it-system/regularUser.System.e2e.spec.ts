import Helper = require("../helper");
import ItSystemEditPo = require("../PageObjects/it-system/it-system-overview.po");

describe("Regular user tests", () => {

    var loginHelper = new Helper.Login();
    var pageObject = new ItSystemEditPo();

    beforeAll(() => {
        loginHelper.login("almenBruger@test.dk", "arne123"); //TODO change user info.
        browser.waitForAngular();
    });

    beforeEach(() => {
        pageObject.getPage();
        browser.waitForAngular();
    });

    it("Apply and delete filter buttons are disabled", () => {
        //Arrange

        //Act 

        //Assert
        expect(pageObject.kendoToolbarWrapper.useFiltersButton.getAttribute("disabled")).toEqual("true");
        expect(pageObject.kendoToolbarWrapper.deleteFiltersButton.getAttribute("disabled")).toEqual("true");
    });

    it("IT systems can be sorted by name", () => {
        //Arrange
        pageObject.kendoToolbarWrapper.columnSystemName.click();
        browser.sleep(5000);
        var firstItemName = pageObject.kendoToolbarWrapper.dataGrid.all(by.partialLinkText("test")).first().getText();
        //Act 
        pageObject.kendoToolbarWrapper.columnSystemName.click();
        browser.sleep(5000);
        //Assert
        expect(pageObject.kendoToolbarWrapper.dataGrid.all(by.partialLinkText("test")).last().getText()).toEqual(firstItemName);
    });


});