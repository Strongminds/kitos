﻿module Kitos.Models.DataProcessing {

    export interface IAssignedRoleDTO {
        user: ISimpleUserDTO,
        role: IDataProcessingRoleDTO,
    }

    export interface IDataProcessingAgreementDTO {
        id: number,
        name: string,
        organizationId: number,
        references: Array<dpaReference>;
		itSystems: Models.Generic.NamedEntity.NamedEntityWithEnabledStatusDTO[];
    }

    export interface dpaReference extends BaseReference {
        title: string;
        externalReferenceId: string;
        url: string;
        masterReference: boolean;
        created: Date;
        itSystems: Models.Generic.NamedEntity.NamedEntityWithEnabledStatusDTO[];
        assignedRoles: IAssignedRoleDTO[],
    }

    export interface IDataProcessingRoleDTO {
        id: number,
        name: string,
        note: string,
        hasWriteAccess: boolean,
    }

    export interface ISimpleUserDTO {
        id: number,
        name: string,
        email: string,
    }
}