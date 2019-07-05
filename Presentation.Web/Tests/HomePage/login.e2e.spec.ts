import loginHelper = require("../Helpers/LoginHelper");

describe("Can login succesfully", () => {

    var loginHelper = new loginHelper.Login();

    beforeAll(() => {
        loginHelper.login("support@kitos.dk", "testpwrd");
        browser.waitForAngular();
    });

    it("Is succesfully logged in", () => {
        //Arrange

        //Act 

        //Assert
        expect(element(by.binding("user.fullName")).getText()).toEqual("Global admin");
    });

});