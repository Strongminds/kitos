class kendoToolbarWrapper {

    public mainGridElement = element(by.id("mainGrid"));

    public resetFiltersButton = this.mainGridElement.element(by.buttonText("Nulstil"));

    public saveFiltersButton = this.mainGridElement.element(by.buttonText("Gem filter"));

    public useFiltersButton = this.mainGridElement.element(by.buttonText("Anvend filter"));

    public deleteFiltersButton = this.mainGridElement.element(by.buttonText("Slet filter"));

    // Need fix how selectors work.
    public roleSelector = this.mainGridElement.element(by.css("span.k-widget.k-dropdown.k-header")); 

    public exportButton = this.mainGridElement.element(by.css("a.k-button.k-button-icontext.pull-right.k-grid-excel"));

    //public columnHeaderValidity = this.mainGridElement.element(by.linkText(""));
    public columnLocalSystemID = this.mainGridElement.element(by.linkText("Lokal system ID"));
    public columnSystemUUID = this.mainGridElement.element(by.linkText("UUID"));
    public columnParentSystem = this.mainGridElement.element(by.linkText("Overordnet IT System"));
    public columnSystemName = this.mainGridElement.element(by.linkText("IT System"));
    public columnSystemVersion = this.mainGridElement.element(by.linkText("Version"));
    public columnLocalCallName = this.mainGridElement.element(by.linkText("Lokal kaldenavn"));
    public columnResponsibleOrganization = this.mainGridElement.element(by.linkText("Ansv. organisationsenhed"));
    public columnBusinessType = this.mainGridElement.element(by.linkText("Forretningstype"));
    public columnApplicationType = this.mainGridElement.element(by.linkText("Applikationstype"));
    public columnKLEID = this.mainGridElement.element(by.linkText("KLE ID"));
    public columnKLEName = this.mainGridElement.element(by.linkText("KLE navn"));
    public columnReference = this.mainGridElement.element(by.linkText("Reference"));
    public columnExternalReference = this.mainGridElement.element(by.linkText("Mappe ref"));
    public columnDataType = this.mainGridElement.element(by.linkText("Datatype"));
    public columnContract = this.mainGridElement.element(by.linkText("Kontrakt"));
    public columnSupplier = this.mainGridElement.element(by.linkText("Leverandør"));
    public columnProject = this.mainGridElement.element(by.linkText("IT Projekt"));
    public columnUsedBy = this.mainGridElement.element(by.linkText("Taget i anvendelse af"));
    public columnLastEditedUser = this.mainGridElement.element(by.linkText("Sidst redigeret: Bruger"));
    public columnLastEditedDate = this.mainGridElement.element(by.linkText("Sidste redigeret: Dato"));
    public columnDateOfUse = this.mainGridElement.element(by.linkText("Ibrugtagningsdato"));
    public columnArchiveDuty = this.mainGridElement.element(by.linkText("Arkiveringspligt"));
    public columnHoldsDocument = this.mainGridElement.element(by.linkText("Er dokumentbærende"));
    public columnEndDate = this.mainGridElement.element(by.linkText("Journalperiode slutdato"));
    public columnRiskEvaluation = this.mainGridElement.element(by.linkText("Risikovurdering"));
    public columnUrlListing = this.mainGridElement.element(by.linkText("Fortegnelse"));
    public columnDataProcessingAgreement = this.mainGridElement.element(by.linkText("Databehandleraftale"));


    public dataGrid = this.mainGridElement.element(by.css("div.k-grid-content.k-auto-scrollable"));


    private gridFooter = this.mainGridElement.element(by.css("div.k-pager-wrap.k-grid-pager.k-widget.k-floatwrap"));

    public gridNavigatorFirstPage = this.gridFooter.all(by.css("a")).get(0);
    public gridNavigatorOnePageBack = this.gridFooter.all(by.css("a")).get(1);
    public gridNavigatorOnePageForward = this.gridFooter.all(by.css("a")).get(2);
    public gridNavigatorLastPage = this.gridFooter.all(by.css("a")).get(3);
    public gridNavigatorRefresh = this.gridFooter.all(by.css("a")).get(4);

    // Need fix how selectors work.
    public gridNavigatorSelector = this.gridFooter.element(by.css("span.k-pager-sizes.k-label"));

}

export = kendoToolbarWrapper;