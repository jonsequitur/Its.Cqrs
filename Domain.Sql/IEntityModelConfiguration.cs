using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    ///     Provides a unit of configuration for composed Entity Framework configurations.
    /// </summary>
    public interface IEntityModelConfiguration
    {
        /// <summary>
        ///     Configures the specified configuration registrar.
        /// </summary>
        /// <param name="configurationRegistrar">The configuration registrar.</param>
        void ConfigureModel(ConfigurationRegistrar configurationRegistrar);
    }
}