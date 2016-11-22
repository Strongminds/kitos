﻿(function (ng, app) {
    'use strict';

    app.directive("helpText", [
        function () {
            return {
                templateUrl: "app/shared/helpText/helpText.view.html",
                scope: {
                    key: "@",
                },
                controller: [
                    '$scope', '$http', '$uibModal', '$sce', function ($scope, $http, $uibModal, $sce) {
                        var parent = $scope;

                        $http.get("odata/HelpTexts?$filter=Key eq '" + $scope.key + "'")
                            .success((result: any) => {
                                $scope.title = result.value[0].Title;
                            })

                        $scope.showHelpTextModal = function () {
                            var modalInstance = $uibModal.open({
                                windowClass: "modal fade in",
                                templateUrl: "app/shared/helpText/helpTextModal.view.html",
                                controller: ["$scope", "$uibModalInstance", "notify", function ($scope, $modalInstance, nofity) {
                                    $http.get("odata/HelpTexts?$filter=Key eq '" + parent.key + "'")
                                        .success((result: any) => {
                                            $scope.title = result.value[0].Title;
                                            $scope.description = $sce.trustAsHtml(result.value[0].Description);
                                        })
                                }]
                            });
                    }
                }]
            };
        }
    ]);
})(angular, app);
