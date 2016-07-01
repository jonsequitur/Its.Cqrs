// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace Microsoft.Its.Domain.Sql
{
    internal static class ExceptionExtensions
    {
        public static bool IsConcurrencyException(this Exception exception) =>
            exception is ConcurrencyException ||
            exception.IsInsertConcurrencyException() ||
            exception is OptimisticConcurrencyException ||
            exception is DbUpdateConcurrencyException ||
            (exception.InnerException?.IsConcurrencyException() == true);

        private static bool IsInsertConcurrencyException(this Exception exception) =>
            (exception is DataException ||
             exception is SqlException) &&
            exception.ToString().Contains("Cannot insert duplicate key");

        public static bool IsUniquenessConstraint(this Exception exception) =>
            exception.IsConcurrencyException() &&
            exception.ToString().Contains("with unique index");
    }
}