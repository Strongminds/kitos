﻿module Kitos.ItProject.Edit {
    "use strict";

    interface IProjectStatusController {
        project: any;
        datepickerOptions: IDatepickerOptions;
        pagination: IPaginationSettings;
        milestonesActivities: Array<any>;
        totalCount: number;

        getPhaseName(phaseNumber: number): string;
    }

    class ProjectStatusController implements IProjectStatusController {
        public datepickerOptions: IDatepickerOptions;
        public milestonesActivities: Array<any>;
        public pagination: IPaginationSettings;
        public totalCount: number;
        public methodOptions: any;
        public allStatusUpdates: any;
        public currentStatusUpdate: any;
        public showCombinedChart: any;
        public combinedStatusUpdates: any;
        public splittedStatusUpdates: any;

        public static $inject: Array<string> = [
            "$scope",
            "$http",
            "$state",
            "notify",
            "project",
            "usersWithRoles",
            "user",
            "statusUpdates",
            "moment"
        ];

        constructor(
            private $scope: ng.IScope,
            private $http,
            private $state,
            private notify,
            public project,
            private usersWithRoles,
            private user,
            public statusUpdates,
            public moment) {

            this.project.updateUrl = `api/itproject/${project.id}`;

            this.datepickerOptions = {
                format: "dd-MM-yyyy",
                parseFormats: ["yyyy-MM-dd"]
            };

            // setup phases
            this.project.phases = [project.phase1, project.phase2, project.phase3, project.phase4, project.phase5];
            var prevPhase: IPhaseData = null;
            _.each(this.project.phases, (phase: IPhaseData) => {
                phase.updateUrl = `api/itProjectPhase/${phase.id}`;
                phase.prevPhase = prevPhase;
                prevPhase = phase;
            });

            // all Assignments - both Assignments ("opgaver") and milestones
            this.milestonesActivities = [];

            _.each(project.itProjectStatuses, (value) => {
                this.addStatus(value, null);
            });

            this.pagination = {
                search: "",
                skip: 0,
                take: 50
            };

            this.$scope.$watchCollection(() => this.pagination, this.loadStatues);


            /* STATUS PROJECT */

            this.methodOptions = [{ label: 'Samlet', val: true }, { label: 'Tid, kvalitet og ressourcer', val: false }];

            this.allStatusUpdates = statusUpdates;

            if (this.allStatusUpdates.length > 0) {
                this.currentStatusUpdate = this.allStatusUpdates[0];
                this.showCombinedChart = (this.currentStatusUpdate.IsCombined) ? this.methodOptions[0] : this.methodOptions[1];
            }

            this.combinedStatusUpdates = _.filter(this.allStatusUpdates, function (s: any) { return s.IsCombined; });
            this.splittedStatusUpdates = _.filter(this.allStatusUpdates, function (s: any) { return !s.IsCombined; });
        }



        private onSelectStatusMethod(showCombined) {
            if (showCombined) {
                this.currentStatusUpdate = (this.combinedStatusUpdates.length > 0) ? this.combinedStatusUpdates[0] : null;
            } else {
                this.currentStatusUpdate = (this.splittedStatusUpdates.length > 0) ? this.splittedStatusUpdates[0] : null;
            }
        }

        getPhaseName = (num: number): string => {
            if (num)
                return this.project.phases[num - 1].name;
            return "";
        };

        private loadStatues = () => {
            var url = `api/itProjectStatus/${this.project.id}?project=true`;

            url += `&skip=${this.pagination.skip}`;
            url += `&take=${this.pagination.take}`;

            if (this.pagination.orderBy) {
                url += `&orderBy=${this.pagination.orderBy}`;
                if (this.pagination.descending) url += `&descending=${this.pagination.descending}`;
            }

            if (this.pagination.search) {
                url += `&q=${this.pagination.search}`;
            } else {
                url += "&q=";
            }

            this.milestonesActivities = [];
            this.$http.get(url)
                .success((result, status, headers) => {
                    var paginationHeader = JSON.parse(headers("X-Pagination"));
                    this.totalCount = paginationHeader.TotalCount;

                    _.each(result.response, (value) => {
                        this.addStatus(value, null);
                    });

                })
                .error((data, status) => {
                    // only display error when an actual error
                    // 404 just says that there are no statuses
                    if (status != 404) {
                        this.notify.addErrorMessage("Kunne ikke hente projekter!");
                    }
                });
        };

        private addStatus = (activity, skipAdding) => {
            activity.show = true;

            if (activity.$type.indexOf("Assignment") > -1) {
                activity.isTask = true;
                activity.updateUrl = `api/Assignment/${activity.id}`;
            } else if (activity.$type.indexOf("Milestone") > -1) {
                activity.isMilestone = true;
                activity.updateUrl = `api/Milestone/${activity.id}`;
            }

            activity.updatePhase = () => {
                activity.phase = _.find(this.project.phases, { id: activity.associatedPhaseId });
            };

            activity.updatePhase();

            activity.updateUser = () => {
                if (activity.associatedUserId) {
                    activity.associatedUser = _.find(this.usersWithRoles, { id: activity.associatedUserId });
                }
            };

            activity.updateUser();

            activity.edit = () => {
                if (activity.isTask) {
                    this.$state.go(".modal", { type: "assignment", activityId: activity.id });
                } else if (activity.isMilestone) {
                    this.$state.go(".modal", { type: "milestone", activityId: activity.id });
                }
            };

            activity.delete = () => {
                var msg = this.notify.addInfoMessage("Sletter...");
                this.$http.delete(activity.updateUrl + "?organizationId=" + this.user.currentOrganizationId).success(() => {
                    activity.show = false;
                    msg.toSuccessMessage("Slettet!");
                }).error(() => {
                    msg.toErrorMessage("Fejl! Kunne ikke slette!");
                });
            };

            if (!skipAdding)
                this.milestonesActivities.push(activity);

            return activity;
        };
    }

    angular
        .module("app")
        .controller("project.EditStatusProjectCtrl", ProjectStatusController)
        .config([
            "$stateProvider", $stateProvider => {
                $stateProvider.state("it-project.edit.status-project", {
                    url: "/status-project",
                    templateUrl: "app/components/it-project/tabs/it-project-tab-status-project.view.html",
                    controller: ProjectStatusController,
                    controllerAs: "projectStatusVm",
                    resolve: {
                        //returns a map with those users who have a role in this project.
                        //the names of the roles is saved in user.roleNames
                        usersWithRoles: [
                            "$http", "$stateParams",
                            ($http, $stateParams) => $http.get(`api/itprojectright/${$stateParams.id}`)
                            .then(rightResult => {
                                var rights = rightResult.data.response;

                                //get the role names
                                //the resulting map
                                var users = {};
                                _.each(rights, (right: { userId; user; roleName; }) => {

                                    //use the user from the map if possible
                                    var user = users[right.userId] || right.user;

                                    var roleNames = user.roleNames || [];
                                    roleNames.push(right.roleName);
                                    user.roleNames = roleNames;

                                    users[right.userId] = user;
                                });

                                return users;
                            })
                        ],
                        statusUpdates: [
                            "$http", "$stateParams",
                            ($http, $stateParams) => $http.get(`odata/ItProjects(${$stateParams.id})?$expand=ItProjectStatusUpdates($orderby=Created desc;$expand=ObjectOwner($select=Name,LastName))`)
                                .then(result => {
                                    return result.data.ItProjectStatusUpdates;
                                })
                        ]
                    }
                });
            }
        ]);
}
