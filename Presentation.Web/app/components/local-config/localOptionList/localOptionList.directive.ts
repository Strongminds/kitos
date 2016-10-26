﻿module Kitos.LocalAdmin.Directives {
    "use strict";

    function setupDirective(): ng.IDirective {
        return {
            scope: {
                editState: "@state",
                dirId: "@",
                optionType: "@"
            },
            controller: LocalOptionListDirective,
            controllerAs: "ctrl",
            bindToController: {
                optionsUrl: "@",
                title: "@"
            },
            template: `<h2>{{ ctrl.title }}</h2><div id="{{ ctrl.dirId }}" data-kendo-grid="{{ ctrl.mainGrid }}" data-k-options="{{ ctrl.mainGridOptions }}"></div>`
        };
    }

    interface IDirectiveScope {
        title: string;
        editState: string;
        optionsUrl: string;
        optionId: string;
        optionType: string;
        dirId: string;
    }

    class LocalOptionListDirective implements IDirectiveScope {
        public optionsUrl: string;
        public title: string;
        public editState: string;
        public optionId: string;
        public dirId: string;
        public optionType: string;

        public mainGrid: IKendoGrid<Models.IOptionEntity>;
        public mainGridOptions: IKendoGridOptions<Models.IOptionEntity>;

        public static $inject: string[] = ["$http", "$timeout", "_", "$", "$state", "notify", "$scope"];

        constructor(
            private $http: ng.IHttpService,
            private $timeout: ng.ITimeoutService,
            private _: ILoDashWithMixins,
            private $: JQueryStatic,
            private $state: ng.ui.IStateService,
            private notify,
            private $scope) {

            this.$scope.$state = $state;
            this.editState = $scope.editState;
            this.dirId = $scope.dirId;
            this.optionType = $scope.optionType;

            this.mainGridOptions = {
                dataSource: {
                    type: "odata-v4",
                    transport: {
                        read: {
                            url: this.optionsUrl,
                            dataType: "json"
                        }
                    },
                    sort: {
                        field: "priority",
                        dir: "desc"
                    },
                    pageSize: 100,
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                    schema: {
                        model: {
                            id: "Id"
                        }
                    }
                } as kendo.data.DataSourceOptions,
                pageable: {
                    refresh: true,
                    pageSizes: [10, 25, 50, 100, 200],
                    buttonCount: 5
                },
                sortable: {
                    mode: "single"
                },
                editable: "popup",
                reorderable: true,
                resizable: true,
                filterable: {
                    mode: "row"
                },
                groupable: false,
                columnMenu: {
                    filterable: false
                },
                columns: [
                    {
                        field: "IsLocallyAvailable", title: "Aktiv", width: 112,
                        persistId: "isLocallyAvailable", // DON'T YOU DARE RENAME!
                        attributes: { "class": "text-center" },
                        template: `# if(IsObligatory) { # <span class="glyphicon glyphicon-check text-grey" aria-hidden="true" title="Obligatorisk"></span> # } else { # <input type="checkbox" data-ng-model="dataItem.IsLocallyAvailable" data-global-option-id="{{ dataItem.Id }}" data-autosave="${this.optionsUrl}" data-field="OptionId"> # } #`,
                        hidden: false,
                        filterable: false,
                        sortable: false
                    },
                    /*{
                        field: "Id", title: "Nr.", width: 230,
                        persistId: "id", // DON'T YOU DARE RENAME!
                        template: (dataItem) => dataItem.Id.toString(),
                        hidden: false
                    },*/
                    {
                        field: "Name", title: "Navn", width: 230,
                        persistId: "name", // DON'T YOU DARE RENAME!
                        template: (dataItem) => dataItem.Name,
                        hidden: false,
                        filterable: {
                            cell: {
                                dataSource: [],
                                showOperators: false,
                                operator: "contains"
                            }
                        }
                    },
                    {
                        field: "Description", title: "Beskrivelse", width: 230,
                        persistId: "description", // DON'T YOU DARE RENAME!
                        template: (dataItem) => dataItem.Description,
                        hidden: false,
                        filterable: {
                            cell: {
                                dataSource: [],
                                showOperators: false,
                                operator: "contains"
                            }
                        }
                    }, {
                        field: "priority",
                        title: "priority",
                        persistId: "priority", // DON'T YOU DARE RENAME!
                        hidden: true,
                        filterable: false
                    },
                    {
                        name: "editOption",
                        text: "Redigér",
                        template: "<button type='button' class='btn btn-link' title='Redigér rolle' ng-click='ctrl.editOption($event)'><i class='fa fa-pencil' aria-hidden='true'></i></button>",
                        title: " ",
                        width: 176
                    } as any
                ]
            };
        }

        private editOption = (e: JQueryEventObject) => {
            e.preventDefault();
            var entityGrid = this.$(`#${this.dirId}`).data("kendoGrid");
            var selectedItem = entityGrid.dataItem(this.$(e.currentTarget).closest("tr"));
            this.optionId = selectedItem.get("id");
            this.$scope.$state.go(this.editState, { id: this.optionId, optionsUrl: this.optionsUrl, optionType: this.optionType });
        }
    }
    angular.module("app")
        .directive("localOptionList", setupDirective);
}