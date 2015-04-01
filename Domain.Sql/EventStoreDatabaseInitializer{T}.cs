// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    public class EventStoreDatabaseInitializer<TContext> : IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        private bool seeded;

        public void InitializeDatabase(TContext context)
        {
            var dbMigrator = new DbMigrator(new EventStoreMigrationConfiguration<TContext>());

            if (dbMigrator.GetPendingMigrations().ToArray().Any())
            {
                dbMigrator.Update();
            }

            OnSeed.IfNotNull()
                  .ThenDo(seed =>
                  {
                      if (!seeded)
                      {
                          seeded = true;
                          seed(context);
                          context.SaveChanges();
                      }
                  });
        }

        public Action<TContext> OnSeed = context =>
        {
        };
    }
}