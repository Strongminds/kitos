﻿(function (ng, app) {
    app.directive('holderFix', function () {
        return {
            link: function (scope, element, attrs) {
                Holder.run({ images: element[0], nocss: true });
            }
        };
    });

    app.directive('confirmClick', [
        function () {
            return {
                link: function (scope, element, attr) {
                    var msg = attr.confirmClick || "Er du sikker?";
                    var clickAction = attr.confirmedClick;
                    element.bind('click', function (event) {
                        if (window.confirm(msg)) {
                            scope.$eval(clickAction);
                        }
                    });
                }
            };
        }]);


    app.directive('addUser', ['$http', '$modal', function($http, $modal) {
        return {
            scope: {
                userResult: '=?addUser',
                selectResult: '=?forSelect2'
            },
            replace: true,
            templateUrl: 'partials/directives/add-user-button.html',
            link: function(scope, element, attr) {

                scope.open = function() {
                    var modal = $modal.open({
                        templateUrl: 'partials/directives/add-user-modal.html',
                        controller: ['$scope', 'growl', '$modalInstance', function($scope, growl, $modalInstance) {

                            $scope.newUser = {};

                            $scope.addUser = function() {

                                if ($scope.newUser.form.$invalid) return;

                                var name = $scope.newUser.name;
                                var email = $scope.newUser.email;

                                var data = {
                                    'Name': name,
                                    'Email': email
                                };

                                $scope.newUser.submitting = true;

                                $http.post("api/user", data).success(function(result) {
                                    growl.addSuccessMessage(name + " er oprettet i KITOS");

                                    $modalInstance.close(result.Response);
                                }).error(function(result) {
                                    $scope.newUser.submitting = false;
                                    growl.addErrorMessage("Fejl! " + name + " blev ikke oprettet i KITOS!");
                                });
                            };

                            $scope.cancel = function() {
                                $modalInstance.dismiss('cancel');
                            };
                        }]
                    });

                    modal.result.then(function(userResult) {
                        scope.userResult = userResult;

                        scope.selectResult = {
                            id: userResult.Id,
                            text: userResult.Name
                        };
                    }, function() {
                        scope.userResult = null;
                        scope.selectResult = null;
                    });
                };

            }
        };
    }]);
    
    app.directive('selectUser', ['$rootScope', '$http', function ($rootScope, $http) {
        return {
            scope: {
                inputName: '@?name',
                userModel: '='
            },
            replace: true,
            templateUrl: 'partials/directives/select-user.html',
            controller: ['$scope', function($scope) {
                $scope.selectUserOptions = {
                    minimumInputLength: 1,
                    initSelection: function(elem, callback) {
                    },
                    ajax: {
                        data: function(term, page) {
                            return { query: term };
                        },
                        quietMillis: 500,
                        transport: function(queryParams) {
                            var res = $http.get('api/user?q=' + queryParams.data.query).then(queryParams.success);
                            res.abort = function() {
                                return null;
                            };

                            return res;
                        },
                        results: function(data, page) {
                            console.log(data);
                            var results = [];

                            _.each(data.data.Response, function(user) {

                                results.push({
                                    id: user.Id,
                                    text: user.Name
                                });
                            });

                            return { results: results };
                        }
                    }
                };
            }]
        };
    }]);
    
})(angular, app);