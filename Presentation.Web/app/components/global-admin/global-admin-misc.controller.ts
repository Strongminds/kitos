﻿(function (ng, app) {
    app.config(["$stateProvider", $stateProvider => {
        $stateProvider.state("global-admin.misc", {
            url: "/misc",
            templateUrl: "app/components/global-admin/global-admin-misc.view.html",
            controller: "globalAdminMisc",
            authRoles: ["GlobalAdmin"],
            resolve: {
                globalConfigs: [
                    "$http", $http => $http.get("/odata/GlobalConfigs").then(result => result.data.value)
                ]
            }
        });
    }]);

    app.controller("globalAdminMisc", ["$rootScope", "$scope", "$http", "uploadFile", "globalConfigs", "_", "notify", "KLEservice", ($rootScope, $scope, $http, uploadFile, globalConfigs, _, notify, KLEservice) => {
        $rootScope.page.title = "Andet";
        $scope.KLEupdateReadyStep1 = false;
        $scope.KLEupdateReadyStep2 = false;

        KLEservice.getStatus().success(dto => {

            if (!dto.response.uptodate) {
                $scope.KLEUpdateAvailableLabel = "KLE Opdatering er klar! " + dto.response.version;
                $scope.KLEupdateReadyStep1 = true;
                $scope.KLEupdateReadyStep2 = false;
            }
            else {
                $scope.KLEUpdateAvailableLabel = "KLE kører med nyeste version! " + dto.response.version;
            }
        });

        $scope.canGlobalAdminOnlyEditReports = _.find(globalConfigs, function (g) {
            return g.key === "CanGlobalAdminOnlyEditReports";
        });

        $scope.uploadFile = function () {
            var fileToBeUploaded = $scope.myFile;   
            uploadFile.uploadFile(fileToBeUploaded);
        };

        $scope.patchConfig = function (config) {
            var payload = {};
            payload["value"] = config.value;
            
            $http.patch("/odata/GlobalConfigs(" + config.Id + ")", payload).then(function (newUser) {
                notify.addSuccessMessage("Feltet er opdateret!");
            }, function () {
                notify.addErrorMessage("Fejl! Kunne ikke opdatere feltet!");
            });
        };

        $scope.GetKLEChanges = function () {
            $scope.KLEupdateReadyStep1 = false;
            $scope.KLEupdateReadyStep2 = false;

            
            KLEservice.getChanges().success((data) => {
                var uri = encodeURI(data);
                var universalBOM = "\uFEFF";
                console.log(data);
                    console.log("URL :" + uri);
                    var anchor = angular.element('<a/>');
                    anchor.attr({   
                        href: 'data:text/csv; charset=utf-8,' + encodeURI(universalBOM+data),
                        target: '_blank',
                        download: 'KLE-Changes.csv'
                    })[0].click();
                notify.addSuccessMessage("Download complete");
                $scope.KLEupdateReadyStep1 = true;
                $scope.KLEupdateReadyStep2 = true;
                }).
                error(function (data, status, headers, config) {
                    $scope.KLEupdateReadyStep1 = true;
                    $scope.KLEupdateReadyStep2 = false;
                    notify.addErrorMessage("There was an issue downloading the excel file, please contact support.");
                });
        }

        $scope.UpdateKLE = function () {
            if (confirm("Sikker på at du vil opdatere KLE til nyeste version?")) {
                //TODO Query database to update
                notify.addSuccessMessage("KLE er nu opdateret!");
                
            } else {
                notify.addInfoMessage("KLE opdatering stoppet!");
            }
        }
    }]);
})(angular, app);