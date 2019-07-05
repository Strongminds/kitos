import CSSLocator = require("./CSSLocatorHelper");
import Constants = require("../../app/utility/Constants");

// Typings not yet working with protractor 
//import protractor = require("protractor");

//type HeaderButtons = {
//    resetFilter: protractor.ElementFinder, saveFilter: protractor.ElementFinder, useFilter: protractor.ElementFinder, deleteFilter: protractor.ElementFinder,
//    export: protractor.ElementFinder
//};

//type ClickableColumnHeaders = {
//    localSystemId: protractor.ElementFinder, systemUUID: protractor.ElementFinder, parentSystem: protractor.ElementFinder, systemName: protractor.ElementFinder,
//    systemVersion: protractor.ElementFinder, localCallName: protractor.ElementFinder, reponsibleOrganization: protractor.ElementFinder, businessType: protractor.ElementFinder,
//    applicationType: protractor.ElementFinder, KLEID: protractor.ElementFinder, KLEName: protractor.ElementFinder, Reference: protractor.ElementFinder,
//    externalReference: protractor.ElementFinder, dataType: protractor.ElementFinder, contract: protractor.ElementFinder, supplier: protractor.ElementFinder,
//    project: protractor.ElementFinder, usedBy: protractor.ElementFinder, lastEditedBy: protractor.ElementFinder, lastEditedAt: protractor.ElementFinder,
//    dateOfUse: protractor.ElementFinder, archiveDuty: protractor.ElementFinder, holdsDocument: protractor.ElementFinder, endDate: protractor.ElementFinder,
//    riskEvaluation: protractor.ElementFinder, urlListing: protractor.ElementFinder, dataProcessingAgreement: protractor.ElementFinder
//};

//type ColumnObjects = {
//    localSystemId: protractor.ElementArrayFinder, systemUUID: protractor.ElementArrayFinder, parentSystem: protractor.ElementArrayFinder, systemName: protractor.ElementArrayFinder,
//    systemVersion: protractor.ElementArrayFinder, localCallName: protractor.ElementArrayFinder, reponsibleOrganization: protractor.ElementArrayFinder, businessType: protractor.ElementArrayFinder,
//    applicationType: protractor.ElementArrayFinder, KLEID: protractor.ElementArrayFinder, KLEName: protractor.ElementArrayFinder, Reference: protractor.ElementArrayFinder,
//    externalReference: protractor.ElementArrayFinder, dataType: protractor.ElementArrayFinder, contract: protractor.ElementArrayFinder, supplier: protractor.ElementArrayFinder,
//    project: protractor.ElementArrayFinder, usedBy: protractor.ElementArrayFinder, lastEditedBy: protractor.ElementArrayFinder, lastEditedAt: protractor.ElementArrayFinder,
//    dateOfUse: protractor.ElementArrayFinder, archiveDuty: protractor.ElementArrayFinder, holdsDocument: protractor.ElementArrayFinder, endDate: protractor.ElementArrayFinder,
//    riskEvaluation: protractor.ElementArrayFinder, urlListing: protractor.ElementArrayFinder, dataProcessingAgreement: protractor.ElementArrayFinder
//};

//type footerNavigationButtons = {
//    firstPage: protractor.ElementFinder, onePageBack: protractor.ElementFinder, onePageForward: protractor.ElementFinder, lastPage: protractor.ElementFinder,
//    refresh: protractor.ElementFinder
//};


type HeaderButtons = {
    resetFilter, saveFilter, useFilter, deleteFilter,
    export
};

type ClickableColumnHeaders = {
    localSystemId, systemUUID, parentSystem, systemName,
    systemVersion, localCallName, reponsibleOrganization, businessType,
    applicationType, KLEID, KLEName, Reference,
    externalReference, dataType, contract, supplier,
    project, usedBy, lastEditedBy, lastEditedAt,
    dateOfUse, archiveDuty, holdsDocument, endDate,
    riskEvaluation, urlListing, dataProcessingAgreement
};

type ColumnObjects = {
    localSystemId, systemUUID, parentSystem, systemName,
    systemVersion, localCallName, reponsibleOrganization, businessType,
    applicationType, KLEID, KLEName, Reference,
    externalReference, dataType, contract, supplier,
    project, usedBy, lastEditedBy, lastEditedAt,
    dateOfUse, archiveDuty, holdsDocument, endDate,
    riskEvaluation, urlListing, dataProcessingAgreement
};

type footerNavigationButtons = {
    firstPage, onePageBack, onePageForward, lastPage,
    refresh
};


var byHook = new CSSLocator().byDataHook;

class kendoToolbarWrapper {

    public headerButtons(): HeaderButtons {
        var buttons: HeaderButtons = {
            resetFilter: element(byHook("resetFilter")),
            saveFilter: element(byHook("saveFilter")),
            useFilter: element(byHook("useFilter")),
            deleteFilter: element(byHook("removeFilter")),
            export: this.notYetAHook()
        };
        return buttons;
    }

    public columnHeaders(): ClickableColumnHeaders {
        var kendo = new kendoHelper();
        var consts = new Constants();

        var columns: ClickableColumnHeaders = {
            localSystemId: this.notYetAHook(),
            systemUUID: this.notYetAHook(),
            parentSystem: this.notYetAHook(),
            systemName: kendo.getColumnHeaderClickable(consts.kendoSystemNameHeader),
            systemVersion: this.notYetAHook(),
            localCallName: this.notYetAHook(),
            reponsibleOrganization: this.notYetAHook(),
            businessType: this.notYetAHook(),
            applicationType: this.notYetAHook(),
            KLEID: this.notYetAHook(),
            KLEName: this.notYetAHook(),
            Reference: this.notYetAHook(),
            externalReference: this.notYetAHook(),
            dataType: this.notYetAHook(),
            contract: this.notYetAHook(),
            supplier: this.notYetAHook(),
            project: this.notYetAHook(),
            usedBy: this.notYetAHook(),
            lastEditedBy: this.notYetAHook(),
            lastEditedAt: this.notYetAHook(),
            dateOfUse: this.notYetAHook(),
            archiveDuty: this.notYetAHook(),
            holdsDocument: this.notYetAHook(),
            endDate: this.notYetAHook(),
            riskEvaluation: this.notYetAHook(),
            urlListing: this.notYetAHook(),
            dataProcessingAgreement: this.notYetAHook()
        };
        return columns;
    }

    public columnObjects(): ColumnObjects {
        var kendo = new kendoHelper();
        var consts = new Constants();

        var columns: ColumnObjects = {
            localSystemId: this.notYetAHook(),
            systemUUID: this.notYetAHook(),
            parentSystem: this.notYetAHook(),
            systemName: kendo.getColumnItemLinks(consts.kendoSystemNameObjects),
            systemVersion: this.notYetAHook(),
            localCallName: this.notYetAHook(),
            reponsibleOrganization: this.notYetAHook(),
            businessType: this.notYetAHook(),
            applicationType: this.notYetAHook(),
            KLEID: this.notYetAHook(),
            KLEName: this.notYetAHook(),
            Reference: this.notYetAHook(),
            externalReference: this.notYetAHook(),
            dataType: this.notYetAHook(),
            contract: this.notYetAHook(),
            supplier: this.notYetAHook(),
            project: this.notYetAHook(),
            usedBy: this.notYetAHook(),
            lastEditedBy: this.notYetAHook(),
            lastEditedAt: this.notYetAHook(),
            dateOfUse: this.notYetAHook(),
            archiveDuty: this.notYetAHook(),
            holdsDocument: this.notYetAHook(),
            endDate: this.notYetAHook(),
            riskEvaluation: this.notYetAHook(),
            urlListing: this.notYetAHook(),
            dataProcessingAgreement: this.notYetAHook()
        };
        return columns;
    }

    public footerNavigationButtons(): footerNavigationButtons {
        var buttons: footerNavigationButtons = {
            firstPage: this.notYetAHook(),
            onePageBack: this.notYetAHook(),
            onePageForward: this.notYetAHook(),
            lastPage: this.notYetAHook(),
            refresh: this.notYetAHook()
        }
        return buttons;
    }
    
    // Need fix how selectors work.

    public roleSelector = null;
    public gridNavigatorSelector = null;

    // Dummy method until every kendo field is filled with a data-hook
    public notYetAHook() {
        return element(null);
    }

}

class kendoHelper {

    // Needed until there's found a way to add a data-hook to the a tag directly
    public getColumnHeaderClickable(headerHook: string) {
        return element(byHook(headerHook)).element(by.css("a[class=k-link]"));
    }

    public getColumnItemLinks(itemHook: string) {
        return element.all(byHook(itemHook)).all(by.tagName("a"));
    }
}

export = kendoToolbarWrapper;

