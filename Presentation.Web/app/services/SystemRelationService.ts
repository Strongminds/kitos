﻿module Kitos.Services {
    import IItSystemUsageRelationDTO = Models.Api.ItSystemUsage.Relation.IItSystemUsageRelationDTO;
    import IItSystemUsageRelationOptionsDTO = Models.Api.ItSystemUsage.Relation.IItSystemUsageRelationOptionsDTO;
    import IItSystemUsageCreateRelationDTO = Models.Api.ItSystemUsage.Relation.IItSystemUsageCreateRelationDTO;

    export interface ISystemRelationService {
        getRelationsFrom(systemUsageId: number): ng.IPromise<IItSystemUsageRelationDTO[]>;
        getRelationsTo(systemUsageId: number): ng.IPromise<IItSystemUsageRelationDTO[]>;
        getRelationWithContract(contractId: number): ng.IPromise<IItSystemUsageRelationDTO[]>;
        getAvailableRelationOptions(fromSystemUsageId: number, toSystemUsageId: number): ng.IPromise<IItSystemUsageRelationOptionsDTO[]>;
        createSystemRelation(systemRelation: IItSystemUsageCreateRelationDTO): ng.IPromise<{}>;
    }

    export class SystemRelationService implements ISystemRelationService {

        static $inject = ["$http"];
        constructor(private readonly $http: ng.IHttpService) {
        }


        getRelationsFrom(systemUsageId: number) {
            return this.$http.get(`api/v1/systemrelations/from/${systemUsageId}`)
                .then(response => {
                    var kitosSystemRelationResponse = response.data as { msg: string, response: IItSystemUsageRelationDTO[] }
                    return kitosSystemRelationResponse.response;
                });
        }

        getRelationsTo(systemUsageId: number) {
            return this.$http.get(`api/v1/systemrelations/to/${systemUsageId}`)
                .then(response => {
                    var kitosSystemRelationResponse = response.data as { msg: string, response: IItSystemUsageRelationDTO[] }
                    return kitosSystemRelationResponse.response;
                });
        }

        getRelationWithContract(contractId: number) {
            return this.$http.get(`api/v1/systemrelations/associated-with/contract/${contractId}`)
                .then(response => {
                    var kitosSystemRelationResponse = response.data as { msg: string, response: IItSystemUsageRelationDTO[] }
                    return kitosSystemRelationResponse.response;
                });
        }

        getRelation(systemUsageId: number, relationId: number) {
            return this.$http.get(`api/v1/systemrelations/from/${systemUsageId}/${relationId}`)
                .then(response => {
                    var kitosSystemRelationResponse = response.data as { msg: string, response: IItSystemUsageRelationDTO }
                    return { error: false, data: kitosSystemRelationResponse.response };
                },
                error => {
                    return { error: true, data: null };
                });
        }

        getAvailableRelationOptions(fromSystemUsageId: number, toSystemUsageId: number) {
            return this.$http.get(`api/v1/systemrelations/options/${fromSystemUsageId}/in-relation-to/${toSystemUsageId}`)
                .then(response => {
                    var kitosSystemRelationResponse = response.data as { msg: string, response: IItSystemUsageRelationOptionsDTO[] }
                    return kitosSystemRelationResponse.response;
                });
        }

        createSystemRelation(systemRelation: IItSystemUsageCreateRelationDTO) {
            return this.$http.post("api/v1/systemrelations", systemRelation);
        }

        patchSystemRelation(systemRelation: IItSystemUsageCreateRelationDTO) {
            return this.$http.patch("api/v1/systemrelations", systemRelation);
        }

        deleteSystemRelation(systemUsageId: number, relationId: number) {
            return this.$http.delete(`api/v1/systemrelations/from/${systemUsageId}/${relationId}`);
        }
    }

    app.service("systemRelationService", SystemRelationService);
}