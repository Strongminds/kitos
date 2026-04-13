using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.DataAccess
{
    public class KitosContextDesignTimeFactory : IDesignTimeDbContextFactory<KitosContext>
    {
        public KitosContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<KitosContext>();
            optionsBuilder.UseSqlServer(
                "Server=.\\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True");

            return new KitosContext(optionsBuilder.Options);
        }
    }
}
