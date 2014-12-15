// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public class CommandReference
    {
        public string CommandName { get; set; }

        public string CommandField { get; set; }

        public override string ToString()
        {
            return CommandName + CommandField.IfNotNull()
                                             .Then(field => "." + field)
                                             .Else(() => "");
        }
    }
}