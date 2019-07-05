export class Login {

    login(usrName: string, pwd: string) {
        browser.get(browser.baseUrl);
        var emailField = element(by.model("email"));
        var passwordField = element(by.model("password"));
        var loginBtn = element(by.buttonText("Log ind"));

        emailField.sendKeys(usrName);
        passwordField.sendKeys(pwd);
        loginBtn.click();
    }

    logout() {
        var logoutBtn = element(by.linkText("Log ud"));
        logoutBtn.click();
    }

}