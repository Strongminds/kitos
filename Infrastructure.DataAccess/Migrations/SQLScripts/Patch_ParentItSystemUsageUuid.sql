BEGIN
    UPDATE [ItSystemUsageOverviewReadModels]
    SET ParentItSystemUsageUuid = 
    (
        SELECT t3.Uuid FROM ItSystemUsage t1
        INNER JOIN ItSystem t2 
        ON t1.ItSystemId = t2.Id
        LEFT JOIN ItSystem t3
        ON t2.ParentId = t3.Id
        WHERE t1.Uuid = [ItSystemUsageOverviewReadModels].SourceEntityUuid
    );
END