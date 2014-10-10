﻿(function(ng, app) {
    app.config([
        '$stateProvider', function($stateProvider) {
            $stateProvider.state('it-project.edit.kle', {
                url: '/kle',
                templateUrl: 'partials/it-project/tab-kle.html',
                controller: 'project.EditKleCtrl',
                resolve: {
                    // re-resolve data from parent cause changes here wont cascade to it
                    project: [
                        '$http', '$stateParams', function($http, $stateParams) {
                            return $http.get("api/itproject/" + $stateParams.id)
                                .then(function(result) {
                                    return result.data.response;
                                });
                        }
                    ]
                }
            });
        }
    ]);

    app.controller('project.EditKleCtrl',
    [
        '$scope', '$http', '$state', '$stateParams', 'notify',
        function($scope, $http, $state, $stateParams, notify) {
            var projectId = $stateParams.id;
            var baseUrl = 'api/itProject/' + projectId;

            $scope.pagination = {
                skip: 0,
                take: 50
            };

            $scope.showAllTasks = true;

            $scope.$watch("selectedTaskGroup", function(newVal, oldVal) {
                $scope.pagination.skip = 0;
                loadTasks();
            });
            $scope.$watchCollection('pagination', loadTasks);

            //change between show all tasks and only show active tasks
            $scope.changeTaskView = function() {
                $scope.showAllTasks = !$scope.showAllTasks;
                $scope.pagination.skip = 0;
                loadTasks();
            };

            function loadTasks() {

                var url = baseUrl + "?tasks";
                url += '&onlySelected=' + !$scope.showAllTasks;
                url += '&taskGroup=' + $scope.selectedTaskGroup;
                url += '&skip=' + $scope.pagination.skip + '&take=' + $scope.pagination.take;

                if ($scope.pagination.orderBy) {
                    url += '&orderBy=' + $scope.pagination.orderBy;
                    if ($scope.pagination.descending) url += '&descending=' + $scope.pagination.descending;
                }

                $http.get(url).success(function(result, status, headers) {
                    $scope.tasklist = result.response;

                    var paginationHeader = JSON.parse(headers('X-Pagination'));
                    $scope.totalCount = paginationHeader.TotalCount;

                }).error(function() {
                    notify.addErrorMessage("Kunne ikke hente opgaver!");
                });

            }

            function add(task) {
                return $http.post(baseUrl + '?taskId=' + task.taskRef.id).success(function(result) {
                    task.isSelected = true;
                });
            }

            function remove(task) {
                return $http.delete(baseUrl + '?taskId=' + task.taskRef.id).success(function(result) {
                    task.isSelected = false;
                });
            }

            $scope.save = function(task) {
                var msg = notify.addInfoMessage("Opdaterer ...", false);

                if (!task.isSelected) {
                    add(task).success(function() {
                        msg.toSuccessMessage("Feltet er opdateret!");
                    }).error(function() {
                        msg.toErrorMessage("Fejl!");
                    });
                } else {
                    remove(task).success(function() {
                        msg.toSuccessMessage("Feltet er opdateret!");
                    }).error(function() {
                        msg.toErrorMessage("Fejl!");
                    });
                }
            };

            $scope.selectAllTasks = function() {
                _.each($scope.tasklist, function(task) {
                    if (!task.isSelected) {
                        add(task);
                    }
                });
            };

            $scope.removeAllTasks = function() {
                _.each($scope.tasklist, function(task) {
                    if (task.isSelected) {
                        remove(task);
                    }
                });
            };

            $scope.selectTaskGroup = function() {
                var url = baseUrl + '?taskId=' + $scope.selectedTaskGroup;

                var msg = notify.addInfoMessage("Opretter tilknytning...", false);
                $http.post(url).success(function() {
                    msg.toSuccessMessage("Tilknytningen er oprettet");
                    reload();
                }).error(function() {
                    msg.toErrorMessage("Fejl! Kunne ikke oprette tilknytningen!");
                });
            };

            $scope.removeTaskGroup = function () {
                var url = baseUrl + '?taskId=' + $scope.selectedTaskGroup;

                var msg = notify.addInfoMessage("Fjerner tilknytning...", false);
                $http.delete(url).success(function () {
                    msg.toSuccessMessage("Tilknytningen er fjernet");
                    reload();
                }).error(function () {
                    msg.toErrorMessage("Fejl! Kunne ikke fjerne tilknytningen!");
                });
            };

            function reload() {
                $state.go('.', null, { reload: true });
            }
        }
    ]);
})(angular, app);
