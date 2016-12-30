namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Sql Azure Database Properties
    /// </summary>
    public class SqlAzureDatabaseProperties
    {
        /// <summary>
        /// Edition
        /// </summary>
        public string Edition { get; set; }
        /// <summary>
        /// Service Objective
        /// </summary>
        public string ServiceObjective { get; set; }

        /// <summary>
        /// Database size
        /// </summary>
        public long MaxSizeInMegaBytes { get; set; }
    }
}