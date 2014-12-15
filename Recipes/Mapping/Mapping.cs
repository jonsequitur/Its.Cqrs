// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using Microsoft.Its.Data;

namespace Microsoft.Its.Domain.Mapping
{
    /// <summary>
    /// Performs convention-based mapping from commands to events, based on property names.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public static class New<TEvent> where TEvent : IEvent, new()
    {
        /// <summary>
        /// Creates a new instance of TEvent from the specified command.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <returns>A new event.</returns>
        public static TEvent From<TCommand>(TCommand command) where TCommand : class, ICommand
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            return Factory<TCommand>.from(command);
        }

        private static class Factory<TCommand> where TCommand : ICommand
        {
            private static readonly Factory<TCommand, TEvent> mapper =
                new MapFrom<TCommand>()
                    .Ignores(c => c.CommandName)
                    .Ignores("Clock")
                    .Ignores("Principal")
                    .Ignores("Validator")
                    .Ignores("CommandValidator")
                    .Ignores("AppliesToVersion")
                    .Ignores("CommandName")
                    .ToNew<TEvent>(m => m.Ignores(c => c.SequenceNumber)
                                         .Ignores(e => e.Timestamp)
                                         .Ignores(e => e.AggregateId));

            public static readonly Factory<TCommand, TEvent> from = command =>
            {
                var @event = mapper(command);

                var concreteEvent = @event as Event;
                if (concreteEvent != null)
                {
                    concreteEvent.SetActor(command.Principal.Identity.Name);
                }

                return @event;
            };
        }
    }
}