BEGIN 
UPDATE [ItSystemUsageOverviewReadModels]
SET [ItSystemUsageOverviewReadModels].[DPIAConducted] = [ItSystemUsage].[DPIA]
FROM [ItSystemUsageOverviewReadModels]
INNER JOIN [ItSystemUsage]
ON [ItSystemUsageOverviewReadModels].[SourceEntityUuid] = [ItSystemUsage].[Uuid]
END