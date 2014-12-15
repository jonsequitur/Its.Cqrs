// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Its.Validation;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles commands.
    /// </summary>
    /// <remarks>
    /// This interface defines the set of methods that can be implemented by an aggregate to handle commands and various failure conditions. Implementing it is optional. These methods will be called dynamically even if the aggregate does not implement them, but the interface can be helpful for getting the signatures correct and understanding your code structure.
    /// </remarks>
    public interface ICommandHandler<in TCommand>
        where TCommand : class, ICommand
    {
        /// <summary>
        /// Called when a command has passed validation and authorization checks.
        /// </summary>
        /// <remarks>In this method, the aggregate should record any events that will be used to indicate the success of the command. This method will only be called when a command has first succeeded, not during event sourcing, so any state changes that need to be captured by the success of the command need to be recorded here, e.g. by calling <see cref="EventSourcedAggregate{T}.RecordEvent" /> or <see cref="EventSourcedAggregate{T}.ScheduleCommand{TCommand}" />.</remarks>
        void EnactCommand(TCommand command);

        /// <summary>
        /// Handles a command validation failure when validation against the aggregate's state fails.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="validationReport">The validation report.</param>
        /// <remarks>This method is not called if the command itself is invalid (i.e., <see cref="Command{T}.CommandValidator" /> failed.)</remarks>
        void HandleCommandValidationFailure(TCommand command, ValidationReport validationReport);
    }
}