﻿(function (ng, app) {
    app.config([
        '$stateProvider', function ($stateProvider) {
            $stateProvider.state('it-contract.edit.main', {
                url: '/main',
                templateUrl: 'app/components/it-contract/tabs/it-contract-tab-main.view.html',
                controller: 'contract.EditMainCtrl',
                resolve: {
                    contractTypes: [
                        '$http', function ($http) {
                            return $http.get('odata/LocalItContractTypes?$filter=IsLocallyAvailable eq true or IsObligatory&$orderby=Priority desc').then(function (result) {
                                return result.data.value;
                            });
                        }
                    ],
                    contractTemplates: [
                        '$http', function ($http) {
                            return $http.get('odata/LocalItContractTemplateTypes?$filter=IsLocallyAvailable eq true or IsObligatory&$orderby=Priority desc').then(function (result) {
                                return result.data.value;
                            });
                        }
                    ],
                    purchaseForms: [
                        '$http', function ($http) {
                            return $http.get('odata/LocalPurchaseFormTypes?$filter=IsLocallyAvailable eq true or IsObligatory&$orderby=Priority desc').then(function (result) {
                                return result.data.value;
                            });
                        }
                    ],
                    procurementStrategies: [
                        '$http', function ($http) {
                            return $http.get('odata/LocalProcurementStrategyTypes?$filter=IsLocallyAvailable eq true or IsObligatory&$orderby=Priority desc').then(function (result) {
                                return result.data.value;
                            });
                        }
                    ],
                    orgUnits: [
                        '$http', 'contract', function ($http, contract) {
                            return $http.get('api/organizationunit/?organizationid=' + contract.organizationId).then(function (result) {
                                return result.data.response;
                            });
                        }
                    ],
                    kitosUsers: [
                        '$http', 'user', '_', function ($http, user, _) {
                            return $http.get(`odata/organizationRights?$filter=OrganizationId eq ${user.currentOrganizationId}&$expand=User($select=Name,LastName,Email,Id)&$select=User`).then(function (result) {
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

                                return results;
                            });
                        }
                    ]
                }
            });
        }
    ]);

    app.controller('contract.EditMainCtrl',
        [
            '$scope', '$http', '$stateParams', 'notify', 'contract', 'contractTypes', 'contractTemplates', 'purchaseForms', 'procurementStrategies', 'orgUnits', 'hasWriteAccess', 'user', 'autofocus', '$timeout', 'kitosUsers',
            function ($scope, $http, $stateParams, notify, contract, contractTypes, contractTemplates, purchaseForms, procurementStrategies, orgUnits, hasWriteAccess, user, autofocus, $timeout, kitosUsers) {

                $scope.autoSaveUrl = 'api/itcontract/' + $stateParams.id;
                $scope.autosaveUrl2 = 'api/itcontract/' + contract.id;
                $scope.contract = contract;
                $scope.hasWriteAccess = hasWriteAccess;
                $scope.hasViewAccess = user.currentOrganizationId == contract.organizationId;
                $scope.kitosUsers = kitosUsers;
                autofocus();
                
                $scope.contractTypes = contractTypes;
                $scope.contractTemplates = contractTemplates;
                $scope.purchaseForms = purchaseForms;
                $scope.procurementStrategies = procurementStrategies;
                $scope.orgUnits = orgUnits;

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
                for (var i = 0; i < 20; i++) {
                    var half = currentDate.quarter() <= 2 ? 1 : 2; // 1 for the first 6 months, 2 for the rest
                    var year = currentDate.year();
                    var obj = { id: i, half: half, year: year };
                    $scope.procurementPlans.push(obj);

                    // add 6 months for next iter
                    currentDate.add(6, 'months');
                }

                var foundPlan: { id } = _.find($scope.procurementPlans, function (plan: { half; year; id; }) {
                    return plan.half == contract.procurementPlanHalf && plan.year == contract.procurementPlanYear;
                });
                if (foundPlan) {
                    // plan is found in the list, replace it to get object equality
                    $scope.contract.procurementPlan = foundPlan.id;
                } else {
                    // plan is not found, add missing plan to begining of list
                    // if not null
                    if (contract.procurementPlanHalf != null) {
                        var plan = { id: $scope.procurementPlans.length, half: contract.procurementPlanHalf, year: contract.procurementPlanYear };
                        $scope.procurementPlans.unshift(plan); // add to list
                        $scope.contract.procurementPlan = plan.id; // select it
                    }
                }

                $scope.saveProcurement = function () {
                    var payload;
                    // if empty the value has been cleared
                    if ($scope.contract.procurementPlan === '') {
                        payload = { procurementPlanHalf: null, procurementPlanYear: null };
                    } else {
                        var id = $scope.contract.procurementPlan;
                        var result = $scope.procurementPlans[id];
                        payload = { procurementPlanHalf: result.half, procurementPlanYear: result.year };
                    }
                    patch(payload, $scope.autoSaveUrl + '?organizationId=' + user.currentOrganizationId);
                };

                $scope.procurementPlanOption = {
                    allowClear: true,
                    initSelection: function (element, callback) {
                        callback({ id: 1, text: 'Text' });
                    }
                };

                function patch(payload, url) {
                    var msg = notify.addInfoMessage("Gemmer...", false);
                    $http({ method: 'PATCH', url: url, data: payload })
                        .success(function () {
                            msg.toSuccessMessage("Feltet er opdateret.");
                        })
                        .error(function () {
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

                $scope.suppliersSelectOptions = selectLazyLoading('api/organization', false, formatSupplier, ['public=true', 'orgId=' + user.currentOrganizationId]);

                function formatSupplier(supplier) {
                    var result = '<div>' + supplier.text + '</div>';
                    if (supplier.cvr) {
                        result += '<div class="small">' + supplier.cvr + '</div>';
                    }
                    return result;
                }

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
                                var res = $http.get(url + '?q=' + queryParams.data.query + extraParams).then(queryParams.success);
                                res.abort = function () {
                                    return null;
                                };

                                return res;
                            },

                            results: function (data, page) {
                                var results = [];

                                _.each(data.data.response, function (obj: { id; name; cvr; }) {
                                    if (excludeSelf && obj.id == contract.id)
                                        return; // don't add self to result

                                    results.push({
                                        id: obj.id,
                                        text: obj.name ? obj.name : 'Unavngiven',
                                        cvr: obj.cvr
                                    });
                                });

                                return { results: results };
                            }
                        }
                    };
                }

                $scope.checkContractValidity = function () {
                    var expirationDateObject, concludedObject;
                    var expirationDate = $scope.contract.expirationDate;
                    var concluded = $scope.contract.concluded;
                    var overrule = $scope.contract.active;
                    var today = new Date();


                    if (expirationDate) {
                        if (expirationDate.length > 10) {
                            //ISO format
                            expirationDateObject = new Date(expirationDate);

                        } else {
                            var splitArray = expirationDate.split("-");
                            expirationDateObject = new Date(splitArray[2], parseInt(splitArray[1], 10) - 1, splitArray[0]);
                        }
                    }

                    if (concluded) {
                        if (concluded.length > 10) {
                            //ISO format
                            concludedObject = new Date(concluded);

                        } else {
                            var splitArray = concluded.split("-");
                            concludedObject = new Date(splitArray[2], parseInt(splitArray[1], 10) - 1, splitArray[0]);
                        }
                    }

                    if (concluded && expirationDate) {

                        var isTodayBetween = (today > concludedObject.setHours(0, 0, 0, 0) && today <= expirationDateObject.setHours(23, 59, 59, 999));

                    }
                    else if (concluded && !expirationDate) {

                        var isTodayBetween = (today > concludedObject.setHours(0, 0, 0, 0));

                    }
                    else if (!concluded && !expirationDate) {
                        isTodayBetween = true;

                    }
                    else if (!concluded && expirationDate) {

                        var isTodayBetween = (today < expirationDateObject.setHours(23, 59, 59, 999));

                    }

                    var isContractActive = (isTodayBetween || overrule);

                    $scope.contract.isActive = isContractActive;
                }
            }]);
})(angular, app);
