using Core.DomainModel.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class TaskRefMap : EntityMap<TaskRef>
    {
        public override void Configure(EntityTypeBuilder<TaskRef> builder)
        {
            base.Configure(builder);
            base.Configure(builder);
            builder.ToTable("TaskRef");

            builder.HasOne(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.OwnedByOrganizationUnit)
                .WithMany(t => t.OwnedTasks)
                .HasForeignKey(d => d.OwnedByOrganizationUnitId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.ItSystems)
                .WithMany(t => t.TaskRefs);

            builder.HasMany(t => t.ItSystemUsages)
                .WithMany(t => t.TaskRefs);

            builder.HasMany(t => t.ItSystemUsagesOptOut)
                .WithMany(t => t.TaskRefsOptOut)
                .UsingEntity(j => j.ToTable("TaskRefItSystemUsageOptOut"));

            builder.Property(x => x.TaskKey)
                .HasMaxLength(TaskRef.MaxTaskKeyLength);
            builder.HasIndex(x => x.TaskKey).IsUnique().HasDatabaseName("UX_TaskKey");

            builder.Property(x => x.Uuid).IsRequired();
            builder.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UX_TaskRef_Uuid");

            builder.Property(x => x.Description)
                .HasMaxLength(TaskRef.MaxDescriptionLength);
        }
    }
}
