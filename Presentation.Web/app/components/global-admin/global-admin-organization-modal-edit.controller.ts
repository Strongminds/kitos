﻿module Kitos.GlobalAdmin.Organization {
    "use strict";

    export class EditOrganizationController {
        public static $inject: string[] = ['$rootScope', '$scope', '$http', 'notify', 'org', 'user'];

        constructor(private $rootScope, private $scope, private $http, private notify, public org, private user) {
            var orgViewModel = new Models.ViewModel.GlobalAdmin.Organization.OrganizationModalViewModel();
            orgViewModel.configureAsEditOrganizationDialog(org.name, org.accessmodifier, org.cvr, org.typeId, org.foreignCvr);
            $rootScope.page.title = orgViewModel.title;
            $scope.title = orgViewModel.title;
            $scope.org = orgViewModel.data;
        }


        public dismiss() {
            this.$scope.$dismiss();
        };

        public submit() {
            var payload = this.$scope.org;

            this.$http({
                method: 'PATCH',
                url: `api/organization/${this.org.id}?organizationId=${this.user.currentOrganizationId}`,
                data: payload
            }).then((success) => {
                this.notify.addSuccessMessage("Ændringerne er blevet gemt!");
                this.$scope.$close(true);
            }, (error) => {
                this.notify.addErrorMessage("Ændringerne kunne ikke gemmes!");
            });
        };
    }

    angular
        .module("app")
        .config([
            '$stateProvider', ($stateProvider) => {
                $stateProvider.state('global-admin.organizations.edit', {
                    url: '/edit/:id',
                    authRoles: ['GlobalAdmin'],
                    onEnter: ['$state', '$stateParams', '$uibModal',
                        ($state: ng.ui.IStateService, $stateParams: ng.ui.IStateParamsService, $modal: ng.ui.bootstrap.IModalService) => {
                            $modal.open({
                                size: 'lg',
                                templateUrl: 'app/components/global-admin/global-admin-organization-modal.view.html',
                                // fade in instead of slide from top, fixes strange cursor placement in IE
                                // http://stackoverflow.com/questions/25764824/strange-cursor-placement-in-modal-when-using-autofocus-in-internet-explorer
                                windowClass: 'modal fade in',
                                resolve: {
                                    org: [
                                        '$http', function ($http) {
                                            return $http.get(`api/organization/${$stateParams['id']}`).then(result => result.data.response);
                                        }
                                    ],
                                    user: [
                                        'userService', function (userService) {
                                            return userService.getUser();
                                        }
                                    ]
                                },
                                controller: EditOrganizationController,
                                controllerAs: 'ctrl'
                            }).result.then(() => {
                                // OK
                                // GOTO parent state and reload
                                $state.go('^', null, { reload: true });
                            }, () => {
                                // Cancel
                                // GOTO parent state
                                $state.go('^');
                            });
                        }
                    ]
                });
            }
        ]);
}
