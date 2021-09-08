﻿using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.KLE;


namespace Core.DomainServices.Repositories.KLE
{
    public interface IKLEUpdateHistoryItemRepository
    {
        IEnumerable<KLEUpdateHistoryItem> Get();
        KLEUpdateHistoryItem Insert(DateTime version);
        Maybe<DateTime> GetLastUpdated();
    }
}