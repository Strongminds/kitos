using Core.DomainModel;
using Core.DomainModel.Organization;

namespace Infrastructure.DataAccess.Mapping
{
    public class TaskUsageMap : EntityMap<TaskUsage>
    {
        public TaskUsageMap()
        {
            // Properties
            // Table & Column Mappings
            this.ToTable("TaskUsage");

            // Relationships
            this.HasRequired(t => t.OrgUnit)
                .WithMany(o => o.TaskUsages)
                .HasForeignKey(t => t.OrgUnitId)
                .WillCascadeOnDelete(false);

            this.HasRequired(t => t.TaskRef)
                .WithMany(r => r.Usages)
                .HasForeignKey(t => t.TaskRefId);

            this.HasOptional(t => t.Parent)
                .WithMany(d => d.Children)
                .HasForeignKey(t => t.ParentId)
                .WillCascadeOnDelete(false);
        }
    }
}
