// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Its.Domain.Authorization;
using Its.Validation;
using Its.Validation.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A command that can be applied to an target to trigger some action and record an applicable state change.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public abstract class Command<TTarget> : Command, ICommand<TTarget>
        where TTarget : class
    {
        private static readonly ConcurrentDictionary<string, Type> knownTypesByName = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     The default authorization method used by all commands for <typeparamref name="TTarget" />.
        /// </summary>
        public static Func<TTarget, Command<TTarget>, bool> AuthorizeDefault = (target, command) => command.Principal.IsAuthorizedTo(command, target);

        private dynamic handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command{TTarget}"/> class.
        /// </summary>
        /// <param name="etag"></param>
        protected Command(string etag = null) : base(etag)
        {
        }

        /// <summary>
        ///     Performs the action of the command upon the target.
        /// </summary>
        /// <param name="target">The target to which to apply the command.</param>
        /// <exception cref="CommandValidationException">
        ///     If the command cannot be applied due its state or the state of the target, it should throw a
        ///     <see
        ///         cref="CommandValidationException" />
        ///     indicating the specifics of the failure.
        /// </exception>
        public virtual void ApplyTo(TTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!string.IsNullOrWhiteSpace(ETag))
            {
                var eventSourced = target as IEventSourced;
                if (eventSourced.HasETag(ETag))
                {
                    return;
                }
            }

            // validate that the command's state is valid in and of itself
            var validationReport = RunAllValidations(target, false);

            using (CommandContext.Establish(this))
            {
                if (validationReport.HasFailures)
                {
                    HandleCommandValidationFailure(target, validationReport);
                }
                else
                {
                    EnactCommand(target);
                }
            }
        }

        /// <summary>
        ///     Performs the action of the command upon the target.
        /// </summary>
        /// <param name="target">The target to which to apply the command.</param>
        /// <exception cref="CommandValidationException">
        ///     If the command cannot be applied due its state or the state of the target, it should throw a
        ///     <see
        ///         cref="CommandValidationException" />
        ///     indicating the specifics of the failure.
        /// </exception>
        public virtual async Task ApplyToAsync(TTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!string.IsNullOrWhiteSpace(ETag))
            {
                var eventSourced = target as IEventSourced;
                if (eventSourced != null && eventSourced.HasETag(ETag))
                {
                    return;
                }
            }

            var validationReport = RunAllValidations(target, false);

            using (CommandContext.Establish(this))
            {
                if (validationReport.HasFailures)
                {
                    HandleCommandValidationFailure(target, validationReport);
                }
                else
                {
                    await EnactCommandAsync(target);
                }
            }
        }

        /// <summary>
        /// Enacts the command once authorizations and validations have succeeded.
        /// </summary>
        /// <param name="target">The target upon which to enact the command.</param>
        protected virtual void EnactCommand(TTarget target)
        {
            Task.Run(() => EnactCommandAsync(target)).Wait();
        }
        
        /// <summary>
        /// Enacts the command once authorizations and validations have succeeded.
        /// </summary>
        /// <param name="target">The target upon which to enact the command.</param>
        protected virtual async Task EnactCommandAsync(TTarget target)
        {
            if (Handler == null)
            {
                Action enactCommand = () => ((dynamic) target).EnactCommand((dynamic) this);
                await Task.Run(enactCommand);
            }
            else
            {
                await (Task) Handler.EnactCommand((dynamic) target, (dynamic) this);
            }
        }

        /// <summary>
        /// Handles a command validation failure.
        /// </summary>
        /// <param name="target">The target of the command.</param>
        /// <param name="validationReport">The validation report.</param>
        /// <exception cref="CommandValidationException"></exception>
        /// <remarks>The default implementation throws a <see cref="CommandValidationException" />.</remarks>
        protected virtual void HandleCommandValidationFailure(TTarget target, ValidationReport validationReport)
        {
            var eventSourcedAggregate = target as EventSourcedAggregate;
            if (eventSourcedAggregate != null)
            {
                ((dynamic) target).HandleCommandValidationFailure((dynamic) this,
                                                                  validationReport);
            }
            else
            {
                throw new CommandValidationException(
                    $"Validation error while applying {CommandName} to a {target.GetType().Name}.",
                    validationReport);
            }
        }

        internal dynamic Handler
        {
            get
            {
                if (handler == null)
                {
                    var commandHandlerType = CommandHandler<TTarget>.ForCommandType(GetType());

                    try
                    {
                        handler = Configuration
                            .Current
                            .Container
                            .Resolve(commandHandlerType);
                    }
                    catch (DomainConfigurationException)
                    {
                        // swallow this exception, allowing fallback to other EnactCommand strategies
                    }
                }

                return handler;
            }
        }

        internal void AuthorizeOrThrow(TTarget target)
        {
            if (!Authorize(target))
            {
                throw new CommandAuthorizationException("Unauthorized");
            }
        }

        internal ValidationReport RunAllValidations(TTarget target, bool throwOnValidationFailure = false)
        {
            AuthorizeOrThrow(target);

            if (AppliesToVersion != null)
            {
                var eventSourced = target as IEventSourced;

                if (eventSourced != null && AppliesToVersion != eventSourced.Version)
                {
                    throw new ConcurrencyException(
                        $"The command's AppliesToVersion value ({AppliesToVersion}) does not match the aggregate's version ({eventSourced.Version})");
                }
            }

            // first validate the command's validity in and of itself
            ValidationReport validationReport;

            try
            {
                validationReport = ExecutePreparedCommandValidator();
            }
            catch (RuntimeBinderException ex)
            {
                throw new InvalidOperationException(
                    $"Property CommandValidator returned a validator of the wrong type. It should return a {typeof (IValidationRule<>).MakeGenericType(GetType())}, but returned a {CommandValidator.GetType()}", ex);
            }

            if (validationReport.HasFailures)
            {
                if (throwOnValidationFailure)
                {
                    throw new CommandValidationException($"{CommandName} is invalid.", validationReport);
                }

                return validationReport;
            }

            return ExecuteValidator(target);
        }

        private ValidationReport ExecuteValidator(TTarget target)
        {
            return (Validator ??
                    new ValidationRule<TTarget>(a => true)
                        .WithSuccessMessage("No rules defined for " + GetType()))
                .Execute(target);
        }

        private ValidationReport ExecutePreparedCommandValidator()
        {
            var validator = PrepareCommandValidator();
            return ((dynamic) validator).Execute((dynamic) this);
        }

        private object PrepareCommandValidator()
        {
            // TODO: (PrepareCommandValidator) optimize
            var commandValidator = CommandValidator;

            if (commandValidator.GetType() == typeof (ValidationPlan<>).MakeGenericType(GetType()))
            {
                // it's a ValidationPlan<TCommand>
                return commandValidator;
            }

            // it's a ValidationRule. wrap it in a ValidationPlan in order to be able to call Execute.
            var plan = Validation.CreateEmptyPlanFor(GetType());
            ((dynamic) plan).Add((dynamic) commandValidator);
            return plan;
        }

        /// <summary>
        ///     Determines whether the command is authorized to be applied to the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>true if the command is authorized; otherwise, false.</returns>
        public virtual bool Authorize(TTarget target) => AuthorizeDefault(target, this);

        /// <summary>
        /// If set, requires that the command be applied to this version of the target; otherwise, <see cref="ApplyTo" /> will throw..
        /// </summary>
        public virtual long? AppliesToVersion { get; set; }

        /// <summary>
        ///     Gets a validator that can be used to check the valididty of the command against the state of the target before it is applied.
        /// </summary>
        [JsonIgnore]
        public virtual IValidationRule<TTarget> Validator => null;

        /// <summary>
        ///     Gets a validator to check the state of the command in and of itself, as distinct from an target.
        /// </summary>
        /// <remarks>
        ///     By default, this returns a <see cref="ValidationPlan{TCommand}" /> where TCommand is the command's actual type, with rules built up from any System.ComponentModel.DataAnnotations attributes applied to its properties.
        /// </remarks>
        [JsonIgnore]
        public virtual IValidationRule CommandValidator => Validation.GetDefaultPlanFor(GetType());

        /// <summary>
        ///     Gets all of the the types implementing <see cref="Command{T}" /> discovered within the AppDomain.
        /// </summary>
        public new static Type[] KnownTypes { get; } =
            Discover.ConcreteTypesDerivedFrom(typeof(ICommand<TTarget>)).ToArray();

        /// <summary>
        ///     Gets the command type having the specified name.
        /// </summary>
        public static Type Named(string name) =>
            knownTypesByName.GetOrAdd(name,
                                      n =>
                                      KnownTypes.SingleOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"{typeof (TTarget).Name}.{CommandName}";
    }
}
