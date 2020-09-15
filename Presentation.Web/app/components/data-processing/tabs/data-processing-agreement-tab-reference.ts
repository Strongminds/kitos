﻿module Kitos.DataProcessing.Agreement.Edit.Ref {
    "use strict";

    export class EditRefDataProcessingAgreementController {
        static $inject: Array<string> = [
            "$scope",
            "$state",
            "user",
            "notify",
            "hasWriteAccess",
            "referenceService",
            "dataProcessingAgreement"
        ];

        constructor(
            private readonly $scope,
            private $state,
            private readonly user: Services.IUser,
            private notify,
            public hasWriteAccess,
            private referenceService,
            private dataProcessingAgreement) {

            this.$scope.mainGridOptions = {
                dataSource: {
                    data: dataProcessingAgreement.references,
                    pageSize: 10
                },
                sortable: true,
                pageable: {
                    refresh: false,
                    pageSizes: true,
                    buttonCount: 5
                },
                columns: [{
                    field: "title",
                    title: "Dokumenttitel",
                    template: data => {
                        if (Utility.Validation.isValidExternalReference(data.url)) {
                            return "<a target=\"_blank\" href=\"" + data.url + "\">" + data.name + "</a>";
                        } else {
                            return data.name;
                        }
                    },
                    width: 240
                }, {
                    field: "referenceId",
                    title: "Evt. dokumentID/Sagsnr./anden referenceContact"
                }, {
                    title: "Rediger",
                    template: dataItem => {
                        var HTML = "<button type='button' data-ng-disabled='" + !this.hasWriteAccess + "' class='btn btn-link' title='Redigér reference' data-ng-click=\"edit(" + dataItem.id + ")\"><i class='fa fa-pencil' aria-hidden='true'></i></button>";
                        HTML += " <button type='button' data-ng-disabled='" + !this.hasWriteAccess + "' data-confirm-click=\"Er du sikker på at du vil slette?\" class='btn btn-link' title='Slet reference' data-confirmed-click='deleteReference(" + dataItem.id + ")'><i class='fa fa-trash-o'  aria-hidden='true'></i></button>";


                        if (Utility.Validation.isValidExternalReference(dataItem.url)) {
                            if (dataItem.id === dataProcessingAgreement.referenceId) {
                                HTML = HTML + "<button data-ng-disabled='" + !this.hasWriteAccess + "' data-uib-tooltip=\"Vises i overblik\" tooltip-placement='right' class='btn btn-link' data-ng-click='setChosenReference(" + dataItem.id + ")'><img class='referenceIcon chosen' src=\"/Content/img/VisIOverblik.svg\"/></button>";//valgt
                            } else {
                                HTML = HTML + "<button data-ng-disabled='" + !this.hasWriteAccess + "' data-uib-tooltip=\"Vis objekt i overblik\"  tooltip-placement='right' class='btn btn-link' data-ng-click='setChosenReference(" + dataItem.id + ")'><img class='referenceIcon' src=\"/Content/img/VisIOverblik.svg\"></img></button>";//vælg

                            }
                        }

                        return HTML;
                    }
                }],
                toolbar: [
                    {
                        name: "addReference",
                        text: "Tilføj reference",
                        template: () => {
                            if (this.hasWriteAccess) {
                                return `<a id="addReference" class="btn btn-success btn-sm" href="\\#/data-processing/edit/${dataProcessingAgreement.id}/reference/createReference/${dataProcessingAgreement.id}"'>Tilføj reference</a>`;
                            } else {
                                return "";
                            }
                        }
                    }]
            };

            this.$scope.deleteReference = referenceId => {
                var msg = notify.addInfoMessage("Sletter...");

                referenceService.deleteReference(referenceId, user.currentOrganizationId)
                    .then(success => {
                            msg.toSuccessMessage("Slettet!");
                            reload();
                        },
                        error => msg.toErrorMessage("Fejl! Kunne ikke slette!"));
            };

            function reload() {
                $state.go(".", null, { reload: true });
            };
        }
    }

    angular
        .module("app")
        .config(["$stateProvider", ($stateProvider: ng.ui.IStateProvider) => {
            $stateProvider.state("data-processing.edit-agreement.reference", {
                url: "/reference",
                templateUrl: "app/components/it-reference/it-reference.view.html",
                controller: EditRefDataProcessingAgreementController,
                controllerAs: "vm",
                resolve: {
                    referenceService: ["referenceServiceFactory", (referenceServiceFactory) => referenceServiceFactory.createDpaReference()],
                },
            });
        }]);
}