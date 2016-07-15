// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [SetUpFixture]
    internal class SetUpDbConfiguration
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            DbConfiguration.SetConfiguration(new TestDbConfiguration());
        }
    }

    public class TestDbConfiguration : DbConfiguration
    {
        public TestDbConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient",
                () =>
                {
                    if (!UseSqlAzureExecutionStrategy)
                    {
                        return new DefaultExecutionStrategy();
                    }

                    return new SqlAzureExecutionStrategy();
                });
        }

        public static bool UseSqlAzureExecutionStrategy { get; set; }
    }
}