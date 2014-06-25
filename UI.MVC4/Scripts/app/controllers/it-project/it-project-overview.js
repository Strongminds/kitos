﻿(function (ng, app) {
    app.config(['$stateProvider', function ($stateProvider) {
        $stateProvider.state('it-project.overview', {
            url: '/overview',
            templateUrl: 'partials/it-project/overview.html',
            controller: 'project.EditOverviewCtrl',
            resolve: {
                projectRoles: ['$http', function ($http) {
                    return $http.get('api/itprojectrole').then(function (result) {
                        return result.data.response;
                    });
                }],
                user: ['userService', function (userService) {
                    return userService.getUser();
                }]
            }
        });
    }]);

    app.controller('project.EditOverviewCtrl',
    ['$scope', '$http', 'projectRoles', 'user',
        function ($scope, $http, projectRoles, user) {
            $scope.pagination = {
                skip: 0,
                take: 20
            };

            $scope.projects = [];

            $scope.projectRoles = projectRoles;
            
            $scope.$watchCollection('pagination', loadProjects);

            function loadProjects() {
                var url = 'api/itProject?orgId=' + user.currentOrganizationId;

                url += '&skip=' + $scope.pagination.skip;
                url += '&take=' + $scope.pagination.take;

                if ($scope.pagination.orderBy) {
                    url += '&orderBy=' + $scope.pagination.orderBy;
                    if ($scope.pagination.descending) url += '&descending=' + $scope.pagination.descending;
                }

                $scope.projects = [];
                $http.get(url).success(function (result, status, headers) {

                    var paginationHeader = JSON.parse(headers('X-Pagination'));
                    $scope.pagination.count = paginationHeader.TotalCount;

                    _.each(result.response, pushProject);

                }).error(function () {
                    notify.addErrorMessage("Kunne ikke hente projekter!");
                });

            }
            
            function pushProject(project) {
                // fetch assigned roles for each project
                $http.get('api/itprojectrights/' + project.id).success(function (result) {
                    project.roles = result.response;
                });
                
                // set current phase
                var phases = [project.phase1, project.phase2, project.phase3, project.phase4, project.phase5];
                project.currentPhase = _.find(phases, function (phase) {
                    return phase.id == project.currentPhaseId;
                });
                $scope.projects.push(project);
            }
        }]);
})(angular, app);
