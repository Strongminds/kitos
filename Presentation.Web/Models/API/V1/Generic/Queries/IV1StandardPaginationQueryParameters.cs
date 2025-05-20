namespace Presentation.Web.Models.API.V1.Generic.Queries
{
    public interface IV1StandardPaginationQueryParameters
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
