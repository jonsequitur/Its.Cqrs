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