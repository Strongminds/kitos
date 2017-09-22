﻿(function (ng, app) {
    app.config(["$stateProvider", function ($stateProvider) {
        $stateProvider.state("it-project.edit.strategy", {
            url: "/strategy",
            templateUrl: "app/components/it-project/tabs/it-project-tab-strategy.view.html",
            controller: "project.EditStrategyCtrl",
            resolve: {
                // re-resolve data from parent cause changes here wont cascade to it
                project: ["$http", "$stateParams", function ($http, $stateParams) {
                    return $http.get("api/itproject/" + $stateParams.id)
                        .then(function (result) {
                            return result.data.response;
                        });
                }],
                jointMunicipalProjects: ["$http", "project", "projectTypes", function ($http, project, projectTypes) {
                    var type: { Id } = _.find(projectTypes, function(t: { Id; Name; }) {
                        return t.Name == "Fælleskommunal"; // TODO hardcoded literal... find better solution!
                    });
                    var typeId = type.Id;
                    var orgId = project.organizationId;
                    return $http.get("api/itproject/?orgId=" + orgId + "&typeId=" + typeId).then(function(result) {
                        return result.data.response;
                    });
                }],
                commonPublicProjects: ["$http", "project", "projectTypes", function ($http, project, projectTypes) {
                    var type = _.find(projectTypes, function (t: { Id; Name; }) {
                        return t.Name == "Fællesoffentlig"; // TODO hardcoded literal... find better solution!
                    });
                    var typeId = type.Id;
                    var orgId = project.organizationId;
                    return $http.get("api/itproject/?orgId=" + orgId + "&typeId=" + typeId).then(function (result) {
                        return result.data.response;
                    });
                }]
            }
        });
    }]);

    app.controller("project.EditStrategyCtrl",
        ["$scope", "$http", "notify", "project", "jointMunicipalProjects", "commonPublicProjects",
            function($scope, $http, notify, project, jointMunicipalProjects, commonPublicProjects) {
                $scope.isStrategy = project.isStrategy;
                $scope.jointMunicipalProjectId = project.jointMunicipalProjectId;
                $scope.jointMunicipalProjects = jointMunicipalProjects;
                $scope.commonPublicProjectId = project.commonPublicProjectId;
                $scope.commonPublicProjects = commonPublicProjects;

                $scope.Options = {
                    allowClear: true,
                    initSelection: function (element, callback) {
                        callback({ id: 1, text: 'Text' });
                    }
                };

                $scope.savejoint = () => {
                    var payload;
                        // if empty the value has been cleared
                        if ($scope.jointMunicipalProjectId === "") {
                            payload = { "JointMunicipalProjectId": null };
                        } else {
                            var id = $scope.jointMunicipalProjectId;
                            payload = { "JointMunicipalProjectId": id };
                    }
                        $http.patch(`/odata/ItProjects(${project.id})`, payload)
                            .then(() => {
                                    notify.addSuccessMessage("Feltet er opdateret!");
                                },
                                () => notify.addErrorMessage("Fejl! Feltet kunne ikke opdateres!"));
                };

                $scope.savecommon = () => {
                    var payload;
                    // if empty the value has been cleared
                    if ($scope.commonPublicProjectId === "") {
                        payload = { "CommonPublicProjectId": null };
                    } else {
                        var id = $scope.commonPublicProjectId;
                        payload = { "CommonPublicProjectId": id };
                    }
                    $http.patch(`/odata/ItProjects(${project.id})`, payload)
                        .then(() => {
                            notify.addSuccessMessage("Feltet er opdateret!");
                        },
                        () => notify.addErrorMessage("Fejl! Feltet kunne ikke opdateres!"));
                };
        }]);
})(angular, app);
