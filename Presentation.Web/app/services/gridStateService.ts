﻿module Kitos.Services {
    "use strict";

    interface IGridSavedState {
        dataSource?: kendo.data.DataSourceOptions;
        columnState?: { [persistId: string]: { index: number; width: number, hidden?: boolean } };
    }

    export interface IGridStateFactory {
        getService: (storageKey: string, userId: number) => IGridStateService;
    }

    export interface IGridStateService {
        saveGridOptions: (grid: Kitos.IKendoGrid<any>) => void;
        loadGridOptions: (grid: Kitos.IKendoGrid<any>, initialFilter?) => void;
        saveGridProfile: (grid: Kitos.IKendoGrid<any>) => void;
        loadGridProfile: (grid: Kitos.IKendoGrid<any>) => void;
        doesGridProfileExist: () => boolean;
        removeProfile: () => void;
        removeLocal: () => void;
        removeSession: () => void;
    }

    gridStateService.$inject = [
        "$window",
        "$timeout",
        "$",
        "JSONfn",
        "_"
    ];

    function gridStateService(
        $window: ng.IWindowService,
        $timeout: ng.ITimeoutService,
        $: JQueryStatic,
        JSONfn: JSONfn.JSONfnStatic,
        _: _.LoDashStatic
    ): IGridStateFactory {
        var factory: IGridStateFactory = {
            getService: getService
        };

        return factory;

        function getService(storageKey: string, userId: number): IGridStateService {
            if (!storageKey)
                throw new Error("Missing parameter: storageKey");

            storageKey = userId+"-"+storageKey;
            var profileStorageKey = storageKey + "-profile";
            var service: IGridStateService = {
                saveGridOptions: saveGridOptions,
                loadGridOptions: loadGridOptions,
                saveGridProfile: saveGridProfile,
                loadGridProfile: loadGridProfile,
                doesGridProfileExist: doesGridProfileExist,
                removeProfile: removeProfile,
                removeLocal: removeLocal,
                removeSession: removeSession
            };
            return service;

            // saves grid state to localStorage
            function saveGridOptions(grid: Kitos.IKendoGrid<any>) {
                // timeout fixes columnReorder saves before the column is actually reordered
                // http://stackoverflow.com/questions/21270748/kendo-grid-saving-state-in-columnreorder-event
                $timeout(() => {
                    var options = grid.getOptions();

                    saveGridStateForever(options);
                    saveGridStateForSession(options);
                });
            }

            // loads kendo grid options from localStorage
            function loadGridOptions(grid: Kitos.IKendoGrid<any>, initialFilter?: kendo.data.DataSourceFilters): void {
                var gridId = grid.element[0].id;
                var storedState = getStoredOptions();
                var columnState = <IGridSavedState> _.pick(storedState, "columnState");

                var gridOptionsWithInitialFitler = _.merge({ dataSource: { filter: initialFilter } }, storedState);
                var gridOptions = _.omit(gridOptionsWithInitialFitler, "columnState");

                var visableColumnIndex = 0;
                _.forEach(columnState.columnState, (state, key) => {
                    var columnIndex = _.findIndex(grid.columns, column => {
                        if (!column.hasOwnProperty("persistId")) {
                            console.error(`Unable to find persistId property in grid column with field=${column.field}`);
                            return false;
                        }

                        return column.persistId === key;
                    });

                    if (columnIndex !== -1) {
                        var columnObj = grid.columns[columnIndex];
                        // reorder column
                        if (state.index !== columnIndex) {
                            // check if index is out of bounds
                            if (state.index < grid.columns.length) {
                                grid.reorderColumn(state.index, columnObj);
                            }
                        }

                        // show / hide column
                        if (state.hidden !== columnObj.hidden) {
                            if (state.hidden) {
                                grid.hideColumn(columnObj);
                            } else {
                                grid.showColumn(columnObj);
                            }
                        }

                        if (!columnObj.hidden) {
                            visableColumnIndex++;
                        }

                        // resize column
                        if (state.width !== columnObj.width) {
                            // manually set the width on the column, cause changing the css doesn't update it
                            columnObj.width = state.width;
                            // $timeout is required here, else the jQuery select doesn't work
                            $timeout(() => {
                                // set width of column header
                                $(`#${gridId} .k-grid-header`)
                                    .find("colgroup col")
                                    .eq(visableColumnIndex)
                                    .width(state.width);

                                // set width of column
                                $(`#${gridId} .k-grid-content`)
                                    .find("colgroup col")
                                    .eq(visableColumnIndex)
                                    .width(state.width);
                            });
                        }
                    }
                });

                grid.setOptions(gridOptions);
                grid.dataSource.pageSize(grid.dataSource.options.pageSize);
            }

            // gets all the saved options, both session and local, and merges
            // them together so that the correct options are overwritten
            function getStoredOptions(): IGridSavedState {
                // load options from local storage
                var localOptions;
                var localStorageItem = $window.localStorage.getItem(storageKey);
                if (localStorageItem) {
                    localOptions = JSONfn.parse(localStorageItem, true);
                }

                // load options profile from local storage
                var profileOptions;
                var profileStorageItem = $window.localStorage.getItem(profileStorageKey);
                if (profileStorageItem) {
                    profileOptions = JSONfn.parse(profileStorageItem, true);
                }

                // load options from session storage
                var sessionOptions;
                var sessionStorageItem = $window.sessionStorage.getItem(storageKey);
                if (sessionStorageItem) {
                    sessionOptions = JSONfn.parse(sessionStorageItem, true);
                }

                var options: IGridSavedState;

                if (sessionOptions) {
                    // if session options are set then use them
                    // note the order the options are merged in (below) is important!
                    options = <IGridSavedState> _.merge({}, localOptions, sessionOptions);
                } else {
                    // else use the profile options
                    // this should only happen the first time the page loads
                    // or when the session optinos are deleted
                    // note the order the options are merged in (below) is important!
                    options = <IGridSavedState> _.merge({}, localOptions, profileOptions);
                }
                return options;
            }

            // save grid options that should be stored in sessionStorage
            function saveGridStateForSession(options: Kitos.IKendoGridOptions<any>): void {
                var pickedOptions: IGridSavedState = {};
                // save filter, sort and page
                pickedOptions.dataSource = <kendo.data.DataSourceOptions> _.pick(options.dataSource, ["filter", "sort", "page"]);
                $window.sessionStorage.setItem(storageKey, JSONfn.stringify(pickedOptions));
            }

            // save grid options that should be stored in localStorage
            function saveGridStateForever(options: Kitos.IKendoGridOptions<any>): void {
                if (options) {
                    var pickedOptions: IGridSavedState = {};
                    // save pageSize
                    pickedOptions.dataSource = <kendo.data.DataSourceOptions> _.pick(options.dataSource, ["pageSize"]);

                    // save column state - dont use the kendo function for it as it breaks more than it fixes...
                    pickedOptions.columnState = {};
                    for (var i = 0; i < options.columns.length; i++) {
                        var column = options.columns[i];
                        pickedOptions.columnState[column.persistId] = { index: i, width: <number> column.width, hidden: column.hidden };
                    }

                    $window.localStorage.setItem(storageKey, JSONfn.stringify(pickedOptions));
                }
            }

            function saveGridProfile(grid: Kitos.IKendoGrid<any>): void {
                var options = grid.getOptions();
                var pickedOptions: IGridSavedState = {};
                // save filter and sort
                pickedOptions.dataSource = <kendo.data.DataSourceOptions> _.pick(options.dataSource, ["filter", "sort"]);

                $window.localStorage.setItem(profileStorageKey, JSONfn.stringify(pickedOptions));
            }

            function loadGridProfile(grid: Kitos.IKendoGrid<any>): void {
                removeSession();
                var storedState = getStoredOptions();
                var gridOptions = _.omit(storedState, "columnState");
                grid.setOptions(gridOptions);
            }

            function doesGridProfileExist(): boolean {
                if ($window.localStorage.getItem(profileStorageKey))
                    return true;
                return false;
            }

            function removeSession(): void {
                $window.sessionStorage.removeItem(storageKey);
            }

            function removeLocal(): void {
                $window.localStorage.removeItem(storageKey);
            }

            function removeProfile(): void {
                $window.localStorage.removeItem(profileStorageKey);
            }
        }
    }

    angular.module("app").factory("gridStateService", gridStateService);
}
