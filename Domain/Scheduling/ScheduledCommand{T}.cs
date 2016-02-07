using System;
using System.Diagnostics;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An event that indicates that a command was scheduled.
    /// </summary>
    /// <typeparam name="TTarget">The type of the aggregate.</typeparam>
    [DebuggerDisplay("{ToString()}")]
    public class ScheduledCommand<TTarget> :
        IScheduledCommand<TTarget>
    {
        private static readonly bool targetIsEventSourced;

        static ScheduledCommand()
        {
            if (typeof (IEventSourced).IsAssignableFrom(typeof (TTarget)))
            {
                targetIsEventSourced = true;
                TargetGuid = command => Guid.Parse(command.TargetId);
            }
            else
            {
                TargetGuid = command => command.TargetId.ToGuidV3();
            }
        }

        public ScheduledCommand(
            ICommand<TTarget> command,
            Guid aggregateId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryPrecondition = null) :
                this(command, aggregateId.ToString("N"),
                     dueTime,
                     deliveryPrecondition)
        {
        }

        public ScheduledCommand(
            ICommand<TTarget> command,
            string targetId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryPrecondition = null)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("Parameter targetId cannot be null, empty or whitespace.");
            }

            if (string.IsNullOrEmpty(command.ETag))
            {
                command.IfTypeIs<Command>()
                       .ThenDo(c => c.ETag = CommandContext.Current
                                                           .IfNotNull()
                                                           .Then(ctx => ctx.NextETag(targetId))
                                                           .Else(() => Guid.NewGuid().ToString("N").ToETag()));
            }

            Command = command;
            TargetId = targetId;
            DueTime = dueTime;
            DeliveryPrecondition = deliveryPrecondition;
        }

        /// <summary>
        /// Gets the command to be applied at a later time.
        /// </summary>
        [JsonConverter(typeof (CommandConverter))]
        public ICommand<TTarget> Command { get; private set; }

        /// <summary>
        /// Gets the id of the object to which the command will be applied when delivered.
        /// </summary>
        public string TargetId { get; private set; }

        public Guid? AggregateId
        {
            get
            {
                return targetIsEventSourced
                           ? (Guid?) Guid.Parse(TargetId)
                           : null;
            }
        }

        /// <summary>
        /// Gets the sequence number of the scheduled command.
        /// </summary>
        internal long SequenceNumber { get; set; }

        /// <summary>
        /// Gets the time at which the command is scheduled to be applied.
        /// </summary>
        /// <remarks>
        /// If this value is null, the command should be delivered as soon as possible.
        /// </remarks>
        public DateTimeOffset? DueTime { get; private set; }

        /// <summary>
        /// Indicates a precondition ETag for a specific aggregate. If no event on the target aggregate exists with this ETag, the command will fail, and the aggregate can decide whether to reschedule or ignore the command.
        /// </summary>
        public IPrecondition DeliveryPrecondition { get; private set; }

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
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Result cannot be set to null.");
                }

                if (result is CommandDelivered)
                {
                    if (value is CommandScheduled)
                    {
                        throw new ArgumentException("Command cannot be scheduled again when it has already been delivered.");
                    }
                }
                
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
        {
            return string.Format("{0}{1}{2}{3}",
                                 Command,
                                 DueTime.IfNotNull()
                                        .Then(due => " due " + due)
                                        .ElseDefault(),
                                 DeliveryPrecondition.IfNotNull()
                                                     .Then(p => ", depends on " + p)
                                                     .ElseDefault(),
                                 Result.IfNotNull()
                                       .Then(r => ", " + r)
                                       .ElseDefault());
        }

        internal static readonly Func<IScheduledCommand<TTarget>, Guid> TargetGuid;
        private ScheduledCommandResult result;
    }
}