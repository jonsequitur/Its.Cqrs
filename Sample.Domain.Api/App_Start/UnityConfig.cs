using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Sql;
using Microsoft.Practices.Unity;
using Sample.Domain.Ordering;

namespace Sample.Domain.Api
{
    public static class UnityConfig
    {
        public static void Register(UnityContainer container)
        {
            container.RegisterType<IEventSourcedRepository<Order>, SqlEventSourcedRepository<Order>>();
        }
    }
}