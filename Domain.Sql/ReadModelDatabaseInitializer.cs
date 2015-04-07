// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Transactions;
using log = Its.Log.Lite.Log;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Initializes a read model database with a single catchup run if the database does not exist or its schema has changed.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the db context.</typeparam>
    public class ReadModelDatabaseInitializer<TDbContext> : DropCreateDatabaseIfModelChanges<TDbContext>
        where TDbContext : ReadModelDbContext, new()
    {
        /// <summary>
        /// See https://msdn.microsoft.com/en-us/library/dn268335.aspx for more info.
        /// 
        /// MAXSIZE = ( [ 100 MB | 500 MB ] | [ { 1 | 5 | 10 | 20 | 30 … 150…500 } GB  ] )
        /// | EDITION = { 'web' | 'business' | 'basic' | 'standard' | 'premium' } 
        /// | SERVICE_OBJECTIVE = { 'shared' | 'basic' | 'S0' | 'S1' | 'S2' | 'P1' | 'P2' | 'P3' } 
        /// </summary>
        /// <param name="context"> The DbContext </param>
        /// <param name="dbSizeInGB"> Size of database in GB </param>
        /// <param name="edition"> Edition of database </param>
        /// <param name="serviceObjective"> Service objective of database </param>
        public void InitializeDatabase(TDbContext context, int dbSizeInGB, string edition, string serviceObjective)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool flag;
            using (new TransactionScope(TransactionScopeOption.Suppress))
                flag = context.Database.Exists();

            if (flag)
            {
                if (context.Database.CompatibleWithModel(true))
                    return;
                context.Database.Delete();
            }

            context.CreateDatabase(dbSizeInGB, edition, serviceObjective);
            Seed(context);
            context.SaveChanges();
        }

        /// <summary>
        /// A that should be overridden to actually add data to the context for seeding. 
        ///                 The default implementation does nothing.
        /// </summary>
        /// <param name="context">The context to seed.</param>
        protected override void Seed(TDbContext context)
        {
            try
            {
                var configurerTypes = Discover.ConcreteTypesDerivedFrom(typeof (IDatabaseConfiguration<TDbContext>));

                foreach (var type in configurerTypes)
                {
                    dynamic configurer = Activator.CreateInstance(type);
                    configurer.ConfigureDatabase(context);
                }
            }
            catch (Exception ex)
            {
                log.Write(() => ex);
                throw;
            }

            base.Seed(context);
        }
    }
}
