// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using Its.Validation;
using Its.Validation.Configuration;

namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    ///     Provides methods for specifying authorization rules.
    /// </summary>
    /// <typeparam name="TPrincipal">The type of the principal.</typeparam>
    public class AuthorizationFor<TPrincipal>
        where TPrincipal : IPrincipal
    {
        /// <summary>
        ///     Specifies an authorization rule for a specific command type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        public static class ToApply<TCommand>
            where TCommand : ICommand
        {
            /// <summary>
            ///     Specifies an authorization rule for a specific resource type.
            /// </summary>
            /// <typeparam name="TResource">The type of the resource.</typeparam>
            public static class ToA<TResource>
            {
                /// <summary>
                ///     Specifies an authorization rule for a principal applying a specific command type to a specific resource type.
                /// </summary>
                /// <param name="requirement">The requirement.</param>
                /// <param name="message">A message indicating the reason for the authorization failure. This message is intended for diagnostic and monitoring purposes only.</param>
                public static void Requires(Func<TPrincipal, TCommand, TResource, bool> requirement, string message = null)
                {
                    ValidateTypeParameters();

                    ValidationPlan<IAuthorizationQuery<TResource, TCommand, TPrincipal>> plan = AuthorizationPolicy.For<TResource, TCommand, TPrincipal>().ValidationPlan;

                    var defaultRule = AuthorizationPolicy<TResource, TCommand, TPrincipal>.Default;
                    if (plan.Any(rule => rule == defaultRule))
                    {
                        // the default rule will cause the authz check to fail, so we remove it
                        plan.Remove(defaultRule);
                    }

                    var authzRule = Validate
                        .That<IAuthorizationQuery<TResource, TCommand, TPrincipal>>(query =>
                                                                                    requirement(query.Principal, query.Command, query.Resource))
                        .WithErrorMessage(message ?? ("Requirement for " + typeof (TCommand)));

                    plan.AddRule(authzRule);
                }

                /// <summary>
                /// Denies authorization.
                /// </summary>
                public static void IsDenied()
                {
                    ValidateTypeParameters();

                    Requires((principal, command, resource) => false);
                }

                private static void ValidateTypeParameters()
                {
                    var commandType = typeof (TCommand);
                    var commandAggregateTypes = commandType.GetInterfaces()
                                                           .Where(i => i.IsGenericType)
                                                           .Where(i => i.GetGenericTypeDefinition() == typeof (ICommand<>))
                                                           .Select(i => i.GenericTypeArguments.Single());

                    if (!commandAggregateTypes.Any(t => t.IsAssignableFrom(typeof (TResource))))
                    {
                        throw new ArgumentException(string.Format("Command type {0} is not applicable to resource type {1}", typeof (TCommand), typeof (TResource)));
                    }
                }
            }

            /// <summary>
            /// Denies authorization.
            /// </summary>
            public static void IsDenied()
            {
                var tresource = typeof (TCommand)
                    .GetInterface("ICommand`1")
                    .GetGenericArguments()
                    .Single();
                typeof (AuthorizationFor<>.ToApply<>.ToA<>)
                    .MakeGenericType(typeof (TPrincipal), typeof (TCommand), tresource)
                    .Member()
                    .IsDenied();
            }
        }

        /// <summary>
        ///     Specifies authorization rules applied to all commands.
        /// </summary>
        public static class ToApplyAnyCommand
        {
            /// <summary>
            ///     Specifies an authorization rule for a specific resource type.
            /// </summary>
            /// <typeparam name="TResource">The type of the resource.</typeparam>
            public static class ToA<TResource> where TResource : class
            {
                /// <summary>
                ///     Specifies an authorization rule for a principal applying any command to a specific resource type.
                /// </summary>
                /// <param name="requirement">The requirement.</param>
                /// <param name="message">A message indicating the reason for the authorization failure. This message is intended for diagnostic and monitoring purposes only.</param>
                public static void Requires(Func<TPrincipal, TResource, bool> requirement, string message = null)
                {
                    Command<TResource>.KnownTypes.ForEach(tcommand =>
                    {
                        Expression<Func<TPrincipal, TResource, bool>> bind = (p, r) => requirement(p, r);

                        var funcType = Expression.GetFuncType(typeof (TPrincipal), tcommand, typeof (TResource), typeof (bool));

                        var principal = Expression.Parameter(typeof (TPrincipal), "principal");
                        var command = Expression.Parameter(tcommand, "command");
                        var resource = Expression.Parameter(typeof (TResource), "resource");

                        var body = Expression.Invoke(bind, principal, resource);

                        var requirement2 = Expression.Lambda(
                            funcType,
                            body,
                            principal, command, resource).Compile();

                        typeof(ToApply<>.ToA<>)
                            .MakeGenericType(typeof(TPrincipal), tcommand, typeof(TResource))
                            .GetMethod("Requires",
                                BindingFlags.FlattenHierarchy |
                                BindingFlags.Public |
                                BindingFlags.Static)
                            .Invoke(null,
                                new object[]
                                {
                                    requirement2,
                                    message ?? "Requirement for all commands for resource " + typeof (TResource)
                                });
                    });
                }
            }
        }
    }
}
