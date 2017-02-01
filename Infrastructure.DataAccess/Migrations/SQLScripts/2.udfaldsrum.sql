/*Udfaldsrum*/
/*Org Roles*/
UPDATE [dbo].[OrganizationUnitRoles]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Projekt*/
/*Roller*/
UPDATE [dbo].[ItProjectRoles]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Typer*/
UPDATE [dbo].[ItProjectTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*M�lTyper*/
UPDATE [dbo].[GoalTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*System*/
/*Roller*/
UPDATE [dbo].[ItSystemRoles]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Applikationstyper*/
UPDATE [dbo].[ItSystemTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Forretningstyper*/
UPDATE [dbo].[BusinessTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Arkiveringstyper*/
UPDATE [dbo].[ArchiveTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Arkiveringssted*/
UPDATE [dbo].[ArchiveLocations]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Datatyper*/
UPDATE [dbo].[DataTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Frekvenser*/
UPDATE [dbo].[FrequencyTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Snitfladetyper*/
UPDATE [dbo].[ItInterfaceTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Gr�nseflader*/
UPDATE [dbo].[InterfaceTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Metoder*/
UPDATE [dbo].[MethodTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Personf�lsomme data*/
UPDATE [dbo].[SensitiveDataTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*TSA*/
UPDATE [dbo].[TsaTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Kontrakter*/
/*Roller*/
UPDATE [dbo].[ItContractRoles]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Kontrakttyper*/
UPDATE [dbo].[ItContractTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Kontraktskabeloner*/
UPDATE [dbo].[ItContractTemplateTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Indk�bsformer*/
UPDATE [dbo].[PurchaseFormTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Betalingsmodeller*/
UPDATE [dbo].[PaymentModelTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Aftaleelementer*/
UPDATE [dbo].[AgreementElementTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Option forl�ng*/
UPDATE [dbo].[OptionExtendTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Betalingsfrekvenser*/
UPDATE [dbo].[PaymentFreqencyTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Pris reguleringer*/
UPDATE [dbo].[PriceRegulationTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Indk�bsstrategier*/
UPDATE [dbo].[ProcurementStrategyTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Opsigelsesfrister*/
UPDATE [dbo].[TerminationDeadlineTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Overtagelsespr�ver*/
UPDATE [dbo].[HandoverTrialTypes]
SET [IsEnabled] = 1, [IsLocallyAvailable] = 1

/*Oprydning af udfaldsrum*/
UPDATE [kitos_old].[dbo].[ItContract]
SET [ContractTemplateId] = 1
WHERE [ContractTemplateId] = 11


