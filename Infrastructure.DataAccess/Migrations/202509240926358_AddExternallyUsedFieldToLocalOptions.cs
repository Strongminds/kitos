namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExternallyUsedFieldToLocalOptions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LocalAgreementElementTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalAgreementElementTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalArchiveLocations", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalArchiveLocations", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalArchiveTestLocations", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalArchiveTestLocations", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalArchiveTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalArchiveTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalBusinessTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalBusinessTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalCriticalityTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalCriticalityTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataProcessingBasisForTransferOptions", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataProcessingBasisForTransferOptions", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataProcessingCountryOptions", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataProcessingCountryOptions", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataProcessingDataResponsibleOptions", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataProcessingDataResponsibleOptions", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataProcessingOversightOptions", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataProcessingOversightOptions", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataProcessingRegistrationRoles", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataProcessingRegistrationRoles", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalDataTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalDataTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalInterfaceTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalInterfaceTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalItContractRoles", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalItContractRoles", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalItContractTemplateTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalItContractTemplateTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalItContractTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalItContractTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalItSystemCategories", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalItSystemCategories", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalItSystemRoles", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalItSystemRoles", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalOptionExtendTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalOptionExtendTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalOrganizationUnitRoles", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalOrganizationUnitRoles", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalPaymentFreqencyTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalPaymentFreqencyTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalPaymentModelTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalPaymentModelTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalPriceRegulationTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalPriceRegulationTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalProcurementStrategyTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalProcurementStrategyTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalPurchaseFormTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalPurchaseFormTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalRegisterTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalRegisterTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalRelationFrequencyTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalRelationFrequencyTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalSensitiveDataTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalSensitiveDataTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalSensitivePersonalDataTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalSensitivePersonalDataTypes", "ExternallyUsedDescription", c => c.String());
            AddColumn("dbo.LocalTerminationDeadlineTypes", "IsExternallyUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.LocalTerminationDeadlineTypes", "ExternallyUsedDescription", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LocalTerminationDeadlineTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalTerminationDeadlineTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalSensitivePersonalDataTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalSensitivePersonalDataTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalSensitiveDataTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalSensitiveDataTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalRelationFrequencyTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalRelationFrequencyTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalRegisterTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalRegisterTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalPurchaseFormTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalPurchaseFormTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalProcurementStrategyTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalProcurementStrategyTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalPriceRegulationTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalPriceRegulationTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalPaymentModelTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalPaymentModelTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalPaymentFreqencyTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalPaymentFreqencyTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalOrganizationUnitRoles", "ExternallyUsedDescription");
            DropColumn("dbo.LocalOrganizationUnitRoles", "IsExternallyUsed");
            DropColumn("dbo.LocalOptionExtendTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalOptionExtendTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalItSystemRoles", "ExternallyUsedDescription");
            DropColumn("dbo.LocalItSystemRoles", "IsExternallyUsed");
            DropColumn("dbo.LocalItSystemCategories", "ExternallyUsedDescription");
            DropColumn("dbo.LocalItSystemCategories", "IsExternallyUsed");
            DropColumn("dbo.LocalItContractTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalItContractTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalItContractTemplateTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalItContractTemplateTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalItContractRoles", "ExternallyUsedDescription");
            DropColumn("dbo.LocalItContractRoles", "IsExternallyUsed");
            DropColumn("dbo.LocalInterfaceTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalInterfaceTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalDataTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalDataProcessingRegistrationRoles", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataProcessingRegistrationRoles", "IsExternallyUsed");
            DropColumn("dbo.LocalDataProcessingOversightOptions", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataProcessingOversightOptions", "IsExternallyUsed");
            DropColumn("dbo.LocalDataProcessingDataResponsibleOptions", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataProcessingDataResponsibleOptions", "IsExternallyUsed");
            DropColumn("dbo.LocalDataProcessingCountryOptions", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataProcessingCountryOptions", "IsExternallyUsed");
            DropColumn("dbo.LocalDataProcessingBasisForTransferOptions", "ExternallyUsedDescription");
            DropColumn("dbo.LocalDataProcessingBasisForTransferOptions", "IsExternallyUsed");
            DropColumn("dbo.LocalCriticalityTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalCriticalityTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalBusinessTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalBusinessTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalArchiveTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalArchiveTypes", "IsExternallyUsed");
            DropColumn("dbo.LocalArchiveTestLocations", "ExternallyUsedDescription");
            DropColumn("dbo.LocalArchiveTestLocations", "IsExternallyUsed");
            DropColumn("dbo.LocalArchiveLocations", "ExternallyUsedDescription");
            DropColumn("dbo.LocalArchiveLocations", "IsExternallyUsed");
            DropColumn("dbo.LocalAgreementElementTypes", "ExternallyUsedDescription");
            DropColumn("dbo.LocalAgreementElementTypes", "IsExternallyUsed");
        }
    }
}
