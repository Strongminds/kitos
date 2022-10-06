﻿((ng, app) => {
    app.config(['$stateProvider', $stateProvider => {
        $stateProvider.state('local-config', {
            url: '/local-config',
            abstract: true,
            template: '<ui-view autoscroll="false" />',
            resolve: {
                user: ['userService', userService => userService.getUser().then(user => user)],
                config: ['user', user => user.currentConfig]
            },
            controller: ['$rootScope', '$scope', 'config', 'user',
                ($rootScope, $scope, config, user: Kitos.Services.IUser) => {
                    $rootScope.page.title = 'Konfiguration';
                    $rootScope.page.subnav = [
                        { state: 'local-config.current-org', text: user.currentOrganizationName },
                        { state: 'local-config.org', text: 'Organisation' },
                        { state: 'local-config.system', text: 'IT System' },
                        { state: 'local-config.contract', text: 'IT Kontrakt' },
                        { state: 'local-config.data-processing', text: 'Databehandling' },
                        { state: 'local-config.import.organization', text: 'Masse Opret' }
                    ];

                    $scope.config = config;
                    $scope.config.autosaveUrl = "odata/Configs(" + config.id + ")";
                    $rootScope.subnavPositionCenter = true;

                    $scope.$on('$viewContentLoaded', () => {
                        $rootScope.positionSubnav();
                    });
                }]
        });
    }]);
})(angular, app);
