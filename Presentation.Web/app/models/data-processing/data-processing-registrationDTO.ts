﻿module Kitos.Models.DataProcessing {
    export interface IAssignedRoleDTO {
        user: ISimpleUserDTO,
        role: IDataProcessingRoleDTO,
    }

    export interface IDataProcessingRegistrationDTO {
        id: number,
        name: string,
        references: Array<IDataProcessingReferenceDTO>;
        itSystems: Models.Generic.NamedEntity.NamedEntityWithEnabledStatusDTO[];
        assignedRoles: IAssignedRoleDTO[];
        dataProcessors: IDataProcessorDTO[];
        hasSubDataProcessors?: Models.Api.Shared.YesNoUndecidedOption;
        subDataProcessors: IDataProcessorDTO[];
        agreementConcluded: Models.Generic.ValueWithOptionalDateAndRemarkDTO<Models.Api.Shared.YesNoIrrelevantOption>;
        transferToInsecureThirdCountries?: Models.Api.Shared.YesNoUndecidedOption;
        insecureThirdCountries: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[];
        basisForTransfer: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO;
        dataResponsible: Models.Generic.ValueWithOptionalRemarkDTO<Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO>;
        oversightInterval: Models.Generic.ValueWithOptionalRemarkDTO<Models.Api.Shared.YearMonthUndecidedIntervalOption>;
        oversightOptions: Models.Generic.ValueWithOptionalRemarkDTO<Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[]>;
        oversightCompleted: Models.Generic.ValueWithOptionalDateAndRemarkDTO<Models.Api.Shared.YesNoUndecidedOption>;
        associatedContracts: Models.Generic.NamedEntity.NamedEntityDTO[];
    }

    export interface IDataProcessingReferenceDTO extends BaseReference {
        title: string;
        externalReferenceId: string;
        url: string;
        masterReference: boolean;
        created: Date;
        itSystems: Models.Generic.NamedEntity.NamedEntityWithEnabledStatusDTO[];
        assignedRoles: IAssignedRoleDTO[],
    }

    export interface IDataProcessorDTO extends Models.Generic.NamedEntity.NamedEntityDTO {
        cvrNumber: string,
    }

    export interface IDataProcessingRoleDTO extends Models.Generic.NamedEntity.NamedEntityDTO {
        note: string,
        hasWriteAccess: boolean,
        expired:boolean;
    }

    export interface ISimpleUserDTO extends Models.Generic.NamedEntity.NamedEntityDTO {
        email: string,
    }

    export interface IDataProcessingRegistrationOptions {
        thirdCountryOptions: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[],
        dataResponsibleOptions: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[],
        basisForTransferOptions: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[],
        oversightOptions: Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO[],
        roles: IDataProcessingRoleDTO[],
    }
}