using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Sql Azure Database Properties
    /// </summary>
    public class AzureSqlDatabaseServiceObjective
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="serviceObjective"></param>
        /// <param name="maxSizeInMegaBytes"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public AzureSqlDatabaseServiceObjective(string edition, string serviceObjective, long maxSizeInMegaBytes)
        {
            if (string.IsNullOrWhiteSpace(edition))
            {
                throw new ArgumentNullException(nameof(edition));
            }
            if (string.IsNullOrWhiteSpace(serviceObjective))
            {
                throw new ArgumentNullException(nameof(serviceObjective));
            }
            if (maxSizeInMegaBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSizeInMegaBytes));
            }
            Edition = edition;
            ServiceObjective = serviceObjective;
            MaxSizeInMegaBytes = maxSizeInMegaBytes;
        }

        /// <summary>
        /// Edition
        /// </summary>
        public string Edition { get; }
        /// <summary>
        /// Service Objective
        /// </summary>
        public string ServiceObjective { get;}

        /// <summary>
        /// Database size
        /// </summary>
        public long MaxSizeInMegaBytes { get;}
    }
}