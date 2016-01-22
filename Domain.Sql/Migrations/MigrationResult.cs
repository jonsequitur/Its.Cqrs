// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Sql.Migrations
{
    public class MigrationResult
    {
        public string Log { get; set; }

        public bool MigrationWasApplied { get; set; }
    }
}