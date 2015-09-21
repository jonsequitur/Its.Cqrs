// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Microsoft.Its.Recipes;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides context for a chain of causality beginning with a command.
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    public class CommandContext : IDisposable
    {
        private static readonly string callContextKey = typeof (CommandContext).FullName;
        private static readonly ConcurrentDictionary<Guid, CommandContext> contexts = new ConcurrentDictionary<Guid, CommandContext>();

        private readonly Stack<CommandStackFrame> commandStack = new Stack<CommandStackFrame>();

        private readonly Guid Id = Guid.NewGuid();

        /// <summary>
        /// Prevents a default instance of the <see cref="CommandContext"/> class from being created.
        /// </summary>
        private CommandContext()
        {
        }

        /// <summary>
        /// Gets the command that is currently being applied.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return commandStack.Peek().Command;
            }
        }

        /// <summary>
        /// Establishes a command context.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="clock">The clock used by the command and any events that result.</param>
        public static CommandContext Establish(ICommand command, IClock clock = null)
        {
            var current = Current;

            if (current == null)
            {
                current = new CommandContext
                {
                    // override the domain clock if a clock is provided
                    Clock = clock ?? Domain.Clock.Current
                };

                CallContext.LogicalSetData(callContextKey, current.Id);
                contexts.GetOrAdd(current.Id, current);
            }
            else
            {
                current.Clock = Domain.Clock.Latest(
                    clock,
                    current.Clock);
            }

            current.commandStack.Push(new CommandStackFrame
            {
                Command = command,
                Clock = current.Clock
            });

            return current;
        }

        /// <summary>
        /// Gets or sets the clock used within the command context.
        /// </summary>
        public IClock Clock { get; set; }

        private int etagCount = 0;

        public string NextETag(string forTargetToken)
        {
            if (forTargetToken == null)
            {
                throw new ArgumentNullException("forTargetToken");
            }

            if (string.IsNullOrWhiteSpace(Command.ETag))
            {
                Command.IfTypeIs<Command>()
                       .ThenDo(c =>
                       {
                           c.AssignRandomETag();
                       })
                       .ElseDo(() =>
                       {
                           throw new InvalidOperationException("No source etag specified on command: " + Command);
                       });
            }

            var count = Interlocked.Increment(ref etagCount);

            var unhashedEtag = string.Format("{0}:{1} ({2})", Command.ETag, forTargetToken, count);

            return Md5Hash(unhashedEtag, true);
        }

        private static string Md5Hash(string input, bool base64)
        {
            // step 1, calculate MD5 hash from input
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            if (base64)
            {
                return Convert.ToBase64String(hash);
            }

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            commandStack.Pop();

            if (commandStack.Count == 0)
            {
                CommandContext commandContext;
                contexts.TryRemove(Id, out commandContext);
                CallContext.FreeNamedDataSlot(callContextKey);
            }
        }

        /// <summary>
        /// Gets the current command context.
        /// </summary>
        public static CommandContext Current
        {
            get
            {
                return CallContext.LogicalGetData(callContextKey)
                                  .IfTypeIs<Guid>()
                                  .Then(id => contexts.IfContains(id))
                                  .ElseDefault();
            }
        }

        internal CommandStackFrame Root
        {
            get
            {
                return Current.commandStack.First();
            }
        }

        internal struct CommandStackFrame
        {
            public ICommand Command;
            public IClock Clock;
        }
    }
}
