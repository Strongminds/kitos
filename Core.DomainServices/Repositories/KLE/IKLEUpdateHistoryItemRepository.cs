﻿using System;
using System.Collections.Generic;
using Core.DomainModel.KLE;

namespace Core.DomainServices.Repositories.KLE
{
    public interface IKLEUpdateHistoryItemRepository
    {
        IEnumerable<KLEUpdateHistoryItem> Get();
        KLEUpdateHistoryItem Insert(DateTime version, int userId);
        DateTime GetLastUpdated();
    }
}