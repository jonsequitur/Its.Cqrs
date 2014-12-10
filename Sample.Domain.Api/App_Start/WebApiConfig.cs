using System;
using System.Linq;
using System.Web.Http;
using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Api;
using Microsoft.Practices.Unity;
using Sample.Domain.Ordering;

namespace Sample.Domain.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config, IUnityContainer container)
        {
            container.RegisterInstance<IEventBus>(InProcessEventBus.Instance);

            config.MapRoutesFor<Order>()
                  .ResolveDependenciesUsing(container);
        }
    }
}