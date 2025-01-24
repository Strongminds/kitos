BEGIN 
    UPDATE [ItSystemUsageOverviewReadModels]
    SET [ItSystemUsageOverviewReadModels].[IsBusinessCritical] = [ItSystemUsage].[isBusinessCritical]
    FROM [ItSystemUsageOverviewReadModels]
        INNER JOIN [ItSystemUsage]
            ON [ItSystemUsageOverviewReadModels].[SourceEntityUuid] = [ItSystemUsage].[Uuid]
END