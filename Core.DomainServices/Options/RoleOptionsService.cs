using Core.DomainModel;
using Core.DomainServices.Model.Options;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.LocalOptions;
using Core.DomainServices.Extensions;

namespace Core.DomainServices.Options
{
    public class RoleOptionsService<TReference, TOption, TLocalOption> : IRoleOptionsService<TReference, TOption>
        where TOption : OptionEntity<TReference>
        where TLocalOption : LocalRoleOptionEntity<TOption>
    {
        private readonly IGenericRepository<TLocalOption> _localOptionRepository;
        private readonly IGenericRepository<TOption> _optionRepository;

        public RoleOptionsService(
            IGenericRepository<TLocalOption> localOptionRepository,
            IGenericRepository<TOption> optionRepository)
        {
            _localOptionRepository = localOptionRepository;
            _optionRepository = optionRepository;
        }

        public IEnumerable<OptionDescriptor<TOption>> GetAvailableOptionsDetails(int organizationId)
        {
            var allOptions = GetAvailableOptionsFromOrganization(organizationId);

            return allOptions
                .descriptors
                .OrderBy(option => option.Option.Name)
                .ToList();
        }

        private (IEnumerable<OptionDescriptor<TOption>> descriptors, IQueryable<TOption> rawOptions)
            GetAvailableOptionsFromOrganization(int organizationId)
        {
            var localOptions = _localOptionRepository
                .AsQueryable()
                .ByOrganizationId(organizationId);

            var activeOptionIds = localOptions
                .Where(x => x.IsActive)
                .Select(x => x.OptionId)
                .ToList();

            // LINQ doesn't like IsNullOrWhiteSpace("")
            var localDescriptions = localOptions
                .Where(x => !(x.Description == null || x.Description.Trim() == string.Empty))
                .ToDictionary(x => x.OptionId, x => x.Description);

            var allLocallyEnabled = _optionRepository
                .AsQueryable()
                .Where(x => x.IsEnabled) //Local cannot include not-enabled options
                .ByIds(activeOptionIds);

            var allObligatory = _optionRepository
                .AsQueryable()
                .Where(x => x.IsEnabled)
                .ExceptEntitiesWithIds(activeOptionIds)
                .Where(x => x.IsObligatory); //Add enabled global options which are obligatory as well

            var allOptions = allObligatory.Concat(allLocallyEnabled);
            return (allOptions
                .AsEnumerable()
                .Select(x => new OptionDescriptor<TOption>(x,
                    localDescriptions.ContainsKey(x.Id) ? localDescriptions[x.Id] : x.Description)), allOptions);
        }
    }
}
