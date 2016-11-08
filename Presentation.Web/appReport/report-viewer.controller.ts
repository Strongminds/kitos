﻿
module Kitos.Reports {
    "use strict";

    export class ReportViewerController {
        viewer: any;
        designer: any;
        report: Kitos.Models.IReport;
        stiReport: any;
        public canDesignReport: boolean;

        emptyreport = { "ReportVersion": "2016.1.28", "ReportGuid": "9ad83767ecc3b68cb62c086805556fce", "ReportName": "Report", "ReportAlias": "Report", "ReportCreated": "/Date(1472651877000+0200)/", "ReportChanged": "/Date(1472651877000+0200)/", "EngineVersion": "EngineV2", "CalculationMode": "Interpretation", "Pages": { "0": { "Ident": "StiPage", "Name": "Page1", "Guid": "a4875a4e-cd43-da99-360b-a7560ca0b913", "Interaction": { "Ident": "StiInteraction" }, "Border": ";;2;;;;;solid:Black", "Brush": "solid:Transparent", "Components": { "0": { "Ident": "StiText", "Name": "Text1", "MinSize": "0,0", "MaxSize": "0,0", "ClientRectangle": "0.6,4.8,17.4,4.2", "Interaction": { "Ident": "StiInteraction" }, "Text": { "Value": "Rapporten er tom og venter på at blive designet." }, "HorAlignment": "Center", "Font": "Verdana;28;;", "Border": ";;;;;;;solid:Black", "Brush": "solid:Transparent", "TextBrush": "solid:Black", "TextOptions": { "WordWrap": true }, "Type": "Expression" } }, "PageWidth": 21.01, "PageHeight": 29.69, "Watermark": { "TextBrush": "solid:50,0,0,0" }, "Margins": { "Left": 1, "Right": 1, "Top": 1, "Bottom": 1 } } } }

        public static $inject = ["stimulsoftService", "reportService", "$window", "notify", "userService"];
        constructor(private stimulsoftService: Kitos.Services.StimulsoftService,
            private reportService: Kitos.Services.ReportService,
            private $window: ng.IWindowService,
            private notify,
            private userService: Services.IUserService) {
            let self = this;

            this.userService.getUser().then((user: Services.IUser) => {
                self.canDesignReport = user.isGlobalAdmin || user.isLocalAdmin || user.isReportAdmin;
                // 02/11 MEMA: The translation is far from done. Add back, when translated.
                //stimulsoftService.setLocalizationFile("./appReport/locales/da-DK.xml")
                this.viewer = stimulsoftService.getViewer(this.buildViewerOptions(), "Viewer");

                // Add the design button event
                this.viewer.onDesignReport = function (e) {
                    this.visible = false;

                    // create designer object
                    this.designer = stimulsoftService.getDesigner(self.buildDesignerOptions(), "designer");
                    // bind events to designer object
                    this.designer.onExit = self.designerOnExit;
                    this.designer.onSaveReport = self.designerSaveReport;
                    // render designer on dom element
                    this.designer.renderHtml("reportDesigner");
                    this.designer.visible = true;
                    this.designer.report = e.report;
                };

                this.viewer.showProcessIndicator();
                this.viewer.renderHtml("reportViewer");
                this.loadReport(this.getReportId());
            })
        }

        getReportId = () => {
            var searchObject = this.$window.location.search;
            var regex = /([0-9]+)/g;
            var matches = searchObject.match(regex);
            let id = matches[0];
            if (matches.length === 1) {
                return parseInt(id);
            }
        };

        buildViewerOptions = () => {
            let options = this.stimulsoftService.getViewerOptions();
            options.appearance.scrollbarsMode = true;
            options.toolbar.showDesignButton = this.canDesignReport;
            options.appearance.fullScreenMode = true;
            return options;
        };

        buildDesignerOptions = () => {
            // set designer options TODO load from db
            let options = this.stimulsoftService.getDesignerOptions();
            options.appearance.fullScreenMode = true;
            options.appearance.showSaveDialog = false;
            return options;
        };

        loadReport = (id: number) => {
            this.reportService.GetById(id).then((result) => {
                this.report = result.data;
                this.stiReport = this.stimulsoftService.getReport();

                if (this.report.Definition && this.report.Definition.length > 0) {
                    //  Load reports from JSON object
                    this.stiReport.load(this.report.Definition);
                }
                else {
                    this.stiReport.load(this.emptyreport);
                }
                //Assign the report to the viewer
                this.viewer.report = this.stiReport;
            })
        }


        designerSaveReport = (saveEvent) => {
            this.report.Definition = saveEvent.report.saveToJsonString();
            this.reportService.saveReport(this.report);
            this.viewer.report = saveEvent.report;
            this.notify.addSuccessMessage("Rapporten er gemt", true);
        };

        designerOnExit = (exitEvent) => {
            this.designer.visible = false;
            this.viewer.report = exitEvent.report;
            this.viewer.visible = true;
        }
    }

    angular.module("reportApp").controller("reportViewerController", Kitos.Reports.ReportViewerController);
}
