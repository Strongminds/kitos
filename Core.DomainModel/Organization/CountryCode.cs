using System.Collections.Generic;

namespace Core.DomainModel.Organization
{
    /// <summary>
    /// Country code for use in the organization countryCode field.
    /// </summary>
    public class CountryCode: OptionEntity<Organization>, IOptionReference<Organization>, IHasUuid
    {
        public virtual ICollection<Organization> References { get; set; } = new HashSet<Organization>();
    }
}
