// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using Its.Validation;
using Its.Validation.Configuration;

namespace Microsoft.Its.Domain.Authorization
{
    public class AuthorizationPolicy
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type, Type>, AuthorizationPolicy> policies =
            new ConcurrentDictionary<Tuple<Type, Type, Type>, AuthorizationPolicy>();

        public AuthorizationPolicy(Type resourceType, Type commandType, Type principalType)
        {
            if (resourceType == null)
            {
                throw new ArgumentNullException("resourceType");
            }
            if (commandType == null)
            {
                throw new ArgumentNullException("commandType");
            }
            if (principalType == null)
            {
                throw new ArgumentNullException("principalType");
            }
            ResourceType = resourceType;
            CommandType = commandType;
            PrincipalType = principalType;
        }

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
            while (principalType.Assembly.IsDynamic)
            {
                principalType = principalType.BaseType;
            }
            return principalType;
        }

        public Type PrincipalType { get; private set; }

        public Type CommandType { get; private set; }

        public Type ResourceType { get; private set; }
    }

    public class AuthorizationPolicy<TResource, TCommand, TPrincipal> : AuthorizationPolicy
        where TPrincipal : IPrincipal
        where TCommand : ICommand
    {
        private readonly ValidationPlan<IAuthorizationQuery<TResource, TCommand, TPrincipal>> validationPlan =
            new ValidationPlan<IAuthorizationQuery<TResource, TCommand, TPrincipal>>
            {
                Default
            };

        internal static IValidationRule<IAuthorizationQuery<TResource, TCommand, TPrincipal>> Default =
            new ValidationRule<IAuthorizationQuery<TResource, TCommand, TPrincipal>>(q => false)
                .WithErrorMessage(
                    string.Format("Unauthorized by default because no authorization rules have been specified for {0}-{1}-{2}",
                                  typeof (TResource),
                                  typeof (TCommand),
                                  typeof (TPrincipal)));

        public AuthorizationPolicy(Type resourceType, Type commandType, Type principalType) : base(resourceType, commandType, principalType)
        {
        }

        public ValidationPlan<IAuthorizationQuery<TResource, TCommand, TPrincipal>> ValidationPlan
        {
            get
            {
                return validationPlan;
            }
        }
    }
}
