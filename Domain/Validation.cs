// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Its.Validation;
using Its.Validation.Configuration;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for building validation rules.
    /// </summary>
    public static class Validation
    {
        private static readonly ConcurrentDictionary<Type, IValidationRule> defaultPlans = new ConcurrentDictionary<Type, IValidationRule>();

        /// <summary>
        /// Specifies a mitigation that can be used when the validation rule fails.
        /// </summary>
        /// <typeparam name="TTarget">The type of the object being validated.</typeparam>
        /// <param name="rule">The validation rule.</param>
        /// <param name="mitigation">The mitigation.</param>
        /// <returns></returns>
        public static ValidationRule<TTarget> WithMitigation<TTarget>(
                this ValidationRule<TTarget> rule,
                CommandReference mitigation) =>
            rule.With(mitigation);

        /// <summary>
        /// Specifies a mitigation that can be used when the validation rule fails.
        /// </summary>
        /// <param name="rule">The validation rule.</param>
        /// <param name="member">An expression specifying a command and member that can be used to mitigate the validation failure.</param>
        public static ValidationRule<TTarget> WithMitigation<TTarget>(
            this ValidationRule<TTarget> rule,
            Expression<Func<TTarget, object>> member) where TTarget : ICommand =>
            rule.WithMitigation(ReferTo.Command(member));

        internal static IValidationRule GetDefaultPlanFor(Type commandType) =>
            defaultPlans.GetOrAdd(commandType, type =>
            {
                var plan = CreateEmptyPlanFor(type);

                var configuredPlan = typeof(DataAnnotationsExtensions)
                    .GetMethod("ConfigureFromAttributes")
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { plan });

                return (IValidationRule) configuredPlan;
            });

        internal static IValidationRule CreateEmptyPlanFor(Type commandType) =>
            (IValidationRule) Activator.CreateInstance(typeof(ValidationPlan<>).MakeGenericType(commandType));
    }
}