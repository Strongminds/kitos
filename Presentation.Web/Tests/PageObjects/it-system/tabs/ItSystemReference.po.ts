﻿import KendoToolbarWrapper = require("../../../object-wrappers/KendoToolbarWrapper")

var ec = protractor.ExpectedConditions;

class ItSystemReference{ 

    public kendoToolbarWrapper = new KendoToolbarWrapper();

    public isReferenceCreateFormLoaded(): webdriver.until.Condition<boolean> {
        return ec.visibilityOf(this.kendoToolbarWrapper.inputFields().referenceCreator);
    }

    public isEditReferenceLoaded(): webdriver.until.Condition<boolean> {
        return ec.visibilityOf(this.kendoToolbarWrapper.headerButtons().editReference);
    }

    public isCreateReferenceLoaded(): webdriver.until.Condition<boolean> {
        return ec.visibilityOf(this.kendoToolbarWrapper.headerButtons().createReference);
    }

    public isElementLoaded(element: protractor.ElementFinder): webdriver.until.Condition<boolean> {
        return ec.visibilityOf(element);
    }
}

export = ItSystemReference;