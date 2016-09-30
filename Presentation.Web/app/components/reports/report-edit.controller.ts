﻿module Kitos.Reports {
    "use strict";

    export class EditReportController {
        public title: string
        public name: string
        public description: string
        public categoryTypeId: number
        public accessModifier: string
        public categories: any
        public selectedCategory: any
        reportId: number

        public static $inject: string[] = ["$uibModalInstance", "$stateParams", "$http", "notify", "reportService", "_"];
        constructor(private $uibModalInstance: ng.ui.bootstrap.IModalServiceInstance,
            private $stateParams: ng.ui.IStateParamsService,
            private $http: ng.IHttpService,
            private notify,
            private reportService: Services.ReportService,
            private _: ILoDashWithMixins) {

            this.init($stateParams["id"])
        }

        init = (id: number) => {
            this.reportId = id;
            if (id === 0) {
                this.title = "Opret rapport"
                this.accessModifier = "Local"
            }
            else {
                this.title = "Redigér rapport"
            }
            this.reportService.getReportCategories().then((result) => {
                this.categories = result.data.value;

                if (id > 0) {
                    this.reportService.GetById(id).then((result) => {
                        let rpt = result.data
                        this.name = rpt.Name
                        this.description = rpt.Description
                        this.categoryTypeId = rpt.CategoryTypeId
                        this.accessModifier = rpt.AccessModifier
                        if (this.categoryTypeId == 0) {
                            this.selectedCategory = this.categories[0];
                        }
                        else {
                            this.selectedCategory = this._.find(this.categories, 'Id', this.categoryTypeId);
                        }
                    })
                }
            })
        }

        public ok() {
            if (this.selectedCategory) {
                this.categoryTypeId = this.selectedCategory.Id;
            }

            if (this.reportId === 0) {
                let payload = {
                    Id: 0,
                    Name: this.name,
                    Description: this.description,
                    CategoryTypeId: this.categoryTypeId,
                    AccessModifier: this.accessModifier
                }
                this.$http.post("/odata/reports", payload);
            }
            else {
                let payload = {
                    Name: this.name,
                    Description: this.description,
                    CategoryTypeId: this.categoryTypeId,
                    AccessModifier: this.accessModifier
                }
                this.$http.patch(`/odata/reports(${this.reportId})`, payload);
            }
        }

        public cancel() {
            this.$uibModalInstance.close();
        }
    }

    angular
        .module("app")
        .config(["$stateProvider", ($stateProvider: ng.ui.IStateProvider) => {
            $stateProvider.state("reports.report-edit", {
                url: "/{id:int}/edit",
                onEnter: [
                    "$state", "$stateParams", "$uibModal",
                    ($state: ng.ui.IStateService, $stateParams: ng.ui.IStateParamsService, $uibModal: ng.ui.bootstrap.IModalService) => {
                        $uibModal.open({
                            templateUrl: "app/components/reports/report-edit.modal.view.html",
                            // fade in instead of slide from top, fixes strange cursor placement in IE
                            // http://stackoverflow.com/questions/25764824/strange-cursor-placement-in-modal-when-using-autofocus-in-internet-explorer
                            windowClass: "modal fade in",
                            controller: EditReportController,
                            controllerAs: "vm",
                        }).result.then(() => {
                            // OK
                            // GOTO parent state and reload
                            $state.go("^", null, { reload: true });
                        },
                            () => {
                                // Cancel
                                // GOTO parent state
                                $state.go("^");
                            });
                    }
                ]
            });
        }]);
}
