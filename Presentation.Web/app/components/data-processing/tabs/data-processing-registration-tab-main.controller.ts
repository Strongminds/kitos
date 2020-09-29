﻿module Kitos.DataProcessing.Registration.Edit.Main {
    "use strict";

    export class EditMainDataProcessingRegistrationController {
        static $inject: Array<string> = [
            "dataProcessingRegistrationService",
            "hasWriteAccess",
            "dataProcessingRegistration",
            "apiUseCaseFactory",
            "select2LoadingService",
            "notify",
            "thirdCountryOptions"
        ];

        private readonly dataProcessingRegistrationId: number;
        constructor(
            private readonly dataProcessingRegistrationService: Services.DataProcessing.IDataProcessingRegistrationService,
            public hasWriteAccess,
            private readonly dataProcessingRegistration: Models.DataProcessing.IDataProcessingRegistrationDTO,
            private readonly apiUseCaseFactory: Services.Generic.IApiUseCaseFactory,
            private readonly select2LoadingService: Services.ISelect2LoadingService,
            private readonly notify,
            private readonly thirdCountryOptions: Models.IOptionEntity[]) {
            this.bindDataProcessors();
            this.bindSubDataProcessors();
            this.bindHasSubDataProcessors();
            this.dataProcessingRegistrationId = this.dataProcessingRegistration.id;
            this.bindIsAgreementConcluded();
            this.bindAgreementConcludedAt();
            this.bindTransferToInsecureThirdCountries();
        }

        headerName = this.dataProcessingRegistration.name;

        insecureThirdCountries: Models.ViewModel.Generic.IMultipleSelectionWithSelect2ConfigViewModel<Models.Generic.NamedEntity.NamedEntityWithExpirationStatusDTO>;

        enableSelectionOfInsecureThirdCountries: boolean;

        transferToInsecureThirdCountries: Models.ViewModel.Generic.ISingleSelectionWithFixedOptionsViewModel<Models.Api.Shared.YesNoUndecidedOption>;

        dataProcessors: Models.ViewModel.Generic.IMultipleSelectionWithSelect2ConfigViewModel<Models.DataProcessing.IDataProcessorDTO>;

        subDataProcessors: Models.ViewModel.Generic.IMultipleSelectionWithSelect2ConfigViewModel<Models.DataProcessing.IDataProcessorDTO>;

        isAgreementConcluded: Models.ViewModel.Generic.ISingleSelectionWithFixedOptionsViewModel<Models.Api.Shared.YesNoIrrelevantOption>;

        agreementConcludedAt: Models.ViewModel.Generic.IDateSelectionViewModel;

        shouldShowAgreementConcludedAt: boolean;

        enableDataProcessorSelection: boolean;

        hasSubDataProcessors: Models.ViewModel.Generic.ISingleSelectionWithFixedOptionsViewModel<Models.Api.Shared.YesNoUndecidedOption>;

        private bindTransferToInsecureThirdCountries() {
            const options = new Models.ViewModel.Shared.YesNoUndecidedOptions();
            this.transferToInsecureThirdCountries = {

                selectedElement: options.getById(this.dataProcessingRegistration.transferToInsecureThirdCountries),
                select2Config: this.select2LoadingService.select2LocalDataNoSearch(() => options.options, false),
                elementSelected: (newElement) => {
                    if (!!newElement) {
                        this.changeTransferToInsecureThirdCountries(newElement.optionalObjectContext);
                    }
                }
            };
            this.enableSelectionOfInsecureThirdCountries = this.dataProcessingRegistration.transferToInsecureThirdCountries === Models.Api.Shared.YesNoUndecidedOption.Yes;

            this.bindMultiSelectConfiguration<Models.Generic.NamedEntity.NamedEntityWithExpirationStatusDTO>(
                config => this.insecureThirdCountries = config,
                () => this.dataProcessingRegistration.insecureThirdCountries,
                element => this.removeInsecureThirdCountry(element.id),
                newElement => this.addInsecureThirdCountry(newElement),
                null,
                () => {
                    const selectedCountries = this
                        .dataProcessingRegistration
                        .insecureThirdCountries
                        .reduce((acc, next, _) => {
                            acc[next.id] = next;
                            return acc;
                        },
                            {});
                    return this.thirdCountryOptions.filter(x => !selectedCountries[x.Id]).map(x => {
                        return {
                            text: x.Name,
                            id: x.Id,
                            optionalObjectContext: {
                                id: x.Id,
                                name: x.Name,
                                expired: false //We only allow selection of non-expired and this object is based on the available objects
                            }
                        };
                    });
                }
            );
        }

        private bindHasSubDataProcessors() {
            const options = new Models.ViewModel.Shared.YesNoUndecidedOptions();
            this.hasSubDataProcessors = {
                selectedElement: options.getById(this.dataProcessingRegistration.hasSubDataProcessors),
                select2Config: this.select2LoadingService.select2LocalDataNoSearch(() => options.options, false),
                elementSelected: (newElement) => {
                    if (!!newElement) {
                        this.changeHasSubDataProcessor(newElement.optionalObjectContext);
                    }
                }
            };
            this.enableDataProcessorSelection = this.dataProcessingRegistration.hasSubDataProcessors === Models.Api.Shared.YesNoUndecidedOption.Yes;
        }

        private bindMultiSelectConfiguration<TElement>(
            setField: ((finalVm: Models.ViewModel.Generic.IMultipleSelectionWithSelect2ConfigViewModel<TElement>) => void),
            getInitialElements: () => TElement[],
            removeFunc: ((element: TElement) => void),
            newFunc: Models.ViewModel.Generic.ElementSelectedFunc<Models.ViewModel.Generic.Select2OptionViewModel<TElement>>,
            searchFunc?: (query: string) => angular.IPromise<Models.ViewModel.Generic.Select2OptionViewModel<TElement>[]>,
            fixedValueRange?: () => Models.ViewModel.Generic.Select2OptionViewModel<TElement>[]) {

            let select2Config;
            if (!!searchFunc) {
                select2Config = this.select2LoadingService.loadSelect2WithDataSource(searchFunc, false);
            } else if (!!fixedValueRange) {
                select2Config = this.select2LoadingService.select2LocalDataNoSearch(() => fixedValueRange(), false);
            } else {
                throw new Error("Either searchFunc or fixedValueRange must be provided");
            }

            const configuration = {
                selectedElements: getInitialElements(),
                removeItemRequested: removeFunc,
                allowAddition: this.hasWriteAccess,
                allowRemoval: this.hasWriteAccess,
                newItemSelectionConfig: {
                    selectedElement: null,
                    select2Config: select2Config,
                    elementSelected: newFunc
                }
            };
            setField(configuration);
        }

        private mapDataProcessingSearchResults(dataProcessors: Models.DataProcessing.IDataProcessorDTO[]) {
            return dataProcessors.map(
                dataProcessor => <Models.ViewModel.Generic.Select2OptionViewModel<Models.DataProcessing.IDataProcessorDTO>>{
                    id: dataProcessor.id,
                    text: dataProcessor.name,
                    optionalObjectContext: dataProcessor
                }
            );
        }

        private bindDataProcessors() {
            this.bindMultiSelectConfiguration<Models.DataProcessing.IDataProcessorDTO>(
                config => this.dataProcessors = config,
                () => this.dataProcessingRegistration.dataProcessors,
                element => this.removeDataProcessor(element.id),
                newElement => this.addDataProcessor(newElement),
                (query) => this
                    .dataProcessingRegistrationService
                    .getApplicableDataProcessors(this.dataProcessingRegistrationId, query)
                    .then(results => this.mapDataProcessingSearchResults(results))
            );
        }
        private bindSubDataProcessors() {
            this.bindMultiSelectConfiguration<Models.DataProcessing.IDataProcessorDTO>(
                config => this.subDataProcessors = config,
                () => this.dataProcessingRegistration.subDataProcessors,
                element => this.removeSubDataProcessor(element.id),
                newElement => this.addSubDataProcessor(newElement),
                (query) => this
                    .dataProcessingRegistrationService
                    .getApplicableSubDataProcessors(this.dataProcessingRegistrationId, query)
                    .then(results => this.mapDataProcessingSearchResults(results))
            );
        }

        changeName(name) {
            this.apiUseCaseFactory
                .createUpdate("Navn", () => this.dataProcessingRegistrationService.rename(this.dataProcessingRegistrationId, name))
                .executeAsync(nameChangeResponse => {
                    this.headerName = name;
                    return nameChangeResponse;
                });
        }

        private addDataProcessor(newElement: Models.ViewModel.Generic.Select2OptionViewModel<Models.DataProcessing.IDataProcessorDTO>) {
            if (!!newElement && !!newElement.optionalObjectContext) {
                const newDp = newElement.optionalObjectContext;
                this.apiUseCaseFactory
                    .createAssignmentCreation(() => this.dataProcessingRegistrationService.assignDataProcessor(this.dataProcessingRegistrationId, newDp.id))
                    .executeAsync(success => {
                        //Update the source collection
                        this.dataProcessingRegistration.dataProcessors.push(newDp);

                        //Trigger UI update
                        this.bindDataProcessors();
                        return success;
                    });
            }
        }

        private removeDataProcessor(id: number) {
            this.apiUseCaseFactory
                .createAssignmentRemoval(() => this.dataProcessingRegistrationService.removeDataProcessor(this.dataProcessingRegistrationId, id))
                .executeAsync(success => {

                    //Update the source collection
                    this.dataProcessingRegistration.dataProcessors = this.dataProcessingRegistration.dataProcessors.filter(x => x.id !== id);

                    //Propagate changes to UI binding
                    this.bindDataProcessors();
                    return success;
                });
        }

        private addSubDataProcessor(newElement: Models.ViewModel.Generic.Select2OptionViewModel<Models.DataProcessing.IDataProcessorDTO>) {
            if (!!newElement && !!newElement.optionalObjectContext) {
                const newDp = newElement.optionalObjectContext as Models.DataProcessing.IDataProcessorDTO;
                this.apiUseCaseFactory
                    .createAssignmentCreation(() => this.dataProcessingRegistrationService.assignSubDataProcessor(this.dataProcessingRegistrationId, newDp.id))
                    .executeAsync(success => {
                        //Update the source collection
                        this.dataProcessingRegistration.subDataProcessors.push(newDp);

                        //Trigger UI update
                        this.bindSubDataProcessors();
                        return success;
                    });
            }
        }

        private removeInsecureThirdCountry(id: number) {
            this.apiUseCaseFactory
                .createAssignmentRemoval(() => this.dataProcessingRegistrationService.removeInsecureThirdCountry(this.dataProcessingRegistrationId, id))
                .executeAsync(success => {

                    //Update the source collection
                    this.dataProcessingRegistration.insecureThirdCountries = this.dataProcessingRegistration.insecureThirdCountries.filter(x => x.id !== id);

                    //Propagate changes to UI binding
                    this.bindTransferToInsecureThirdCountries();
                    return success;
                });
        }

        private addInsecureThirdCountry(newElement: Models.ViewModel.Generic.Select2OptionViewModel<Models.Generic.NamedEntity.NamedEntityWithExpirationStatusDTO>) {
            if (!!newElement && !!newElement.optionalObjectContext) {
                const country = newElement.optionalObjectContext as Models.Generic.NamedEntity.NamedEntityWithExpirationStatusDTO;
                this.apiUseCaseFactory
                    .createAssignmentCreation(() => this.dataProcessingRegistrationService.assignInsecureThirdCountry(this.dataProcessingRegistrationId, country.id))
                    .executeAsync(success => {
                        //Update the source collection
                        this.dataProcessingRegistration.insecureThirdCountries.push(country);

                        //Trigger UI update
                        this.bindTransferToInsecureThirdCountries();
                        return success;
                    });
            }
        }

        private removeSubDataProcessor(id: number) {
            this.apiUseCaseFactory
                .createAssignmentRemoval(() => this.dataProcessingRegistrationService.removeSubDataProcessor(this.dataProcessingRegistrationId, id))
                .executeAsync(success => {

                    //Update the source collection
                    this.dataProcessingRegistration.subDataProcessors = this.dataProcessingRegistration.subDataProcessors.filter(x => x.id !== id);

                    //Propagate changes to UI binding
                    this.bindSubDataProcessors();
                    return success;
                });
        }

        private changeIsAgreementConcluded(isAgreementConcluded: Models.ViewModel.Generic.Select2OptionViewModel<Models.Api.Shared.YesNoIrrelevantOption>) {
            this.apiUseCaseFactory
                .createUpdate("Databehandleraftale indgået", () => this.dataProcessingRegistrationService.updateIsAgreementConcluded(this.dataProcessingRegistration.id, isAgreementConcluded.optionalObjectContext))
                .executeAsync(success => {
                    this.dataProcessingRegistration.agreementConcluded.value = isAgreementConcluded.optionalObjectContext;
                    this.bindIsAgreementConcluded();
                    return success;
                });
        }

        private changeHasSubDataProcessor(value: Models.Api.Shared.YesNoUndecidedOption) {
            this
                .apiUseCaseFactory
                .createUpdate("Underdatabehandlere", () => this.dataProcessingRegistrationService.updateSubDataProcessorsState(this.dataProcessingRegistrationId, value))
                .executeAsync(success => {
                    this.dataProcessingRegistration.hasSubDataProcessors = value;
                    this.bindHasSubDataProcessors();
                    return success;
                });
        }

        private changeTransferToInsecureThirdCountries(value: Models.Api.Shared.YesNoUndecidedOption) {
            this
                .apiUseCaseFactory
                .createUpdate("Overførsel til usikkert 3. land", () => this.dataProcessingRegistrationService.updateTransferToInsecureThirdCountry(this.dataProcessingRegistrationId, value))
                .executeAsync(success => {
                    this.dataProcessingRegistration.transferToInsecureThirdCountries = value;
                    this.bindTransferToInsecureThirdCountries();
                    return success;
                });
        }

        private changeAgreementConcludedAt(agreementConcludedAt: string) {
            var formattedDate = Helpers.DateStringFormat.fromDDMMYYYYToYYYYMMDD(agreementConcludedAt);
            if (!!formattedDate.convertedValue) {
                return this.apiUseCaseFactory
                    .createUpdate("Dato for databehandleraftale indgået", () => this.dataProcessingRegistrationService.updateAgreementConcludedAt(this.dataProcessingRegistration.id, formattedDate.convertedValue))
                    .executeAsync(success => {
                        this.dataProcessingRegistration.agreementConcluded.optionalDateValue = agreementConcludedAt;
                        this.bindAgreementConcludedAt();
                        return success;
                    });
            }
            return this.notify.addErrorMessage(formattedDate.errorMessage);
        }

        private bindIsAgreementConcluded() {
            this.isAgreementConcluded = {
                selectedElement: this.getYesNoIrrelevantOptionFromId(this.dataProcessingRegistration.agreementConcluded.value),
                select2Config: this.select2LoadingService.select2LocalDataNoSearch(() => new Models.ViewModel.Shared.YesNoIrrelevantOptions().options, false),
                elementSelected: (newElement) => this.changeIsAgreementConcluded(newElement)
            };

            this.shouldShowAgreementConcludedAt =
                this.isAgreementConcluded.selectedElement !== null &&
                this.isAgreementConcluded.selectedElement.optionalObjectContext === Models.Api.Shared.YesNoIrrelevantOption.YES;
        }

        private getYesNoIrrelevantOptionFromId(id?: number): Models.ViewModel.Generic.Select2OptionViewModel<Models.Api.Shared.YesNoIrrelevantOption> {
            if (id === null) {
                return null;
            }
            return new Models.ViewModel.Shared.YesNoIrrelevantOptions().getById(id);
        }

        private bindAgreementConcludedAt() {
            this.agreementConcludedAt = new Models.ViewModel.Generic.DateSelectionViewModel(
                this.dataProcessingRegistration.agreementConcluded.optionalDateValue,
                (newDate) => this.changeAgreementConcludedAt(newDate));
        }

    }

    angular
        .module("app")
        .config(["$stateProvider", ($stateProvider: ng.ui.IStateProvider) => {
            $stateProvider.state("data-processing.edit-registration.main", {
                url: "/main",
                templateUrl: "app/components/data-processing/tabs/data-processing-registration-tab-main.view.html",
                controller: EditMainDataProcessingRegistrationController,
                controllerAs: "vm",
                resolve: {
                    thirdCountryOptions: ["localOptionServiceFactory", (localOptionServiceFactory: Services.LocalOptions.ILocalOptionServiceFactory) => localOptionServiceFactory.create(Services.LocalOptions.LocalOptionType.DataProcessingCountryOptions).getAll()
                    ],
                }
            });
        }]);
}
