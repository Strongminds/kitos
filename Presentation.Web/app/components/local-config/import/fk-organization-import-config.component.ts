﻿module Kitos.LocalAdmin.Components {
    "use strict";

    function setupComponent(): ng.IComponentOptions {
        return {
            bindings: {
                currentOrganizationUuid: "<"
            },
            controller: FkOrganizationImportController,
            controllerAs: "ctrl",
            templateUrl: `app/components/local-config/import/fk-organization-import-config.view.html`
        };
    }

    enum CommandCategory {
        Create = "create",
        Update = "update",
        Delete = "delete"
    }

    interface IFkOrganizationCommand {
        id: string
        text: string
        onClick: () => void
        enabled: boolean
        category: CommandCategory
    }

    interface IFkOrganizationSynchronizationStatus {
        connected: boolean
        subscribesToUpdates: boolean
        synchronizationDepth: number | null
    }

    interface IFkOrganizationImportController extends ng.IComponentController {
        currentOrganizationUuid: string
        accessGranted: boolean | null
        accessError: string | null
        synchronizationStatus: IFkOrganizationSynchronizationStatus | null
        commands: Array<IFkOrganizationCommand> | null
        busy: boolean
    }

    class FkOrganizationImportController implements IFkOrganizationImportController {
        currentOrganizationUuid: string | null = null; //note set by bindings
        accessGranted: boolean | null = null;
        accessError: string | null = null;
        synchronizationStatus: IFkOrganizationSynchronizationStatus | null = null;
        commands: Array<IFkOrganizationCommand> | null = null;
        busy: boolean = false;

        static $inject: string[] = ["stsOrganizationSyncService", "fkOrganisationImportDialogFactory"];
        constructor(
            private readonly stsOrganizationSyncService: Kitos.Services.Organization.IStsOrganizationSyncService,
            private readonly fkOrganisationImportDialogFactory: Kitos.LocalAdmin.FkOrganisation.Modals.IFKOrganisationImportDialogFactory) {
        }

        $onInit() {
            if (this.currentOrganizationUuid === null) {
                console.error("missing attribute: 'currentOrganizationUuid'");
            } else {
                this.loadState();
            }
        }

        private resetState() {
            this.accessGranted = null;
            this.accessError = null;
            this.synchronizationStatus = null;
            this.commands = null;
            this.busy = false;
        }

        private loadState() {
            this.resetState();
            this.stsOrganizationSyncService
                .getConnectionStatus(this.currentOrganizationUuid)
                .then(result => {
                    this.bindAccessProperties(result);
                    this.bindSynchronizationStatus(result);
                    this.bindCommands(result);
                }, error => {
                    console.error(error);
                    this.accessGranted = false;
                    this.accessError = "Der skete en fejl ifm. tjek for forbindelsen til FK Organisation. Genindlæs venligst siden for at prøve igen.";
                });
        }

        private bindCommands(result: Models.Api.Organization.StsOrganizationSynchronizationStatusResponseDTO) {
            const newCommands: Array<IFkOrganizationCommand> = [];
            if (result.connected) {
                newCommands.push({
                    id: "updateSync",
                    text: "Rediger",
                    category: CommandCategory.Update,
                    enabled: result.canUpdateConnection,
                    onClick: () => {
                        this.fkOrganisationImportDialogFactory
                            .open(Kitos.LocalAdmin.FkOrganisation.Modals.FKOrganisationImportFlow.Update, this.currentOrganizationUuid, this.synchronizationStatus.synchronizationDepth, this.synchronizationStatus.subscribesToUpdates)
                            .closed.then(() => {
                                //Reload state from backend if the dialog was closed 
                                this.loadState();
                            });
                    }
                });
                newCommands.push({
                    id: "breakSync",
                    text: "Afbryd",
                    category: CommandCategory.Delete,
                    enabled: result.canDeleteConnection,
                    onClick: () => {
                        if (confirm("Afbryd forbindelsen til FK Organisation? Ved afbrydelse af forbindelsen, konverteres alle organisationsenheder til KITOS enheder, hvorefter de frit kan redigeres.")) {
                            this.busy = true;
                            this.stsOrganizationSyncService
                                .disconnect(this.currentOrganizationUuid)
                                .then(success => {
                                    if (success) {
                                        this.loadState();
                                    } else {
                                        this.busy = false;
                                    }
                                }, _ => {
                                    this.busy = false;
                                });
                        }
                    }
                });
            } else {
                newCommands.push({
                    id: "createSync",
                    text: "Forbind",
                    category: CommandCategory.Create,
                    enabled: result.canCreateConnection,
                    onClick: () => {
                        this.fkOrganisationImportDialogFactory
                            .open(Kitos.LocalAdmin.FkOrganisation.Modals.FKOrganisationImportFlow.Create, this.currentOrganizationUuid, null, false)
                            .closed.then(() => {
                                //Reload state from backend if the dialog was closed 
                                this.loadState();
                            });
                    }
                });
            }

            this.commands = newCommands;
        }

        private bindSynchronizationStatus(result: Models.Api.Organization.StsOrganizationSynchronizationStatusResponseDTO) {
            this.synchronizationStatus = {
                connected: result.connected,
                synchronizationDepth: result.synchronizationDepth,
                subscribesToUpdates: result.subscribesToUpdates
            };
        }

        private bindAccessProperties(result: Models.Api.Organization.StsOrganizationSynchronizationStatusResponseDTO) {
            if (result.accessStatus.accessGranted) {
                this.accessGranted = true;
            } else {
                this.accessGranted = false;
                switch (result.accessStatus.error) {
                    case Models.Api.Organization.CheckConnectionError.ExistingServiceAgreementIssue:
                        this.accessError = "Der er problemer med den eksisterende serviceaftale, der giver KITOS adgang til data fra din kommune i FK Organisatoin. Kontakt venligst den KITOS ansvarlige i din kommune for hjælp.";
                        break;
                    case Models.Api.Organization.CheckConnectionError.InvalidCvrOnOrganization:
                        this.accessError = "Der enten mangler eller er registreret et ugyldigt CVR nummer på din kommune i KITOS.";
                        break;
                    case Models.Api.Organization.CheckConnectionError.MissingServiceAgreement:
                        this.accessError = "Din organisation mangler en gyldig serviceaftale der giver KITOS adgang til data fra din kommune i FK Organisation. Kontakt venligst den KITOS ansvarlige i din kommune for hjælp.";
                        break;
                    case Models.Api.Organization.CheckConnectionError.Unknown: //intended fallthrough
                    default:
                        this.accessError = "Der skete en ukendt fejl ifm. tjek for forbindelsen til FK Organisation. Genindlæs venligst siden for at prøve igen.";
                        break;
                }
            }
        }
    }
    angular.module("app")
        .component("fkOrgnizationImportConfig", setupComponent());
}