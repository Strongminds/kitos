﻿import ContractPage = require("../PageObjects/It-contract/ItContractOverview.po");
import ContractDprPage = require("../PageObjects/It-contract/Tabs/ContractDpr.po");
import CssHelper = require("../Object-wrappers/CSSLocatorHelper");
import Constants = require("../Utility/Constants");
import WaitTimers = require("../Utility/WaitTimers");
import NavigationHelper = require("../Utility/NavigationHelper");
import Select2Helper = require("./Select2Helper");

class ContractHelper {

    private static contractPage = new ContractPage();
    private static cssHelper = new CssHelper();
    private static consts = new Constants();
    private static navigation = new NavigationHelper();
    private static waitUpTo = new WaitTimers();
    private static contractDprPage = new ContractDprPage();

    public static createContract(name: string) {
        console.log(`Creating contract with name: ${name}`);
        return this.inputContractData(name)
            .then(() => this.contractPage.getSaveContractButton().click())
            .then(() => browser.waitForAngular())
            .then(() => console.log("Contract created"));
    }

    public static openContract(name: string) {
        console.log(`open details for contract: ${name}`);
        return this.contractPage.getPage()
            .then(() => this.waitForEconomyPageKendoGrid())
            .then(() => this.findCatalogColumnsFor(name).first().click())
            .then(() => browser.waitForAngular());
    }

    public static createContractAndProceed(name: string) {
        console.log(`Creating and proceeding to the details for contract: ${name}`);
        return this.inputContractData(name)
            .then(() => element(ContractHelper.cssHelper.byDataElementType(ContractHelper.consts.saveContractAndProceedButton)).click())
            .then(() => browser.waitForAngular())
            .then(() => console.log("Contract created"));
    }

    public static getRelationCountFromContractName(name: string) {
        return this.contractPage.getPage()
            .then(() => this.waitForEconomyPageKendoGrid())
            .then(() => {
                const filteredRows = this.findCatalogColumnsFor(name);
                return filteredRows.first().element(by.xpath("../.."))
                    .element(this.cssHelper.byDataElementType(this.consts.kendoRelationCountObject)).getText();
            });
    }

    public static findCatalogColumnsFor(name: string) {
        return this.contractPage.kendoToolbarWrapper.getFilteredColumnElement(this.contractPage.kendoToolbarWrapper.columnObjects().contractName, name);
    }

    public static waitForEconomyPageKendoGrid() {
        browser.wait(this.contractPage.waitForKendoGrid(), this.waitUpTo.twentySeconds);
    }

    public static goToDpr() {
        return ContractHelper.navigation.goToSubMenuElement("it-contract.edit.data-processing");
    }

    public static assignDpr(name: string) {
        console.log("Assigning dpr with name: " + name);
        return Select2Helper.searchFor(name, "data-processing-registration_select-new")
            .then(() => Select2Helper.waitForDataAndSelect());
    }

    public static removeDpr(name: string) {
        console.log("Removing dpr with name: " + name);
        return this.contractDprPage.getRemoveDprButton(name)
            .click()
            .then(() => browser.switchTo().alert().accept())
            .then(() => browser.waitForAngular());
    }

    public static assignSupplier(name: string) {
        console.log(`Assigning Supplier with name: ${name}`);
        return Select2Helper.searchFor(name, "s2id_contract-supplier")
            .then(() => Select2Helper.waitForDataAndSelect());
    }

    public static validateSupplierHasCorrectValue(name: string) {
        console.log(`Validating Supplier: ${name}`);
        return expect(Select2Helper.getData("s2id_contract-supplier").getText()).toEqual(name);
    }

    public static inputContractData(name: string) {
        console.log(`Input ${name} contract data`);
        return this.contractPage.getPage()
            .then(() => this.contractPage.getCreateContractButton().click())
            .then(() => expect(this.contractPage.getContractNameInputField().isPresent()))
            .then(() => this.contractPage.getContractNameInputField().sendKeys(name))
            .then(() => browser.waitForAngular());
    }

    static clickDpr(dprName: string) { return element(by.linkText(dprName)).click() };

}

export = ContractHelper;