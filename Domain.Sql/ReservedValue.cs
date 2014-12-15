// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    public class ReservedValue
    {
        public string OwnerToken { get; set; }
        
        public string Value { get; set; }

        public string ConfirmationToken { get; set; }

        public string Scope { get; set; }

        public DateTimeOffset? Expiration { get; set; }
    }
}