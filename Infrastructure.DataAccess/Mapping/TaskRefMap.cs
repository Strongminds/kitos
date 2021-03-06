using Core.DomainModel;
using Core.DomainModel.Organization;

namespace Infrastructure.DataAccess.Mapping
{
    public class TaskRefMap : EntityMap<TaskRef>
    {
        public TaskRefMap()
        {
            // Properties
            // Table & Column Mappings
            this.ToTable("TaskRef");

            // Relationships
            this.HasOptional(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(d => d.ParentId)
                .WillCascadeOnDelete(false);

            this.HasRequired(t => t.OwnedByOrganizationUnit)
                .WithMany(t => t.OwnedTasks)
                .HasForeignKey(d => d.OwnedByOrganizationUnitId);

            this.HasMany(t => t.ItSystems)
                .WithMany(t => t.TaskRefs);

            this.HasMany(t => t.ItSystemUsages)
                .WithMany(t => t.TaskRefs);

            this.HasMany(t => t.ItSystemUsagesOptOut)
                .WithMany(t => t.TaskRefsOptOut)
                .Map(t =>
                {
                    t.ToTable("TaskRefItSystemUsageOptOut");
                });

            this.Property(x => x.TaskKey).HasMaxLength(50);

            this.Property(x => x.TaskKey).HasUniqueIndexAnnotation("UX_TaskKey", 0);

       }
    }
}
