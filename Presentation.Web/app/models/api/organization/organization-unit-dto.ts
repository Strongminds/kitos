﻿module Kitos.Models.Api.Organization {
    export interface IOrganizationUnitDto {
        id: number,
        name: string,
        ean: string,
        localId: number,
        parentId: number,
        organizationId: number,
    }
}
