using System;
using System.Diagnostics;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command that has been scheduled for future execution against a specific aggregate type.
    /// </summary>
    /// <typeparam name="TTarget">The type of the aggregate.</typeparam>
    [DebuggerDisplay("{ToString()}")]
    public class ScheduledCommand<TTarget> :
        IScheduledCommand<TTarget> where TTarget : class
    {
        private static readonly bool targetIsEventSourced;
        internal static readonly Func<IScheduledCommand<TTarget>, Guid> TargetGuid;
        private ScheduledCommandResult result;
        private IClock clock;

        static ScheduledCommand()
        {
            if (typeof(IEventSourced).IsAssignableFrom(typeof(TTarget)))
            {
                targetIsEventSourced = true;
                TargetGuid = command => Guid.Parse(command.TargetId);
            }
            else
            {
                TargetGuid = command => command.TargetId.ToGuidV3();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCommand{TTarget}"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="dueTime">The due time.</param>
        /// <param name="deliveryPrecondition">The delivery precondition.</param>
        /// <param name="clock">The clock.</param>
        public ScheduledCommand(
            ICommand<TTarget> command,
            Guid aggregateId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryPrecondition = null,
            IClock clock = null) :
                this(command,
                    aggregateId.ToString(),
                    dueTime,
                    deliveryPrecondition,
                    clock)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCommand{TTarget}"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="targetId">The target identifier.</param>
        /// <param name="dueTime">The due time.</param>
        /// <param name="deliveryPrecondition">The delivery precondition.</param>
        /// <param name="clock">The clock.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">Parameter targetId cannot be null, empty or whitespace.</exception>
        public ScheduledCommand(
            ICommand<TTarget> command,
            string targetId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryPrecondition = null,
            IClock clock = null)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("Parameter targetId cannot be null, empty or whitespace.");
            }

            var constructorCommand = command as ConstructorCommand<TTarget>;
            if (constructorCommand != null)
            {
                if (typeof(IEventSourced).IsAssignableFrom(typeof(TTarget)))
                {
                    if (constructorCommand.AggregateId != Guid.Parse(targetId))
                    {
                        throw new ArgumentException($"ConstructorCommand.AggregateId ({constructorCommand.AggregateId}) does not match ScheduledCommand.AggregateId ({targetId})");
                    }
                }
                else
                {
                    if (constructorCommand.TargetId != targetId)
                    {
                        throw new ArgumentException($"ConstructorCommand.TargetId ({constructorCommand.TargetId}) does not match ScheduledCommand.TargetId ({targetId})");
                    }
                }
            }

            Command = command;
            TargetId = targetId;
            DueTime = dueTime;
            DeliveryPrecondition = deliveryPrecondition;
            Clock = clock;

            this.EnsureCommandHasETag();
        }

        [JsonConstructor]
        private ScheduledCommand(
            ICommand<TTarget> command,
            string targetId = null,
            Guid? aggregateId = null,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryPrecondition = null) :
                this(command,
                    targetId
                        .IfNotNull()
                        .Else(() => aggregateId?.ToString()),
                    dueTime,
                    deliveryPrecondition)
        {
        }

        /// <summary>
        /// Gets the clock on which the command is scheduled.
        /// </summary>
        [JsonIgnore]
        public IClock Clock
        {
            get
            {
                return clock ?? (clock = Domain.Clock.Current);
            }
            set
            {
                clock = value;
            }
        }

        /// <summary>
        /// Gets the command to be applied at a later time.
        /// </summary>
        [JsonConverter(typeof(CommandConverter))]
        public ICommand<TTarget> Command { get; }

        /// <summary>
        /// Gets the number of times the scheduler has previously attempted to deliver the command.
        /// </summary>
        [JsonIgnore]
        public int NumberOfPreviousAttempts { get; set; }

        /// <summary>
        /// Gets the id of the object to which the command will be applied when delivered.
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// Gets the aggregate identifier, if command target is event sourced.
        /// </summary>
        public Guid? AggregateId =>
            targetIsEventSourced
                ? (Guid?) Guid.Parse(TargetId)
                : null;

        /// <summary>
        /// Gets the sequence number of the scheduled command.
        /// </summary>
        internal long SequenceNumber { get; set; }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName => Command.CommandName;

        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>
        /// If this to is null, the command should be delivered as soon as possible.
        /// </remarks>
        public DateTimeOffset? DueTime { get; set; }

        /// <summary>
        /// Indicates a precondition ETag for a specific aggregate. If no event on the target aggregate exists with this ETag, the command will fail, and the aggregate can decide whether to reschedule or ignore the command.
        /// </summary>
        public IPrecondition DeliveryPrecondition { get; }

        /// <summary>
        /// Gets or sets the result of the scheduled command if it has been sent to the scheduler.
        /// </summary>
        [JsonIgnore]
        public ScheduledCommandResult Result
        {
            get
            {
                return result;
            }
            set
            {
                result.ThrowIfNotAllowedToChangeTo(value);
                result = value;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
            => string.Format("{0} ({1} .. {2}) {3}{4}{5}",
                Command,
                TargetId,
                Command.ETag,
                DueTime.IfNotNull()
                    .Then(due => " due " + due)
                    .ElseDefault(),
                DeliveryPrecondition.IfNotNull()
                    .Then(p => $", depends on {p}")
                    .ElseDefault(),
                Result.IfNotNull()
                    .Then(r => $", {r}")
                    .ElseDefault());
    }
}