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
        private static readonly ConcurrentDictionary<string, Type> handlerTypesByName = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static readonly Type[] knownTypes = Discover.ConcreteTypesDerivedFrom(typeof (ICommand<TTarget>)).ToArray();

        /// <summary>
        ///     The default authorization method used by all commands for <typeparamref name="TTarget" />.
        /// </summary>
        public static Func<TTarget, Command<TTarget>, bool> AuthorizeDefault = (target, command) => command.Principal.IsAuthorizedTo(command, target);

        private dynamic handler;

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
                throw new ArgumentNullException("target");
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
                throw new ArgumentNullException("target");
            }

            if (!string.IsNullOrWhiteSpace(ETag))
            {
                var eventSourced = target as IEventSourced;
                if (eventSourced.HasETag(ETag))
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

        private bool CommandHandlerIsRegistered()
        {
            return handlerTypesByName.GetOrAdd(CommandName, name =>
            {
                var handlerType = CommandHandler.Type(typeof (TTarget), GetType());
                var handlerTypes = CommandHandler.KnownTypes.DerivedFrom(handlerType).ToArray();

                var numberOfHandlerTypes = handlerTypes.Length;

                if (numberOfHandlerTypes == 1)
                {
                    return handlerTypes.Single();
                }

                if (numberOfHandlerTypes > 1)
                {
                    throw new DomainConfigurationException(
                        string.Format("Multiple handler implementations were found for {0}: {1}. This might be a mistake. If not, you must register one explicitly using Configuration.UseDependency.",
                                      handlerType.FullName,
                                      handlerTypes.Select(t => t.FullName).ToDelimitedString(", ")));
                }

                return null;

            }) != null;
        }

        protected virtual void HandleCommandValidationFailure(TTarget target, ValidationReport validationReport)
        {
            if (target is IEventSourced)
            {
                ((dynamic) target).HandleCommandValidationFailure((dynamic) this,
                                                                  validationReport);
            }
            else
            {
                throw new CommandValidationException(
                    string.Format("Validation error while applying {0} to a {1}.",
                                  CommandName,
                                  target.GetType().Name),
                    validationReport);
            }
        }

        internal dynamic Handler
        {
            get
            {
                if (handler == null && CommandHandlerIsRegistered())
                {
                    handler = Configuration.Current.Container.Resolve(handlerTypesByName[CommandName]);
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
                        string.Format("The command's AppliesToVersion value ({0}) does not match the aggregate's version ({1})",
                                      AppliesToVersion,
                                      eventSourced.Version));
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
                throw new InvalidOperationException(string.Format(
                    "Property CommandValidator returned a validator of the wrong type. It should return a {0}, but returned a {1}",
                    typeof (IValidationRule<>).MakeGenericType(GetType()),
                    CommandValidator.GetType()), ex);
            }

            if (validationReport.HasFailures)
            {
                if (throwOnValidationFailure)
                {
                    throw new CommandValidationException(string.Format("{0} is invalid.", CommandName), validationReport);
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
        public virtual bool Authorize(TTarget target)
        {
            return AuthorizeDefault(target, this);
        }

        /// <summary>
        /// If set, requires that the command be applied to this version of the target; otherwise, <see cref="ApplyTo" /> will throw..
        /// </summary>
        public virtual long? AppliesToVersion { get; set; }

        /// <summary>
        ///     Gets a validator that can be used to check the valididty of the command against the state of the target before it is applied.
        /// </summary>
        [JsonIgnore]
        public virtual IValidationRule<TTarget> Validator
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        ///     Gets a validator to check the state of the command in and of itself, as distinct from an target.
        /// </summary>
        /// <remarks>
        ///     By default, this returns a <see cref="ValidationPlan{TCommand}" /> where TCommand is the command's actual type, with rules built up from any System.ComponentModel.DataAnnotations attributes applied to its properties.
        /// </remarks>
        [JsonIgnore]
        public virtual IValidationRule CommandValidator
        {
            get
            {
                return Validation.GetDefaultPlanFor(GetType());
            }
        }

        /// <summary>
        ///     Gets all of the the types implementing <see cref="Command{T}" /> discovered within the AppDomain.
        /// </summary>
        public new static Type[] KnownTypes
        {
            get
            {
                return knownTypes;
            }
        }

        /// <summary>
        ///     Gets the command type having the specified name.
        /// </summary>
        public static Type Named(string name)
        {
            return
                knownTypesByName.GetOrAdd(name,
                                          n =>
                                          KnownTypes.SingleOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        public static IValidationRule<TTarget> CommandHasNotBeenApplied(ICommand command)
        {
            if (string.IsNullOrWhiteSpace(command.ETag) || typeof (EventSourcedAggregate).IsAssignableFrom(typeof (TTarget)))
            {
                return Validate.That<TTarget>(a => true)
                               .WithSuccessMessage("Command is not checked for idempotency via the ETag property.");
            }

            return Validate.That<TTarget>(target => (target as EventSourcedAggregate)
                                                              .Events()
                                                              .OfType<Event>()
                                                              .Every(e => e.ETag != command.ETag))
                           .WithErrorMessage(string.Format("Command with ETag '{0}' has already been applied.", command.ETag));
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}",
                                 typeof (TTarget).Name,
                                 CommandName);
        }
    }
}
