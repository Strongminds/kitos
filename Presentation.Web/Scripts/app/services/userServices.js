﻿(function (ng, app) {

    var _user = null;

    app.factory('userService', ['$http', '$q', '$rootScope', '$modal', function ($http, $q, $rootScope, $modal) {
        
        //formats and saves the user
        function saveUser(response, currOrg) {
            var isLocalAdmin = _.some(response.adminRights, function(userRight) {
                return userRight.roleName == "LocalAdmin" && userRight.organizationId == currOrg.id;
            });

            //the current org unit is either:
            //a) the user selected default org unit (persisted in db) iff it belongs to the currently selected organization context 
            //b) the root of the currently selected organization context otherwise
            var currentOrgUnitId = response.defaultOrganizationUnitId;
            var currentOrgUnitName = response.defaultOrganizationUnitName;
            var isUsingDefaultOrgUnit = true;
            
            if (response.defaultOrganizationUnitOrganizationId != currOrg.id) {
                currentOrgUnitId = currOrg.root.id;
                currentOrgUnitName = currOrg.root.name;

                isUsingDefaultOrgUnit = false;
            }

            _user = {
                isAuth: true,
                id: response.id,
                name: response.name,
                lastName: response.lastName,
                qualifiedName: response.qualifiedName,
                email: response.email,
                phoneNumber: response.phoneNumber,
                
                isGlobalAdmin: response.isGlobalAdmin,
                isLocalAdmin: isLocalAdmin,
                
                currentOrganizationUnitId: currentOrgUnitId,
                currentOrganizationUnitName: currentOrgUnitName,
                defaultOrganizationUnitId: response.defaultOrganizationUnitId,
                isUsingDefaultOrgUnit: isUsingDefaultOrgUnit,
                
                currentOrganization: currOrg,
                currentOrganizationId: currOrg.id,
                currentOrganizationName: currOrg.name,
                currentConfig: currOrg.config
            };
            
            $rootScope.user = _user;

            return _user;
        }
        
        function patchUser(payload) {
            var deferred = $q.defer();

            if (_user == null) {
                deferred.reject("Not authenticated.");
            } else {
                $http({
                    method: 'PATCH',
                    url: 'api/user/' + _user.id + '?organizationId=' + _user.currentOrganizationId,
                    data: payload
                }).success(function (result) {
                    var newUser = result.response;
                    
                    saveUser(newUser, _user.currentOrganization);
                    loadUserDeferred = null;
                    deferred.resolve(_user);

                }).error(function () {
                    deferred.reject("Couldn't patch the user!");
                });

            }
            return deferred.promise;
        }

        function getUser() {

            var deferred = $q.defer();

            deferred.resolve(loadUser());

            return deferred.promise;
        }

        var loadUserDeferred = null;
        function loadUser(payload)
        {
            if (!loadUserDeferred) {
                loadUserDeferred = $q.defer();

                //login or re-auth?
                var httpDeferred = payload ? $http.post('api/authorize', payload) : $http.get('api/authorize');

                httpDeferred.success(function (result) {

                    resolveOrganization().then(function(currOrg) {
                        saveUser(result.response, currOrg);
                        loadUserDeferred.resolve(_user);

                    }, function() {
                        loadUserDeferred.reject("No organization selected");
                        loadUserDeferred = null;
                    });


                }).error(function (result) {
                    
                    loadUserDeferred.reject(result);
                    loadUserDeferred = null;
                    clearSavedOrgId();
                });
            }

            return loadUserDeferred.promise;
        }
        
        function login(email, password, rememberMe) {
            var deferred = $q.defer();

            if (!email || !password) {

                deferred.reject("Email or password cannot be empty");

            } else {
                
                 var data = {
                    "email": email,
                    "password": password,
                    "rememberMe": rememberMe
                 };

                deferred.resolve(loadUser(data));
                
            }

            return deferred.promise;
        }

        function logout() {
            

            var deferred = $q.defer();

            $http.post('api/authorize?logout').success(function (result) {
                loadUserDeferred = null;
                _user = null;
                $rootScope.user = null;

                deferred.resolve();

                clearSavedOrgId();

            }).error(function(result) {
                deferred.reject("Could not log out");

            });
            
            return deferred.promise;
        }
        
        function auth(adminRoles) {

            return getUser().then(function (user) {
                
                if (adminRoles) {
                    var hasRequiredRole = _.some(adminRoles, function(role) {
                        //if the state role is global admin, and the user is global admin, it's cool
                        //same for local admin
                        return (role == "GlobalAdmin" && user.isGlobalAdmin) || (role == "LocalAdmin" && user.isLocalAdmin);
                    });

                    if (!hasRequiredRole) return $q.reject("User doesn't have the required permissions");
                }

                return true;

            });

        }
        
        function getSavedOrgId() {
            var orgId = localStorage.getItem("currentOrgId");
            return orgId != null && JSON.parse(orgId);
        }
            
        function setSavedOrgId(orgId) {
            localStorage.setItem("currentOrgId", JSON.stringify(orgId));
        }
        
        function clearSavedOrgId() {
            localStorage.setItem("currentOrgId", null);
        }

        //resolve which organization context, the user will be working in.
        //when a user logs in, the user is prompted with a select-organization modal.
        //the organization that is selected here, will be saved in local storage, for the next
        //time the user is visiting.
        function resolveOrganization() {

            var deferred = $q.defer();
            
            //first try to get previous selected organization id from the local storage
            var storedOrgId = getSavedOrgId();

            if (storedOrgId) {
                
                //given the saved org id, fetch the org details and config from the server 
                $http.get("api/organization/" + storedOrgId).success(function(result) {
                    deferred.resolve(result.response);
                    
                }).error(function(result) {

                    //the saved org was probably bad
                    clearSavedOrgId();
                    
                    //prompt the user to select an org via modal
                    openModal();
                });

            } else {

                //no previous selected org in local storage. 
                
                //prompt the user to select an org via modal
                openModal();
            }
            
            //helper function for displaying the choose-organization modal
            function openModal() {
                //fetch the relevant organizations
                $http.get("api/user?organizations").success(function (result) {

                    var organizations = result.response.organizations;

                    //if there's only one, just select that
                    if (organizations.length == 1) {

                        var firstOrg = organizations[0];
                        setSavedOrgId(firstOrg.id);

                        deferred.resolve(firstOrg);

                    } else {

                        //otherwise, open a modal 
                        var modal = $modal.open({
                            backdrop: 'static',
                            templateUrl: 'partials/home/choose-organization.html',
                            controller: ['$scope', '$modalInstance', 'autofocus', function ($modalScope, $modalInstance, autofocus) {
                                autofocus();
                                $modalScope.orgChooser = {
                                    selectedId: result.response.defaultOrganizationId
                                };
                                
                                $modalScope.organizations = organizations;

                                $modalScope.ok = function () {

                                    var selectedOrg = _.findWhere(organizations, { id: $modalScope.orgChooser.selectedId });

                                    $modalInstance.close(selectedOrg);

                                };

                            }]
                        });

                        modal.result.then(function (selectedOrg) {

                            setSavedOrgId(selectedOrg.id);
                            deferred.resolve(selectedOrg);

                        }, function () {

                            deferred.reject("Modal dismissed");
                        });

                    }

                }).error(function () {
                    deferred.reject("Request for the users organizations failed!");
                });
            }

            return deferred.promise;
        }


        return {
            getUser: getUser,
            login: login,
            logout: logout,
            auth: auth,
            patchUser: patchUser
        };

    }]);


})(angular, app);