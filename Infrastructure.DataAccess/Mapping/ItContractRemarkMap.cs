﻿namespace Infrastructure.DataAccess.Mapping
{
    using Core.DomainModel.ItContract;

    public class ItContractRemarkMap : EntityMap<ItContractRemark>
    {
        public ItContractRemarkMap()
        {
            this.ToTable("ItContractRemarks");
        }
    }
}
