using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    internal static class PocketContainerExtensions
    {
        public static PocketContainer UseImmediateCommandScheduling(this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface &&  
                    type.IsGenericType && 
                    type.GetGenericTypeDefinition() == typeof(ICommandScheduler<>))
                {
                    var aggregateType = type.GetGenericArguments().First();
                    var schedulerType = typeof (ImmediateCommandScheduler<>).MakeGenericType(aggregateType);

                    return c => c.Resolve(schedulerType);
                }

                return null;
            });
        }
    }
}