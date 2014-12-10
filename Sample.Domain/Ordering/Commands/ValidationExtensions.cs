using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain.Ordering.Commands
{
    public static class ValidationExtensions
    {
        public sealed class ActionIsRetryable
        {
            private ActionIsRetryable()
            {
            }

            public static ActionIsRetryable Instance = new ActionIsRetryable();
        }

        public static ValidationRule<T> Retryable<T>(this ValidationRule<T> o)
        {
            return o.With(ActionIsRetryable.Instance);
        }

        public static bool IsRetryable(this FailedEvaluation evaluation)
        {
            return evaluation.Result<ActionIsRetryable>() != null;
        }
    }
}