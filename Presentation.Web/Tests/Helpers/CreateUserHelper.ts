﻿import HomePageObjects = require("../PageObjects/Organization/UsersPage.po");
import CreatePage = require("../PageObjects/Organization/CreateUserPage.po");
import CSSLocator = require("../object-wrappers/CSSLocatorHelper");

class CreateUserHelper {
    private cssHelper = new CSSLocator();
    private pageCreateObject = new CreatePage();
    private pageObject = new HomePageObjects();
    private assertCheckboxState(email: string, checkbox: protractor.ElementFinder, expectedState: boolean) {
        return this.openEditUser(email)
            .then(() => {
                var expectedValue = expectedState ? "true" : null;
                return expect(checkbox.getAttribute("checked")).toEqual(expectedValue);
            });
    }

    public checkApiRoleStatusOnUser(email: string, apiStatus: boolean) {
        return this.assertCheckboxState(email, this.pageObject.hasAPiCheckBox, apiStatus);
    }

    public checkRightsHolderAccessRoleStatusOnUser(email: string, hasAccess: boolean) {
        return this.assertCheckboxState(email, this.pageObject.hasRightsHolderAccessCheckBox, hasAccess);
    }

    public checkStakeHolderAccessRoleStatusOnUser(email: string, hasAccess: boolean) {
        return this.assertCheckboxState(email, this.pageObject.hasStakeHolderAccessCheckBox, hasAccess);
    }

    private setUserCheckboxState(email: string, checkbox: protractor.ElementFinder, toState: boolean) {
        return this.openEditUser(email)
            .then(() => {
                return checkbox.isSelected()
                    .then(selected => {
                        if (selected !== toState) {
                            return checkbox.click()
                                .then(() => {
                                    return this.pageCreateObject.editUserButton.click();
                                });
                        } else {
                            return this.pageCreateObject.cancelEditUserButton.click();
                        }
                    });
            })
            .then(() => browser.waitForAngular());
    }

    public updateApiOnUser(email: string, apiAccess: boolean) {
        return this.setUserCheckboxState(email, this.pageObject.hasAPiCheckBox, apiAccess);
    }

    public updateRightsHolderAccessOnUser(email: string, hasAccess: boolean) {
        return this.setUserCheckboxState(email, this.pageObject.hasRightsHolderAccessCheckBox, hasAccess);
    }

    public updateStakeHolderAccessOnUser(email: string, hasAccess: boolean) {
        return this.setUserCheckboxState(email, this.pageObject.hasStakeHolderAccessCheckBox, hasAccess);
    }

    private getUserRow(email: string) {
        const emailColumnElementType = "userEmailObject";

        return this.pageObject.mainGridAllTableRows.filter((row, index): protractor.promise.Promise<boolean> => {
            console.log("Searching for email column");
            var column = row.element(this.cssHelper.byDataElementType(emailColumnElementType));
            return column.isPresent()
                .then(present => {
                    if (present) {
                        console.log("Found email column - checking if row is the right one");
                        return column.getText()
                            .then(text => {
                                return text === email;
                            });
                    }
                    return protractor.promise.fullyResolved(false);
                });
        }).then(elements => {
            if (elements.length === 0) {
                throw `No rows found for email:${email}`;
            }
            return elements[0];
        });
    }

    openEditUser(email: string) {
        return this.getUserRow(email)
            .then(row => {
                expect(row).not.toBe(null);
                return row.element(by.linkText("Redigér")).click();
            })
            .then(() => browser.waitForAngular());
    }

    assertAllPrimaryStartUnitsAvailable(): protractor.promise.Promise<any> {
        return this.pageObject
            .getPrimaryStartUnitElementIds()
            .reduce<protractor.promise.Promise<any>>((previous, optionId) => previous.then(_ => expect(element(by.id(optionId)).isPresent()).toBe(true, "Failed to find:" + optionId)), protractor.promise.fullyResolved(true));
    }
}
export = CreateUserHelper;