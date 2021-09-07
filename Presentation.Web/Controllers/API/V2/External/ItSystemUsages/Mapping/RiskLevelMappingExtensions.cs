﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Extensions;
using Core.DomainModel.ItSystem.DataTypes;
using Presentation.Web.Models.API.V2.Types.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public static class RiskLevelMappingExtensions
    {
        private static readonly IReadOnlyDictionary<RiskLevelChoice, RiskLevel> ApiToDataMap;
        private static readonly IReadOnlyDictionary<RiskLevel, RiskLevelChoice> DataToApiMap;

        static RiskLevelMappingExtensions()
        {
            ApiToDataMap = new Dictionary<RiskLevelChoice, RiskLevel>
            {
                { RiskLevelChoice.High, RiskLevel.HIGH },
                { RiskLevelChoice.Medium, RiskLevel.MIDDLE },
                { RiskLevelChoice.Low, RiskLevel.LOW },
                { RiskLevelChoice.Undecided, RiskLevel.UNDECIDED }
            }.AsReadOnly();
            
            DataToApiMap = ApiToDataMap
                .ToDictionary(kvp => kvp.Value, kvp => kvp.Key)
                .AsReadOnly();
        }

        public static RiskLevel ToRiskLevel(this RiskLevelChoice value)
        {
            return ApiToDataMap.TryGetValue(value, out var result)
                ? result
                : throw new ArgumentException($@"Unmapped choice:{value:G}", nameof(value));
        }

        public static RiskLevelChoice ToRiskLevelChoice(this RiskLevel value)
        {
            return DataToApiMap.TryGetValue(value, out var result)
                ? result
                : throw new ArgumentException($@"Unmapped domain value:{value:G}", nameof(value));
        }
    }
}