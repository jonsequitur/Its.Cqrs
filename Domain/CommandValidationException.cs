// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Its.Validation;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     An exception thrown when a command is invalid due its state or the state of an aggregate to which an attempt was made to apply the command.
    /// </summary>
    [Serializable]
    public class CommandValidationException : InvalidOperationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandValidationException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected CommandValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandValidationException" /> class.
        /// </summary>
        /// <param name="validationReport">The validation report.</param>
        /// <exception cref="System.ArgumentNullException">validationReport</exception>
        public CommandValidationException(ValidationReport validationReport) : this(validationReport.ToString(), validationReport)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandValidationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="validationReport">The validation report.</param>
        public CommandValidationException(string message, ValidationReport validationReport) : base(message + "\n" + validationReport)
        {
            ValidationReport = validationReport;
        }

        /// <summary>
        ///     Gets or sets the validation report containing validation failures that caused the exception to be thrown.
        /// </summary>
        /// <value>
        ///     The validation report.
        /// </value>
        public ValidationReport ValidationReport { get; private set; }
    }
}