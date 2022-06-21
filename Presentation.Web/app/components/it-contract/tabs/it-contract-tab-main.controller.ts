﻿(function (ng, app) {
    app.config([
        '$stateProvider', function ($stateProvider) {
            $stateProvider.state('it-contract.edit.main', {
                url: '/main',
                templateUrl: 'app/components/it-contract/tabs/it-contract-tab-main.view.html',
                controller: 'contract.EditMainCtrl',
                resolve: {
                    contractTypes: [
                        'localOptionServiceFactory',
                        (localOptionServiceFactory: Kitos.Services.LocalOptions.ILocalOptionServiceFactory) =>
                        localOptionServiceFactory.create(Kitos.Services.LocalOptions.LocalOptionType.ItContractTypes)
                        .getAll()
                    ],
                    contractTemplates: [
                        'localOptionServiceFactory',
                        (localOptionServiceFactory: Kitos.Services.LocalOptions.ILocalOptionServiceFactory) =>
                        localOptionServiceFactory
                        .create(Kitos.Services.LocalOptions.LocalOptionType.ItContractTemplateTypes).getAll()
                    ],
                    purchaseForms: [
                        "localOptionServiceFactory",
                        (localOptionServiceFactory: Kitos.Services.LocalOptions.ILocalOptionServiceFactory) =>
                        localOptionServiceFactory.create(Kitos.Services.LocalOptions.LocalOptionType.PurchaseFormTypes)
                        .getAll()
                    ],
                    procurementStrategies: [
                        "localOptionServiceFactory",
                        (localOptionServiceFactory: Kitos.Services.LocalOptions.ILocalOptionServiceFactory) =>
                        localOptionServiceFactory
                        .create(Kitos.Services.LocalOptions.LocalOptionType.ProcurementStrategyTypes).getAll()
                    ],
                    orgUnits: [
                        '$http', 'contract', function ($http, contract) {
                            return $http.get('api/organizationUnit?organization=' + contract.organizationId).then(function (result) {
                                var options: Kitos.Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<number>[] = [];

                                function visit(orgUnit: Kitos.Models.Api.Organization.OrganizationUnit, indentationLevel: number) {
                                    var option = {
                                        id: String(orgUnit.id),
                                        text: orgUnit.name,
                                        indentationLevel: indentationLevel
                                    };

                                    options.push(option);

                                    _.each(orgUnit.children, function (child) {
                                        return visit(child, indentationLevel + 1);
                                    });

                                }
                                visit(result.data.response, 0);
                                return options;
                            });
                        }
                    ],
                    kitosUsers: [
                        '$http', 'user', '_', function ($http, user, _) {
                            return $http.get(`odata/Organizations(${user.currentOrganizationId})/Rights?$expand=User($select=Name,LastName,Email,Id)&$select=User`).then(function (result) {
                                let uniqueUsers = _.uniqBy(result.data.value, "User.Id");

                                let results = [];
                                _.forEach(uniqueUsers, data => {
                                    results.push({
                                        Name: data.User.Name,
                                        LastName: data.User.LastName,
                                        Email: data.User.Email,
                                        Id: data.User.Id
                                    });
                                });
                                results = _.orderBy(results, x => x.Name, 'asc');
                                return results;
                            });
                        }
                    ],
                    uiState: [
                        "uiCustomizationStateService", (uiCustomizationStateService: Kitos.Services.UICustomization.IUICustomizationStateService) => uiCustomizationStateService.getCurrentState(Kitos.Models.UICustomization.CustomizableKitosModule.ItContract)
                    ]
                }
            });
        }
    ]);

    app.controller('contract.EditMainCtrl',
        [
            '$scope', '$http', '_', '$stateParams',
            'notify', 'contract', 'contractTypes', 'contractTemplates',
            'purchaseForms', 'procurementStrategies', 'orgUnits', 'hasWriteAccess',
            'user', 'autofocus', 'kitosUsers', "uiState",
            "criticalityOptions", "select2LoadingService",
            function ($scope, $http, _, $stateParams,
                notify, contract, contractTypes, contractTemplates,
                purchaseForms, procurementStrategies, orgUnits: Kitos.Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<number>[], hasWriteAccess,
                user: Kitos.Services.IUser, autofocus, kitosUsers, uiState: Kitos.Models.UICustomization.ICustomizedModuleUI,
                criticalityOptions: Kitos.Models.IOptionEntity[], select2LoadingService: Kitos.Services.ISelect2LoadingService) {

                const blueprint = Kitos.Models.UICustomization.Configs.BluePrints.ItContractUiCustomizationBluePrint;

                bindCriticalities();
                $scope.autoSaveUrl = 'api/itcontract/' + $stateParams.id;
                $scope.autosaveUrl2 = 'api/itcontract/' + contract.id;
                $scope.contract = contract;
                $scope.contract.lastChanged = Kitos.Helpers.RenderFieldsHelper.renderDate(contract.lastChanged);
                $scope.hasWriteAccess = hasWriteAccess;
                $scope.hasViewAccess = user.currentOrganizationId == contract.organizationId;
                $scope.kitosUsers = kitosUsers;
                autofocus();
                $scope.contractTypes = contractTypes;
                $scope.contractTemplates = contractTemplates;
                $scope.purchaseForms = purchaseForms;
                $scope.procurementStrategies = procurementStrategies;
                $scope.orgUnits = orgUnits;
                $scope.allowClear = true;
                $scope.showprocurementPlanSelection = uiState.isBluePrintNodeAvailable(blueprint.children.frontPage.children.procurementPlan);
                $scope.showProcurementStrategySelection = uiState.isBluePrintNodeAvailable(blueprint.children.frontPage.children.procurementStrategy);
                var today = new Date();

                if (!contract.active) {
                    if (contract.concluded < today && today < contract.expirationDate) {
                        $scope.displayActive = true;
                    } else {
                        $scope.displayActive = false;
                    }
                } else {
                    $scope.displayActive = false;
                }

                $scope.datepickerOptions = {
                    format: "dd-MM-yyyy",
                    parseFormats: ["yyyy-MM-dd"]
                };

                $scope.procurementPlans = [];
                var currentDate = moment();
                for (var i = 0; i < 40; i++) {
                    var quarter = currentDate.quarter();
                    var year = currentDate.year();
                    var obj = { id: String(i), text: `Q${quarter} | ${year}`, quarter: quarter, year: year };
                    $scope.procurementPlans.push(obj);

                    // add 3 months for next iter
                    currentDate.add(3, 'months');
                }

                var foundPlan: { id } = _.find($scope.procurementPlans, function (plan: { quarter; year; id; }) {
                    return plan.quarter == contract.procurementPlanQuarter && plan.year == contract.procurementPlanYear;
                });
                if (foundPlan) {
                    // plan is found in the list, replace it to get object equality
                    $scope.procurementPlanId = foundPlan;
                } else {
                    // plan is not found, add missing plan to begining of list
                    // if not null
                    if (contract.procurementPlanQuarter != null) {
                        var plan = { id: String($scope.procurementPlans.length), text: contract.procurementPlanQuarter + " | " + contract.procurementPlanYear, quarter: contract.procurementPlanQuarter, year: contract.procurementPlanYear };
                        $scope.procurementPlans.unshift(plan); // add to list
                        $scope.procurementPlanId = plan; // select it
                    }
                }

                $scope.patchDate = (field, value) => {
                    var date = moment(moment(value, Kitos.Constants.DateFormat.DanishDateFormat, true).format());
                    if(value === "") {
                        var payload = {};
                        payload[field] = null;
                        patch(payload, $scope.autosaveUrl2 + '?organizationId=' + user.currentOrganizationId);
                    } else if (value == null) {

                    } else if (!date.isValid() || isNaN(date.valueOf()) || date.year() < 1000 || date.year() > 2099) {
                        notify.addErrorMessage("Den indtastede dato er ugyldig.");

                    } else {
                        var dateString = date.format("YYYY-MM-DD");
                        var payload = {};
                        payload[field] = dateString;
                        patch(payload, $scope.autosaveUrl2 + '?organizationId=' + user.currentOrganizationId);
                    }
                }

                $scope.saveProcurement = function (id) {
                    if (id === null && contract.procurementPlanQuarter !== null && contract.procurementPlanYear !== null) {
                        updateProcurement(null, null);
                    }
                    else {
                        if (id === null) {
                            return;
                        }

                        var result = _.find($scope.procurementPlans, (plan) => plan.id === id);
                        if (result.quarter === contract.procurementPlanQuarter && result.year === contract.procurementPlanYear) {
                            return;
                        }
                        updateProcurement(result.quarter, result.year);
                    }
                };

                function updateProcurement(procurementPlanQuarter, procurementPlanYear) {
                    contract = $scope.contract;

                    var payload = { procurementPlanQuarter: procurementPlanQuarter, procurementPlanYear: procurementPlanYear };
                    $scope.contract.procurementPlanQuarter = payload.procurementPlanQuarter;
                    $scope.contract.procurementPlanYear = payload.procurementPlanYear;
                    patch(payload, $scope.autoSaveUrl + '?organizationId=' + user.currentOrganizationId);
                }

                function patch(payload, url) {
                    var msg = notify.addInfoMessage("Gemmer...", false);
                    $http({ method: 'PATCH', url: url, data: payload })
                        .then(function onSuccess(result) {
                            msg.toSuccessMessage("Feltet er opdateret.");
                        }, function onError(result) {
                            msg.toErrorMessage("Fejl! Feltet kunne ikke ændres!");
                        });
                }

                if (contract.parentId) {
                    $scope.contract.parent = {
                        id: contract.parentId,
                        text: contract.parentName
                    };
                }

                $scope.itContractsSelectOptions = selectLazyLoading('api/itcontract', true, formatContract, ['orgId=' + user.currentOrganizationId]);

                function formatContract(supplier) {
                    return '<div>' + supplier.text + '</div>';
                }

                if (contract.supplierId) {
                    $scope.contract.supplier = {
                        id: contract.supplierId,
                        text: contract.supplierName
                    };
                }

                $scope.suppliersSelectOptions = selectLazyLoading('api/organization', false, Kitos.Helpers.Select2OptionsFormatHelper.formatOrganizationWithCvr, ['take=100','orgId=' + user.currentOrganizationId]);

                function selectLazyLoading(url, excludeSelf, format, paramAry) {
                    return {
                        minimumInputLength: 1,
                        allowClear: true,
                        placeholder: ' ',
                        formatResult: format,
                        initSelection: function (elem, callback) {
                        },
                        ajax: {
                            data: function (term, page) {
                                return { query: term };
                            },
                            quietMillis: 500,
                            transport: function (queryParams) {
                                var extraParams = paramAry ? '&' + paramAry.join('&') : '';
                                var res = $http.get(url + '?q=' + encodeURIComponent(queryParams.data.query) + extraParams).then(queryParams.success);
                                res.abort = function () {
                                    return null;
                                };

                                return res;
                            },

                            results: function (data, page) {
                                var results = [];

                                _.each(data.data.response, function (obj: { id; name; cvrNumber; }) {
                                    if (excludeSelf && obj.id == contract.id)
                                        return; // don't add self to result

                                    results.push({
                                        id: obj.id,
                                        text: obj.name ? obj.name : 'Unavngiven',
                                        cvr: obj.cvrNumber
                                    });
                                });

                                return { results: results };
                            }
                        }
                    };
                }

                $scope.override = () =>
                {
                    isActive();
                }

                function isActive() {
                    var today = moment();
                    let fromDate = moment($scope.contract.concluded, Kitos.Constants.DateFormat.DanishDateFormat).startOf('day');
                    let endDate = moment($scope.contract.expirationDate, Kitos.Constants.DateFormat.DanishDateFormat).endOf('day');
                    if ($scope.contract.active || today.isBetween(fromDate, endDate, null, '[]') ||
                        (today.isSameOrAfter(fromDate) && !endDate.isValid()) ||
                        (today.isSameOrBefore(endDate) && !fromDate.isValid()) ||
                        (!fromDate.isValid() && !endDate.isValid())) {
                        $scope.contract.isActive = true;
                    }
                    else {
                        $scope.contract.isActive = false;
                    }
                }

                $scope.checkContractValidity = (field, value) => {
                    var expirationDate = $scope.contract.expirationDate;
                    var concluded = $scope.contract.concluded;
                    var formatDateString = "YYYY-MM-DD";
                    var fromDate = moment(concluded, [Kitos.Constants.DateFormat.DanishDateFormat, formatDateString]).startOf('day');
                    var endDate = moment(expirationDate, [Kitos.Constants.DateFormat.DanishDateFormat, formatDateString]).endOf('day');
                    var date = moment(value, [Kitos.Constants.DateFormat.DanishDateFormat, "YYYY-MM-DDTHH:mm:ssZ"], true);
                    var payload = {};
                    if (value === "") {
                        payload[field] = null;
                        patch(payload, $scope.autosaveUrl2 + '?organizationId=' + user.currentOrganizationId);
                        isActive();
                    }
                    else if (value == null) {
                        //made to prevent error message on empty value i.e. open close datepicker
                    }
                    else if (!date.isValid() || isNaN(date.valueOf()) || date.year() < 1000 || date.year() > 2099) {
                        notify.addErrorMessage("Den indtastede dato er ugyldig.");
                    }
                    else if (fromDate >= endDate) {
                        notify.addErrorMessage("Den indtastede slutdato er før startdatoen.");
                    }
                    else {
                        var dateString = date.format("YYYY-MM-DD");
                        payload[field] = dateString;
                        patch(payload, $scope.autosaveUrl2 + '?organizationId=' + user.currentOrganizationId);
                        isActive();
                    }
                }

                function bindCriticalities() {
                    
                    const optionMap = criticalityOptions.reduce((acc, next, _) => {
                        acc[next.Id] = {
                            text: next.Name,
                            id: next.Id,
                            optionalObjectContext: {
                                id: next.Id,
                                name: next.Name,
                                description: next.Description
                            }
                        };
                        return acc;
                    }, {});

                    //If selected state is expired, add it for presentation reasons
                    const existingChoice = {
                        id: $scope.contract.criticalityTypeId,
                        name: $scope.contract.criticalityTypeName
                    };
                    if(existingChoice.id && !optionMap[existingChoice.id]) {
                        optionMap[existingChoice.id] = {
                            text: existingChoice.name,
                            id: existingChoice.id,
                            disabled: true,
                            optionalObjectContext: existingChoice
                        }
                    }

                    const options = criticalityOptions.map(option => optionMap[option.Id]);

                    $scope.criticality = {
                        selectedElement: (existingChoice.id && optionMap[existingChoice.id]) ?? existingChoice,
                        select2Config: select2LoadingService.select2LocalDataNoSearch(() => options, true),
                        elementSelected: (newElement) => {
                            if (!newElement)
                                return;

                            var payload = { criticalityTypeId: newElement.id };
                            patch(payload, $scope.autosaveUrl2 + '?organizationId=' + user.currentOrganizationId);
                        }
                    };
                }
            }]);
})(angular, app);
