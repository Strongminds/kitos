﻿(function (ng, app) {
    app.config(['$stateProvider', function ($stateProvider) {
        $stateProvider.state('it-project.edit.status-goal', {
            url: '/status-goal',
            templateUrl: 'partials/it-project/tab-status-goal.html',
            controller: 'project.EditStatusGoalCtrl',
            resolve: {
                // re-resolve data from parent cause changes here wont cascade to it
                project: ['$http', '$stateParams', function ($http, $stateParams) {
                    return $http.get("api/itproject/" + $stateParams.id)
                        .then(function (result) {
                            return result.data.response;
                        });
                }],
                goalTypes: ['$http', function($http) {
                    return $http.get("api/goalType").then(function(result) {
                        return result.data.response;
                    });
                }]
            }
        });
    }]);

    app.controller('project.EditStatusGoalCtrl',
    ['$scope', '$http', 'notify', '$modal', 'project', 'goalTypes',
        function ($scope, $http, notify, $modal, project, goalTypes) {
            $scope.goalStatus = project.goalStatus;
            $scope.goalStatus.updateUrl = "api/goalStatus/" + project.goalStatus.id;

            $scope.getGoalTypeName = function(goalTypeId) {
                var type = _.findWhere(goalTypes, { id: goalTypeId });

                return type && type.name;
            };

            $scope.goals = [];
            function addGoal(goal) {
                //add goals means show goal in list
                goal.show = true;

                //see if goal already in list - in that case, just update it
                var prevEntry = _.findWhere($scope.goals, { id: goal.id });
                if (prevEntry) {
                    prevEntry = goal;
                    return;
                }
                
                //otherwise:

                //easy-access functions
                goal.edit = function() {
                    editGoal(goal);
                };
                
                goal.delete = function() {

                    var msg = notify.addInfoMessage("Sletter... ");
                    $http.delete(goal.updateUrl).success(function() {

                        goal.show = false;

                        msg.toSuccessMessage("Slettet!");

                    }).error(function() {

                        msg.toErrorMessage("Fejl! Kunne ikke slette!");
                    });

                };
                
                goal.updateUrl = "api/goal/" + goal.id;
                $scope.goals.push(goal);
            }

            _.each($scope.goalStatus.goals, addGoal);

            function patch(url, field, value) {
                var payload = {};
                payload[field] = value;

                return $http({
                    method: 'PATCH',
                    url: url,
                    data: payload
                });
            }
            
            $scope.updateStatusDate = function () {
                patch($scope.goalStatus.updateUrl, "statusDate", $scope.project.statusDate)
                    .success(function () {
                        notify.addSuccessMessage("Feltet er opdateret");
                    }).error(function () {
                        notify.addErrorMessage("Fejl!");
                    });
            };

            $scope.addGoal = function() {
                $http.post("api/goal", {
                    goalStatusId: project.goalStatus.id,
                    goalTypeId: 1
                }).success(function(result) {
                    notify.addSuccessMessage("Nyt mål tilføjet!");

                    addGoal(result.response);

                }).error(function() {
                    notify.addErrorMessage("Kunne ikke oprette nyt mål!");
                });
            };

            function editGoal(goal) {
                var modal = $modal.open({
                    size: 'lg',
                    templateUrl: 'partials/it-project/modal-goal-edit.html',
                    controller: ['$scope', 'autofocus', function ($modalScope, autofocus) {
                        autofocus();
                        $modalScope.goal = goal;
                        $modalScope.goalTypes = goalTypes;
                                                
                        $modalScope.opened = {};
                        $modalScope.open = function ($event, datepicker) {
                            $event.preventDefault();
                            $event.stopPropagation();

                            $modalScope.opened[datepicker] = true;
                        };
                    }]
                });
            }
        }]);
})(angular, app);
