// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Used by a command validation rule to reference a mitigating command.
    /// </summary>
    public class CommandReference
    {
        /// <summary>
        /// Gets or sets the name of the command.
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Gets or sets a field on the command, if applicable.
        /// </summary>
        public string CommandField { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            CommandName + CommandField.IfNotNull()
                                      .Then(field => "." + field)
                                      .Else(() => "");
    }
}