import loginHelper = require("./LoginHelper");

export class Login {



    createSystem(sysName: string) {
        new loginHelper.Login().login("support@kitos.dk", "testpwrd");
        browser.get("");
    }

}