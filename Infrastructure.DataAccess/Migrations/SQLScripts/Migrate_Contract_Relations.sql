﻿/*
User story reference: 
    https://os2web.atlassian.net/browse/KITOSUDV-499
 
Content:
 Migrates existing contract relations according to the following logic

 If an ItInterfaceUse points to an ItInterface for which the exposing system IS IN USE in the organization 
    AND that system is not the same as the using system, 
    THEN the interface usage is migrated to a system relation 
    between the "Using" system usage and the local usage of the exposing system. If data is migrated, the old data is marked with a reference to the new rows in the SystemRelations table.
*/

BEGIN
    -- Build an operation context - the from-to context needed to migrate the relation
    DECLARE @MigrationContext TABLE
    (
        OrganizationId int,
        FromSystemUsageId int,
        ToSystemUsageId int,
        UsingSystemId int,
        ExposingSystemId int,
        ItInterfaceId int,
        ItContractId int,
        ObjectOwnerId int,
        LastChanged datetime,
        LastChangedByUserId int,
        MigratedToUuid uniqueidentifier
    );
    
    -- Scrape candidate data from
    INSERT INTO @MigrationContext
        SELECT
            FROM_USAGE.OrganizationId	AS OrganizationId,
            RELATION.ItSystemUsageId	AS FromSystemUsageId,
            TO_USAGE.Id					AS ToSystemUsageId,
            FROM_USAGE.ItSystemId       AS UsingSystemId,
            EXH.ItSystemId				AS ExposingSystemId,
            RELATION.ItInterfaceId      AS ItInterfaceId,
            RELATION.ItContractId		AS ItContractId,
            FROM_USAGE.ObjectOwnerId	AS ObjectOwnerId,
            GETUTCDATE()				AS LastChanged,
            FROM_USAGE.ObjectOwnerId	AS LastChangedByUserId,
            NEWID()						AS MigratedToUuid
        FROM ItInterfaceUsage RELATION
		INNER JOIN ItSystemUsage FROM_USAGE ON
			FROM_USAGE.Id = RELATION.ItSystemUsageId
		INNER JOIN Exhibit EXH ON
			EXH.Id = RELATION.ItInterfaceId
		INNER JOIN ItSystemUsage TO_USAGE ON
			TO_USAGE.ItSystemId = EXH.ItSystemId AND 
			TO_USAGE.OrganizationId = FROM_USAGE.OrganizationId
        WHERE ItContractId IS NOT NULL -- if null, the user has actively deleted the relation from the UI.. an operation which only "nulls" the contract on the created relation in the backend

    -- Delete all circular references, which are invalid in the systemrelations model but possible in the old model due to lack of validation rules
    DELETE FROM @MigrationContext
    WHERE FromSystemUsageId = ToSystemUsageId;

    -- Insert system relations
    INSERT INTO SystemRelations
        SELECT
           FromSystemUsageId AS FromSystemUsageId
           ,ToSystemUsageId AS ToSystemUsageId
           ,NULL AS Description
           ,ObjectOwnerId AS ObjectOwnerId
           ,LastChanged AS LastChanged
           ,LastChangedByUserId AS LastChangedByUserId
           ,ItContractId AS AssociatedContractId
           ,ItInterfaceId AS RelationInterfaceId
           ,NULL AS UsageFrequencyId
           ,NULL AS Reference
           ,MigratedToUuid AS Uuid
    FROM @MigrationContext;

    -- Patch a reference to where the data was migrated to in the original table
    MERGE INTO ItInterfaceUsage
        USING @MigrationContext
            ON  [@MigrationContext].FromSystemUsageId = ItInterfaceUsage.ItSystemUsageId AND
                [@MigrationContext].UsingSystemId = ItInterfaceUsage.ItSystemId AND
                [@MigrationContext].ItInterfaceId = ItInterfaceUsage.ItInterfaceId
    WHEN MATCHED THEN
        UPDATE
            SET [ItInterfaceUsage].MigratedToUuid = [@MigrationContext].MigratedToUuid;
END