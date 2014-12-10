using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Its.Validation;
using Its.Validation.Configuration;

namespace Microsoft.Its.Domain
{
    public static class Validation
    {
        private static readonly ConcurrentDictionary<Type, IValidationRule> defaultPlans = new ConcurrentDictionary<Type, IValidationRule>();

        public static ValidationRule<TTarget> WithMitigation<TTarget>(
            this ValidationRule<TTarget> rule,
            CommandReference mitigation)
        {
            return rule.With(mitigation);
        }

        public static ValidationRule<TTarget> WithMitigation<TTarget>(
            this ValidationRule<TTarget> rule,
            Expression<Func<TTarget, object>> member) where TTarget : ICommand
        {
            return rule.WithMitigation(ReferTo.Command(member));
        }

        internal static IValidationRule GetDefaultPlanFor(Type commandType)
        {
            return defaultPlans.GetOrAdd(commandType, type =>
            {
                var plan = CreateEmptyPlanFor(type);

                var configuredPlan = typeof (DataAnnotationsExtensions)
                    .GetMethod("ConfigureFromAttributes")
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { plan });

                return (IValidationRule) configuredPlan;
            });
        }

        internal static IValidationRule CreateEmptyPlanFor(Type commandType)
        {
            return (IValidationRule) Activator.CreateInstance(typeof (ValidationPlan<>).MakeGenericType(commandType));
        }
    }
}