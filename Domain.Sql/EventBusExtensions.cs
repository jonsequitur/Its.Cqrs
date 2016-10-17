// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for working with the event bus.
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// Reports event handling errors via the specified database.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="createDbContext">The database.</param>
        /// <returns></returns>
        public static IDisposable ReportErrorsToDatabase(this IEventBus bus, Func<DbContext> createDbContext) =>
            bus.Errors.Subscribe(e => ReadModelUpdate.ReportFailure(e, createDbContext));
    }
}