﻿module Kitos.DataProcessing.Registration.Overview {
    "use strict";

    export interface IOverviewController extends Utility.KendoGrid.IGridViewAccess<Models.DataProcessing.IDataProcessingRegistration> {
    }

    export class OverviewController implements IOverviewController {
        mainGrid: IKendoGrid<Models.DataProcessing.IDataProcessingRegistration>;
        mainGridOptions: IKendoGridOptions<Models.DataProcessing.IDataProcessingRegistration>;
        canCreate: boolean;
        projectIdToAccessLookup = {};

        static $inject: Array<string> = [
            "$rootScope",
            "$scope",
            "$state",
            "user",
            "userAccessRights",
            "kendoGridLauncherFactory",
            "dataProcessingRegistrationOptions"
        ];

        constructor(
            $rootScope: IRootScope,
            $scope: ng.IScope,
            $state: ng.ui.IStateService,
            user,
            userAccessRights: Models.Api.Authorization.EntitiesAccessRightsDTO,
            kendoGridLauncherFactory: Utility.KendoGrid.IKendoGridLauncherFactory,
            dataProcessingRegistrationOptions: Models.DataProcessing.IDataProcessingRegistrationOptions) {

            //Prepare the page
            $rootScope.page.title = "Databehandling - Overblik";

            //Helper functions
            const getRoleKey = (role: Kitos.Models.DataProcessing.IDataProcessingRoleDTO) => `role${role.id}`;

            const replaceRoleQuery = (filterUrl, roleName, roleId) => {
                var pattern = new RegExp(`(\\w+\\()${roleName}(,.*?\\))`, "i");
                return filterUrl.replace(pattern, `RoleAssignments/any(c: $1c/UserFullName$2 and c/RoleId eq ${roleId})`);
            };

            //Lookup maps
            var dpaRoleIdToUserNamesMap = {};

            //Build and launch kendo grid
            var launcher =
                kendoGridLauncherFactory
                    .create<Models.DataProcessing.IDataProcessingRegistration>()
                    .withScope($scope)
                    .withGridBinding(this)
                    .withUser(user)
                    .withEntityTypeName("Databehandling")
                    .withExcelOutputName("Databehandling - Overblik")
                    .withStorageKey("data-processing-registration-overview-options")
                    .withFixedSourceUrl(`/odata/Organizations(${user.currentOrganizationId})/DataProcessingRegistrationReadModels?$expand=RoleAssignments`)
                    .withParameterMapping((options, type) => {
                        // get kendo to map parameters to an odata url
                        var parameterMap = kendo.data.transports["odata-v4"].parameterMap(options, type);

                        if (parameterMap.$filter) {
                            dataProcessingRegistrationOptions.roles.forEach(role => {
                                parameterMap.$filter =
                                    replaceRoleQuery(parameterMap.$filter, getRoleKey(role), role.id);
                            });
                        }

                        return parameterMap;
                    })
                    .withResponseParser(response => {
                        //Reset all response state
                        dpaRoleIdToUserNamesMap = {};

                        //Build lookups/mutations
                        response.forEach(dpa => {
                            dpaRoleIdToUserNamesMap[dpa.Id] = {};

                            //Update the role assignment map
                            dpa.RoleAssignments.forEach(assignment => {
                                if (!dpaRoleIdToUserNamesMap[dpa.Id][assignment.RoleId])
                                    dpaRoleIdToUserNamesMap[dpa.Id][assignment.RoleId] = assignment.UserFullName;
                                else {
                                    dpaRoleIdToUserNamesMap[dpa.Id][assignment.RoleId] += `, ${assignment.UserFullName}`;
                                }
                            });
                        });
                        return response;
                    })
                    .withToolbarEntry({
                        id: "createDpa",
                        title: "Opret Registrering",
                        color: Utility.KendoGrid.KendoToolbarButtonColor.Green,
                        position: Utility.KendoGrid.KendoToolbarButtonPosition.Right,
                        enabled: () => userAccessRights.canCreate,
                        onClick: () => $state.go("data-processing.overview.create-registration")
                    } as Utility.KendoGrid.IKendoToolbarEntry)
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("Name")
                            .withTitle("Databehandling")
                            .withId("dpaName")
                            .withStandardWidth(200)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderInternalReference("kendo-dpa-name-rendering", "data-processing.edit-registration.main", dataItem.SourceEntityId, dataItem.Name))
                            .withSourceValueEchoExcelOutput())
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("MainReferenceTitle")
                            .withTitle("Reference")
                            .withId("dpReferenceId")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderReference(dataItem.MainReferenceTitle, dataItem.MainReferenceUrl))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderReference(dataItem.MainReferenceTitle, dataItem.MainReferenceUrl)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("MainReferenceUserAssignedId")
                            .withTitle("Dokument ID / Sagsnr.")
                            .withId("dpReferenceUserAssignedId")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderReferenceId(dataItem.MainReferenceUserAssignedId))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderReferenceId(dataItem.MainReferenceUserAssignedId))
                            .withInitialVisibility(false))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("SystemNamesAsCsv")
                            .withTitle("IT Systemer")
                            .withId("dpSystemNamesAsCsv")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.SystemNamesAsCsv))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.SystemNamesAsCsv)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("DataProcessorNamesAsCsv")
                            .withTitle("Databehandlere")
                            .withId("dpDataProcessorNamesAsCsv")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.DataProcessorNamesAsCsv))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.DataProcessorNamesAsCsv)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("SubDataProcessorNamesAsCsv")
                            .withTitle("Underdatabehandlere")
                            .withId("dpSubDataProcessorNamesAsCsv")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.SubDataProcessorNamesAsCsv))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.SubDataProcessorNamesAsCsv)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("TransferToInsecureThirdCountries")
                            .withTitle("Overførsel til usikkert 3. land")
                            .withId("dpTransferToInsecureThirdCountries")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                [
                                    Models.Api.Shared.YesNoUndecidedOption.Yes,
                                    Models.Api.Shared.YesNoUndecidedOption.No,
                                    Models.Api.Shared.YesNoUndecidedOption.Undecided
                                ].map(value => {
                                    return {
                                        textValue: Models.ViewModel.Shared.YesNoUndecidedOptions.getText(value),
                                        remoteValue: value
                                    }
                                })
                                , false
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.TransferToInsecureThirdCountries && Models.ViewModel.Shared.YesNoUndecidedOptions.getText(dataItem.TransferToInsecureThirdCountries)))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.TransferToInsecureThirdCountries && Models.ViewModel.Shared.YesNoUndecidedOptions.getText(dataItem.TransferToInsecureThirdCountries))))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("BasisForTransfer")
                            .withTitle("Overførselsgrundlag")
                            .withId("dpBasisForTransfer")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                dataProcessingRegistrationOptions
                                    .basisForTransferOptions
                                    .map(value => {
                                        return {
                                            textValue: value.name,
                                            remoteValue: value.name
                                        }
                                    })
                                , false
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.BasisForTransfer))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.BasisForTransfer)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("DataResponsible")
                            .withTitle("Dataansvarlig")
                            .withId("dpDataResponsible")
                            .withStandardWidth(170)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                dataProcessingRegistrationOptions
                                    .dataResponsibleOptions
                                    .map(value => {
                                    return {
                                        textValue: value.name,
                                        remoteValue: value.name
                                    }
                                    })
                                , false
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.DataResponsible))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.DataResponsible)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("IsAgreementConcluded")
                            .withTitle("Databehandleraftale er indgået")
                            .withId("agreementConcluded")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                [
                                    Models.Api.Shared.YesNoIrrelevantOption.YES,
                                    Models.Api.Shared.YesNoIrrelevantOption.NO,
                                    Models.Api.Shared.YesNoIrrelevantOption.IRRELEVANT,
                                    Models.Api.Shared.YesNoIrrelevantOption.UNDECIDED
                                ].map(value => {
                                    return {
                                        textValue: Models.ViewModel.Shared.YesNoIrrelevantOptions.getText(value),
                                        remoteValue: value
                                    }
                                })
                                , false
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.IsAgreementConcluded && Models.ViewModel.Shared.YesNoIrrelevantOptions.getText(dataItem.IsAgreementConcluded)))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.IsAgreementConcluded && Models.ViewModel.Shared.YesNoIrrelevantOptions.getText(dataItem.IsAgreementConcluded))))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("AgreementConcludedAt")
                            .withTitle("Dato for indgåelse af databehandleraftale")
                            .withId("agreementConcludedAt")
                            .withStandardWidth(160)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Date)
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderDate(dataItem.AgreementConcludedAt))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderDate(dataItem.AgreementConcludedAt)))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("OversightInterval")
                            .withTitle("Tilsynsinterval")
                            .withId("oversightInterval")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                [
                                    Models.Api.Shared.YearMonthUndecidedIntervalOption.Half_yearly,
                                    Models.Api.Shared.YearMonthUndecidedIntervalOption.Yearly,
                                    Models.Api.Shared.YearMonthUndecidedIntervalOption.Every_second_year,
                                    Models.Api.Shared.YearMonthUndecidedIntervalOption.Other,
                                    Models.Api.Shared.YearMonthUndecidedIntervalOption.Undecided
                                ].map(value => {
                                    return {
                                        textValue: Models.ViewModel.Shared.YearMonthUndecidedIntervalOption.getText(value),
                                        remoteValue: value
                                    }
                                })
                                , false
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.OversightInterval && Models.ViewModel.Shared.YearMonthUndecidedIntervalOption.getText(dataItem.OversightInterval)))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.OversightInterval && Models.ViewModel.Shared.YearMonthUndecidedIntervalOption.getText(dataItem.OversightInterval))))
                    .withColumn(builder =>
                        builder
                            .withDataSourceName("OversightOptionNamesAsCsv")
                            .withTitle("Tilsynsmuligheder")
                            .withId("dpOversightOptionNamesAsCsv")
                            .withStandardWidth(150)
                            .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                            .withFixedValueRange
                            (
                                dataProcessingRegistrationOptions
                                    .oversightOptions
                                    .map(value => {
                                        return {
                                            textValue: value.name,
                                            remoteValue: value.name
                                        }
                                    })
                                , true
                            )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.OversightOptionNamesAsCsv))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.OversightOptionNamesAsCsv)))
                    .withColumn(builder =>
                        builder
                        .withDataSourceName("IsOversightCompleted")
                        .withTitle("Gennemført tilsyn")
                        .withId("isOversightCompleted")
                        .withStandardWidth(150)
                        .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.FixedValueRange)
                        .withFixedValueRange
                        (
                            [
                                Models.Api.Shared.YesNoUndecidedOption.Yes,
                                Models.Api.Shared.YesNoUndecidedOption.No,
                                Models.Api.Shared.YesNoUndecidedOption.Undecided
                            ].map(value => {
                                return {
                                    textValue: Models.ViewModel.Shared.YesNoUndecidedOptions.getText(value),
                                    remoteValue: value
                                }
                            })
                            , false
                        )
                            .withRendering(dataItem => Helpers.RenderFieldsHelper.renderString(dataItem.IsOversightCompleted && Models.ViewModel.Shared.YesNoUndecidedOptions.getText(dataItem.IsOversightCompleted)))
                            .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dataItem.IsOversightCompleted && Models.ViewModel.Shared.YesNoUndecidedOptions.getText(dataItem.IsOversightCompleted))))
                    .withStandardSorting("Name");

            dataProcessingRegistrationOptions.roles.forEach(role =>
                launcher = launcher.withColumn(builder =>
                    builder
                        .withDataSourceName(getRoleKey(role))
                        .withTitle(role.name)
                        .withId(`dpa${getRoleKey(role)}`)
                        .withStandardWidth(150)
                        .withFilteringOperation(Utility.KendoGrid.KendoGridColumnFiltering.Contains)
                        .withoutSorting() //Sorting is not possible on expressions which are required on role columns since they are generated in the UI as a result of content of a complex typed child collection
                        .withInitialVisibility(false)
                        .withRendering(dataItem => Helpers.RenderFieldsHelper.renderInternalReference(`kendo-dpa-${getRoleKey(role)}-rendering`, "data-processing.edit-registration.roles", dataItem.SourceEntityId, dpaRoleIdToUserNamesMap[dataItem.Id][role.id]))
                        .withExcelOutput(dataItem => Helpers.ExcelExportHelper.renderString(dpaRoleIdToUserNamesMap[dataItem.Id][role.id])))
            );

            //Launch kendo grid
            launcher.launch();
        }
    }

    angular
        .module("app")
        .config([
            "$stateProvider", $stateProvider => {
                $stateProvider.state("data-processing.overview", {
                    url: "/overview",
                    templateUrl: "app/components/data-processing/data-processing-registration-overview.view.html",
                    controller: OverviewController,
                    controllerAs: "vm",
                    resolve: {
                        user: [
                            "userService", userService => userService.getUser()
                        ],
                        userAccessRights: ["authorizationServiceFactory", (authorizationServiceFactory: Services.Authorization.IAuthorizationServiceFactory) =>
                            authorizationServiceFactory
                                .createDataProcessingRegistrationAuthorization()
                                .getOverviewAuthorization()],
                        dataProcessingRegistrationOptions: [
                            "dataProcessingRegistrationService", "user", (dataProcessingRegistrationService: Services.DataProcessing.IDataProcessingRegistrationService, user) => dataProcessingRegistrationService.getApplicableDataProcessingRegistrationOptions(user.currentOrganizationId)
                        ]
                    }
                });
            }
        ]);
}
