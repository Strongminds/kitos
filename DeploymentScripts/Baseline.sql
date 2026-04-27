IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [BrokenExternalReferencesReports] (
    [Id] int NOT NULL IDENTITY,
    [Created] datetime2 NOT NULL,
    CONSTRAINT [PK_BrokenExternalReferencesReports] PRIMARY KEY ([Id])
);

CREATE TABLE [OrganizationTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Category] int NOT NULL,
    CONSTRAINT [PK_OrganizationTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [PendingReadModelUpdates] (
    [Id] int NOT NULL IDENTITY,
    [SourceId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Category] int NOT NULL,
    CONSTRAINT [PK_PendingReadModelUpdates] PRIMARY KEY ([Id])
);

CREATE TABLE [User] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Email] nvarchar(100) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Salt] nvarchar(max) NOT NULL,
    [LastAdvisDate] datetime2 NULL,
    [DeletedDate] datetime2 NULL,
    [Deleted] bit NOT NULL,
    [DefaultUserStartPreference] nvarchar(max) NULL,
    [HasApiAccess] bit NULL,
    [HasStakeHolderAccess] bit NOT NULL,
    [LockedOutDate] datetime2 NULL,
    [FailedAttempts] int NOT NULL,
    [IsGlobalAdmin] bit NOT NULL,
    [IsSystemIntegrator] bit NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_User_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_User_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Advice] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [RelationId] int NULL,
    [Type] int NOT NULL,
    [Scheduling] int NULL,
    [IsActive] bit NOT NULL,
    [Name] nvarchar(max) NULL,
    [AlarmDate] datetime2 NULL,
    [StopDate] datetime2 NULL,
    [SentDate] datetime2 NULL,
    [Body] nvarchar(max) NULL,
    [Subject] nvarchar(max) NULL,
    [JobId] nvarchar(max) NULL,
    [AdviceType] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_Advice] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Advice_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Advice_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AgreementElementTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AgreementElementTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AgreementElementTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AgreementElementTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ArchiveLocations] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArchiveLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ArchiveLocations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ArchiveLocations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ArchiveTestLocations] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Name] nvarchar(max) NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArchiveTestLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ArchiveTestLocations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_ArchiveTestLocations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [ArchiveTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArchiveTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ArchiveTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ArchiveTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AttachedOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectId] int NOT NULL,
    [ObjectType] int NOT NULL,
    [OptionId] int NOT NULL,
    [OptionType] int NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_AttachedOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttachedOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttachedOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BusinessTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_BusinessTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BusinessTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BusinessTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ContactPersons] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [LastName] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [OrganizationId] int NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_ContactPersons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContactPersons_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_ContactPersons_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [CountryCodes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CountryCodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CountryCodes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CountryCodes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CriticalityTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CriticalityTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CriticalityTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CriticalityTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingBasisForTransferOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataProcessingBasisForTransferOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingBasisForTransferOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingBasisForTransferOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingCountryOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataProcessingCountryOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingCountryOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingCountryOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingDataResponsibleOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataProcessingDataResponsibleOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingDataResponsibleOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingDataResponsibleOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingOversightOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataProcessingOversightOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingOversightOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingOversightOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingRegistrationRoles] (
    [Id] int NOT NULL IDENTITY,
    [HasReadAccess] bit NOT NULL,
    [HasWriteAccess] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrationRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingRegistrationRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_DataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HelpTexts] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NULL,
    [Key] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_HelpTexts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HelpTexts_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_HelpTexts_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [InterfaceTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_InterfaceTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InterfaceTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InterfaceTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractRoles] (
    [Id] int NOT NULL IDENTITY,
    [HasReadAccess] bit NOT NULL,
    [HasWriteAccess] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ItContractRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractTemplateTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ItContractTemplateTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractTemplateTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractTemplateTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ItContractTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemCategories] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ItSystemCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemCategories_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemCategories_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemRoles] (
    [Id] int NOT NULL IDENTITY,
    [HasReadAccess] bit NOT NULL,
    [HasWriteAccess] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ItSystemRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [KLEUpdateHistoryItems] (
    [Id] int NOT NULL IDENTITY,
    [Version] datetime2 NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_KLEUpdateHistoryItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KLEUpdateHistoryItems_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_KLEUpdateHistoryItems_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [OptionExtendTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_OptionExtendTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OptionExtendTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OptionExtendTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganizationUnitRoles] (
    [Id] int NOT NULL IDENTITY,
    [HasReadAccess] bit NOT NULL,
    [HasWriteAccess] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_OrganizationUnitRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationUnitRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnitRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PasswordResetRequest] (
    [Id] int NOT NULL IDENTITY,
    [Hash] nvarchar(max) NULL,
    [Time] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_PasswordResetRequest] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetRequest_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PasswordResetRequest_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PasswordResetRequest_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PaymentFreqencyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_PaymentFreqencyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentFreqencyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PaymentFreqencyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PaymentModelTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_PaymentModelTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentModelTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PaymentModelTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PriceRegulationTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_PriceRegulationTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PriceRegulationTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PriceRegulationTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProcurementStrategyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ProcurementStrategyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProcurementStrategyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProcurementStrategyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PublicMessages] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [Title] nvarchar(50) NULL,
    [LongDescription] nvarchar(max) NULL,
    [Status] int NULL,
    [ShortDescription] nvarchar(200) NULL,
    [Link] nvarchar(max) NULL,
    [IconType] int NULL,
    [IsMain] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_PublicMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PublicMessages_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicMessages_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PurchaseFormTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_PurchaseFormTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseFormTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseFormTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RegisterTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Name] nvarchar(max) NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RegisterTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RegisterTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_RegisterTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [RelationFrequencyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RelationFrequencyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RelationFrequencyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RelationFrequencyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SensitiveDataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_SensitiveDataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SensitiveDataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SensitiveDataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SensitivePersonalDataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Name] nvarchar(max) NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_SensitivePersonalDataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SensitivePersonalDataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_SensitivePersonalDataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [SsoUserIdentities] (
    [Id] int NOT NULL IDENTITY,
    [ExternalUuid] uniqueidentifier NOT NULL,
    [User_Id] int NOT NULL,
    CONSTRAINT [PK_SsoUserIdentities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SsoUserIdentities_User_User_Id] FOREIGN KEY ([User_Id]) REFERENCES [User] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TerminationDeadlineTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [IsLocallyAvailable] bit NOT NULL,
    [IsObligatory] bit NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [Priority] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_TerminationDeadlineTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TerminationDeadlineTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TerminationDeadlineTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Text] (
    [Id] int NOT NULL IDENTITY,
    [Value] nvarchar(max) NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_Text] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Text_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Text_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdviceSents] (
    [Id] int NOT NULL IDENTITY,
    [AdviceSentDate] datetime2 NOT NULL,
    [AdviceId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_AdviceSents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdviceSents_Advice_AdviceId] FOREIGN KEY ([AdviceId]) REFERENCES [Advice] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AdviceSents_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdviceSents_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Organization] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NULL,
    [Phone] nvarchar(max) NULL,
    [Adress] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [TypeId] int NOT NULL,
    [Cvr] nvarchar(10) NULL,
    [ForeignCvr] nvarchar(max) NULL,
    [ForeignCountryCodeId] int NULL,
    [AccessModifier] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [Disabled] bit NOT NULL,
    [ContactPersonId] int NULL,
    [ContactPerson_Id] int NULL,
    [IsDefaultOrganization] bit NULL,
    [IsSupplier] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Organization_ContactPersons_ContactPerson_Id] FOREIGN KEY ([ContactPerson_Id]) REFERENCES [ContactPersons] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Organization_CountryCodes_ForeignCountryCodeId] FOREIGN KEY ([ForeignCountryCodeId]) REFERENCES [CountryCodes] ([Id]),
    CONSTRAINT [FK_Organization_OrganizationTypes_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [OrganizationTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Organization_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Organization_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdviceUserRelations] (
    [Id] int NOT NULL IDENTITY,
    [AdviceId] int NULL,
    [ItContractRoleId] int NULL,
    [ItSystemRoleId] int NULL,
    [DataProcessingRegistrationRoleId] int NULL,
    [Email] nvarchar(max) NULL,
    [RecieverType] int NOT NULL,
    [RecpientType] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_AdviceUserRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdviceUserRelations_Advice_AdviceId] FOREIGN KEY ([AdviceId]) REFERENCES [Advice] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AdviceUserRelations_DataProcessingRegistrationRoles_DataProcessingRegistrationRoleId] FOREIGN KEY ([DataProcessingRegistrationRoleId]) REFERENCES [DataProcessingRegistrationRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdviceUserRelations_ItContractRoles_ItContractRoleId] FOREIGN KEY ([ItContractRoleId]) REFERENCES [ItContractRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdviceUserRelations_ItSystemRoles_ItSystemRoleId] FOREIGN KEY ([ItSystemRoleId]) REFERENCES [ItSystemRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdviceUserRelations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdviceUserRelations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Config] (
    [Id] int NOT NULL,
    [ShowItSystemModule] bit NOT NULL,
    [ShowItContractModule] bit NOT NULL,
    [ShowDataProcessing] bit NOT NULL,
    [ShowItSystemPrefix] bit NOT NULL,
    [ShowItContractPrefix] bit NOT NULL,
    [ItSupportModuleNameId] int NOT NULL,
    [ItSupportGuide] nvarchar(max) NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_Config] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Config_Organization_Id] FOREIGN KEY ([Id]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Config_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Config_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProtectionAdvisors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Cvr] nvarchar(10) NULL,
    [Phone] nvarchar(max) NULL,
    [Adress] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [OrganizationId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_DataProtectionAdvisors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProtectionAdvisors_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DataProtectionAdvisors_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProtectionAdvisors_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataResponsibles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Cvr] nvarchar(10) NULL,
    [Phone] nvarchar(max) NULL,
    [Adress] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [OrganizationId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_DataResponsibles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataResponsibles_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DataResponsibles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataResponsibles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItInterface] (
    [Id] int NOT NULL IDENTITY,
    [Url] nvarchar(max) NULL,
    [Version] nvarchar(20) NULL,
    [ItInterfaceId] nvarchar(100) NOT NULL,
    [InterfaceId] int NULL,
    [Note] nvarchar(max) NULL,
    [Disabled] bit NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [Description] nvarchar(max) NULL,
    [AccessModifier] int NOT NULL,
    [OrganizationId] int NOT NULL,
    [Created] datetime2 NULL,
    CONSTRAINT [PK_ItInterface] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItInterface_InterfaceTypes_InterfaceId] FOREIGN KEY ([InterfaceId]) REFERENCES [InterfaceTypes] ([Id]),
    CONSTRAINT [FK_ItInterface_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItInterface_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItInterface_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [KendoOrganizationalConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [OverviewType] int NOT NULL,
    [Version] nvarchar(max) NOT NULL,
    [OrganizationId] int NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_KendoOrganizationalConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KendoOrganizationalConfigurations_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_KendoOrganizationalConfigurations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_KendoOrganizationalConfigurations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LifeCycleTrackingEvents] (
    [Id] int NOT NULL IDENTITY,
    [EventType] int NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [EntityUuid] uniqueidentifier NOT NULL,
    [EntityType] int NOT NULL,
    [OptionalOrganizationReferenceId] int NULL,
    [OptionalAccessModifier] int NULL,
    [OptionalRightsHolderOrganizationId] int NULL,
    [UserId] int NULL,
    CONSTRAINT [PK_LifeCycleTrackingEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LifeCycleTrackingEvents_Organization_OptionalOrganizationReferenceId] FOREIGN KEY ([OptionalOrganizationReferenceId]) REFERENCES [Organization] ([Id]),
    CONSTRAINT [FK_LifeCycleTrackingEvents_Organization_OptionalRightsHolderOrganizationId] FOREIGN KEY ([OptionalRightsHolderOrganizationId]) REFERENCES [Organization] ([Id]),
    CONSTRAINT [FK_LifeCycleTrackingEvents_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalAgreementElementTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalAgreementElementTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalAgreementElementTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalAgreementElementTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalAgreementElementTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalArchiveLocations] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalArchiveLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalArchiveLocations_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalArchiveLocations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalArchiveLocations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalArchiveTestLocations] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalArchiveTestLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalArchiveTestLocations_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalArchiveTestLocations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalArchiveTestLocations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalArchiveTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalArchiveTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalArchiveTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalArchiveTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalArchiveTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalBusinessTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalBusinessTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalBusinessTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalBusinessTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalBusinessTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalCriticalityTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalCriticalityTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalCriticalityTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalCriticalityTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalCriticalityTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataProcessingBasisForTransferOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalDataProcessingBasisForTransferOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataProcessingBasisForTransferOptions_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataProcessingBasisForTransferOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataProcessingBasisForTransferOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataProcessingCountryOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalDataProcessingCountryOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataProcessingCountryOptions_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataProcessingCountryOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataProcessingCountryOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataProcessingDataResponsibleOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalDataProcessingDataResponsibleOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataProcessingDataResponsibleOptions_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataProcessingDataResponsibleOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataProcessingDataResponsibleOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataProcessingOversightOptions] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalDataProcessingOversightOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataProcessingOversightOptions_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataProcessingOversightOptions_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataProcessingOversightOptions_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataProcessingRegistrationRoles] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsExternallyUsed] bit NOT NULL,
    [ExternallyUsedDescription] nvarchar(max) NULL,
    CONSTRAINT [PK_LocalDataProcessingRegistrationRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataProcessingRegistrationRoles_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataProcessingRegistrationRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataProcessingRegistrationRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalDataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalDataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalDataTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalDataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalDataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalRelationFrequencyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalRelationFrequencyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalRelationFrequencyTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalRelationFrequencyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalRelationFrequencyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalInterfaceTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalInterfaceTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalInterfaceTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalInterfaceTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalInterfaceTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalItContractRoles] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsExternallyUsed] bit NOT NULL,
    [ExternallyUsedDescription] nvarchar(max) NULL,
    CONSTRAINT [PK_LocalItContractRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalItContractRoles_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalItContractRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalItContractRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalItContractTemplateTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalItContractTemplateTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalItContractTemplateTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalItContractTemplateTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalItContractTemplateTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalItContractTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalItContractTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalItContractTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalItContractTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalItContractTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalItSystemCategories] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalItSystemCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalItSystemCategories_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalItSystemCategories_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalItSystemCategories_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalItSystemRoles] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsExternallyUsed] bit NOT NULL,
    [ExternallyUsedDescription] nvarchar(max) NULL,
    CONSTRAINT [PK_LocalItSystemRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalItSystemRoles_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalItSystemRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalItSystemRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalOptionExtendTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalOptionExtendTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalOptionExtendTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalOptionExtendTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalOptionExtendTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalOrganizationUnitRoles] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsExternallyUsed] bit NOT NULL,
    [ExternallyUsedDescription] nvarchar(max) NULL,
    CONSTRAINT [PK_LocalOrganizationUnitRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalOrganizationUnitRoles_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalOrganizationUnitRoles_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalOrganizationUnitRoles_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalPaymentFreqencyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalPaymentFreqencyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalPaymentFreqencyTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalPaymentFreqencyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalPaymentFreqencyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalPaymentModelTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalPaymentModelTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalPaymentModelTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalPaymentModelTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalPaymentModelTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalPriceRegulationTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalPriceRegulationTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalPriceRegulationTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalPriceRegulationTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalPriceRegulationTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalProcurementStrategyTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalProcurementStrategyTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalProcurementStrategyTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalProcurementStrategyTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalProcurementStrategyTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalPurchaseFormTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalPurchaseFormTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalPurchaseFormTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalPurchaseFormTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalPurchaseFormTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalRegisterTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalRegisterTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalRegisterTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalRegisterTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalRegisterTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalSensitiveDataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalSensitiveDataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalSensitiveDataTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalSensitiveDataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalSensitiveDataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalSensitivePersonalDataTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalSensitivePersonalDataTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalSensitivePersonalDataTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalSensitivePersonalDataTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalSensitivePersonalDataTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [LocalTerminationDeadlineTypes] (
    [Id] int NOT NULL IDENTITY,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    [Description] nvarchar(max) NULL,
    [OrganizationId] int NOT NULL,
    [OptionId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LocalTerminationDeadlineTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocalTerminationDeadlineTypes_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LocalTerminationDeadlineTypes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_LocalTerminationDeadlineTypes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [OrganizationSuppliers] (
    [OrganizationId] int NOT NULL,
    [SupplierId] int NOT NULL,
    CONSTRAINT [PK_OrganizationSuppliers] PRIMARY KEY ([SupplierId], [OrganizationId]),
    CONSTRAINT [FK_OrganizationSuppliers_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationSuppliers_Organization_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganizationUnit] (
    [Id] int NOT NULL IDENTITY,
    [Origin] int NOT NULL,
    [ExternalOriginUuid] uniqueidentifier NULL,
    [LocalId] nvarchar(100) NULL,
    [Name] nvarchar(max) NULL,
    [Ean] bigint NULL,
    [ParentId] int NULL,
    [OrganizationId] int NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_OrganizationUnit] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationUnit_OrganizationUnit_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [OrganizationUnit] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnit_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationUnit_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnit_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StsOrganizationIdentities] (
    [Id] int NOT NULL IDENTITY,
    [ExternalUuid] uniqueidentifier NOT NULL,
    [Organization_Id] int NOT NULL,
    CONSTRAINT [PK_StsOrganizationIdentities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StsOrganizationIdentities_Organization_Organization_Id] FOREIGN KEY ([Organization_Id]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StsOrganizationConnections] (
    [Id] int NOT NULL IDENTITY,
    [OrganizationId] int NOT NULL,
    [Connected] bit NOT NULL,
    [SynchronizationDepth] int NULL,
    [SubscribeToUpdates] bit NOT NULL,
    [DateOfLatestCheckBySubscription] datetime2 NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_StsOrganizationConnections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StsOrganizationConnections_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StsOrganizationConnections_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StsOrganizationConnections_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UIModuleCustomizations] (
    [Id] int NOT NULL IDENTITY,
    [OrganizationId] int NOT NULL,
    [Module] nvarchar(200) NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_UIModuleCustomizations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UIModuleCustomizations_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UIModuleCustomizations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_UIModuleCustomizations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [BrokenLinkInInterfaces] (
    [Id] int NOT NULL IDENTITY,
    [ValueOfCheckedUrl] nvarchar(max) NULL,
    [Cause] int NOT NULL,
    [ErrorResponseCode] int NULL,
    [ReferenceDateOfLatestLinkChange] datetime2 NOT NULL,
    [BrokenReferenceOrigin_Id] int NOT NULL,
    [ParentReport_Id] int NOT NULL,
    CONSTRAINT [PK_BrokenLinkInInterfaces] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BrokenLinkInInterfaces_BrokenExternalReferencesReports_ParentReport_Id] FOREIGN KEY ([ParentReport_Id]) REFERENCES [BrokenExternalReferencesReports] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BrokenLinkInInterfaces_ItInterface_BrokenReferenceOrigin_Id] FOREIGN KEY ([BrokenReferenceOrigin_Id]) REFERENCES [ItInterface] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DataRow] (
    [Id] int NOT NULL IDENTITY,
    [ItInterfaceId] int NOT NULL,
    [DataTypeId] int NULL,
    [Data] nvarchar(max) NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_DataRow] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataRow_DataTypes_DataTypeId] FOREIGN KEY ([DataTypeId]) REFERENCES [DataTypes] ([Id]),
    CONSTRAINT [FK_DataRow_ItInterface_ItInterfaceId] FOREIGN KEY ([ItInterfaceId]) REFERENCES [ItInterface] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataRow_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataRow_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [KendoColumnConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [PersistId] nvarchar(max) NULL,
    [Index] int NOT NULL,
    [KendoOrganizationalConfigurationId] int NOT NULL,
    CONSTRAINT [PK_KendoColumnConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KendoColumnConfigurations_KendoOrganizationalConfigurations_KendoOrganizationalConfigurationId] FOREIGN KEY ([KendoOrganizationalConfigurationId]) REFERENCES [KendoOrganizationalConfigurations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganizationRights] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Role] int NOT NULL,
    [OrganizationId] int NOT NULL,
    [DefaultOrgUnitId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_OrganizationRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationRights_OrganizationUnit_DefaultOrgUnitId] FOREIGN KEY ([DefaultOrgUnitId]) REFERENCES [OrganizationUnit] ([Id]),
    CONSTRAINT [FK_OrganizationRights_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationRights_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationRights_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationRights_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganizationUnitRights] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [ObjectId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_OrganizationUnitRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrganizationUnitRights_OrganizationUnitRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [OrganizationUnitRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnitRights_OrganizationUnit_ObjectId] FOREIGN KEY ([ObjectId]) REFERENCES [OrganizationUnit] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrganizationUnitRights_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnitRights_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganizationUnitRights_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaskRef] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [Type] nvarchar(max) NULL,
    [TaskKey] nvarchar(15) NULL,
    [Description] nvarchar(150) NULL,
    [ActiveFrom] datetime2 NULL,
    [ActiveTo] datetime2 NULL,
    [ParentId] int NULL,
    [OwnedByOrganizationUnitId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_TaskRef] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskRef_OrganizationUnit_OwnedByOrganizationUnitId] FOREIGN KEY ([OwnedByOrganizationUnitId]) REFERENCES [OrganizationUnit] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskRef_TaskRef_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [TaskRef] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskRef_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskRef_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StsOrganizationChangeLogs] (
    [Id] int NOT NULL IDENTITY,
    [StsOrganizationConnectionId] int NOT NULL,
    [ResponsibleUserId] int NULL,
    [ResponsibleType] int NOT NULL,
    [LogTime] datetime2 NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_StsOrganizationChangeLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StsOrganizationChangeLogs_StsOrganizationConnections_StsOrganizationConnectionId] FOREIGN KEY ([StsOrganizationConnectionId]) REFERENCES [StsOrganizationConnections] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StsOrganizationChangeLogs_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StsOrganizationChangeLogs_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StsOrganizationChangeLogs_User_ResponsibleUserId] FOREIGN KEY ([ResponsibleUserId]) REFERENCES [User] ([Id])
);

CREATE TABLE [CustomizedUiNodes] (
    [Id] int NOT NULL IDENTITY,
    [ModuleId] int NOT NULL,
    [Key] nvarchar(max) NOT NULL,
    [Enabled] bit NOT NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_CustomizedUiNodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomizedUiNodes_UIModuleCustomizations_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [UIModuleCustomizations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CustomizedUiNodes_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_CustomizedUiNodes_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [StsOrganizationConsequenceLogs] (
    [Id] int NOT NULL IDENTITY,
    [ChangeLogId] int NOT NULL,
    [ExternalUnitUuid] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Type] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_StsOrganizationConsequenceLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StsOrganizationConsequenceLogs_StsOrganizationChangeLogs_ChangeLogId] FOREIGN KEY ([ChangeLogId]) REFERENCES [StsOrganizationChangeLogs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StsOrganizationConsequenceLogs_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StsOrganizationConsequenceLogs_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ArchivePeriod] (
    [Id] int NOT NULL IDENTITY,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [UniqueArchiveId] nvarchar(max) NULL,
    [ItSystemUsageId] int NOT NULL,
    [Approved] bit NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_ArchivePeriod] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ArchivePeriod_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ArchivePeriod_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BrokenLinkInExternalReferences] (
    [Id] int NOT NULL IDENTITY,
    [ValueOfCheckedUrl] nvarchar(max) NULL,
    [Cause] int NOT NULL,
    [ErrorResponseCode] int NULL,
    [ReferenceDateOfLatestLinkChange] datetime2 NOT NULL,
    [BrokenReferenceOrigin_Id] int NOT NULL,
    [ParentReport_Id] int NOT NULL,
    CONSTRAINT [PK_BrokenLinkInExternalReferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BrokenLinkInExternalReferences_BrokenExternalReferencesReports_ParentReport_Id] FOREIGN KEY ([ParentReport_Id]) REFERENCES [BrokenExternalReferencesReports] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DataProcessingRegistrationDataProcessingCountryOptions] (
    [DataProcessingCountryOption_Id] int NOT NULL,
    [DataProcessingRegistration_Id] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationDataProcessingCountryOptions] PRIMARY KEY ([DataProcessingCountryOption_Id], [DataProcessingRegistration_Id]),
    CONSTRAINT [FK_DataProcessingRegistrationDataProcessingCountryOptions_DataProcessingCountryOptions_DataProcessingCountryOption_Id] FOREIGN KEY ([DataProcessingCountryOption_Id]) REFERENCES [DataProcessingCountryOptions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DataProcessingRegistrationDataProcessingOversightOptions] (
    [DataProcessingOversightOption_Id] int NOT NULL,
    [DataProcessingRegistration_Id] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationDataProcessingOversightOptions] PRIMARY KEY ([DataProcessingOversightOption_Id], [DataProcessingRegistration_Id]),
    CONSTRAINT [FK_DataProcessingRegistrationDataProcessingOversightOptions_DataProcessingOversightOptions_DataProcessingOversightOption_Id] FOREIGN KEY ([DataProcessingOversightOption_Id]) REFERENCES [DataProcessingOversightOptions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractDataProcessingRegistrations] (
    [ItContract_Id] int NOT NULL,
    [DataProcessingRegistration_Id] int NOT NULL,
    CONSTRAINT [PK_ItContractDataProcessingRegistrations] PRIMARY KEY ([ItContract_Id], [DataProcessingRegistration_Id])
);

CREATE TABLE [DataProcessingRegistrationItSystemUsages] (
    [DataProcessingRegistration_Id] int NOT NULL,
    [ItSystemUsage_Id] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationItSystemUsages] PRIMARY KEY ([DataProcessingRegistration_Id], [ItSystemUsage_Id])
);

CREATE TABLE [DataProcessingRegistrationOrganizations] (
    [DataProcessingRegistration_Id] int NOT NULL,
    [Organization_Id] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationOrganizations] PRIMARY KEY ([DataProcessingRegistration_Id], [Organization_Id]),
    CONSTRAINT [FK_DataProcessingRegistrationOrganizations_Organization_Organization_Id] FOREIGN KEY ([Organization_Id]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DataProcessingRegistrationOversightDates] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [OversightDate] datetime2 NOT NULL,
    [OversightRemark] nvarchar(max) NULL,
    [OversightReportLink] nvarchar(max) NULL,
    [OversightReportLinkName] nvarchar(max) NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationOversightDates] PRIMARY KEY ([Id])
);

CREATE TABLE [DataProcessingRegistrationReadModels] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [OrganizationId] int NOT NULL,
    [SourceEntityId] int NOT NULL,
    [SourceEntityUuid] uniqueidentifier NOT NULL,
    [MainReferenceUserAssignedId] nvarchar(max) NULL,
    [MainReferenceUrl] nvarchar(max) NULL,
    [MainReferenceTitle] nvarchar(100) NULL,
    [SystemNamesAsCsv] nvarchar(max) NULL,
    [SystemUuidsAsCsv] nvarchar(max) NULL,
    [DataProcessorNamesAsCsv] nvarchar(max) NULL,
    [SubDataProcessorNamesAsCsv] nvarchar(max) NULL,
    [IsAgreementConcluded] int NULL,
    [TransferToInsecureThirdCountries] int NULL,
    [AgreementConcludedAt] datetime2 NULL,
    [LatestOversightDate] datetime2 NULL,
    [LatestOversightRemark] nvarchar(max) NULL,
    [LatestOversightReportLink] nvarchar(max) NULL,
    [LatestOversightReportLinkName] nvarchar(max) NULL,
    [BasisForTransfer] nvarchar(100) NULL,
    [BasisForTransferUuid] uniqueidentifier NULL,
    [OversightInterval] int NULL,
    [DataResponsible] nvarchar(100) NULL,
    [DataResponsibleUuid] uniqueidentifier NULL,
    [OversightOptionNamesAsCsv] nvarchar(max) NULL,
    [IsOversightCompleted] int NULL,
    [OversightScheduledInspectionDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [ActiveAccordingToMainContract] bit NOT NULL,
    [ContractNamesAsCsv] nvarchar(max) NULL,
    [LastChangedById] int NULL,
    [LastChangedByName] nvarchar(100) NULL,
    [LastChangedAt] datetime2 NOT NULL,
    [ResponsibleOrgUnitUuid] uniqueidentifier NULL,
    [ResponsibleOrgUnitId] int NULL,
    [ResponsibleOrgUnitName] nvarchar(max) NULL,
    CONSTRAINT [PK_DataProcessingRegistrationReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrationReadModels_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingRegistrationRoleAssignmentReadModels] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NULL,
    [RoleId] int NOT NULL,
    [UserId] int NOT NULL,
    [UserFullName] nvarchar(100) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationRoleAssignmentReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrationRoleAssignmentReadModels_DataProcessingRegistrationReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [DataProcessingRegistrationReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DataProcessingRegistrationRights] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [ObjectId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_DataProcessingRegistrationRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrationRights_DataProcessingRegistrationRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [DataProcessingRegistrationRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingRegistrationRights_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingRegistrationRights_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingRegistrationRights_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DataProcessingRegistrations] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [OrganizationId] int NOT NULL,
    [HasSubDataProcessors] int NULL,
    [TransferToInsecureThirdCountries] int NULL,
    [DataResponsible_Id] int NULL,
    [DataResponsibleRemark] nvarchar(max) NULL,
    [OversightOptionRemark] nvarchar(max) NULL,
    [ReferenceId] int NULL,
    [IsAgreementConcluded] int NULL,
    [AgreementConcludedAt] datetime2 NULL,
    [AgreementConcludedRemark] nvarchar(max) NULL,
    [BasisForTransferId] int NULL,
    [OversightInterval] int NULL,
    [OversightIntervalRemark] nvarchar(max) NULL,
    [ResponsibleOrganizationUnitId] int NULL,
    [IsOversightCompleted] int NULL,
    [OversightCompletedRemark] nvarchar(max) NULL,
    [OversightScheduledInspectionDate] datetime2 NULL,
    [MainContractId] int NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_DataProcessingRegistrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrations_DataProcessingBasisForTransferOptions_BasisForTransferId] FOREIGN KEY ([BasisForTransferId]) REFERENCES [DataProcessingBasisForTransferOptions] ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrations_DataProcessingDataResponsibleOptions_DataResponsible_Id] FOREIGN KEY ([DataResponsible_Id]) REFERENCES [DataProcessingDataResponsibleOptions] ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrations_OrganizationUnit_ResponsibleOrganizationUnitId] FOREIGN KEY ([ResponsibleOrganizationUnitId]) REFERENCES [OrganizationUnit] ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrations_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DataProcessingRegistrations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_DataProcessingRegistrations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [SubDataProcessors] (
    [OrganizationId] int NOT NULL,
    [DataProcessingRegistrationId] int NOT NULL,
    [SubDataProcessorBasisForTransferId] int NULL,
    [TransferToInsecureCountry] int NULL,
    [InsecureCountryId] int NULL,
    CONSTRAINT [PK_SubDataProcessors] PRIMARY KEY ([OrganizationId], [DataProcessingRegistrationId]),
    CONSTRAINT [FK_SubDataProcessors_DataProcessingBasisForTransferOptions_SubDataProcessorBasisForTransferId] FOREIGN KEY ([SubDataProcessorBasisForTransferId]) REFERENCES [DataProcessingBasisForTransferOptions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubDataProcessors_DataProcessingCountryOptions_InsecureCountryId] FOREIGN KEY ([InsecureCountryId]) REFERENCES [DataProcessingCountryOptions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SubDataProcessors_DataProcessingRegistrations_DataProcessingRegistrationId] FOREIGN KEY ([DataProcessingRegistrationId]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SubDataProcessors_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EconomyStream] (
    [Id] int NOT NULL IDENTITY,
    [ExternPaymentForId] int NULL,
    [InternPaymentForId] int NULL,
    [OrganizationUnitId] int NULL,
    [Acquisition] int NOT NULL,
    [Operation] int NOT NULL,
    [Other] int NOT NULL,
    [AccountingEntry] nvarchar(max) NULL,
    [AuditStatus] int NOT NULL,
    [AuditDate] datetime2 NULL,
    [Note] nvarchar(max) NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_EconomyStream] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EconomyStream_OrganizationUnit_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [OrganizationUnit] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EconomyStream_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EconomyStream_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Exhibit] (
    [Id] int NOT NULL,
    [ItSystemId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_Exhibit] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Exhibit_ItInterface_Id] FOREIGN KEY ([Id]) REFERENCES [ItInterface] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Exhibit_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Exhibit_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ExternalReferences] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [ItContract_Id] int NULL,
    [ItSystemUsage_Id] int NULL,
    [ItSystem_Id] int NULL,
    [DataProcessingRegistration_Id] int NULL,
    [Title] nvarchar(max) NULL,
    [ExternalReferenceId] nvarchar(max) NULL,
    [URL] nvarchar(max) NULL,
    [Created] datetime2 NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_ExternalReferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExternalReferences_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExternalReferences_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ExternalReferences_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContract] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [ReferenceId] int NULL,
    [Name] nvarchar(200) NOT NULL,
    [Active] bit NOT NULL,
    [Note] nvarchar(max) NULL,
    [ItContractId] nvarchar(max) NULL,
    [SupplierContractSigner] nvarchar(max) NULL,
    [HasSupplierSigned] bit NOT NULL,
    [SupplierSignedDate] datetime2 NULL,
    [ContractSigner] nvarchar(max) NULL,
    [IsSigned] bit NOT NULL,
    [SignedDate] datetime2 NULL,
    [ResponsibleOrganizationUnitId] int NULL,
    [OrganizationId] int NOT NULL,
    [SupplierId] int NULL,
    [ProcurementStrategyId] int NULL,
    [ProcurementPlanQuarter] int NULL,
    [ProcurementPlanYear] int NULL,
    [ProcurementInitiated] int NULL,
    [ContractTemplateId] int NULL,
    [ContractTypeId] int NULL,
    [PurchaseFormId] int NULL,
    [ParentId] int NULL,
    [RequireValidParent] bit NOT NULL,
    [CriticalityId] int NULL,
    [Concluded] datetime2 NULL,
    [DurationYears] int NULL,
    [DurationMonths] int NULL,
    [DurationOngoing] bit NOT NULL,
    [IrrevocableTo] datetime2 NULL,
    [ExpirationDate] datetime2 NULL,
    [Terminated] datetime2 NULL,
    [TerminationDeadlineId] int NULL,
    [OptionExtendId] int NULL,
    [ExtendMultiplier] int NOT NULL,
    [Running] int NULL,
    [ByEnding] int NULL,
    [OperationRemunerationBegun] datetime2 NULL,
    [PaymentFreqencyId] int NULL,
    [PaymentModelId] int NULL,
    [PriceRegulationId] int NULL,
    [ObjectOwnerId] int NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NULL,
    CONSTRAINT [PK_ItContract] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContract_CriticalityTypes_CriticalityId] FOREIGN KEY ([CriticalityId]) REFERENCES [CriticalityTypes] ([Id]),
    CONSTRAINT [FK_ItContract_ExternalReferences_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [ExternalReferences] ([Id]),
    CONSTRAINT [FK_ItContract_ItContractTemplateTypes_ContractTemplateId] FOREIGN KEY ([ContractTemplateId]) REFERENCES [ItContractTemplateTypes] ([Id]),
    CONSTRAINT [FK_ItContract_ItContractTypes_ContractTypeId] FOREIGN KEY ([ContractTypeId]) REFERENCES [ItContractTypes] ([Id]),
    CONSTRAINT [FK_ItContract_ItContract_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItContract] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContract_OptionExtendTypes_OptionExtendId] FOREIGN KEY ([OptionExtendId]) REFERENCES [OptionExtendTypes] ([Id]),
    CONSTRAINT [FK_ItContract_OrganizationUnit_ResponsibleOrganizationUnitId] FOREIGN KEY ([ResponsibleOrganizationUnitId]) REFERENCES [OrganizationUnit] ([Id]),
    CONSTRAINT [FK_ItContract_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContract_Organization_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Organization] ([Id]),
    CONSTRAINT [FK_ItContract_PaymentFreqencyTypes_PaymentFreqencyId] FOREIGN KEY ([PaymentFreqencyId]) REFERENCES [PaymentFreqencyTypes] ([Id]),
    CONSTRAINT [FK_ItContract_PaymentModelTypes_PaymentModelId] FOREIGN KEY ([PaymentModelId]) REFERENCES [PaymentModelTypes] ([Id]),
    CONSTRAINT [FK_ItContract_PriceRegulationTypes_PriceRegulationId] FOREIGN KEY ([PriceRegulationId]) REFERENCES [PriceRegulationTypes] ([Id]),
    CONSTRAINT [FK_ItContract_ProcurementStrategyTypes_ProcurementStrategyId] FOREIGN KEY ([ProcurementStrategyId]) REFERENCES [ProcurementStrategyTypes] ([Id]),
    CONSTRAINT [FK_ItContract_PurchaseFormTypes_PurchaseFormId] FOREIGN KEY ([PurchaseFormId]) REFERENCES [PurchaseFormTypes] ([Id]),
    CONSTRAINT [FK_ItContract_TerminationDeadlineTypes_TerminationDeadlineId] FOREIGN KEY ([TerminationDeadlineId]) REFERENCES [TerminationDeadlineTypes] ([Id]),
    CONSTRAINT [FK_ItContract_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_ItContract_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id])
);

CREATE TABLE [ItSystem] (
    [Id] int NOT NULL IDENTITY,
    [BelongsToId] int NULL,
    [ExternalUuid] uniqueidentifier NULL,
    [ItSystemId] nvarchar(max) NULL,
    [PreviousName] nvarchar(max) NULL,
    [ParentId] int NULL,
    [BusinessTypeId] int NULL,
    [Disabled] bit NOT NULL,
    [ReferenceId] int NULL,
    [ArchiveDuty] int NULL,
    [ArchiveDutyComment] nvarchar(max) NULL,
    [LegalName] nvarchar(100) NULL,
    [LegalDataProcessorName] nvarchar(100) NULL,
    [SensitivePersonalDataTypeId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [Description] nvarchar(max) NULL,
    [AccessModifier] int NOT NULL,
    [OrganizationId] int NOT NULL,
    [Created] datetime2 NULL,
    CONSTRAINT [PK_ItSystem] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystem_BusinessTypes_BusinessTypeId] FOREIGN KEY ([BusinessTypeId]) REFERENCES [BusinessTypes] ([Id]),
    CONSTRAINT [FK_ItSystem_ExternalReferences_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [ExternalReferences] ([Id]),
    CONSTRAINT [FK_ItSystem_ItSystem_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystem] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystem_Organization_BelongsToId] FOREIGN KEY ([BelongsToId]) REFERENCES [Organization] ([Id]),
    CONSTRAINT [FK_ItSystem_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId] FOREIGN KEY ([SensitivePersonalDataTypeId]) REFERENCES [SensitivePersonalDataTypes] ([Id]),
    CONSTRAINT [FK_ItSystem_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystem_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractAgreementElementTypes] (
    [ItContract_Id] int NOT NULL,
    [AgreementElementType_Id] int NOT NULL,
    CONSTRAINT [PK_ItContractAgreementElementTypes] PRIMARY KEY ([AgreementElementType_Id], [ItContract_Id]),
    CONSTRAINT [FK_ItContractAgreementElementTypes_AgreementElementTypes_AgreementElementType_Id] FOREIGN KEY ([AgreementElementType_Id]) REFERENCES [AgreementElementTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractAgreementElementTypes_ItContract_ItContract_Id] FOREIGN KEY ([ItContract_Id]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractOverviewReadModels] (
    [Id] int NOT NULL IDENTITY,
    [OrganizationId] int NOT NULL,
    [SourceEntityId] int NOT NULL,
    [SourceEntityUuid] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [ContractId] nvarchar(max) NULL,
    [ParentContractId] int NULL,
    [ParentContractName] nvarchar(200) NULL,
    [ParentContractUuid] uniqueidentifier NULL,
    [CriticalityId] int NULL,
    [CriticalityUuid] uniqueidentifier NULL,
    [CriticalityName] nvarchar(150) NULL,
    [ResponsibleOrgUnitId] int NULL,
    [ResponsibleOrgUnitName] nvarchar(max) NULL,
    [SupplierId] int NULL,
    [SupplierName] nvarchar(100) NULL,
    [ContractSigner] nvarchar(max) NULL,
    [ContractTypeId] int NULL,
    [ContractTypeUuid] uniqueidentifier NULL,
    [ContractTypeName] nvarchar(150) NULL,
    [ContractTemplateId] int NULL,
    [ContractTemplateUuid] uniqueidentifier NULL,
    [ContractTemplateName] nvarchar(150) NULL,
    [PurchaseFormId] int NULL,
    [PurchaseFormUuid] uniqueidentifier NULL,
    [PurchaseFormName] nvarchar(150) NULL,
    [ProcurementStrategyId] int NULL,
    [ProcurementStrategyUuid] uniqueidentifier NULL,
    [ProcurementStrategyName] nvarchar(150) NULL,
    [ProcurementPlanYear] int NULL,
    [ProcurementPlanQuarter] int NULL,
    [ProcurementInitiated] int NULL,
    [DataProcessingAgreementsCsv] nvarchar(max) NULL,
    [ItSystemUsagesCsv] nvarchar(max) NULL,
    [ItSystemUsagesSystemUuidCsv] nvarchar(max) NULL,
    [NumberOfAssociatedSystemRelations] int NOT NULL,
    [ActiveReferenceTitle] nvarchar(max) NULL,
    [ActiveReferenceUrl] nvarchar(max) NULL,
    [ActiveReferenceExternalReferenceId] nvarchar(max) NULL,
    [AccumulatedAcquisitionCost] int NOT NULL,
    [AccumulatedOperationCost] int NOT NULL,
    [AccumulatedOtherCost] int NOT NULL,
    [OperationRemunerationBegunDate] datetime2 NULL,
    [PaymentModelId] int NULL,
    [PaymentModelUuid] uniqueidentifier NULL,
    [PaymentModelName] nvarchar(150) NULL,
    [PaymentFrequencyId] int NULL,
    [PaymentFrequencyUuid] uniqueidentifier NULL,
    [PaymentFrequencyName] nvarchar(150) NULL,
    [LatestAuditDate] datetime2 NULL,
    [AuditStatusWhite] int NOT NULL,
    [AuditStatusRed] int NOT NULL,
    [AuditStatusYellow] int NOT NULL,
    [AuditStatusGreen] int NOT NULL,
    [Duration] nvarchar(100) NULL,
    [OptionExtendId] int NULL,
    [OptionExtendUuid] uniqueidentifier NULL,
    [OptionExtendName] nvarchar(150) NULL,
    [TerminationDeadlineId] int NULL,
    [TerminationDeadlineUuid] uniqueidentifier NULL,
    [TerminationDeadlineName] nvarchar(150) NULL,
    [IrrevocableTo] datetime2 NULL,
    [TerminatedAt] datetime2 NULL,
    [LastEditedByUserId] int NULL,
    [LastEditedByUserName] nvarchar(100) NULL,
    [LastEditedAtDate] datetime2 NULL,
    [Concluded] datetime2 NULL,
    [ExpirationDate] datetime2 NULL,
    CONSTRAINT [PK_ItContractOverviewReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractOverviewReadModels_ItContract_SourceEntityId] FOREIGN KEY ([SourceEntityId]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItContractOverviewReadModels_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractRights] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [ObjectId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_ItContractRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractRights_ItContractRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [ItContractRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractRights_ItContract_ObjectId] FOREIGN KEY ([ObjectId]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItContractRights_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractRights_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItContractRights_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaskRefItSystems] (
    [ItSystem_Id] int NOT NULL,
    [TaskRef_Id] int NOT NULL,
    CONSTRAINT [PK_TaskRefItSystems] PRIMARY KEY ([ItSystem_Id], [TaskRef_Id]),
    CONSTRAINT [FK_TaskRefItSystems_ItSystem_ItSystem_Id] FOREIGN KEY ([ItSystem_Id]) REFERENCES [ItSystem] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskRefItSystems_TaskRef_TaskRef_Id] FOREIGN KEY ([TaskRef_Id]) REFERENCES [TaskRef] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsage] (
    [Id] int NOT NULL IDENTITY,
    [Concluded] datetime2 NULL,
    [ExpirationDate] datetime2 NULL,
    [Note] nvarchar(max) NULL,
    [LocalSystemId] nvarchar(200) NULL,
    [Version] nvarchar(100) NULL,
    [LocalCallName] nvarchar(100) NULL,
    [LifeCycleStatus] int NULL,
    [OrganizationId] int NOT NULL,
    [ItSystemId] int NOT NULL,
    [ArchiveTypeId] int NULL,
    [SensitiveDataTypeId] int NULL,
    [ReferenceId] int NULL,
    [ArchiveDuty] int NULL,
    [ArchiveNotes] nvarchar(max) NULL,
    [ArchiveFreq] int NULL,
    [Registertype] bit NULL,
    [ArchiveSupplierId] int NULL,
    [ArchiveLocationId] int NULL,
    [ArchiveTestLocationId] int NULL,
    [ItSystemCategoriesId] int NULL,
    [GdprCriticality] int NULL,
    [UserCount] int NULL,
    [ContainsAITechnology] int NULL,
    [GeneralPurpose] nvarchar(max) NULL,
    [isBusinessCritical] int NULL,
    [LinkToDirectoryUrl] nvarchar(max) NULL,
    [LinkToDirectoryUrlName] nvarchar(150) NULL,
    [precautions] int NULL,
    [precautionsOptionsEncryption] bit NOT NULL,
    [precautionsOptionsPseudonomisering] bit NOT NULL,
    [precautionsOptionsAccessControl] bit NOT NULL,
    [precautionsOptionsLogning] bit NOT NULL,
    [TechnicalSupervisionDocumentationUrlName] nvarchar(max) NULL,
    [TechnicalSupervisionDocumentationUrl] nvarchar(max) NULL,
    [UserSupervision] int NULL,
    [UserSupervisionDate] datetime2 NULL,
    [UserSupervisionDocumentationUrlName] nvarchar(max) NULL,
    [UserSupervisionDocumentationUrl] nvarchar(max) NULL,
    [riskAssessment] int NULL,
    [riskAssesmentDate] datetime2 NULL,
    [preriskAssessment] int NULL,
    [PlannedRiskAssessmentDate] datetime2 NULL,
    [RiskSupervisionDocumentationUrlName] nvarchar(150) NULL,
    [RiskSupervisionDocumentationUrl] nvarchar(max) NULL,
    [noteRisks] nvarchar(max) NULL,
    [DPIA] int NULL,
    [DPIADateFor] datetime2 NULL,
    [DPIASupervisionDocumentationUrlName] nvarchar(max) NULL,
    [DPIASupervisionDocumentationUrl] nvarchar(max) NULL,
    [answeringDataDPIA] int NULL,
    [DPIAdeleteDate] datetime2 NULL,
    [numberDPIA] int NOT NULL,
    [HostedAt] int NULL,
    [WebAccessibilityCompliance] int NULL,
    [LastWebAccessibilityCheck] datetime2 NULL,
    [WebAccessibilityNotes] nvarchar(max) NULL,
    [ArchiveFromSystem] bit NULL,
    [Uuid] uniqueidentifier NOT NULL,
    [RegisterTypeId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsage] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ArchiveLocations_ArchiveLocationId] FOREIGN KEY ([ArchiveLocationId]) REFERENCES [ArchiveLocations] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ArchiveTestLocations_ArchiveTestLocationId] FOREIGN KEY ([ArchiveTestLocationId]) REFERENCES [ArchiveTestLocations] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ArchiveTypes_ArchiveTypeId] FOREIGN KEY ([ArchiveTypeId]) REFERENCES [ArchiveTypes] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ExternalReferences_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [ExternalReferences] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ItSystemCategories_ItSystemCategoriesId] FOREIGN KEY ([ItSystemCategoriesId]) REFERENCES [ItSystemCategories] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_ItSystem_ItSystemId] FOREIGN KEY ([ItSystemId]) REFERENCES [ItSystem] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsage_Organization_ArchiveSupplierId] FOREIGN KEY ([ArchiveSupplierId]) REFERENCES [Organization] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsage_RegisterTypes_RegisterTypeId] FOREIGN KEY ([RegisterTypeId]) REFERENCES [RegisterTypes] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_SensitiveDataTypes_SensitiveDataTypeId] FOREIGN KEY ([SensitiveDataTypeId]) REFERENCES [SensitiveDataTypes] ([Id]),
    CONSTRAINT [FK_ItSystemUsage_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsage_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItContractOverviewReadModelDataProcessingAgreements] (
    [Id] int NOT NULL IDENTITY,
    [DataProcessingRegistrationId] int NOT NULL,
    [DataProcessingRegistrationUuid] uniqueidentifier NOT NULL,
    [DataProcessingRegistrationName] nvarchar(200) NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItContractOverviewReadModelDataProcessingAgreements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractOverviewReadModelDataProcessingAgreements_ItContractOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItContractOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractOverviewReadModelItSystemUsages] (
    [Id] int NOT NULL IDENTITY,
    [ItSystemUsageId] int NOT NULL,
    [ItSystemUsageUuid] uniqueidentifier NOT NULL,
    [ItSystemUsageSystemUuid] nvarchar(50) NULL,
    [ItSystemUsageName] nvarchar(200) NULL,
    [ItSystemIsDisabled] bit NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItContractOverviewReadModelItSystemUsages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractOverviewReadModelItSystemUsages_ItContractOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItContractOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractOverviewReadModelSystemRelations] (
    [Id] int NOT NULL IDENTITY,
    [RelationId] int NOT NULL,
    [FromSystemUsageId] int NOT NULL,
    [ToSystemUsageId] int NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItContractOverviewReadModelSystemRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractOverviewReadModelSystemRelations_ItContractOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItContractOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractOverviewRoleAssignmentReadModels] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [UserId] int NOT NULL,
    [UserFullName] nvarchar(100) NULL,
    [Email] nvarchar(max) NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItContractOverviewRoleAssignmentReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItContractOverviewRoleAssignmentReadModels_ItContractOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItContractOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItContractItSystemUsages] (
    [ItContractId] int NOT NULL,
    [ItSystemUsageId] int NOT NULL,
    [ItSystemUsage_Id] int NULL,
    CONSTRAINT [PK_ItContractItSystemUsages] PRIMARY KEY ([ItContractId], [ItSystemUsageId]),
    CONSTRAINT [FK_ItContractItSystemUsages_ItContract_ItContractId] FOREIGN KEY ([ItContractId]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItContractItSystemUsages_ItSystemUsage_ItSystemUsageId] FOREIGN KEY ([ItSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItContractItSystemUsages_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemRights] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [ObjectId] int NOT NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_ItSystemRights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemRights_ItSystemRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [ItSystemRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemRights_ItSystemUsage_ObjectId] FOREIGN KEY ([ObjectId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ItSystemRights_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemRights_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemRights_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemUsageOrgUnitUsages] (
    [ItSystemUsageId] int NOT NULL,
    [OrganizationUnitId] int NOT NULL,
    [ResponsibleItSystemUsage_Id] int NULL,
    CONSTRAINT [PK_ItSystemUsageOrgUnitUsages] PRIMARY KEY ([ItSystemUsageId], [OrganizationUnitId]),
    CONSTRAINT [FK_ItSystemUsageOrgUnitUsages_ItSystemUsage_ItSystemUsageId] FOREIGN KEY ([ItSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsageOrgUnitUsages_ItSystemUsage_ResponsibleItSystemUsage_Id] FOREIGN KEY ([ResponsibleItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsageOrgUnitUsages_OrganizationUnit_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [OrganizationUnit] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemUsageOverviewReadModels] (
    [Id] int NOT NULL IDENTITY,
    [OrganizationId] int NOT NULL,
    [SourceEntityId] int NOT NULL,
    [SourceEntityUuid] uniqueidentifier NOT NULL,
    [ExternalSystemUuid] uniqueidentifier NULL,
    [SystemName] nvarchar(100) NOT NULL,
    [SystemPreviousName] nvarchar(max) NULL,
    [SystemDescription] nvarchar(max) NULL,
    [ItSystemDisabled] bit NOT NULL,
    [ActiveAccordingToValidityPeriod] bit NOT NULL,
    [ActiveAccordingToLifeCycle] bit NOT NULL,
    [SystemActive] bit NOT NULL,
    [Note] nvarchar(max) NULL,
    [ParentItSystemName] nvarchar(100) NULL,
    [ParentItSystemId] int NULL,
    [ParentItSystemUuid] uniqueidentifier NULL,
    [ParentItSystemDisabled] bit NULL,
    [ParentItSystemUsageUuid] uniqueidentifier NULL,
    [Version] nvarchar(100) NULL,
    [ContainsAITechnology] int NULL,
    [LocalCallName] nvarchar(100) NULL,
    [LocalSystemId] nvarchar(200) NULL,
    [ItSystemUuid] nvarchar(50) NULL,
    [ResponsibleOrganizationUnitUuid] uniqueidentifier NULL,
    [ResponsibleOrganizationUnitId] int NULL,
    [ResponsibleOrganizationUnitName] nvarchar(100) NULL,
    [ItSystemBusinessTypeUuid] uniqueidentifier NULL,
    [ItSystemBusinessTypeId] int NULL,
    [ItSystemBusinessTypeName] nvarchar(150) NULL,
    [ItSystemRightsHolderId] int NULL,
    [ItSystemRightsHolderName] nvarchar(100) NULL,
    [ItSystemCategoriesId] int NULL,
    [ItSystemCategoriesUuid] uniqueidentifier NULL,
    [ItSystemCategoriesName] nvarchar(100) NULL,
    [ItSystemKLEIdsAsCsv] nvarchar(max) NULL,
    [ItSystemKLENamesAsCsv] nvarchar(max) NULL,
    [LocalReferenceDocumentId] nvarchar(max) NULL,
    [LocalReferenceUrl] nvarchar(max) NULL,
    [LocalReferenceTitle] nvarchar(100) NULL,
    [ObjectOwnerId] int NULL,
    [ObjectOwnerName] nvarchar(100) NULL,
    [LastChangedById] int NULL,
    [LastChangedByName] nvarchar(100) NULL,
    [LastChangedAt] datetime2 NOT NULL,
    [Concluded] datetime2 NULL,
    [ExpirationDate] datetime2 NULL,
    [MainContractId] int NULL,
    [MainContractSupplierId] int NULL,
    [MainContractSupplierName] nvarchar(100) NULL,
    [MainContractIsActive] int NOT NULL,
    [SensitiveDataLevelsAsCsv] nvarchar(max) NULL,
    [RiskAssessmentDate] datetime2 NULL,
    [PlannedRiskAssessmentDate] datetime2 NULL,
    [ArchiveDuty] int NULL,
    [IsHoldingDocument] bit NOT NULL,
    [RiskSupervisionDocumentationName] nvarchar(150) NULL,
    [RiskSupervisionDocumentationUrl] nvarchar(max) NULL,
    [LinkToDirectoryName] nvarchar(150) NULL,
    [LinkToDirectoryUrl] nvarchar(max) NULL,
    [LifeCycleStatus] int NULL,
    [DataProcessingRegistrationsConcludedAsCsv] nvarchar(max) NULL,
    [DataProcessingRegistrationNamesAsCsv] nvarchar(max) NULL,
    [GeneralPurpose] nvarchar(200) NULL,
    [HostedAt] int NOT NULL,
    [UserCount] int NOT NULL,
    [GdprCriticality] int NULL,
    [DependsOnInterfacesNamesAsCsv] nvarchar(max) NULL,
    [IncomingRelatedItSystemUsagesNamesAsCsv] nvarchar(max) NULL,
    [OutgoingRelatedItSystemUsagesNamesAsCsv] nvarchar(max) NULL,
    [RelevantOrganizationUnitNamesAsCsv] nvarchar(max) NULL,
    [AssociatedContractsNamesCsv] nvarchar(max) NULL,
    [DPIAConducted] int NULL,
    [IsBusinessCritical] int NULL,
    [CatalogArchiveDuty] int NULL,
    [CatalogArchiveDutyComment] nvarchar(max) NULL,
    [WebAccessibilityCompliance] int NULL,
    [LastWebAccessibilityCheck] datetime2 NULL,
    [WebAccessibilityNotes] nvarchar(max) NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewReadModels_ItSystemUsage_SourceEntityId] FOREIGN KEY ([SourceEntityId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ItSystemUsageOverviewReadModels_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemUsagePersonalDatas] (
    [Id] int NOT NULL IDENTITY,
    [ItSystemUsageId] int NOT NULL,
    [PersonalData] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsagePersonalDatas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsagePersonalDatas_ItSystemUsage_ItSystemUsageId] FOREIGN KEY ([ItSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageSensitiveDataLevels] (
    [Id] int NOT NULL IDENTITY,
    [ItSystemUsage_Id] int NOT NULL,
    [SensitivityDataLevel] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageSensitiveDataLevels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageSensitiveDataLevels_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaskRefItSystemUsages] (
    [ItSystemUsage_Id] int NOT NULL,
    [TaskRef_Id] int NOT NULL,
    CONSTRAINT [PK_TaskRefItSystemUsages] PRIMARY KEY ([ItSystemUsage_Id], [TaskRef_Id]),
    CONSTRAINT [FK_TaskRefItSystemUsages_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskRefItSystemUsages_TaskRef_TaskRef_Id] FOREIGN KEY ([TaskRef_Id]) REFERENCES [TaskRef] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SystemRelations] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [FromSystemUsageId] int NOT NULL,
    [ToSystemUsageId] int NOT NULL,
    [RelationInterfaceId] int NULL,
    [Description] nvarchar(max) NULL,
    [Reference] nvarchar(max) NULL,
    [UsageFrequencyId] int NULL,
    [AssociatedContractId] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_SystemRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemRelations_ItContract_AssociatedContractId] FOREIGN KEY ([AssociatedContractId]) REFERENCES [ItContract] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemRelations_ItInterface_RelationInterfaceId] FOREIGN KEY ([RelationInterfaceId]) REFERENCES [ItInterface] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemRelations_ItSystemUsage_FromSystemUsageId] FOREIGN KEY ([FromSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SystemRelations_ItSystemUsage_ToSystemUsageId] FOREIGN KEY ([ToSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemRelations_RelationFrequencyTypes_UsageFrequencyId] FOREIGN KEY ([UsageFrequencyId]) REFERENCES [RelationFrequencyTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemRelations_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SystemRelations_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaskRefItSystemUsageOptOut] (
    [ItSystemUsage_Id] int NOT NULL,
    [TaskRef_Id] int NOT NULL,
    CONSTRAINT [PK_TaskRefItSystemUsageOptOut] PRIMARY KEY ([ItSystemUsage_Id], [TaskRef_Id]),
    CONSTRAINT [FK_TaskRefItSystemUsageOptOut_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskRefItSystemUsageOptOut_TaskRef_TaskRef_Id] FOREIGN KEY ([TaskRef_Id]) REFERENCES [TaskRef] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserNotifications] (
    [Id] int NOT NULL IDENTITY,
    [Uuid] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [NotificationMessage] nvarchar(max) NOT NULL,
    [NotificationType] int NOT NULL,
    [NotificationRecipientId] int NOT NULL,
    [Created] datetime2 NOT NULL,
    [OrganizationId] int NOT NULL,
    [Itcontract_Id] int NULL,
    [ItSystemUsage_Id] int NULL,
    [DataProcessingRegistration_Id] int NULL,
    [ObjectOwnerId] int NOT NULL,
    [LastChanged] datetime2 NOT NULL,
    [LastChangedByUserId] int NOT NULL,
    CONSTRAINT [PK_UserNotifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserNotifications_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]),
    CONSTRAINT [FK_UserNotifications_ItContract_Itcontract_Id] FOREIGN KEY ([Itcontract_Id]) REFERENCES [ItContract] ([Id]),
    CONSTRAINT [FK_UserNotifications_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]),
    CONSTRAINT [FK_UserNotifications_Organization_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organization] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserNotifications_User_LastChangedByUserId] FOREIGN KEY ([LastChangedByUserId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserNotifications_User_NotificationRecipientId] FOREIGN KEY ([NotificationRecipientId]) REFERENCES [User] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserNotifications_User_ObjectOwnerId] FOREIGN KEY ([ObjectOwnerId]) REFERENCES [User] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ItSystemUsageOverviewArchivePeriodReadModels] (
    [Id] int NOT NULL IDENTITY,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewArchivePeriodReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewArchivePeriodReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewDataProcessingRegistrationReadModels] (
    [Id] int NOT NULL IDENTITY,
    [DataProcessingRegistrationUuid] uniqueidentifier NOT NULL,
    [DataProcessingRegistrationId] int NOT NULL,
    [DataProcessingRegistrationName] nvarchar(200) NOT NULL,
    [IsAgreementConcluded] int NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewDataProcessingRegistrationReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewDataProcessingRegistrationReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewInterfaceReadModels] (
    [Id] int NOT NULL IDENTITY,
    [InterfaceUuid] uniqueidentifier NOT NULL,
    [InterfaceId] int NOT NULL,
    [InterfaceName] nvarchar(100) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewInterfaceReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewInterfaceReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewItContractReadModels] (
    [Id] int NOT NULL IDENTITY,
    [ItContractUuid] uniqueidentifier NOT NULL,
    [ItContractId] int NOT NULL,
    [ItContractName] nvarchar(200) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewItContractReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewItContractReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewUsedBySystemUsageReadModels] (
    [Id] int NOT NULL IDENTITY,
    [ItSystemUsageUuid] uniqueidentifier NOT NULL,
    [ItSystemUsageId] int NOT NULL,
    [ItSystemUsageName] nvarchar(100) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewUsedBySystemUsageReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewUsedBySystemUsageReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewRelevantOrgUnitReadModels] (
    [Id] int NOT NULL IDENTITY,
    [OrganizationUnitUuid] uniqueidentifier NOT NULL,
    [OrganizationUnitId] int NOT NULL,
    [OrganizationUnitName] nvarchar(100) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewRelevantOrgUnitReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewRelevantOrgUnitReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewRoleAssignmentReadModels] (
    [Id] int NOT NULL IDENTITY,
    [RoleUuid] uniqueidentifier NOT NULL,
    [RoleId] int NOT NULL,
    [UserId] int NOT NULL,
    [UserFullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewRoleAssignmentReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewRoleAssignmentReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewSensitiveDataLevelReadModels] (
    [Id] int NOT NULL IDENTITY,
    [SensitivityDataLevel] int NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewSensitiveDataLevelReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewSensitiveDataLevelReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewTaskRefReadModels] (
    [Id] int NOT NULL IDENTITY,
    [KLEId] nvarchar(15) NULL,
    [KLEName] nvarchar(150) NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewTaskRefReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewTaskRefReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ItSystemUsageOverviewUsingSystemUsageReadModels] (
    [Id] int NOT NULL IDENTITY,
    [ItSystemUsageUuid] uniqueidentifier NOT NULL,
    [ItSystemUsageId] int NOT NULL,
    [ItSystemUsageName] nvarchar(100) NOT NULL,
    [ParentId] int NOT NULL,
    CONSTRAINT [PK_ItSystemUsageOverviewUsingSystemUsageReadModels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItSystemUsageOverviewUsingSystemUsageReadModels_ItSystemUsageOverviewReadModels_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ItSystemUsageOverviewReadModels] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Advice_LastChangedByUserId] ON [Advice] ([LastChangedByUserId]);

CREATE INDEX [IX_Advice_ObjectOwnerId] ON [Advice] ([ObjectOwnerId]);

CREATE INDEX [IX_AdviceSents_AdviceId] ON [AdviceSents] ([AdviceId]);

CREATE INDEX [IX_AdviceSents_LastChangedByUserId] ON [AdviceSents] ([LastChangedByUserId]);

CREATE INDEX [IX_AdviceSents_ObjectOwnerId] ON [AdviceSents] ([ObjectOwnerId]);

CREATE INDEX [IX_AdviceUserRelations_AdviceId] ON [AdviceUserRelations] ([AdviceId]);

CREATE INDEX [IX_AdviceUserRelations_DataProcessingRegistrationRoleId] ON [AdviceUserRelations] ([DataProcessingRegistrationRoleId]);

CREATE INDEX [IX_AdviceUserRelations_ItContractRoleId] ON [AdviceUserRelations] ([ItContractRoleId]);

CREATE INDEX [IX_AdviceUserRelations_ItSystemRoleId] ON [AdviceUserRelations] ([ItSystemRoleId]);

CREATE INDEX [IX_AdviceUserRelations_LastChangedByUserId] ON [AdviceUserRelations] ([LastChangedByUserId]);

CREATE INDEX [IX_AdviceUserRelations_ObjectOwnerId] ON [AdviceUserRelations] ([ObjectOwnerId]);

CREATE INDEX [IX_AgreementElementTypes_LastChangedByUserId] ON [AgreementElementTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_AgreementElementTypes_ObjectOwnerId] ON [AgreementElementTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [AgreementElementTypes] ([Uuid]);

CREATE INDEX [IX_ArchiveLocations_LastChangedByUserId] ON [ArchiveLocations] ([LastChangedByUserId]);

CREATE INDEX [IX_ArchiveLocations_ObjectOwnerId] ON [ArchiveLocations] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ArchiveLocations] ([Uuid]);

CREATE INDEX [IX_ArchivePeriod_ItSystemUsageId] ON [ArchivePeriod] ([ItSystemUsageId]);

CREATE INDEX [IX_ArchivePeriod_LastChangedByUserId] ON [ArchivePeriod] ([LastChangedByUserId]);

CREATE INDEX [IX_ArchivePeriod_ObjectOwnerId] ON [ArchivePeriod] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_ArchivePeriod_Uuid] ON [ArchivePeriod] ([Uuid]);

CREATE INDEX [IX_ArchiveTestLocations_LastChangedByUserId] ON [ArchiveTestLocations] ([LastChangedByUserId]);

CREATE INDEX [IX_ArchiveTestLocations_ObjectOwnerId] ON [ArchiveTestLocations] ([ObjectOwnerId]);

CREATE INDEX [IX_ArchiveTypes_LastChangedByUserId] ON [ArchiveTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_ArchiveTypes_ObjectOwnerId] ON [ArchiveTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ArchiveTypes] ([Uuid]);

CREATE INDEX [IX_AttachedOptions_LastChangedByUserId] ON [AttachedOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_AttachedOptions_ObjectOwnerId] ON [AttachedOptions] ([ObjectOwnerId]);

CREATE INDEX [UX_ObjectId] ON [AttachedOptions] ([ObjectId]);

CREATE INDEX [UX_ObjectType] ON [AttachedOptions] ([ObjectType]);

CREATE INDEX [UX_OptionId] ON [AttachedOptions] ([OptionId]);

CREATE INDEX [UX_OptionType] ON [AttachedOptions] ([OptionType]);

CREATE INDEX [IX_BrokenLinkInExternalReferences_BrokenReferenceOrigin_Id] ON [BrokenLinkInExternalReferences] ([BrokenReferenceOrigin_Id]);

CREATE INDEX [IX_BrokenLinkInExternalReferences_ParentReport_Id] ON [BrokenLinkInExternalReferences] ([ParentReport_Id]);

CREATE INDEX [IX_BrokenLinkInInterfaces_BrokenReferenceOrigin_Id] ON [BrokenLinkInInterfaces] ([BrokenReferenceOrigin_Id]);

CREATE INDEX [IX_BrokenLinkInInterfaces_ParentReport_Id] ON [BrokenLinkInInterfaces] ([ParentReport_Id]);

CREATE INDEX [IX_BusinessTypes_LastChangedByUserId] ON [BusinessTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_BusinessTypes_ObjectOwnerId] ON [BusinessTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [BusinessTypes] ([Uuid]);

CREATE INDEX [IX_Config_LastChangedByUserId] ON [Config] ([LastChangedByUserId]);

CREATE INDEX [IX_Config_ObjectOwnerId] ON [Config] ([ObjectOwnerId]);

CREATE INDEX [IX_ContactPersons_LastChangedByUserId] ON [ContactPersons] ([LastChangedByUserId]);

CREATE INDEX [IX_ContactPersons_ObjectOwnerId] ON [ContactPersons] ([ObjectOwnerId]);

CREATE INDEX [IX_CountryCodes_LastChangedByUserId] ON [CountryCodes] ([LastChangedByUserId]);

CREATE INDEX [IX_CountryCodes_ObjectOwnerId] ON [CountryCodes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [CountryCodes] ([Uuid]);

CREATE INDEX [IX_CriticalityTypes_LastChangedByUserId] ON [CriticalityTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_CriticalityTypes_ObjectOwnerId] ON [CriticalityTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [CriticalityTypes] ([Uuid]);

CREATE INDEX [IX_CustomizedUiNodes_LastChangedByUserId] ON [CustomizedUiNodes] ([LastChangedByUserId]);

CREATE INDEX [IX_CustomizedUiNodes_ModuleId] ON [CustomizedUiNodes] ([ModuleId]);

CREATE INDEX [IX_CustomizedUiNodes_ObjectOwnerId] ON [CustomizedUiNodes] ([ObjectOwnerId]);

CREATE INDEX [IX_DataProcessingBasisForTransferOptions_LastChangedByUserId] ON [DataProcessingBasisForTransferOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingBasisForTransferOptions_ObjectOwnerId] ON [DataProcessingBasisForTransferOptions] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataProcessingBasisForTransferOptions] ([Uuid]);

CREATE INDEX [IX_DataProcessingRegistrationDataProcessingCountryOptions_DataProcessingRegistration_Id] ON [DataProcessingRegistrationDataProcessingCountryOptions] ([DataProcessingRegistration_Id]);

CREATE INDEX [IX_DataProcessingCountryOptions_LastChangedByUserId] ON [DataProcessingCountryOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingCountryOptions_ObjectOwnerId] ON [DataProcessingCountryOptions] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataProcessingCountryOptions] ([Uuid]);

CREATE INDEX [IX_DataProcessingDataResponsibleOptions_LastChangedByUserId] ON [DataProcessingDataResponsibleOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingDataResponsibleOptions_ObjectOwnerId] ON [DataProcessingDataResponsibleOptions] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataProcessingDataResponsibleOptions] ([Uuid]);

CREATE INDEX [IX_DataProcessingRegistrationDataProcessingOversightOptions_DataProcessingRegistration_Id] ON [DataProcessingRegistrationDataProcessingOversightOptions] ([DataProcessingRegistration_Id]);

CREATE INDEX [IX_DataProcessingOversightOptions_LastChangedByUserId] ON [DataProcessingOversightOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingOversightOptions_ObjectOwnerId] ON [DataProcessingOversightOptions] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataProcessingOversightOptions] ([Uuid]);

CREATE INDEX [IX_ItContractDataProcessingRegistrations_DataProcessingRegistration_Id] ON [ItContractDataProcessingRegistrations] ([DataProcessingRegistration_Id]);

CREATE INDEX [IX_DataProcessingRegistrationItSystemUsages_ItSystemUsage_Id] ON [DataProcessingRegistrationItSystemUsages] ([ItSystemUsage_Id]);

CREATE INDEX [IX_DataProcessingRegistrationOrganizations_Organization_Id] ON [DataProcessingRegistrationOrganizations] ([Organization_Id]);

CREATE INDEX [IX_DataProcessingRegistrationOversightDates_ParentId] ON [DataProcessingRegistrationOversightDates] ([ParentId]);

CREATE INDEX [DataProcessingRegistrationReadModel_Index_LastChangedAt] ON [DataProcessingRegistrationReadModels] ([LastChangedAt]);

CREATE INDEX [DataProcessingRegistrationReadModel_Index_LastChangedById] ON [DataProcessingRegistrationReadModels] ([LastChangedById]);

CREATE INDEX [DataProcessingRegistrationReadModel_Index_LastChangedByName] ON [DataProcessingRegistrationReadModels] ([LastChangedByName]);

CREATE INDEX [DataProcessingRegistrationReadModel_Index_MainReferenceTitle] ON [DataProcessingRegistrationReadModels] ([MainReferenceTitle]);

CREATE INDEX [DataProcessingRegistrationReadModel_Index_Name] ON [DataProcessingRegistrationReadModels] ([Name]);

CREATE INDEX [IX_DataProcessingRegistrationReadModels_OrganizationId] ON [DataProcessingRegistrationReadModels] ([OrganizationId]);

CREATE INDEX [IX_DataProcessingRegistrationReadModels_SourceEntityId] ON [DataProcessingRegistrationReadModels] ([SourceEntityId]);

CREATE INDEX [IX_DPR_Concluded] ON [DataProcessingRegistrationReadModels] ([IsAgreementConcluded]);

CREATE INDEX [IX_DPR_DataResponsible] ON [DataProcessingRegistrationReadModels] ([DataResponsible]);

CREATE INDEX [IX_DPR_DataResponsibleUuid] ON [DataProcessingRegistrationReadModels] ([DataResponsibleUuid]);

CREATE INDEX [IX_DPR_IsActive] ON [DataProcessingRegistrationReadModels] ([IsActive]);

CREATE INDEX [IX_DPR_IsOversightCompleted] ON [DataProcessingRegistrationReadModels] ([IsOversightCompleted]);

CREATE INDEX [IX_DPR_MainContractIsActive] ON [DataProcessingRegistrationReadModels] ([ActiveAccordingToMainContract]);

CREATE INDEX [IX_DPR_OversightInterval] ON [DataProcessingRegistrationReadModels] ([OversightInterval]);

CREATE INDEX [IX_DPR_OversightScheduledInspectionDate] ON [DataProcessingRegistrationReadModels] ([OversightScheduledInspectionDate]);

CREATE INDEX [IX_DPR_ResponsibleOrgUnitId] ON [DataProcessingRegistrationReadModels] ([ResponsibleOrgUnitId]);

CREATE INDEX [IX_DPR_ResponsibleOrgUnitUuid] ON [DataProcessingRegistrationReadModels] ([ResponsibleOrgUnitUuid]);

CREATE INDEX [IX_DPR_TransferToInsecureThirdCountries] ON [DataProcessingRegistrationReadModels] ([TransferToInsecureThirdCountries]);

CREATE INDEX [IX_DRP_BasisForTransfer] ON [DataProcessingRegistrationReadModels] ([BasisForTransfer]);

CREATE INDEX [IX_DRP_BasisForTransferUuid] ON [DataProcessingRegistrationReadModels] ([BasisForTransferUuid]);

CREATE INDEX [IX_DataProcessingRegistrationRights_LastChangedByUserId] ON [DataProcessingRegistrationRights] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingRegistrationRights_ObjectId] ON [DataProcessingRegistrationRights] ([ObjectId]);

CREATE INDEX [IX_DataProcessingRegistrationRights_ObjectOwnerId] ON [DataProcessingRegistrationRights] ([ObjectOwnerId]);

CREATE INDEX [IX_DataProcessingRegistrationRights_RoleId] ON [DataProcessingRegistrationRights] ([RoleId]);

CREATE INDEX [IX_DataProcessingRegistrationRights_UserId] ON [DataProcessingRegistrationRights] ([UserId]);

CREATE INDEX [IX_DataProcessingRegistrationRoleAssignmentReadModels_ParentId] ON [DataProcessingRegistrationRoleAssignmentReadModels] ([ParentId]);

CREATE INDEX [IX_RoleId] ON [DataProcessingRegistrationRoleAssignmentReadModels] ([RoleId]);

CREATE INDEX [IX_UserFullName] ON [DataProcessingRegistrationRoleAssignmentReadModels] ([UserFullName]);

CREATE INDEX [IX_UserId] ON [DataProcessingRegistrationRoleAssignmentReadModels] ([UserId]);

CREATE INDEX [IX_DataProcessingRegistrationRoles_LastChangedByUserId] ON [DataProcessingRegistrationRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingRegistrationRoles_ObjectOwnerId] ON [DataProcessingRegistrationRoles] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataProcessingRegistrationRoles] ([Uuid]);

CREATE INDEX [IX_DataProcessingRegistrations_BasisForTransferId] ON [DataProcessingRegistrations] ([BasisForTransferId]);

CREATE INDEX [IX_DataProcessingRegistrations_DataResponsible_Id] ON [DataProcessingRegistrations] ([DataResponsible_Id]);

CREATE INDEX [IX_DataProcessingRegistrations_LastChangedByUserId] ON [DataProcessingRegistrations] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProcessingRegistrations_MainContractId] ON [DataProcessingRegistrations] ([MainContractId]);

CREATE INDEX [IX_DataProcessingRegistrations_ObjectOwnerId] ON [DataProcessingRegistrations] ([ObjectOwnerId]);

CREATE INDEX [IX_DataProcessingRegistrations_ReferenceId] ON [DataProcessingRegistrations] ([ReferenceId]);

CREATE INDEX [IX_DataProcessingRegistrations_ResponsibleOrganizationUnitId] ON [DataProcessingRegistrations] ([ResponsibleOrganizationUnitId]);

CREATE INDEX [IX_Name] ON [DataProcessingRegistrations] ([Name]);

CREATE INDEX [IX_OrganizationId] ON [DataProcessingRegistrations] ([OrganizationId]);

CREATE UNIQUE INDEX [UX_DataProcessingRegistration_Uuid] ON [DataProcessingRegistrations] ([Uuid]);

CREATE UNIQUE INDEX [UX_NameUniqueToOrg] ON [DataProcessingRegistrations] ([OrganizationId], [Name]);

CREATE INDEX [IX_DataProtectionAdvisors_Cvr] ON [DataProtectionAdvisors] ([Cvr]);

CREATE INDEX [IX_DataProtectionAdvisors_LastChangedByUserId] ON [DataProtectionAdvisors] ([LastChangedByUserId]);

CREATE INDEX [IX_DataProtectionAdvisors_ObjectOwnerId] ON [DataProtectionAdvisors] ([ObjectOwnerId]);

CREATE INDEX [IX_DataProtectionAdvisors_OrganizationId] ON [DataProtectionAdvisors] ([OrganizationId]);

CREATE INDEX [IX_DataResponsibles_Cvr] ON [DataResponsibles] ([Cvr]);

CREATE INDEX [IX_DataResponsibles_LastChangedByUserId] ON [DataResponsibles] ([LastChangedByUserId]);

CREATE INDEX [IX_DataResponsibles_ObjectOwnerId] ON [DataResponsibles] ([ObjectOwnerId]);

CREATE INDEX [IX_DataResponsibles_OrganizationId] ON [DataResponsibles] ([OrganizationId]);

CREATE INDEX [IX_DataRow_DataTypeId] ON [DataRow] ([DataTypeId]);

CREATE INDEX [IX_DataRow_ItInterfaceId] ON [DataRow] ([ItInterfaceId]);

CREATE INDEX [IX_DataRow_LastChangedByUserId] ON [DataRow] ([LastChangedByUserId]);

CREATE INDEX [IX_DataRow_ObjectOwnerId] ON [DataRow] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_uuid] ON [DataRow] ([Uuid]);

CREATE INDEX [IX_DataTypes_LastChangedByUserId] ON [DataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_DataTypes_ObjectOwnerId] ON [DataTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [DataTypes] ([Uuid]);

CREATE INDEX [IX_EconomyStream_ExternPaymentForId] ON [EconomyStream] ([ExternPaymentForId]);

CREATE INDEX [IX_EconomyStream_InternPaymentForId] ON [EconomyStream] ([InternPaymentForId]);

CREATE INDEX [IX_EconomyStream_LastChangedByUserId] ON [EconomyStream] ([LastChangedByUserId]);

CREATE INDEX [IX_EconomyStream_ObjectOwnerId] ON [EconomyStream] ([ObjectOwnerId]);

CREATE INDEX [IX_EconomyStream_OrganizationUnitId] ON [EconomyStream] ([OrganizationUnitId]);

CREATE INDEX [IX_Exhibit_ItSystemId] ON [Exhibit] ([ItSystemId]);

CREATE INDEX [IX_Exhibit_LastChangedByUserId] ON [Exhibit] ([LastChangedByUserId]);

CREATE INDEX [IX_Exhibit_ObjectOwnerId] ON [Exhibit] ([ObjectOwnerId]);

CREATE INDEX [IX_ExternalReferences_DataProcessingRegistration_Id] ON [ExternalReferences] ([DataProcessingRegistration_Id]);

CREATE INDEX [IX_ExternalReferences_ItContract_Id] ON [ExternalReferences] ([ItContract_Id]);

CREATE INDEX [IX_ExternalReferences_ItSystem_Id] ON [ExternalReferences] ([ItSystem_Id]);

CREATE INDEX [IX_ExternalReferences_ItSystemUsage_Id] ON [ExternalReferences] ([ItSystemUsage_Id]);

CREATE INDEX [IX_ExternalReferences_LastChangedByUserId] ON [ExternalReferences] ([LastChangedByUserId]);

CREATE INDEX [IX_ExternalReferences_ObjectOwnerId] ON [ExternalReferences] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_ExternalReference_Uuid] ON [ExternalReferences] ([Uuid]);

CREATE INDEX [IX_HelpTexts_LastChangedByUserId] ON [HelpTexts] ([LastChangedByUserId]);

CREATE INDEX [IX_HelpTexts_ObjectOwnerId] ON [HelpTexts] ([ObjectOwnerId]);

CREATE INDEX [IX_InterfaceTypes_LastChangedByUserId] ON [InterfaceTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_InterfaceTypes_ObjectOwnerId] ON [InterfaceTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [InterfaceTypes] ([Uuid]);

CREATE INDEX [IX_ItContract_ContractTemplateId] ON [ItContract] ([ContractTemplateId]);

CREATE INDEX [IX_ItContract_ContractTypeId] ON [ItContract] ([ContractTypeId]);

CREATE INDEX [IX_ItContract_CriticalityId] ON [ItContract] ([CriticalityId]);

CREATE INDEX [IX_ItContract_LastChangedByUserId] ON [ItContract] ([LastChangedByUserId]);

CREATE INDEX [IX_ItContract_ObjectOwnerId] ON [ItContract] ([ObjectOwnerId]);

CREATE INDEX [IX_ItContract_OptionExtendId] ON [ItContract] ([OptionExtendId]);

CREATE INDEX [IX_ItContract_ParentId] ON [ItContract] ([ParentId]);

CREATE INDEX [IX_ItContract_PaymentFreqencyId] ON [ItContract] ([PaymentFreqencyId]);

CREATE INDEX [IX_ItContract_PaymentModelId] ON [ItContract] ([PaymentModelId]);

CREATE INDEX [IX_ItContract_PriceRegulationId] ON [ItContract] ([PriceRegulationId]);

CREATE INDEX [IX_ItContract_ProcurementStrategyId] ON [ItContract] ([ProcurementStrategyId]);

CREATE INDEX [IX_ItContract_PurchaseFormId] ON [ItContract] ([PurchaseFormId]);

CREATE INDEX [IX_ItContract_ReferenceId] ON [ItContract] ([ReferenceId]);

CREATE INDEX [IX_ItContract_ResponsibleOrganizationUnitId] ON [ItContract] ([ResponsibleOrganizationUnitId]);

CREATE INDEX [IX_ItContract_SupplierId] ON [ItContract] ([SupplierId]);

CREATE INDEX [IX_ItContract_TerminationDeadlineId] ON [ItContract] ([TerminationDeadlineId]);

CREATE INDEX [IX_Name] ON [ItContract] ([Name]);

CREATE INDEX [IX_OrganizationId] ON [ItContract] ([OrganizationId]);

CREATE INDEX [IX_ProcurementInitiated] ON [ItContract] ([ProcurementInitiated]);

CREATE UNIQUE INDEX [UX_Contract_Uuid] ON [ItContract] ([Uuid]);

CREATE UNIQUE INDEX [UX_NameUniqueToOrg] ON [ItContract] ([OrganizationId], [Name]);

CREATE INDEX [IX_ItContractAgreementElementTypes_ItContract_Id] ON [ItContractAgreementElementTypes] ([ItContract_Id]);

CREATE UNIQUE INDEX [IX_ItContractItSystemUsages_ItSystemUsage_Id] ON [ItContractItSystemUsages] ([ItSystemUsage_Id]) WHERE [ItSystemUsage_Id] IS NOT NULL;

CREATE INDEX [IX_ItContractItSystemUsages_ItSystemUsageId] ON [ItContractItSystemUsages] ([ItSystemUsageId]);

CREATE INDEX [IX_ItContract_Read_Dpr_Name] ON [ItContractOverviewReadModelDataProcessingAgreements] ([DataProcessingRegistrationName]);

CREATE INDEX [IX_ItContract_Read_Dpr_Uuid] ON [ItContractOverviewReadModelDataProcessingAgreements] ([DataProcessingRegistrationUuid]);

CREATE INDEX [IX_ItContractOverviewReadModelDataProcessingAgreements_ParentId] ON [ItContractOverviewReadModelDataProcessingAgreements] ([ParentId]);

CREATE INDEX [IX_ItContract_Read_System_Name] ON [ItContractOverviewReadModelItSystemUsages] ([ItSystemUsageName]);

CREATE INDEX [IX_ItContract_Read_System_Usage_Uuid] ON [ItContractOverviewReadModelItSystemUsages] ([ItSystemUsageUuid]);

CREATE INDEX [IX_ItContract_Read_System_Uuid] ON [ItContractOverviewReadModelItSystemUsages] ([ItSystemUsageSystemUuid]);

CREATE INDEX [IX_ItContractOverviewReadModelItSystemUsages_ParentId] ON [ItContractOverviewReadModelItSystemUsages] ([ParentId]);

CREATE INDEX [IX_AccumulatedAcquisitionCost] ON [ItContractOverviewReadModels] ([AccumulatedAcquisitionCost]);

CREATE INDEX [IX_AccumulatedOperationCost] ON [ItContractOverviewReadModels] ([AccumulatedOperationCost]);

CREATE INDEX [IX_AccumulatedOtherCost] ON [ItContractOverviewReadModels] ([AccumulatedOtherCost]);

CREATE INDEX [IX_Concluded] ON [ItContractOverviewReadModels] ([Concluded]);

CREATE INDEX [IX_Contract_Active] ON [ItContractOverviewReadModels] ([IsActive]);

CREATE INDEX [IX_Contract_Name] ON [ItContractOverviewReadModels] ([Name]);

CREATE INDEX [IX_CriticalityType_Id] ON [ItContractOverviewReadModels] ([CriticalityId]);

CREATE INDEX [IX_CriticalityType_Name] ON [ItContractOverviewReadModels] ([CriticalityName]);

CREATE INDEX [IX_CriticalityType_Uuid] ON [ItContractOverviewReadModels] ([CriticalityUuid]);

CREATE INDEX [IX_Duration] ON [ItContractOverviewReadModels] ([Duration]);

CREATE INDEX [IX_ExpirationDate] ON [ItContractOverviewReadModels] ([ExpirationDate]);

CREATE INDEX [IX_IrrevocableTo] ON [ItContractOverviewReadModels] ([IrrevocableTo]);

CREATE INDEX [IX_ItContractOverviewReadModels_OrganizationId] ON [ItContractOverviewReadModels] ([OrganizationId]);

CREATE INDEX [IX_ItContractOverviewReadModels_SourceEntityId] ON [ItContractOverviewReadModels] ([SourceEntityId]);

CREATE INDEX [IX_ItContractTemplateType_Id] ON [ItContractOverviewReadModels] ([ContractTemplateId]);

CREATE INDEX [IX_ItContractTemplateType_Name] ON [ItContractOverviewReadModels] ([ContractTemplateName]);

CREATE INDEX [IX_ItContractTemplateType_Uuid] ON [ItContractOverviewReadModels] ([ContractTemplateUuid]);

CREATE INDEX [IX_ItContractType_Id] ON [ItContractOverviewReadModels] ([ContractTypeId]);

CREATE INDEX [IX_ItContractType_Name] ON [ItContractOverviewReadModels] ([ContractTypeName]);

CREATE INDEX [IX_ItContractType_Uuid] ON [ItContractOverviewReadModels] ([ContractTypeUuid]);

CREATE INDEX [IX_LastEditedAtDate] ON [ItContractOverviewReadModels] ([LastEditedAtDate]);

CREATE INDEX [IX_LastEditedByUserId] ON [ItContractOverviewReadModels] ([LastEditedByUserId]);

CREATE INDEX [IX_LastEditedByUserName] ON [ItContractOverviewReadModels] ([LastEditedByUserName]);

CREATE INDEX [IX_LatestAuditDate] ON [ItContractOverviewReadModels] ([LatestAuditDate]);

CREATE INDEX [IX_NumberOfAssociatedSystemRelations] ON [ItContractOverviewReadModels] ([NumberOfAssociatedSystemRelations]);

CREATE INDEX [IX_OperationRemunerationBegunDate] ON [ItContractOverviewReadModels] ([OperationRemunerationBegunDate]);

CREATE INDEX [IX_OptionExtendType_Id] ON [ItContractOverviewReadModels] ([OptionExtendId]);

CREATE INDEX [IX_OptionExtendType_Name] ON [ItContractOverviewReadModels] ([OptionExtendName]);

CREATE INDEX [IX_OptionExtendType_Uuid] ON [ItContractOverviewReadModels] ([OptionExtendUuid]);

CREATE INDEX [IX_ParentContract_Id] ON [ItContractOverviewReadModels] ([ParentContractId]);

CREATE INDEX [IX_ParentContract_Name] ON [ItContractOverviewReadModels] ([ParentContractName]);

CREATE INDEX [IX_ParentContract_Uuid] ON [ItContractOverviewReadModels] ([ParentContractUuid]);

CREATE INDEX [IX_PaymentFreqencyType_Id] ON [ItContractOverviewReadModels] ([PaymentFrequencyId]);

CREATE INDEX [IX_PaymentFreqencyType_Name] ON [ItContractOverviewReadModels] ([PaymentFrequencyName]);

CREATE INDEX [IX_PaymentFreqencyType_Uuid] ON [ItContractOverviewReadModels] ([PaymentFrequencyUuid]);

CREATE INDEX [IX_PaymentModelType_Id] ON [ItContractOverviewReadModels] ([PaymentModelId]);

CREATE INDEX [IX_PaymentModelType_Name] ON [ItContractOverviewReadModels] ([PaymentModelName]);

CREATE INDEX [IX_PaymentModelType_Uuid] ON [ItContractOverviewReadModels] ([PaymentModelUuid]);

CREATE INDEX [IX_ProcurementInitiated] ON [ItContractOverviewReadModels] ([ProcurementInitiated]);

CREATE INDEX [IX_ProcurementPlanQuarter] ON [ItContractOverviewReadModels] ([ProcurementPlanQuarter]);

CREATE INDEX [IX_ProcurementPlanYear] ON [ItContractOverviewReadModels] ([ProcurementPlanYear]);

CREATE INDEX [IX_ProcurementStrategyType_Id] ON [ItContractOverviewReadModels] ([ProcurementStrategyId]);

CREATE INDEX [IX_ProcurementStrategyType_Name] ON [ItContractOverviewReadModels] ([ProcurementStrategyName]);

CREATE INDEX [IX_ProcurementStrategyType_Uuid] ON [ItContractOverviewReadModels] ([ProcurementStrategyUuid]);

CREATE INDEX [IX_PurchaseFormType_Id] ON [ItContractOverviewReadModels] ([PurchaseFormId]);

CREATE INDEX [IX_PurchaseFormType_Name] ON [ItContractOverviewReadModels] ([PurchaseFormName]);

CREATE INDEX [IX_PurchaseFormType_Uuid] ON [ItContractOverviewReadModels] ([PurchaseFormUuid]);

CREATE INDEX [IX_ResponsibleOrgUnitId] ON [ItContractOverviewReadModels] ([ResponsibleOrgUnitId]);

CREATE INDEX [IX_SupplierId] ON [ItContractOverviewReadModels] ([SupplierId]);

CREATE INDEX [IX_SupplierName] ON [ItContractOverviewReadModels] ([SupplierName]);

CREATE INDEX [IX_TerminatedAt] ON [ItContractOverviewReadModels] ([TerminatedAt]);

CREATE INDEX [IX_TerminationDeadlineType_Id] ON [ItContractOverviewReadModels] ([TerminationDeadlineId]);

CREATE INDEX [IX_TerminationDeadlineType_Name] ON [ItContractOverviewReadModels] ([TerminationDeadlineName]);

CREATE INDEX [IX_TerminationDeadlineType_Uuid] ON [ItContractOverviewReadModels] ([TerminationDeadlineUuid]);

CREATE INDEX [IX_FromSystemUsageId] ON [ItContractOverviewReadModelSystemRelations] ([FromSystemUsageId]);

CREATE INDEX [IX_ItContractOverviewReadModelSystemRelations_ParentId] ON [ItContractOverviewReadModelSystemRelations] ([ParentId]);

CREATE INDEX [IX_RelationId] ON [ItContractOverviewReadModelSystemRelations] ([RelationId]);

CREATE INDEX [IX_ToSystemUsageId] ON [ItContractOverviewReadModelSystemRelations] ([ToSystemUsageId]);

CREATE INDEX [IX_ItContract_Read_Role_Id] ON [ItContractOverviewRoleAssignmentReadModels] ([RoleId]);

CREATE INDEX [IX_ItContract_Read_User_Id] ON [ItContractOverviewRoleAssignmentReadModels] ([UserId]);

CREATE INDEX [IX_ItContract_Read_User_Name] ON [ItContractOverviewRoleAssignmentReadModels] ([UserFullName]);

CREATE INDEX [IX_ItContractOverviewRoleAssignmentReadModels_ParentId] ON [ItContractOverviewRoleAssignmentReadModels] ([ParentId]);

CREATE INDEX [IX_ItContractRights_LastChangedByUserId] ON [ItContractRights] ([LastChangedByUserId]);

CREATE INDEX [IX_ItContractRights_ObjectId] ON [ItContractRights] ([ObjectId]);

CREATE INDEX [IX_ItContractRights_ObjectOwnerId] ON [ItContractRights] ([ObjectOwnerId]);

CREATE INDEX [IX_ItContractRights_RoleId] ON [ItContractRights] ([RoleId]);

CREATE INDEX [IX_ItContractRights_UserId] ON [ItContractRights] ([UserId]);

CREATE INDEX [IX_ItContractRoles_LastChangedByUserId] ON [ItContractRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_ItContractRoles_ObjectOwnerId] ON [ItContractRoles] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ItContractRoles] ([Uuid]);

CREATE INDEX [IX_ItContractTemplateTypes_LastChangedByUserId] ON [ItContractTemplateTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_ItContractTemplateTypes_ObjectOwnerId] ON [ItContractTemplateTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ItContractTemplateTypes] ([Uuid]);

CREATE INDEX [IX_ItContractTypes_LastChangedByUserId] ON [ItContractTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_ItContractTypes_ObjectOwnerId] ON [ItContractTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ItContractTypes] ([Uuid]);

CREATE INDEX [IX_ItInterface_InterfaceId] ON [ItInterface] ([InterfaceId]);

CREATE INDEX [IX_ItInterface_LastChangedByUserId] ON [ItInterface] ([LastChangedByUserId]);

CREATE INDEX [IX_ItInterface_ObjectOwnerId] ON [ItInterface] ([ObjectOwnerId]);

CREATE INDEX [IX_Name] ON [ItInterface] ([Name]);

CREATE INDEX [IX_OrganizationId] ON [ItInterface] ([OrganizationId]);

CREATE INDEX [IX_Version] ON [ItInterface] ([Version]);

CREATE INDEX [UX_AccessModifier] ON [ItInterface] ([AccessModifier]);

CREATE UNIQUE INDEX [UX_ItInterface_Uuid] ON [ItInterface] ([Uuid]);

CREATE UNIQUE INDEX [UX_NameAndVersionUniqueToOrg] ON [ItInterface] ([OrganizationId], [Name], [ItInterfaceId]);

CREATE INDEX [ItSystem_IX_LegalDataProcessorName] ON [ItSystem] ([LegalDataProcessorName]);

CREATE INDEX [ItSystem_IX_LegalName] ON [ItSystem] ([LegalName]);

CREATE INDEX [IX_ItSystem_BelongsToId] ON [ItSystem] ([BelongsToId]);

CREATE INDEX [IX_ItSystem_BusinessTypeId] ON [ItSystem] ([BusinessTypeId]);

CREATE INDEX [IX_ItSystem_LastChangedByUserId] ON [ItSystem] ([LastChangedByUserId]);

CREATE INDEX [IX_ItSystem_ObjectOwnerId] ON [ItSystem] ([ObjectOwnerId]);

CREATE INDEX [IX_ItSystem_ParentId] ON [ItSystem] ([ParentId]);

CREATE INDEX [IX_ItSystem_ReferenceId] ON [ItSystem] ([ReferenceId]);

CREATE INDEX [IX_ItSystem_SensitivePersonalDataTypeId] ON [ItSystem] ([SensitivePersonalDataTypeId]);

CREATE INDEX [IX_Name] ON [ItSystem] ([Name]);

CREATE INDEX [IX_OrganizationId] ON [ItSystem] ([OrganizationId]);

CREATE INDEX [UX_AccessModifier] ON [ItSystem] ([AccessModifier]);

CREATE UNIQUE INDEX [UX_NameUniqueToOrg] ON [ItSystem] ([OrganizationId], [Name]);

CREATE UNIQUE INDEX [UX_System_Uuuid] ON [ItSystem] ([Uuid]);

CREATE INDEX [IX_ItSystemCategories_LastChangedByUserId] ON [ItSystemCategories] ([LastChangedByUserId]);

CREATE INDEX [IX_ItSystemCategories_ObjectOwnerId] ON [ItSystemCategories] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ItSystemCategories] ([Uuid]);

CREATE INDEX [IX_ItSystemRights_LastChangedByUserId] ON [ItSystemRights] ([LastChangedByUserId]);

CREATE INDEX [IX_ItSystemRights_ObjectId] ON [ItSystemRights] ([ObjectId]);

CREATE INDEX [IX_ItSystemRights_ObjectOwnerId] ON [ItSystemRights] ([ObjectOwnerId]);

CREATE INDEX [IX_ItSystemRights_RoleId] ON [ItSystemRights] ([RoleId]);

CREATE INDEX [IX_ItSystemRights_UserId] ON [ItSystemRights] ([UserId]);

CREATE INDEX [IX_ItSystemRoles_LastChangedByUserId] ON [ItSystemRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_ItSystemRoles_ObjectOwnerId] ON [ItSystemRoles] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ItSystemRoles] ([Uuid]);

CREATE INDEX [IX_TaskRefItSystems_TaskRef_Id] ON [TaskRefItSystems] ([TaskRef_Id]);

CREATE INDEX [ItSystemUsage_Index_GdprCriticality] ON [ItSystemUsage] ([GdprCriticality]);

CREATE INDEX [ItSystemUsage_Index_LifeCycleStatus] ON [ItSystemUsage] ([LifeCycleStatus]);

CREATE INDEX [ItSystemUsage_Index_LinkToDirectoryUrlName] ON [ItSystemUsage] ([LinkToDirectoryUrlName]);

CREATE INDEX [ItSystemUsage_Index_LocalCallName] ON [ItSystemUsage] ([LocalCallName]);

CREATE INDEX [ItSystemUsage_Index_LocalSystemId] ON [ItSystemUsage] ([LocalSystemId]);

CREATE INDEX [ItSystemUsage_Index_RiskSupervisionDocumentationUrlName] ON [ItSystemUsage] ([RiskSupervisionDocumentationUrlName]);

CREATE INDEX [ItSystemUsage_Index_Version] ON [ItSystemUsage] ([Version]);

CREATE INDEX [IX_ItSystemUsage_ArchiveLocationId] ON [ItSystemUsage] ([ArchiveLocationId]);

CREATE INDEX [IX_ItSystemUsage_ArchiveSupplierId] ON [ItSystemUsage] ([ArchiveSupplierId]);

CREATE INDEX [IX_ItSystemUsage_ArchiveTestLocationId] ON [ItSystemUsage] ([ArchiveTestLocationId]);

CREATE INDEX [IX_ItSystemUsage_ArchiveTypeId] ON [ItSystemUsage] ([ArchiveTypeId]);

CREATE INDEX [IX_ItSystemUsage_ItSystemCategoriesId] ON [ItSystemUsage] ([ItSystemCategoriesId]);

CREATE INDEX [IX_ItSystemUsage_ItSystemId] ON [ItSystemUsage] ([ItSystemId]);

CREATE INDEX [IX_ItSystemUsage_LastChangedByUserId] ON [ItSystemUsage] ([LastChangedByUserId]);

CREATE INDEX [IX_ItSystemUsage_ObjectOwnerId] ON [ItSystemUsage] ([ObjectOwnerId]);

CREATE INDEX [IX_ItSystemUsage_OrganizationId] ON [ItSystemUsage] ([OrganizationId]);

CREATE INDEX [IX_ItSystemUsage_ReferenceId] ON [ItSystemUsage] ([ReferenceId]);

CREATE INDEX [IX_ItSystemUsage_RegisterTypeId] ON [ItSystemUsage] ([RegisterTypeId]);

CREATE INDEX [IX_ItSystemUsage_SensitiveDataTypeId] ON [ItSystemUsage] ([SensitiveDataTypeId]);

CREATE UNIQUE INDEX [UX_ItSystemUsage_Uuid] ON [ItSystemUsage] ([Uuid]);

CREATE INDEX [IX_ItSystemUsageOrgUnitUsages_OrganizationUnitId] ON [ItSystemUsageOrgUnitUsages] ([OrganizationUnitId]);

CREATE UNIQUE INDEX [IX_ItSystemUsageOrgUnitUsages_ResponsibleItSystemUsage_Id] ON [ItSystemUsageOrgUnitUsages] ([ResponsibleItSystemUsage_Id]) WHERE [ResponsibleItSystemUsage_Id] IS NOT NULL;

CREATE INDEX [ItSystemUsageOverviewArchivePeriodReadModel_index_EndDate] ON [ItSystemUsageOverviewArchivePeriodReadModels] ([EndDate]);

CREATE INDEX [ItSystemUsageOverviewArchivePeriodReadModel_index_StartDate] ON [ItSystemUsageOverviewArchivePeriodReadModels] ([StartDate]);

CREATE INDEX [IX_ItSystemUsageOverviewArchivePeriodReadModels_ParentId] ON [ItSystemUsageOverviewArchivePeriodReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewArchivePeriodReadModel_index_DataProcessingRegistrationId] ON [ItSystemUsageOverviewDataProcessingRegistrationReadModels] ([DataProcessingRegistrationId]);

CREATE INDEX [ItSystemUsageOverviewArchivePeriodReadModel_index_DataProcessingRegistrationName] ON [ItSystemUsageOverviewDataProcessingRegistrationReadModels] ([DataProcessingRegistrationName]);

CREATE INDEX [IX_ItSystemUsageOverviewDataProcessingRegistrationReadModels_ParentId] ON [ItSystemUsageOverviewDataProcessingRegistrationReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewInterfaceReadModel_index_InterfaceId] ON [ItSystemUsageOverviewInterfaceReadModels] ([InterfaceId]);

CREATE INDEX [ItSystemUsageOverviewInterfaceReadModel_index_InterfaceName] ON [ItSystemUsageOverviewInterfaceReadModels] ([InterfaceName]);

CREATE INDEX [IX_ItSystemUsageOverviewInterfaceReadModels_ParentId] ON [ItSystemUsageOverviewInterfaceReadModels] ([ParentId]);

CREATE INDEX [ItContractId] ON [ItSystemUsageOverviewItContractReadModels] ([ItContractId]);

CREATE INDEX [ItContractNameName] ON [ItSystemUsageOverviewItContractReadModels] ([ItContractName]);

CREATE INDEX [IX_ItSystemUsageOverviewItContractReadModels_ParentId] ON [ItSystemUsageOverviewItContractReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewItSystemUsageReadModel_index_ItSystemUsageId] ON [ItSystemUsageOverviewUsedBySystemUsageReadModels] ([ItSystemUsageId]);

CREATE INDEX [ItSystemUsageOverviewItSystemUsageReadModel_index_ItSystemUsageName] ON [ItSystemUsageOverviewUsedBySystemUsageReadModels] ([ItSystemUsageName]);

CREATE INDEX [ItSystemUsageOverviewItSystemUsageReadModel_index_ItSystemUsageUuid] ON [ItSystemUsageOverviewUsedBySystemUsageReadModels] ([ItSystemUsageUuid]);

CREATE INDEX [IX_ItSystemUsageOverviewUsedBySystemUsageReadModels_ParentId] ON [ItSystemUsageOverviewUsedBySystemUsageReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ActiveAccordingToLifeCycle] ON [ItSystemUsageOverviewReadModels] ([ActiveAccordingToLifeCycle]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ActiveAccordingToValidityPeriod] ON [ItSystemUsageOverviewReadModels] ([ActiveAccordingToValidityPeriod]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ArchiveDuty] ON [ItSystemUsageOverviewReadModels] ([ArchiveDuty]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_CatalogArchiveDuty] ON [ItSystemUsageOverviewReadModels] ([CatalogArchiveDuty]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_DPIAConducted] ON [ItSystemUsageOverviewReadModels] ([DPIAConducted]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_GdprCriticality] ON [ItSystemUsageOverviewReadModels] ([GdprCriticality]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_GeneralPurpose] ON [ItSystemUsageOverviewReadModels] ([GeneralPurpose]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_HostedAt] ON [ItSystemUsageOverviewReadModels] ([HostedAt]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_IsBusinessCritical] ON [ItSystemUsageOverviewReadModels] ([IsBusinessCritical]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_IsHoldingDocument] ON [ItSystemUsageOverviewReadModels] ([IsHoldingDocument]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemBelongsToId] ON [ItSystemUsageOverviewReadModels] ([ItSystemRightsHolderId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemBelongsToName] ON [ItSystemUsageOverviewReadModels] ([ItSystemRightsHolderName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemBusinessTypeId] ON [ItSystemUsageOverviewReadModels] ([ItSystemBusinessTypeId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemBusinessTypeName] ON [ItSystemUsageOverviewReadModels] ([ItSystemBusinessTypeName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemBusinessTypeUuid] ON [ItSystemUsageOverviewReadModels] ([ItSystemBusinessTypeUuid]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemCategoriesId] ON [ItSystemUsageOverviewReadModels] ([ItSystemCategoriesId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemCategoriesName] ON [ItSystemUsageOverviewReadModels] ([ItSystemCategoriesName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemCategoriesUuid] ON [ItSystemUsageOverviewReadModels] ([ItSystemCategoriesUuid]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemDisabled] ON [ItSystemUsageOverviewReadModels] ([ItSystemDisabled]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemParentName] ON [ItSystemUsageOverviewReadModels] ([ParentItSystemName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ItSystemUuid] ON [ItSystemUsageOverviewReadModels] ([ItSystemUuid]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LastChangedById] ON [ItSystemUsageOverviewReadModels] ([LastChangedById]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LastChangedByName] ON [ItSystemUsageOverviewReadModels] ([LastChangedByName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LifeCycleStatus] ON [ItSystemUsageOverviewReadModels] ([LifeCycleStatus]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LinkToDirectoryName] ON [ItSystemUsageOverviewReadModels] ([LinkToDirectoryName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LocalCallName] ON [ItSystemUsageOverviewReadModels] ([LocalCallName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LocalReferenceTitle] ON [ItSystemUsageOverviewReadModels] ([LocalReferenceTitle]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_LocalSystemId] ON [ItSystemUsageOverviewReadModels] ([LocalSystemId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_MainContractId] ON [ItSystemUsageOverviewReadModels] ([MainContractId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_MainContractSupplierId] ON [ItSystemUsageOverviewReadModels] ([MainContractSupplierId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_MainContractSupplierName] ON [ItSystemUsageOverviewReadModels] ([MainContractSupplierName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_Name] ON [ItSystemUsageOverviewReadModels] ([SystemName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ObjectOwnerId] ON [ItSystemUsageOverviewReadModels] ([ObjectOwnerId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ObjectOwnerName] ON [ItSystemUsageOverviewReadModels] ([ObjectOwnerName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ParentItSystemUsageUuid] ON [ItSystemUsageOverviewReadModels] ([ParentItSystemUsageUuid]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ResponsibleOrganizationId] ON [ItSystemUsageOverviewReadModels] ([ResponsibleOrganizationUnitId]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ResponsibleOrganizationName] ON [ItSystemUsageOverviewReadModels] ([ResponsibleOrganizationUnitName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_ResponsibleOrganizationUuid] ON [ItSystemUsageOverviewReadModels] ([ResponsibleOrganizationUnitUuid]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_RiskSupervisionDocumentationName] ON [ItSystemUsageOverviewReadModels] ([RiskSupervisionDocumentationName]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_SystemActive] ON [ItSystemUsageOverviewReadModels] ([SystemActive]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_UserCount] ON [ItSystemUsageOverviewReadModels] ([UserCount]);

CREATE INDEX [ItSystemUsageOverviewReadModel_Index_Version] ON [ItSystemUsageOverviewReadModels] ([Version]);

CREATE INDEX [IX_Concluded] ON [ItSystemUsageOverviewReadModels] ([Concluded]);

CREATE INDEX [IX_ExpirationDate] ON [ItSystemUsageOverviewReadModels] ([ExpirationDate]);

CREATE INDEX [IX_ItSystemUsageOverviewReadModels_OrganizationId] ON [ItSystemUsageOverviewReadModels] ([OrganizationId]);

CREATE INDEX [IX_ItSystemUsageOverviewReadModels_SourceEntityId] ON [ItSystemUsageOverviewReadModels] ([SourceEntityId]);

CREATE INDEX [IX_LastChangedAt] ON [ItSystemUsageOverviewReadModels] ([LastChangedAt]);

CREATE INDEX [IX_LastWebAccessibilityCheck] ON [ItSystemUsageOverviewReadModels] ([LastWebAccessibilityCheck]);

CREATE INDEX [IX_PlannedRiskAssessmentDate] ON [ItSystemUsageOverviewReadModels] ([PlannedRiskAssessmentDate]);

CREATE INDEX [IX_RiskAssessmentDate] ON [ItSystemUsageOverviewReadModels] ([RiskAssessmentDate]);

CREATE INDEX [IX_WebAccessibilityCompliance] ON [ItSystemUsageOverviewReadModels] ([WebAccessibilityCompliance]);

CREATE INDEX [IX_ItSystemUsageOverviewRelevantOrgUnitReadModels_ParentId] ON [ItSystemUsageOverviewRelevantOrgUnitReadModels] ([ParentId]);

CREATE INDEX [IX_Name] ON [ItSystemUsageOverviewRelevantOrgUnitReadModels] ([OrganizationUnitName]);

CREATE INDEX [IX_OrgUnitId] ON [ItSystemUsageOverviewRelevantOrgUnitReadModels] ([OrganizationUnitId]);

CREATE INDEX [IX_OrgUnitUuid] ON [ItSystemUsageOverviewRelevantOrgUnitReadModels] ([OrganizationUnitUuid]);

CREATE INDEX [IX_Email] ON [ItSystemUsageOverviewRoleAssignmentReadModels] ([Email]);

CREATE INDEX [IX_ItSystemUsageOverviewRoleAssignmentReadModels_ParentId] ON [ItSystemUsageOverviewRoleAssignmentReadModels] ([ParentId]);

CREATE INDEX [IX_RoleId] ON [ItSystemUsageOverviewRoleAssignmentReadModels] ([RoleId]);

CREATE INDEX [IX_UserFullName] ON [ItSystemUsageOverviewRoleAssignmentReadModels] ([UserFullName]);

CREATE INDEX [IX_UserId] ON [ItSystemUsageOverviewRoleAssignmentReadModels] ([UserId]);

CREATE INDEX [ItSystemUsageOverviewSensitiveDataLevelReadModel_Index_SensitiveDataLevel] ON [ItSystemUsageOverviewSensitiveDataLevelReadModels] ([SensitivityDataLevel]);

CREATE INDEX [IX_ItSystemUsageOverviewSensitiveDataLevelReadModels_ParentId] ON [ItSystemUsageOverviewSensitiveDataLevelReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewTaskRefReadModel_Index_KLEId] ON [ItSystemUsageOverviewTaskRefReadModels] ([KLEId]);

CREATE INDEX [ItSystemUsageOverviewTaskRefReadModel_Index_KLEName] ON [ItSystemUsageOverviewTaskRefReadModels] ([KLEName]);

CREATE INDEX [IX_ItSystemUsageOverviewTaskRefReadModels_ParentId] ON [ItSystemUsageOverviewTaskRefReadModels] ([ParentId]);

CREATE INDEX [ItSystemUsageOverviewUsingSystemUsageReadModel_index_ItSystemUsageId] ON [ItSystemUsageOverviewUsingSystemUsageReadModels] ([ItSystemUsageId]);

CREATE INDEX [ItSystemUsageOverviewUsingSystemUsageReadModel_index_ItSystemUsageName] ON [ItSystemUsageOverviewUsingSystemUsageReadModels] ([ItSystemUsageName]);

CREATE INDEX [ItSystemUsageOverviewUsingSystemUsageReadModel_index_ItSystemUsageUuid] ON [ItSystemUsageOverviewUsingSystemUsageReadModels] ([ItSystemUsageUuid]);

CREATE INDEX [IX_ItSystemUsageOverviewUsingSystemUsageReadModels_ParentId] ON [ItSystemUsageOverviewUsingSystemUsageReadModels] ([ParentId]);

CREATE INDEX [IX_ItSystemUsagePersonalDatas_ItSystemUsageId] ON [ItSystemUsagePersonalDatas] ([ItSystemUsageId]);

CREATE INDEX [IX_ItSystemUsageSensitiveDataLevels_ItSystemUsage_Id] ON [ItSystemUsageSensitiveDataLevels] ([ItSystemUsage_Id]);

CREATE INDEX [IX_TaskRefItSystemUsages_TaskRef_Id] ON [TaskRefItSystemUsages] ([TaskRef_Id]);

CREATE INDEX [IX_KendoColumnConfigurations_KendoOrganizationalConfigurationId] ON [KendoColumnConfigurations] ([KendoOrganizationalConfigurationId]);

CREATE INDEX [IX_KendoOrganizationalConfigurations_LastChangedByUserId] ON [KendoOrganizationalConfigurations] ([LastChangedByUserId]);

CREATE INDEX [IX_KendoOrganizationalConfigurations_ObjectOwnerId] ON [KendoOrganizationalConfigurations] ([ObjectOwnerId]);

CREATE INDEX [IX_KendoOrganizationalConfigurations_OrganizationId] ON [KendoOrganizationalConfigurations] ([OrganizationId]);

CREATE INDEX [KendoOrganizationalConfiguration_OverviewType] ON [KendoOrganizationalConfigurations] ([OverviewType]);

CREATE INDEX [IX_KLEUpdateHistoryItems_LastChangedByUserId] ON [KLEUpdateHistoryItems] ([LastChangedByUserId]);

CREATE INDEX [IX_KLEUpdateHistoryItems_ObjectOwnerId] ON [KLEUpdateHistoryItems] ([ObjectOwnerId]);

CREATE INDEX [IX_EventType_OccurredAt_EntityType_EventType] ON [LifeCycleTrackingEvents] ([EventType], [OccurredAtUtc], [EntityType]);

CREATE INDEX [IX_LifeCycleTrackingEvents_EntityUuid] ON [LifeCycleTrackingEvents] ([EntityUuid]);

CREATE INDEX [IX_LifeCycleTrackingEvents_OptionalOrganizationReferenceId] ON [LifeCycleTrackingEvents] ([OptionalOrganizationReferenceId]);

CREATE INDEX [IX_LifeCycleTrackingEvents_OptionalRightsHolderOrganizationId] ON [LifeCycleTrackingEvents] ([OptionalRightsHolderOrganizationId]);

CREATE INDEX [IX_LifeCycleTrackingEvents_UserId] ON [LifeCycleTrackingEvents] ([UserId]);

CREATE INDEX [IX_Org_AccessModifier_EventType_OccurredAt_EntityType] ON [LifeCycleTrackingEvents] ([OptionalOrganizationReferenceId], [OptionalAccessModifier], [EventType], [OccurredAtUtc], [EntityType]);

CREATE INDEX [IX_Org_EventType_OccurredAt_EntityType] ON [LifeCycleTrackingEvents] ([OptionalOrganizationReferenceId], [EventType], [OccurredAtUtc], [EntityType]);

CREATE INDEX [IX_RightsHolder_EventType_OccurredAt_EntityType] ON [LifeCycleTrackingEvents] ([OptionalRightsHolderOrganizationId], [EventType], [OccurredAtUtc], [EntityType]);

CREATE INDEX [IX_RightsHolder_Org_EventType_OccurredAt_EntityType] ON [LifeCycleTrackingEvents] ([OptionalRightsHolderOrganizationId], [OptionalOrganizationReferenceId], [EventType], [OccurredAtUtc], [EntityType]);

CREATE INDEX [IX_LocalAgreementElementTypes_LastChangedByUserId] ON [LocalAgreementElementTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalAgreementElementTypes_ObjectOwnerId] ON [LocalAgreementElementTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalAgreementElementTypes_OrganizationId] ON [LocalAgreementElementTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalArchiveLocations_LastChangedByUserId] ON [LocalArchiveLocations] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalArchiveLocations_ObjectOwnerId] ON [LocalArchiveLocations] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalArchiveLocations_OrganizationId] ON [LocalArchiveLocations] ([OrganizationId]);

CREATE INDEX [IX_LocalArchiveTestLocations_LastChangedByUserId] ON [LocalArchiveTestLocations] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalArchiveTestLocations_ObjectOwnerId] ON [LocalArchiveTestLocations] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalArchiveTestLocations_OrganizationId] ON [LocalArchiveTestLocations] ([OrganizationId]);

CREATE INDEX [IX_LocalArchiveTypes_LastChangedByUserId] ON [LocalArchiveTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalArchiveTypes_ObjectOwnerId] ON [LocalArchiveTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalArchiveTypes_OrganizationId] ON [LocalArchiveTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalBusinessTypes_LastChangedByUserId] ON [LocalBusinessTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalBusinessTypes_ObjectOwnerId] ON [LocalBusinessTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalBusinessTypes_OrganizationId] ON [LocalBusinessTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalCriticalityTypes_LastChangedByUserId] ON [LocalCriticalityTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalCriticalityTypes_ObjectOwnerId] ON [LocalCriticalityTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalCriticalityTypes_OrganizationId] ON [LocalCriticalityTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalDataProcessingBasisForTransferOptions_LastChangedByUserId] ON [LocalDataProcessingBasisForTransferOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataProcessingBasisForTransferOptions_ObjectOwnerId] ON [LocalDataProcessingBasisForTransferOptions] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataProcessingBasisForTransferOptions_OrganizationId] ON [LocalDataProcessingBasisForTransferOptions] ([OrganizationId]);

CREATE INDEX [IX_LocalDataProcessingCountryOptions_LastChangedByUserId] ON [LocalDataProcessingCountryOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataProcessingCountryOptions_ObjectOwnerId] ON [LocalDataProcessingCountryOptions] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataProcessingCountryOptions_OrganizationId] ON [LocalDataProcessingCountryOptions] ([OrganizationId]);

CREATE INDEX [IX_LocalDataProcessingDataResponsibleOptions_LastChangedByUserId] ON [LocalDataProcessingDataResponsibleOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataProcessingDataResponsibleOptions_ObjectOwnerId] ON [LocalDataProcessingDataResponsibleOptions] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataProcessingDataResponsibleOptions_OrganizationId] ON [LocalDataProcessingDataResponsibleOptions] ([OrganizationId]);

CREATE INDEX [IX_LocalDataProcessingOversightOptions_LastChangedByUserId] ON [LocalDataProcessingOversightOptions] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataProcessingOversightOptions_ObjectOwnerId] ON [LocalDataProcessingOversightOptions] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataProcessingOversightOptions_OrganizationId] ON [LocalDataProcessingOversightOptions] ([OrganizationId]);

CREATE INDEX [IX_LocalDataProcessingRegistrationRoles_LastChangedByUserId] ON [LocalDataProcessingRegistrationRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataProcessingRegistrationRoles_ObjectOwnerId] ON [LocalDataProcessingRegistrationRoles] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataProcessingRegistrationRoles_OrganizationId] ON [LocalDataProcessingRegistrationRoles] ([OrganizationId]);

CREATE INDEX [IX_LocalDataTypes_LastChangedByUserId] ON [LocalDataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalDataTypes_ObjectOwnerId] ON [LocalDataTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalDataTypes_OrganizationId] ON [LocalDataTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalRelationFrequencyTypes_LastChangedByUserId] ON [LocalRelationFrequencyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalRelationFrequencyTypes_ObjectOwnerId] ON [LocalRelationFrequencyTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalRelationFrequencyTypes_OrganizationId] ON [LocalRelationFrequencyTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalInterfaceTypes_LastChangedByUserId] ON [LocalInterfaceTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalInterfaceTypes_ObjectOwnerId] ON [LocalInterfaceTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalInterfaceTypes_OrganizationId] ON [LocalInterfaceTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalItContractRoles_LastChangedByUserId] ON [LocalItContractRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalItContractRoles_ObjectOwnerId] ON [LocalItContractRoles] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalItContractRoles_OrganizationId] ON [LocalItContractRoles] ([OrganizationId]);

CREATE INDEX [IX_LocalItContractTemplateTypes_LastChangedByUserId] ON [LocalItContractTemplateTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalItContractTemplateTypes_ObjectOwnerId] ON [LocalItContractTemplateTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalItContractTemplateTypes_OrganizationId] ON [LocalItContractTemplateTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalItContractTypes_LastChangedByUserId] ON [LocalItContractTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalItContractTypes_ObjectOwnerId] ON [LocalItContractTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalItContractTypes_OrganizationId] ON [LocalItContractTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalItSystemCategories_LastChangedByUserId] ON [LocalItSystemCategories] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalItSystemCategories_ObjectOwnerId] ON [LocalItSystemCategories] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalItSystemCategories_OrganizationId] ON [LocalItSystemCategories] ([OrganizationId]);

CREATE INDEX [IX_LocalItSystemRoles_LastChangedByUserId] ON [LocalItSystemRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalItSystemRoles_ObjectOwnerId] ON [LocalItSystemRoles] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalItSystemRoles_OrganizationId] ON [LocalItSystemRoles] ([OrganizationId]);

CREATE INDEX [IX_LocalOptionExtendTypes_LastChangedByUserId] ON [LocalOptionExtendTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalOptionExtendTypes_ObjectOwnerId] ON [LocalOptionExtendTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalOptionExtendTypes_OrganizationId] ON [LocalOptionExtendTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalOrganizationUnitRoles_LastChangedByUserId] ON [LocalOrganizationUnitRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalOrganizationUnitRoles_ObjectOwnerId] ON [LocalOrganizationUnitRoles] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalOrganizationUnitRoles_OrganizationId] ON [LocalOrganizationUnitRoles] ([OrganizationId]);

CREATE INDEX [IX_LocalPaymentFreqencyTypes_LastChangedByUserId] ON [LocalPaymentFreqencyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalPaymentFreqencyTypes_ObjectOwnerId] ON [LocalPaymentFreqencyTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalPaymentFreqencyTypes_OrganizationId] ON [LocalPaymentFreqencyTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalPaymentModelTypes_LastChangedByUserId] ON [LocalPaymentModelTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalPaymentModelTypes_ObjectOwnerId] ON [LocalPaymentModelTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalPaymentModelTypes_OrganizationId] ON [LocalPaymentModelTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalPriceRegulationTypes_LastChangedByUserId] ON [LocalPriceRegulationTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalPriceRegulationTypes_ObjectOwnerId] ON [LocalPriceRegulationTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalPriceRegulationTypes_OrganizationId] ON [LocalPriceRegulationTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalProcurementStrategyTypes_LastChangedByUserId] ON [LocalProcurementStrategyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalProcurementStrategyTypes_ObjectOwnerId] ON [LocalProcurementStrategyTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalProcurementStrategyTypes_OrganizationId] ON [LocalProcurementStrategyTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalPurchaseFormTypes_LastChangedByUserId] ON [LocalPurchaseFormTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalPurchaseFormTypes_ObjectOwnerId] ON [LocalPurchaseFormTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalPurchaseFormTypes_OrganizationId] ON [LocalPurchaseFormTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalRegisterTypes_LastChangedByUserId] ON [LocalRegisterTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalRegisterTypes_ObjectOwnerId] ON [LocalRegisterTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalRegisterTypes_OrganizationId] ON [LocalRegisterTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalSensitiveDataTypes_LastChangedByUserId] ON [LocalSensitiveDataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalSensitiveDataTypes_ObjectOwnerId] ON [LocalSensitiveDataTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalSensitiveDataTypes_OrganizationId] ON [LocalSensitiveDataTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalSensitivePersonalDataTypes_LastChangedByUserId] ON [LocalSensitivePersonalDataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalSensitivePersonalDataTypes_ObjectOwnerId] ON [LocalSensitivePersonalDataTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalSensitivePersonalDataTypes_OrganizationId] ON [LocalSensitivePersonalDataTypes] ([OrganizationId]);

CREATE INDEX [IX_LocalTerminationDeadlineTypes_LastChangedByUserId] ON [LocalTerminationDeadlineTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_LocalTerminationDeadlineTypes_ObjectOwnerId] ON [LocalTerminationDeadlineTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_LocalTerminationDeadlineTypes_OrganizationId] ON [LocalTerminationDeadlineTypes] ([OrganizationId]);

CREATE INDEX [IX_OptionExtendTypes_LastChangedByUserId] ON [OptionExtendTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_OptionExtendTypes_ObjectOwnerId] ON [OptionExtendTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [OptionExtendTypes] ([Uuid]);

CREATE INDEX [IX_DEFAULT_ORG] ON [Organization] ([IsDefaultOrganization]);

CREATE UNIQUE INDEX [IX_Organization_ContactPerson_Id] ON [Organization] ([ContactPerson_Id]) WHERE [ContactPerson_Id] IS NOT NULL;

CREATE INDEX [IX_Organization_Cvr] ON [Organization] ([Cvr]);

CREATE INDEX [IX_Organization_ForeignCountryCodeId] ON [Organization] ([ForeignCountryCodeId]);

CREATE INDEX [IX_Organization_LastChangedByUserId] ON [Organization] ([LastChangedByUserId]);

CREATE INDEX [IX_Organization_Name] ON [Organization] ([Name]);

CREATE INDEX [IX_Organization_ObjectOwnerId] ON [Organization] ([ObjectOwnerId]);

CREATE INDEX [IX_Organization_TypeId] ON [Organization] ([TypeId]);

CREATE INDEX [UX_AccessModifier] ON [Organization] ([AccessModifier]);

CREATE UNIQUE INDEX [UX_Organization_UUID] ON [Organization] ([Uuid]);

CREATE INDEX [IX_OrganizationRights_DefaultOrgUnitId] ON [OrganizationRights] ([DefaultOrgUnitId]);

CREATE INDEX [IX_OrganizationRights_LastChangedByUserId] ON [OrganizationRights] ([LastChangedByUserId]);

CREATE INDEX [IX_OrganizationRights_ObjectOwnerId] ON [OrganizationRights] ([ObjectOwnerId]);

CREATE INDEX [IX_OrganizationRights_OrganizationId] ON [OrganizationRights] ([OrganizationId]);

CREATE INDEX [IX_OrganizationRights_UserId] ON [OrganizationRights] ([UserId]);

CREATE INDEX [IX_OrganizationSuppliers_OrganizationId] ON [OrganizationSuppliers] ([OrganizationId]);

CREATE INDEX [IX_OrganizationUnit_LastChangedByUserId] ON [OrganizationUnit] ([LastChangedByUserId]);

CREATE INDEX [IX_OrganizationUnit_ObjectOwnerId] ON [OrganizationUnit] ([ObjectOwnerId]);

CREATE INDEX [IX_OrganizationUnit_Origin] ON [OrganizationUnit] ([Origin]);

CREATE INDEX [IX_OrganizationUnit_ParentId] ON [OrganizationUnit] ([ParentId]);

CREATE INDEX [IX_OrganizationUnit_UUID] ON [OrganizationUnit] ([ExternalOriginUuid]);

CREATE UNIQUE INDEX [UX_LocalId] ON [OrganizationUnit] ([OrganizationId], [LocalId]) WHERE [LocalId] IS NOT NULL;

CREATE UNIQUE INDEX [UX_OrganizationUnit_UUID] ON [OrganizationUnit] ([Uuid]);

CREATE INDEX [IX_OrganizationUnitRights_LastChangedByUserId] ON [OrganizationUnitRights] ([LastChangedByUserId]);

CREATE INDEX [IX_OrganizationUnitRights_ObjectId] ON [OrganizationUnitRights] ([ObjectId]);

CREATE INDEX [IX_OrganizationUnitRights_ObjectOwnerId] ON [OrganizationUnitRights] ([ObjectOwnerId]);

CREATE INDEX [IX_OrganizationUnitRights_RoleId] ON [OrganizationUnitRights] ([RoleId]);

CREATE INDEX [IX_OrganizationUnitRights_UserId] ON [OrganizationUnitRights] ([UserId]);

CREATE INDEX [IX_OrganizationUnitRoles_LastChangedByUserId] ON [OrganizationUnitRoles] ([LastChangedByUserId]);

CREATE INDEX [IX_OrganizationUnitRoles_ObjectOwnerId] ON [OrganizationUnitRoles] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [OrganizationUnitRoles] ([Uuid]);

CREATE INDEX [IX_PasswordResetRequest_LastChangedByUserId] ON [PasswordResetRequest] ([LastChangedByUserId]);

CREATE INDEX [IX_PasswordResetRequest_ObjectOwnerId] ON [PasswordResetRequest] ([ObjectOwnerId]);

CREATE INDEX [IX_PasswordResetRequest_UserId] ON [PasswordResetRequest] ([UserId]);

CREATE INDEX [IX_PaymentFreqencyTypes_LastChangedByUserId] ON [PaymentFreqencyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_PaymentFreqencyTypes_ObjectOwnerId] ON [PaymentFreqencyTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [PaymentFreqencyTypes] ([Uuid]);

CREATE INDEX [IX_PaymentModelTypes_LastChangedByUserId] ON [PaymentModelTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_PaymentModelTypes_ObjectOwnerId] ON [PaymentModelTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [PaymentModelTypes] ([Uuid]);

CREATE INDEX [IX_Category] ON [PendingReadModelUpdates] ([Category]);

CREATE INDEX [IX_CreatedAt] ON [PendingReadModelUpdates] ([CreatedAt]);

CREATE INDEX [IX_SourceId] ON [PendingReadModelUpdates] ([SourceId]);

CREATE INDEX [IX_PriceRegulationTypes_LastChangedByUserId] ON [PriceRegulationTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_PriceRegulationTypes_ObjectOwnerId] ON [PriceRegulationTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [PriceRegulationTypes] ([Uuid]);

CREATE INDEX [IX_ProcurementStrategyTypes_LastChangedByUserId] ON [ProcurementStrategyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_ProcurementStrategyTypes_ObjectOwnerId] ON [ProcurementStrategyTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [ProcurementStrategyTypes] ([Uuid]);

CREATE INDEX [IX_PublicMessages_LastChangedByUserId] ON [PublicMessages] ([LastChangedByUserId]);

CREATE INDEX [IX_PublicMessages_ObjectOwnerId] ON [PublicMessages] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_PublicMessage_Uuid] ON [PublicMessages] ([Uuid]);

CREATE INDEX [IX_PurchaseFormTypes_LastChangedByUserId] ON [PurchaseFormTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_PurchaseFormTypes_ObjectOwnerId] ON [PurchaseFormTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [PurchaseFormTypes] ([Uuid]);

CREATE INDEX [IX_RegisterTypes_LastChangedByUserId] ON [RegisterTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_RegisterTypes_ObjectOwnerId] ON [RegisterTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_RelationFrequencyTypes_LastChangedByUserId] ON [RelationFrequencyTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_RelationFrequencyTypes_ObjectOwnerId] ON [RelationFrequencyTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [RelationFrequencyTypes] ([Uuid]);

CREATE INDEX [IX_SensitiveDataTypes_LastChangedByUserId] ON [SensitiveDataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_SensitiveDataTypes_ObjectOwnerId] ON [SensitiveDataTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [SensitiveDataTypes] ([Uuid]);

CREATE INDEX [IX_SensitivePersonalDataTypes_LastChangedByUserId] ON [SensitivePersonalDataTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_SensitivePersonalDataTypes_ObjectOwnerId] ON [SensitivePersonalDataTypes] ([ObjectOwnerId]);

CREATE INDEX [IX_StsOrganizationIdentities_Organization_Id] ON [StsOrganizationIdentities] ([Organization_Id]);

CREATE UNIQUE INDEX [UX_ExternalUuid] ON [StsOrganizationIdentities] ([ExternalUuid]);

CREATE INDEX [IX_SsoUserIdentities_User_Id] ON [SsoUserIdentities] ([User_Id]);

CREATE UNIQUE INDEX [UX_ExternalUuid] ON [SsoUserIdentities] ([ExternalUuid]);

CREATE INDEX [IX_ChangeLogName] ON [StsOrganizationChangeLogs] ([ResponsibleUserId]);

CREATE INDEX [IX_ChangeLogResponsibleType] ON [StsOrganizationChangeLogs] ([ResponsibleType]);

CREATE INDEX [IX_LogTime] ON [StsOrganizationChangeLogs] ([LogTime]);

CREATE INDEX [IX_StsOrganizationChangeLogs_LastChangedByUserId] ON [StsOrganizationChangeLogs] ([LastChangedByUserId]);

CREATE INDEX [IX_StsOrganizationChangeLogs_ObjectOwnerId] ON [StsOrganizationChangeLogs] ([ObjectOwnerId]);

CREATE INDEX [IX_StsOrganizationChangeLogs_StsOrganizationConnectionId] ON [StsOrganizationChangeLogs] ([StsOrganizationConnectionId]);

CREATE INDEX [IX_Connected] ON [StsOrganizationConnections] ([Connected]);

CREATE INDEX [IX_DateOfLatestCheckBySubscription] ON [StsOrganizationConnections] ([DateOfLatestCheckBySubscription]);

CREATE INDEX [IX_Required] ON [StsOrganizationConnections] ([SubscribeToUpdates]);

CREATE INDEX [IX_StsOrganizationConnections_LastChangedByUserId] ON [StsOrganizationConnections] ([LastChangedByUserId]);

CREATE INDEX [IX_StsOrganizationConnections_ObjectOwnerId] ON [StsOrganizationConnections] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [IX_StsOrganizationConnections_OrganizationId] ON [StsOrganizationConnections] ([OrganizationId]);

CREATE INDEX [IX_StsOrganizationConsequenceLogs_ChangeLogId] ON [StsOrganizationConsequenceLogs] ([ChangeLogId]);

CREATE INDEX [IX_StsOrganizationConsequenceLogs_LastChangedByUserId] ON [StsOrganizationConsequenceLogs] ([LastChangedByUserId]);

CREATE INDEX [IX_StsOrganizationConsequenceLogs_ObjectOwnerId] ON [StsOrganizationConsequenceLogs] ([ObjectOwnerId]);

CREATE INDEX [IX_StsOrganizationConsequenceType] ON [StsOrganizationConsequenceLogs] ([Type]);

CREATE INDEX [IX_StsOrganizationConsequenceUuid] ON [StsOrganizationConsequenceLogs] ([ExternalUnitUuid]);

CREATE INDEX [IX_SubDataProcessors_DataProcessingRegistrationId] ON [SubDataProcessors] ([DataProcessingRegistrationId]);

CREATE INDEX [IX_SubDataProcessors_InsecureCountryId] ON [SubDataProcessors] ([InsecureCountryId]);

CREATE INDEX [IX_SubDataProcessors_SubDataProcessorBasisForTransferId] ON [SubDataProcessors] ([SubDataProcessorBasisForTransferId]);

CREATE INDEX [IX_SystemRelations_AssociatedContractId] ON [SystemRelations] ([AssociatedContractId]);

CREATE INDEX [IX_SystemRelations_FromSystemUsageId] ON [SystemRelations] ([FromSystemUsageId]);

CREATE INDEX [IX_SystemRelations_LastChangedByUserId] ON [SystemRelations] ([LastChangedByUserId]);

CREATE INDEX [IX_SystemRelations_ObjectOwnerId] ON [SystemRelations] ([ObjectOwnerId]);

CREATE INDEX [IX_SystemRelations_RelationInterfaceId] ON [SystemRelations] ([RelationInterfaceId]);

CREATE INDEX [IX_SystemRelations_ToSystemUsageId] ON [SystemRelations] ([ToSystemUsageId]);

CREATE INDEX [IX_SystemRelations_UsageFrequencyId] ON [SystemRelations] ([UsageFrequencyId]);

CREATE INDEX [IX_TaskRef_LastChangedByUserId] ON [TaskRef] ([LastChangedByUserId]);

CREATE INDEX [IX_TaskRef_ObjectOwnerId] ON [TaskRef] ([ObjectOwnerId]);

CREATE INDEX [IX_TaskRef_OwnedByOrganizationUnitId] ON [TaskRef] ([OwnedByOrganizationUnitId]);

CREATE INDEX [IX_TaskRef_ParentId] ON [TaskRef] ([ParentId]);

CREATE UNIQUE INDEX [UX_TaskKey] ON [TaskRef] ([TaskKey]) WHERE [TaskKey] IS NOT NULL;

CREATE UNIQUE INDEX [UX_TaskRef_Uuid] ON [TaskRef] ([Uuid]);

CREATE INDEX [IX_TaskRefItSystemUsageOptOut_TaskRef_Id] ON [TaskRefItSystemUsageOptOut] ([TaskRef_Id]);

CREATE INDEX [IX_TerminationDeadlineTypes_LastChangedByUserId] ON [TerminationDeadlineTypes] ([LastChangedByUserId]);

CREATE INDEX [IX_TerminationDeadlineTypes_ObjectOwnerId] ON [TerminationDeadlineTypes] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_Option_Uuid] ON [TerminationDeadlineTypes] ([Uuid]);

CREATE INDEX [IX_Text_LastChangedByUserId] ON [Text] ([LastChangedByUserId]);

CREATE INDEX [IX_Text_ObjectOwnerId] ON [Text] ([ObjectOwnerId]);

CREATE INDEX [IX_UIModuleCustomizations_LastChangedByUserId] ON [UIModuleCustomizations] ([LastChangedByUserId]);

CREATE INDEX [IX_UIModuleCustomizations_ObjectOwnerId] ON [UIModuleCustomizations] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [UX_OrganizationId_UIModuleCustomization_Module] ON [UIModuleCustomizations] ([OrganizationId], [Module]);

CREATE INDEX [IX_User_IsSystemIntegrator] ON [User] ([IsSystemIntegrator]);

CREATE INDEX [IX_User_LastChangedByUserId] ON [User] ([LastChangedByUserId]);

CREATE INDEX [IX_User_ObjectOwnerId] ON [User] ([ObjectOwnerId]);

CREATE UNIQUE INDEX [User_Index_Email] ON [User] ([Email]);

CREATE INDEX [User_Index_Name] ON [User] ([Name], [LastName]);

CREATE UNIQUE INDEX [UX_User_Uuid] ON [User] ([Uuid]);

CREATE INDEX [IX_UserNotifications_DataProcessingRegistration_Id] ON [UserNotifications] ([DataProcessingRegistration_Id]);

CREATE INDEX [IX_UserNotifications_Itcontract_Id] ON [UserNotifications] ([Itcontract_Id]);

CREATE INDEX [IX_UserNotifications_ItSystemUsage_Id] ON [UserNotifications] ([ItSystemUsage_Id]);

CREATE INDEX [IX_UserNotifications_LastChangedByUserId] ON [UserNotifications] ([LastChangedByUserId]);

CREATE INDEX [IX_UserNotifications_NotificationRecipientId] ON [UserNotifications] ([NotificationRecipientId]);

CREATE INDEX [IX_UserNotifications_ObjectOwnerId] ON [UserNotifications] ([ObjectOwnerId]);

CREATE INDEX [IX_UserNotifications_OrganizationId] ON [UserNotifications] ([OrganizationId]);

ALTER TABLE [ArchivePeriod] ADD CONSTRAINT [FK_ArchivePeriod_ItSystemUsage_ItSystemUsageId] FOREIGN KEY ([ItSystemUsageId]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE;

ALTER TABLE [BrokenLinkInExternalReferences] ADD CONSTRAINT [FK_BrokenLinkInExternalReferences_ExternalReferences_BrokenReferenceOrigin_Id] FOREIGN KEY ([BrokenReferenceOrigin_Id]) REFERENCES [ExternalReferences] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationDataProcessingCountryOptions] ADD CONSTRAINT [FK_DataProcessingRegistrationDataProcessingCountryOptions_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationDataProcessingOversightOptions] ADD CONSTRAINT [FK_DataProcessingRegistrationDataProcessingOversightOptions_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ItContractDataProcessingRegistrations] ADD CONSTRAINT [FK_ItContractDataProcessingRegistrations_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ItContractDataProcessingRegistrations] ADD CONSTRAINT [FK_ItContractDataProcessingRegistrations_ItContract_ItContract_Id] FOREIGN KEY ([ItContract_Id]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationItSystemUsages] ADD CONSTRAINT [FK_DataProcessingRegistrationItSystemUsages_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationItSystemUsages] ADD CONSTRAINT [FK_DataProcessingRegistrationItSystemUsages_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationOrganizations] ADD CONSTRAINT [FK_DataProcessingRegistrationOrganizations_DataProcessingRegistrations_DataProcessingRegistration_Id] FOREIGN KEY ([DataProcessingRegistration_Id]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationOversightDates] ADD CONSTRAINT [FK_DataProcessingRegistrationOversightDates_DataProcessingRegistrations_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrationReadModels] ADD CONSTRAINT [FK_DataProcessingRegistrationReadModels_DataProcessingRegistrations_SourceEntityId] FOREIGN KEY ([SourceEntityId]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [DataProcessingRegistrationRights] ADD CONSTRAINT [FK_DataProcessingRegistrationRights_DataProcessingRegistrations_ObjectId] FOREIGN KEY ([ObjectId]) REFERENCES [DataProcessingRegistrations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [DataProcessingRegistrations] ADD CONSTRAINT [FK_DataProcessingRegistrations_ExternalReferences_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [ExternalReferences] ([Id]);

ALTER TABLE [DataProcessingRegistrations] ADD CONSTRAINT [FK_DataProcessingRegistrations_ItContract_MainContractId] FOREIGN KEY ([MainContractId]) REFERENCES [ItContract] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [EconomyStream] ADD CONSTRAINT [FK_EconomyStream_ItContract_ExternPaymentForId] FOREIGN KEY ([ExternPaymentForId]) REFERENCES [ItContract] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [EconomyStream] ADD CONSTRAINT [FK_EconomyStream_ItContract_InternPaymentForId] FOREIGN KEY ([InternPaymentForId]) REFERENCES [ItContract] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Exhibit] ADD CONSTRAINT [FK_Exhibit_ItSystem_ItSystemId] FOREIGN KEY ([ItSystemId]) REFERENCES [ItSystem] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ExternalReferences] ADD CONSTRAINT [FK_ExternalReferences_ItContract_ItContract_Id] FOREIGN KEY ([ItContract_Id]) REFERENCES [ItContract] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ExternalReferences] ADD CONSTRAINT [FK_ExternalReferences_ItSystemUsage_ItSystemUsage_Id] FOREIGN KEY ([ItSystemUsage_Id]) REFERENCES [ItSystemUsage] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ExternalReferences] ADD CONSTRAINT [FK_ExternalReferences_ItSystem_ItSystem_Id] FOREIGN KEY ([ItSystem_Id]) REFERENCES [ItSystem] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260413095837_InitialBaseline', N'10.0.5');

COMMIT;
GO

