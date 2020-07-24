(function(ng, app) {
    app.factory('taskService', [
        '$http', '$q', function($http, $q) {
            var baseUrl = 'api/taskRef/';

            function getRoots() {
                var deferred = $q.defer();

                var url = baseUrl + '?roots=true&take=200';

                $http.get(url).then(function onSuccess(response) {
                    deferred.resolve(response.data);
                }, function onFailure(response) {
                    deferred.reject("Couldn't load tasks");
                });

                return deferred.promise;
            }

            function getChildren(id) {
                var deferred = $q.defer();

                var url = baseUrl + id + '?children=true';

                $http.get(url).then(function onSuccess(response) {

                    deferred.resolve(response.data.response);

                }, function onError(response) {
                    reject("Couldn't load tasks")
                });

                function reject(reason) {
                    return function() {
                        deferred.reject(reason);
                    };
                }

                return deferred.promise;
            }

            return {
                getRoots: getRoots,
                getChildren: getChildren
            };
        }
    ]);
})(angular, app);
