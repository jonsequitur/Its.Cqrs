using System;
using System.Diagnostics;
using System.Linq;
using Its.Validation;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Assertions to help with testing code build on Its.Domain.
    /// </summary>
    [DebuggerStepThrough]
    public static class AssertionExtensions
    {
        /// <summary>
        /// Asserts that a validation passed.
        /// </summary>
        /// <param name="validationReport">The validation report.</param>
        /// <exception cref="Microsoft.Its.Domain.Testing.AssertionException"></exception>
        public static void ShouldBeValid(this ValidationReport validationReport)
        {
            if (validationReport.HasFailures)
            {
                var msg = string.Format("Expected validation report to be valid but it was not.{0}{0}{1}", Environment.NewLine, validationReport);
                throw new AssertionException(msg);
            }
        }

        /// <summary>
        /// Asserts that a validation fails with a specific message.
        /// </summary>
        /// <param name="validationReport">The validation report.</param>
        /// <param name="withMessage">The message that is expected on at least one failure in the validation report.</param>
        /// <exception cref="Microsoft.Its.Domain.Testing.AssertionException">
        /// </exception>
        public static void ShouldBeInvalid(
            this ValidationReport validationReport,
            string withMessage)
        {
            if (!validationReport.HasFailures)
            {
                var msg = string.Format("Expected validation report to have failures but it did not.{0}{0}{1}", Environment.NewLine, validationReport);
                throw new AssertionException(msg);
            }

            if (validationReport.Failures.All(f => f.Message != withMessage))
            {
                throw new AssertionException(string.Format("Expected validation report to have a failure with message \"{0}\" but it did not.\n\nFailures:\n\n{1}", withMessage, validationReport));
            }
        }
    }
}