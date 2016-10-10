// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using Its.Validation;
using Its.Validation.Configuration;

namespace Microsoft.Its.Domain.Authorization
{
    /// <summary>
    /// Defines an authorization policy in the form of subject, verb, object, where the subject is principal, the command is the verb, and the resource is the object..
    /// </summary>
    public class AuthorizationPolicy
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type, Type>, AuthorizationPolicy> policies =
            new ConcurrentDictionary<Tuple<Type, Type, Type>, AuthorizationPolicy>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationPolicy"/> class.
        /// </summary>
        /// <param name="resourceType">Type of the resource to be authorized.</param>
        /// <param name="commandType">Type of the command to be authorized.</param>
        /// <param name="principalType">Type of the principal to be authorized.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public AuthorizationPolicy(Type resourceType, Type commandType, Type principalType)
        {
            if (resourceType == null)
            {
                throw new ArgumentNullException(nameof(resourceType));
            }
            if (commandType == null)
            {
                throw new ArgumentNullException(nameof(commandType));
            }
            if (principalType == null)
            {
                throw new ArgumentNullException(nameof(principalType));
            }
            ResourceType = resourceType;
            CommandType = commandType;
            PrincipalType = principalType;
        }

        /// <summary>
        /// Creates an instance of an authorization policy for the specified principal, resource, and command types.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <typeparam name="TPrincipal">The type of the principal.</typeparam>
        /// <returns></returns>
        public static AuthorizationPolicy<TResource, TCommand, TPrincipal> For<TResource, TCommand, TPrincipal>()
            where TPrincipal : IPrincipal
            where TCommand : ICommand
        {
            var resourceType = typeof (TResource);
            var commandType = typeof (TCommand);
            var principalType = typeof (TPrincipal);
            var key = Tuple.Create(resourceType, commandType, principalType);

            var authorizationPolicy = policies.GetOrAdd(key,
                                                        k => new AuthorizationPolicy<TResource, TCommand, TPrincipal>(resourceType, commandType, principalType));
            return (AuthorizationPolicy<TResource, TCommand, TPrincipal>) authorizationPolicy;
        }

        /// <summary>
        /// Creates an instance of an authorization policy for the specified principal, resource, and command types.
        /// </summary>
        /// <param name="resourceType">Type of the resource.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="principalType">Type of the principal.</param>
        /// <returns></returns>
        public static AuthorizationPolicy For(Type resourceType, Type commandType, Type principalType)
        {
            var key = Tuple.Create(resourceType, commandType, RelevantTypeFor(principalType));
            
            return policies.GetOrAdd(key, k =>
            {
                var authPolicyType = typeof (AuthorizationPolicy<,,>).MakeGenericType(resourceType, commandType, principalType);
                var authPolicy = Activator.CreateInstance(authPolicyType, resourceType, commandType, principalType);
                return (AuthorizationPolicy) authPolicy;
            });
        }

        /// <summary>
        /// Looks up a non-dynamic base type, for the case in which the actual runtime type is proxied (e.g. by Entity Framework) from the configured type.
        /// </summary>
        private static Type RelevantTypeFor(Type principalType)
        {
            if (principalType == null)
            {
                throw new ArgumentNullException(nameof(principalType));
            }

            while (principalType.Assembly.IsDynamic)
            {
                principalType = principalType.BaseType;
            }

            return principalType;
        }

        /// <summary>
        /// Gets the type of the principal authorized by this policy.
        /// </summary>
        public Type PrincipalType { get; private set; }

        /// <summary>
        /// Gets the type of the command authorized by the policy.
        /// </summary>
        public Type CommandType { get; private set; }

        /// <summary>
        /// Gets the type of the resource authorized by the policy.
        /// </summary>
        public Type ResourceType { get; private set; }
    }

    /// <summary>
    /// Defines an authorization policy in the form of subject, verb, object, where the subject is principal, the command is the verb, and the resource is the object..
    /// </summary>
    public class AuthorizationPolicy<TResource, TCommand, TPrincipal> : AuthorizationPolicy
        where TPrincipal : IPrincipal
        where TCommand : ICommand
    {
        internal static IValidationRule<IAuthorizationQuery<TResource, TCommand, TPrincipal>> Default =
            new ValidationRule<IAuthorizationQuery<TResource, TCommand, TPrincipal>>(q => false)
                .WithErrorMessage(
                    $"Unauthorized by default because no authorization rules have been specified for {typeof(TResource)}-{typeof(TCommand)}-{typeof(TPrincipal)}");

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationPolicy{TResource, TCommand, TPrincipal}"/> class.
        /// </summary>
        /// <param name="resourceType">Type of the resource to be authorized.</param>
        /// <param name="commandType">Type of the command to be authorized.</param>
        /// <param name="principalType">Type of the principal to be authorized.</param>
        public AuthorizationPolicy(Type resourceType, Type commandType, Type principalType) : base(resourceType, commandType, principalType)
        {
        }

        /// <summary>
        /// Gets the validation plan that is evaluated when an authorization operation is performed.
        /// </summary>
        public ValidationPlan<IAuthorizationQuery<TResource, TCommand, TPrincipal>> ValidationPlan { get; } = new ValidationPlan
            <IAuthorizationQuery<TResource, TCommand, TPrincipal>>
            {
                Default
            };
    }
}
