// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    public class ReservationServiceDatabaseInitializer : CreateDatabaseIfNotExists<ReservationServiceDbContext>
    {
        protected override void Seed(ReservationServiceDbContext context)
        {
            context.Unique<ReservedValue>(c => c.ConfirmationToken, c => c.Scope, schema: "Reservations");

            OnSeed.IfNotNull()
                  .ThenDo(seed => seed(context));

            base.Seed(context);
        }

        public static Action<ReservationServiceDbContext> OnSeed = context => { };
    }
}