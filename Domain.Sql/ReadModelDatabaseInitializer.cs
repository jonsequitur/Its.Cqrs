// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
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
        public void InitializeDatabase(
            TDbContext context, 
            int dbSizeInGB, 
            string edition, 
            string serviceObjective, 
            DbReadonlyUser readonlyUser = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.Database.Exists())
            {
                if (context.Database.CompatibleWithModel(true))
                    return;
                context.Database.Delete();
            }

            if (context.IsAzureDatabase())
            {
                context.CreateAzureDatabase(dbSizeInGB, edition, serviceObjective);
                if (readonlyUser != null)
                {
                    context.CreateReadonlyUser(readonlyUser);
                }
            }
            else
            {
                context.Database.Create();
            }

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
