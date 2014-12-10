using System;

namespace Microsoft.Its.Domain
{
    public static class ConfigurationExtensions
    {
        public static Configuration UseEventBus(
            this Configuration configuration,
            IEventBus bus)
        {
            configuration.Container.RegisterSingle(c => bus);
            return configuration;
        }

        public static Configuration UseDependency<T>(
            this Configuration configuration,
            Func<Func<Type, object>, T> resolve)
        {
            // ReSharper disable RedundantTypeArgumentsOfMethod
            configuration.Container.Register<T>(c => resolve(c.Resolve));
            // ReSharper restore RedundantTypeArgumentsOfMethod
            return configuration;
        }

        /// <remarks>For example:
        /// 
        /// <code>
        /// 
        /// MyContainer myContainer; 
        /// 
        /// configuration.UseDependencies(
        ///     type => {       
        ///        if (myContainer.IsRegistered(type)) 
        ///        {  
        ///             return () => myContainer.Resolve(type);
        ///        } 
        ///        
        ///        return null;
        /// 
        ///     });
        /// 
        /// </code>
        /// 
        /// </remarks>
        public static Configuration UseDependencies(
            this Configuration configuration,
            Func<Type, Func<object>> strategy)
        {
            configuration.Container.AddStrategy(t =>
            {
                Func<object> resolveFunc = strategy(t);
                if (resolveFunc != null)
                {
                    return container => resolveFunc();
                }
                return null;
            });
            return configuration;
        }
    }
}