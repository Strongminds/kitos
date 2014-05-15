﻿(function (ng, app) {
    app.config(['$stateProvider', function ($stateProvider) {
        $stateProvider.state('it-project.edit.handover', {
            url: '/handover',
            templateUrl: 'partials/it-project/tab-handover.html',
            controller: 'project.EditHandoverCtrl',
            resolve: {
                handover: ['$http', '$stateParams', function ($http, $stateParams) {
                    var projectId = $stateParams.id;
                    return $http.get('api/handover/' + projectId)
                        .then(function (result) {
                            return result.data.response;
                        });
                }],
                //returns a map with those users who have a role in this project.
                //the names of the roles is saved in user.roleNames
                usersWithRoles: ['$http', '$stateParams', function ($http, $stateParams) {

                    //get the rights of the projects
                    return $http.get('api/itprojectright/' + $stateParams.id)
                        .then(function(rightResult) {
                            var rights = rightResult.data.response;

                            //get the role names
                            return $http.get('api/itprojectrole/')
                                .then(function(roleResult) {
                                    var roles = roleResult.data.response;

                                    //the resulting map
                                    var users = {};
                                    _.each(rights, function(right) {

                                        //use the user from the map if possible
                                        var user = users[right.userId] || right.user;

                                        var role = _.findWhere(roles, { id: right.roleId });

                                        var roleNames = user.roleNames || [];
                                        roleNames.push(role.name);
                                        user.roleNames = roleNames;

                                        users[right.userId] = user;
                                    });

                                    return users;
                                });
                        });
                }]
            }
        });
    }]);

    app.controller('project.EditHandoverCtrl',
    ['$scope', '$stateParams', 'dateFilter', 'handover', 'usersWithRoles',
        function ($scope, $stateParams, dateFilter, handover, usersWithRoles) {
            $scope.handover = handover;
            $scope.usersWithRoles = usersWithRoles;
            //$scope.meetingDateObj = dateFilter(new Date(handover.meetingDate), 'yyyy-MM-dd'); // this is nessarcy for angular to 'get it'
            $scope.autosaveUrl = 'api/handover/' + $stateParams.id;
        }]);
})(angular, app);
