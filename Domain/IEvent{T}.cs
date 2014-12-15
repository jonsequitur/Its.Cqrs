// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A sequential event applicable to a specific type, which can be used to rebuild the object's historical states.
    /// </summary>
    /// <typeparam name="T">The type to which the event applies.</typeparam>
    public interface IEvent<in T> : IEvent 
        where T : IEventSourced
    {
        /// <summary>
        /// Updates the specified object when the event is applied.
        /// </summary>
        /// <param name="aggregate">The order.</param>
        /// <remarks>This is used to update the object's state either upon applying a command, or when building up the object from an event source. Execution of this method should not create an side effects outside the object's state.</remarks>
        void Update(T aggregate);
    }
}