﻿import CatalogPage = require("../PageObjects/it-system/Catalog/ItSystemCatalog.po");
import SystemPage = require("../PageObjects/It-system/Tabs/ItSystemFrontpage.po");
import CSSLocator = require("../object-wrappers/CSSLocatorHelper");
import Constants = require("../Utility/Constants");
import WaitTimers = require("../Utility/WaitTimers");
import Select2 = require("./Select2Helper");

class SystemCatalogHelper {
    private static consts = new Constants();
    private static cssHelper = new CSSLocator();
    private static pageObject = new CatalogPage();
    private static systemPage = new SystemPage();
    private static waitUpTo = new WaitTimers();
    private static readonly visibilitySelect2 = "s2id_system-access";

    public static createSystem(name: string) {
        console.log(`Creating system: ${name}`);
        return SystemCatalogHelper.pageObject.getPage()
            .then(() => SystemCatalogHelper.waitForKendoGrid())
            .then(() => SystemCatalogHelper.pageObject.kendoToolbarWrapper.headerButtons().systemCatalogCreate.click())
            .then(() => browser.wait(SystemCatalogHelper.pageObject.isCreateCatalogAvailable(), SystemCatalogHelper.waitUpTo.twentySeconds))
            .then(() => element(SystemCatalogHelper.cssHelper.byDataElementType(SystemCatalogHelper.consts.nameOfSystemInput)).sendKeys(name))
            .then(() => element(SystemCatalogHelper.cssHelper.byDataElementType(SystemCatalogHelper.consts.saveCatalogButton)).click())
            .then(() => console.log("System created"));
    }

    public static createSystemAndActivateLocally(name: string) {
        console.log(`Creating system: ${name}`);
        return SystemCatalogHelper
            .createSystem(name)
            .then(() => SystemCatalogHelper.createLocalSystem(name));
    }

    public static deleteSystem(name: string) {
        console.log(`Deleting system: ${name}`);
        return SystemCatalogHelper.getDeleteButtonForSystem(name)
            .then((button) => button.click())
            .then(() => browser.switchTo().alert().accept());
    }

    public static getDeleteButtonForSystem(name: string) {
        console.log(`Getting button for system: ${name}`);
        return SystemCatalogHelper.openSystem(name)
            .then(() => browser.wait(SystemCatalogHelper.systemPage.isDeleteButtonLoaded(), SystemCatalogHelper.waitUpTo.twentySeconds))
            .then(() => SystemCatalogHelper.systemPage.getDeleteButton());
    }

    public static setMainSystem(mainSystemName: string, childSystemName: string) {
        console.log(`Deleting system: ${childSystemName}`);
        return SystemCatalogHelper.openSystem(childSystemName)
            .then(() => Select2.searchFor(mainSystemName, "s2id_system-parent"))
            .then(() => Select2.waitForDataAndSelect());
    }

    public static setSystemToPublic(system: string) {
        console.log(`Setting system: ${system} to Public`);
        return SystemCatalogHelper.openSystem(system)
            .then(() => Select2.selectWithNoSearch("Offentlig", this.visibilitySelect2));

    }

    public static createLocalSystem(name: string) {
        console.log(`Creating local system for system: ${name}`);
        return SystemCatalogHelper.pageObject.getPage()
            .then(() => SystemCatalogHelper.waitForKendoGrid())
            .then(() => SystemCatalogHelper.getActivationToggleButton(name).click())
            .then(() => console.log("Local system created"));
    }

    public static openSystem(name: string) {
        console.log(`open details for system: ${name}`);
        return SystemCatalogHelper.pageObject.getPage()
            .then(() => SystemCatalogHelper.waitForKendoGrid())
            .then(() => SystemCatalogHelper.findCatalogColumnsFor(name).first().click());
    }

    public static findCatalogColumnsFor(name: string) {
        return SystemCatalogHelper.pageObject.kendoToolbarWrapper.getFilteredColumnElement(SystemCatalogHelper.pageObject.kendoToolbarWrapper.columnObjects().catalogName, name);
    }

    public static getActivationToggleButton(name: string) {
        const filteredRows = SystemCatalogHelper.findCatalogColumnsFor(name);
        return filteredRows.first().element(by.xpath("../..")).element(this.cssHelper.byDataElementType(this.consts.toggleActivatingSystem));
    }

    public static resetFilters() {
        console.log("Resetting system catalog filters");
        return SystemCatalogHelper.pageObject.getPage()
            .then(() => SystemCatalogHelper.waitForKendoGrid())
            .then(() => SystemCatalogHelper.pageObject.kendoToolbarWrapper.headerButtons().resetFilter.click());
    }

    public static waitForKendoGrid() {
        console.log("Waiting for kendo grid to be ready");
        return SystemCatalogHelper.pageObject.waitForKendoGrid();

    }
}
export = SystemCatalogHelper;