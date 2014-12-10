using System;
using System.Data.Entity.Infrastructure;

namespace Microsoft.Its.Domain.Sql
{
    internal static class ExceptionExtensions
    {
        public static bool IsConcurrencyException(this Exception exception)
        {
            return exception is DbUpdateConcurrencyException ||
                   exception is DbUpdateException &&
                   exception.ToString().Contains("Cannot insert duplicate key");
        }

        public static bool IsUniquenessConstraint(this Exception exception)
        {
            return exception.IsConcurrencyException() &&
            exception.ToString().Contains("with unique index \'IX");
        }
    }
}