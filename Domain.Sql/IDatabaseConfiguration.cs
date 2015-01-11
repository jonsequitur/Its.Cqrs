// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql
{
    public interface IDatabaseConfiguration<in TContext> where TContext : DbContext
    {
        void ConfigureDatabase(TContext context);
    }
}
