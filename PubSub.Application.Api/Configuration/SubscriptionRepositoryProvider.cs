using PubSub.Core.DomainModel.Repositories;
using PubSub.Infrastructure.DataAccess;

namespace PubSub.Application.Api.Configuration
{
    public class SubscriptionRepositoryProvider : ISubscriptionRepositoryProvider
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public SubscriptionRepositoryProvider(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public ISubscriptionRepository Get()
        {
            using var scope = serviceScopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<PubSubContext>();
            return scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        }
    }
}
