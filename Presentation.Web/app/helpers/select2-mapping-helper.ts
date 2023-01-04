﻿module Kitos.Helpers {
    export class Select2MappingHelper {
        static mapNamedEntityWithDescriptionAndExpirationStatusDtoArrayToOptionMap(dtos: Models.Generic.NamedEntity.
            NamedEntityWithDescriptionAndExpirationStatusDTO[]): Models.ViewModel.Generic.Select2OptionViewModel<Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO>[] {
            return dtos.reduce((acc, next, _) => {
                acc[next.id] = {
                    id: next.id,
                    text: next.name,
                    optionalObjectContext: {
                        id: next.id,
                        name: next.name,
                        expired: false, //We only allow selection of non-expired and this object is based on the available objects
                        description: next.description
                    }
                };
                return acc;
            }, {}) as Models.ViewModel.Generic.Select2OptionViewModel<Models.Generic.NamedEntity.NamedEntityWithDescriptionAndExpirationStatusDTO>[];
        }

        static mapDataProcessingSearchResults(dataProcessors: Models.DataProcessing.IDataProcessorDTO[]) {
            return dataProcessors.map(
                dataProcessor => <Models.ViewModel.Generic.
                Select2OptionViewModel<Models.DataProcessing.IDataProcessorDTO>>{
                    id: dataProcessor.id,
                    text: dataProcessor.name,
                    optionalObjectContext: dataProcessor,
                    cvrNumber: dataProcessor.cvrNumber
                }
            );
        }
    }
}