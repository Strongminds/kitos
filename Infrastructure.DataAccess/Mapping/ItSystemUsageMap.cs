using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystemUsage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemUsageMap : EntityMap<ItSystemUsage>
    {
        public override void Configure(EntityTypeBuilder<ItSystemUsage> builder)
        {
            base.Configure(builder);
            builder.ToTable("ItSystemUsage");

            builder.HasMany(t => t.ExternalReferences)
                .WithOne(d => d.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsage_Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Organization)
                .WithMany(t => t.ItSystemUsages)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ResponsibleUsage)
                .WithOne(t => t.ResponsibleItSystemUsage)
                .HasForeignKey<ItSystemUsageOrgUnitUsage>("ResponsibleItSystemUsage_Id")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ItSystem)
                .WithMany(t => t.Usages)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ArchiveType)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ArchiveTypeId);

            builder.HasOne(t => t.SensitiveDataType)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.SensitiveDataTypeId);

            builder.HasMany(t => t.UsedBy)
                .WithOne(t => t.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.MainContract)
                .WithOne()
                .HasForeignKey<ItContractItSystemUsage>("ItSystemUsage_Id")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.Contracts)
                .WithOne(t => t.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.ArchiveLocation)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ArchiveLocationId);

            builder.HasOne(t => t.ArchiveTestLocation)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ArchiveTestLocationId);

            builder.HasOne(t => t.ItSystemCategories)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ItSystemCategoriesId);

            builder.HasOne(t => t.SystemUsageCriticalityLevel)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.SystemUsageCriticalityLevelId);

            builder.HasOne(t => t.ArchiveSupplier)
                .WithMany(t => t.ArchiveSupplierForItSystems)
                .HasForeignKey(d => d.ArchiveSupplierId);

            builder.HasMany(t => t.SensitiveDataLevels)
                .WithOne(t => t.ItSystemUsage)
                .IsRequired()
                .OnDelete(DeleteBehavior.ClientCascade);

            builder.HasMany(x => x.PersonalDataOptions)
                .WithOne(o => o.ItSystemUsage)
                .HasForeignKey(o => o.ItSystemUsageId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Version).HasMaxLength(ItSystemUsage.DefaultMaxLength);
            builder.HasIndex(x => x.Version).HasDatabaseName("ItSystemUsage_Index_Version");

            builder.Property(x => x.LocalCallName).HasMaxLength(ItSystemUsage.DefaultMaxLength);
            builder.HasIndex(x => x.LocalCallName).HasDatabaseName("ItSystemUsage_Index_LocalCallName");

            builder.Property(x => x.LocalSystemId).HasMaxLength(ItSystemUsage.LongProperyMaxLength);
            builder.HasIndex(x => x.LocalSystemId).HasDatabaseName("ItSystemUsage_Index_LocalSystemId");

            builder.Property(x => x.RiskSupervisionDocumentationUrlName).HasMaxLength(ItSystemUsage.LinkNameMaxLength);
            builder.HasIndex(x => x.RiskSupervisionDocumentationUrlName).HasDatabaseName("ItSystemUsage_Index_RiskSupervisionDocumentationUrlName");

            builder.Property(x => x.LinkToDirectoryUrlName).HasMaxLength(ItSystemUsage.LinkNameMaxLength);
            builder.HasIndex(x => x.LinkToDirectoryUrlName).HasDatabaseName("ItSystemUsage_Index_LinkToDirectoryUrlName");

            builder.HasIndex(x => x.LifeCycleStatus).HasDatabaseName("ItSystemUsage_Index_LifeCycleStatus");

            builder.Property(x => x.CriticalityFieldsLastChanged);

            builder.Property(x => x.CriticalityFieldsLastChanged);

            builder.Property(x => x.Uuid).IsRequired();
            builder.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UX_ItSystemUsage_Uuid");

            builder.Property(x => x.LicensingAndCodeModels)
                .HasConversion(
                    models => SerializeLicensingAndCodeModels(models),
                    value => DeserializeLicensingAndCodeModels(value))
                .Metadata.SetValueComparer(LicensingAndCodeModelsComparer);

            builder.HasIndex(x => x.ItSystemId)
                .HasDatabaseName("IX_ItSystemUsage_ItSystemId");
        }

        private static readonly ValueComparer<ICollection<LicensingAndCodeModel>> LicensingAndCodeModelsComparer =
            new(
                (left, right) => AreSameLicensingAndCodeModels(left, right),
                models => GetLicensingAndCodeModelsHashCode(models),
                models => CloneLicensingAndCodeModels(models));

        private static string SerializeLicensingAndCodeModels(ICollection<LicensingAndCodeModel>? models)
        {
            var values = (models ?? [])
                .Distinct()
                .Select(model => (int)model)
                .ToArray();

            return JsonSerializer.Serialize(values);
        }

        private static ICollection<LicensingAndCodeModel> DeserializeLicensingAndCodeModels(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<LicensingAndCodeModel>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (TryParseLicensingAndCodeModel(item, out var model) && result.Contains(model) == false)
                {
                    result.Add(model);
                }
            }

            return result;
        }

        private static bool AreSameLicensingAndCodeModels(
            ICollection<LicensingAndCodeModel>? left,
            ICollection<LicensingAndCodeModel>? right)
        {
            return (left ?? Enumerable.Empty<LicensingAndCodeModel>())
                .SequenceEqual(right ?? Enumerable.Empty<LicensingAndCodeModel>());
        }

        private static int GetLicensingAndCodeModelsHashCode(ICollection<LicensingAndCodeModel>? models)
        {
            return (models ?? Enumerable.Empty<LicensingAndCodeModel>())
                .Aggregate(0, (current, model) => HashCode.Combine(current, model));
        }

        private static ICollection<LicensingAndCodeModel> CloneLicensingAndCodeModels(ICollection<LicensingAndCodeModel>? models)
        {
            return models == null ? new List<LicensingAndCodeModel>() : models.ToList();
        }

        private static bool TryParseLicensingAndCodeModel(JsonElement item, out LicensingAndCodeModel model)
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.Number when item.TryGetInt32(out var parsedNumber)
                                                && System.Enum.IsDefined(typeof(LicensingAndCodeModel), parsedNumber):
                    model = (LicensingAndCodeModel)parsedNumber;
                    return true;
                case JsonValueKind.String:
                    var text = item.GetString();
                    if (int.TryParse(text, out var parsedFromText) && System.Enum.IsDefined(typeof(LicensingAndCodeModel), parsedFromText))
                    {
                        model = (LicensingAndCodeModel)parsedFromText;
                        return true;
                    }

                    if (System.Enum.TryParse<LicensingAndCodeModel>(text, ignoreCase: true, out model)
                        && System.Enum.IsDefined(model))
                    {
                        return true;
                    }

                    break;
            }

            model = default;
            return false;
        }
    }
}
