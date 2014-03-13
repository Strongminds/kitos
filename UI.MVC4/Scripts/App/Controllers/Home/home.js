﻿(function(ng, app) {

    app.config(["$stateProvider", "$urlRouterProvider", function ($stateProvider, $urlRouterProvider) {
        
        $stateProvider.state("index", {
            url: "/",
            templateUrl: "partials/home/index.html",
            controller: "home.IndexCtrl",
            noAuth: true
        }).state("login", {
            url: "/login",
            templateUrl: "partials/home/login.html",
            controller: "home.LoginCtrl",
            noAuth: true
        }).state("forgot-password", {
            url: "/hforgot-password",
            templateUrl: "partials/home/forgot-password.html",
            controller: "home.ForgotPasswordCtrl",
            noAuth: true
        }).state("reset-password", {
            url: "/reset-password/:requestId",
            templateUrl: "partials/home/reset-password.html",
            controller: "home.ResetPasswordCtrl",
            noAuth: true
        });

    }]);

    app.controller("home.IndexCtrl", function ($rootScope, $scope) {
        $rootScope.page.title = "Index";
    });
    
    app.controller("home.LoginCtrl", function ($rootScope, $scope, $http, $state) {
        $rootScope.page.title = "Log ind";
        
        //login
        $scope.submit = function () {
            if ($scope.loginForm.$invalid) return;

            var data = {
                "Email": $scope.email,
                "Password": $scope.password,
                "RememberMe": $scope.remember
            };

            $http.post("api/authorize", data).success(function (result) {

                $rootScope.saveUser(result);
                
                $state.go("index");
            });

        };
    });

    app.controller("home.ForgotPasswordCtrl", function ($rootScope, $scope, $http) {
        $rootScope.page.title = "Glemt password";
        
        //submit 
        $scope.submit = function() {
            if ($scope.requestForm.$invalid) return;

            var data = { "Email": $scope.email };

            $scope.requestSuccess = $scope.requestFailure = false;
            
            $http.post("api/passwordresetrequest", data).success(function(result) {
                $scope.requestSuccess = true;
                $scope.email = "";
                console.log(result);
            }).error(function (result) {
                $scope.requestFailure = true;
            });
        };
    });

    app.controller("home.ResetPasswordCtrl", function ($rootScope, $scope, $http, $stateParams) {
        $rootScope.page.title = "Nyt password";
        
        var requestId = $stateParams.requestId;
        $http.get("api/passwordresetrequest?requestId=" + requestId).success(function(result) {
            $scope.resetStatus = "enterPassword";
            $scope.email = result.Response.UserEmail;
        }).error(function() {
            $scope.resetStatus = "missingRequest";
        });

        $scope.submit = function() {
            if ($scope.resetForm.$invalid) return;

            var data = { "RequestId": requestId, "NewPassword": $scope.password };
            
            $http.post("api/authorize?resetPassword", data).success(function (result) {
                $scope.resetStatus = "success";
                $scope.email = "";
            }).error(function (result) {
                $scope.requestFailure = true;
            });
        };


    });

})(angular, App);