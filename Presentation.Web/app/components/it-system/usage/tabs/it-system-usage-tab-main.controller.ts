﻿((ng, app) => {
    app.config(["$stateProvider", $stateProvider => {
        $stateProvider.state("it-system.usage.main", {
            url: "/main",
            templateUrl: "app/components/it-system/usage/tabs/it-system-usage-tab-main.view.html",
            controller: "system.EditMain",
            resolve: {
                systemCategories: [
                    "localOptionServiceFactory", (localOptionServiceFactory: Kitos.Services.LocalOptions.ILocalOptionServiceFactory) =>
                    localOptionServiceFactory.create(Kitos.Services.LocalOptions.LocalOptionType.ItSystemCategories).getAll()
                ]
            }
        });
    }]);

    app.controller("system.EditMain", ["$rootScope", "$scope", "$http", "notify", "user", "systemCategories", "autofocus",
        ($rootScope, $scope, $http, notify, user, systemCategories, autofocus) => {
            var itSystemUsage = new Kitos.Models.ViewModel.ItSystemUsage.SystemUsageViewModel($scope.usage);
            $rootScope.page.title = "IT System - Anvendelse";
            $scope.autoSaveUrl = `api/itSystemUsage/${itSystemUsage.id}`;
            $scope.hasViewAccess = user.currentOrganizationId === itSystemUsage.organizationId;
            $scope.systemCategories = systemCategories;
            $scope.shouldShowCategories = systemCategories.length > 0;
            $scope.system = itSystemUsage.itSystem;
            $scope.lastChangedBy = itSystemUsage.lastChangedBy;
            $scope.lastChanged = itSystemUsage.lastChanged;
            autofocus();
            $scope.isValidUrl = (url: string) => Kitos.Utility.Validation.isValidExternalReference(url);

            $scope.numberOfUsersOptions = [
                { id: "4", text: Kitos.Constants.Select2.EmptyField },
                { id: "0", text: "<10" },
                { id: "1", text: "10-50" },
                { id: "2", text: "50-100" },
                { id: "3", text: ">100" }
            ];

            $scope.datepickerOptions = {
                format: "dd-MM-yyyy",
                parseFormats: ["yyyy-MM-dd"]
            };

            $scope.patchDate = (field, value) => {
                var expirationDate = itSystemUsage.expirationDate;
                var concluded = itSystemUsage.concluded;
                var formatDateString = "YYYY-MM-DD";
                var fromDate = moment(concluded, [Kitos.Constants.DateFormat.DanishDateFormat, formatDateString]).startOf("day");
                var endDate = moment(expirationDate, [Kitos.Constants.DateFormat.DanishDateFormat, formatDateString]).endOf("day");
                var date = moment(value, Kitos.Constants.DateFormat.DanishDateFormat);
                if (value === "" || value == undefined) {
                    var payload = {};
                    payload[field] = null;
                    patch(payload, $scope.autoSaveUrl + "?organizationId=" + user.currentOrganizationId);
                } else if (!date.isValid() || isNaN(date.valueOf()) || date.year() < 1000 || date.year() > 2099) {
                    notify.addErrorMessage("Den indtastede dato er ugyldig.");
                }
                else if (fromDate != null && endDate != null && fromDate >= endDate) {
                    notify.addErrorMessage("Den indtastede slutdato er før startdatoen.");
                }
                else {
                    checkIfActive();

                    var dateString = date.format("YYYY-MM-DD");
                    var payload = {};
                    payload[field] = dateString;
                    patch(payload, $scope.autoSaveUrl + "?organizationId=" + user.currentOrganizationId);
                }
            }

            function patch(payload: any, url: any);
            function patch(payload, url) {
                var msg = notify.addInfoMessage("Gemmer...", false);
                $http({ method: "PATCH", url: url, data: payload })
                    .then(function onSuccess(result) {
                        msg.toSuccessMessage("Feltet er opdateret.");
                    }, function onError(result) {
                        msg.toErrorMessage("Fejl! Feltet kunne ikke ændres!");
                    });
            }

            $scope.checkSystemValidity = () => {
                checkIfActive();
            }

            function checkIfActive() {
                const today = moment();

                if (today.isBetween(moment(itSystemUsage.concluded, Kitos.Constants.DateFormat.DanishDateFormat).startOf("day"), moment(itSystemUsage.expirationDate, Kitos.Constants.DateFormat.DanishDateFormat).endOf("day"), null, "[]") ||
                    (today > moment(itSystemUsage.concluded, Kitos.Constants.DateFormat.DanishDateFormat).startOf("day") && itSystemUsage.expirationDate == null) ||
                    (today < moment(itSystemUsage.expirationDate, Kitos.Constants.DateFormat.DanishDateFormat).endOf("day") && itSystemUsage.concluded == null) ||
                    (itSystemUsage.expirationDate == null && itSystemUsage.concluded == null)) {
                    itSystemUsage.activeAccordingToValidityPeriod = true;
                }
                else {
                    if (itSystemUsage.active) {
                        itSystemUsage.activeAccordingToValidityPeriod = true;
                    }
                    else {
                        itSystemUsage.activeAccordingToValidityPeriod = false;
                    }
                }
            }
        }
    ]);
})(angular, app);
