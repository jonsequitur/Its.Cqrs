// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Creates and migrates a reservation service database.
    /// </summary>
    public class ReservationServiceDatabaseInitializer : CreateAndMigrate<ReservationServiceDbContext>
    {
    }
}