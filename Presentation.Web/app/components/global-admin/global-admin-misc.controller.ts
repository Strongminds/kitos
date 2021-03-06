﻿((ng, app) => {
    app.config(["$stateProvider", $stateProvider => {
        $stateProvider.state("global-admin.misc", {
            url: "/misc",
            templateUrl: "app/components/global-admin/global-admin-misc.view.html",
            controller: "globalAdminMisc",
            authRoles: ["GlobalAdmin"],
            resolve: {
                globalConfigs: [
                    "$http", $http => $http.get("/odata/GlobalConfigs").then(result => result.data.value)
                ],
                brokenLinkStatus: [
                    "brokenLinksReportService", (brokenLinksReportService: Kitos.Services.BrokenLinksReport.IBrokenLinksReportService) => brokenLinksReportService.getStatus()
                ]
            }
        });
    }]);
    app.controller("globalAdminMisc", ["$rootScope", "$scope", "$http", "uploadFile", "globalConfigs", "_", "notify", "kleService", "$window", "brokenLinkStatus", ($rootScope, $scope, $http, uploadFile, globalConfigs, _, notify, kleService, $window, brokenLinkStatus:Kitos.Models.Api.BrokenLinksReport.IBrokenLinksReportDTO) => {
        $rootScope.page.title = "Andet";
        $scope.brokenLinksVm = Kitos.Models.ViewModel.GlobalAdmin.BrokenLinks.BrokenLinksViewModelMapper.mapFromApiResponse(brokenLinkStatus);

        getKleStatus();
        function getKleStatus() {
            $scope.KLEUpdateAvailableLabel = "Undersøger om der er en ny version af KLE...";
            toggleKleButtonsClickAbility(false, false);
            kleService.getStatus().success((dto, status) => {
                    if (status !== 200) {
                        notify.addErrorMessage("Der skete en fejl ifm. tjek af ny KLE version");
                        return;
                    }
                    if (!dto.response.upToDate) {
                        $scope.KLEUpdateAvailableLabel = "Der er en ny version af KLE, udgivet " + dto.response.version;
                        toggleKleButtonsClickAbility(true, false);
                    }
                    else {
                        $scope.KLEUpdateAvailableLabel = "KITOS baserer sig på den seneste KLE version, udgivet  " + dto.response.version;
                        toggleKleButtonsClickAbility(false, false);
                    }
                }).
                error(() => {
                    toggleKleButtonsClickAbility(false, false);
                    notify.addErrorMessage("Der skete en fejl ifm. tjek af ny KLE version");
                });
        }

        $scope.canGlobalAdminOnlyEditReports = _.find(globalConfigs, g => g.key === "CanGlobalAdminOnlyEditReports");

        $scope.uploadFile = () => {
            var fileToBeUploaded = $scope.myFile;   
            uploadFile.uploadFile(fileToBeUploaded);
        };

        $scope.patchConfig = config => {
            var payload = {};
            payload["value"] = config.value;
            
            $http.patch("/odata/GlobalConfigs(" + config.Id + ")", payload).then(newUser => {
                notify.addSuccessMessage("Feltet er opdateret!");
            }, () => {
                notify.addErrorMessage("Fejl! Kunne ikke opdatere feltet!");
            });
        };

        $scope.GetKLEChanges = () => {
            toggleKleButtonsClickAbility(false, false);
            kleService.getChanges().success((data, status) => {
                    if (status !== 200)
                    {
                        toggleKleButtonsClickAbility(true, false);
                        notify.addErrorMessage("Der skete en fejl under hentning af ændringer");
                        return;
                    }
                    var universalBOM = "\uFEFF";
                    var anchor = angular.element(document.getElementById("KLEDownloadAnchor"));
                    anchor.attr("data-element-type", "KLEDownloadAnchor");
                    anchor.attr({
                        href: 'data:text/csv; charset=utf-8,' + encodeURI(universalBOM + data),
                        target: "_blank",
                        download: "KLE-Changes.csv"
                    })[0].click();
                    notify.addSuccessMessage("Download af ændringer færdig");
                    toggleKleButtonsClickAbility(true, true);
                }).
                error(() => {
                    toggleKleButtonsClickAbility(true, false);
                    notify.addErrorMessage("Der skete en fejl under henting af ændringer");
                });
        }

        $scope.UpdateKLE = () => {
            toggleKleButtonsClickAbility(false, false);
            if (confirm("Er du sikker på, at du vil opdatere KLE til nyeste version?")) {
                kleService.applyUpdateKLE().
                    then((response) => {
                        if (response.status !== 200) {
                            toggleKleButtonsClickAbility(true, false);
                            angular.element(document.getElementById("overlay")).remove();
                            notify.addErrorMessage("Der skete en fejl under opdatering af KLE");
                            return;
                        }
                        notify.addSuccessMessage("KLE er opdateret");
                        angular.element(document.getElementById("overlay")).remove();
                        getKleStatus();
                    }).
                    catch(() => {
                        toggleKleButtonsClickAbility(true, false);
                        notify.addErrorMessage("Der skete en fejl under opdatering af KLE");
                        angular.element(document.getElementById("overlay")).remove();
                    });
            } else {
                notify.addInfoMessage("KLE opdatering stoppet!");
            }
            
        }

        function toggleKleButtonsClickAbility(updateAvailButton: boolean, updateButton: boolean) {
            $scope.KleUpdateAvailableButtonInteraction = updateAvailButton;
            $scope.KleApplyUpdateButtonInteraction = updateButton;
        }
    }]);
})(angular, app);