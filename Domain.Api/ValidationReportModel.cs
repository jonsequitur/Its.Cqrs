// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Its.Validation;

namespace Microsoft.Its.Domain.Api
{
    internal class ValidationReportModel
    {
        private readonly ValidationReport validationReport;

        private readonly IList<Result> failures = new List<Result>();
        private readonly IList<Result> successes = new List<Result>();

        public ValidationReportModel(ValidationReport validationReport)
        {
            if (validationReport == null)
            {
                throw new ArgumentNullException("validationReport");
            }
            this.validationReport = validationReport;

            Initialize();
        }

        private void Initialize()
        {
            foreach (var failure in validationReport.Failures.Where(f => !string.IsNullOrEmpty(f.Message)))
            {
                Add(failure, failures);
            }

            foreach (var success in validationReport.Successes.Where(f => !string.IsNullOrEmpty(f.Message)))
            {
                Add(success, successes);
            }
        }

        private static void Add(RuleEvaluation failure, IList<Result> failures)
        {
            failures.Add(new Result
            {
                Message = failure.Message,
                Mitigation = failure.Result<CommandReference>()
                                    .IfNotNull()
                                    .Then(r => r.ToString())
                                    .ElseDefault()
            });
        }

        public IList<Result> Successes
        {
            get
            {
                return successes;
            }
        }

        public IList<Result> Failures
        {
            get
            {
                return failures;
            }
        }

        public class Result
        {
            public string Message { get; set; }
            public string Mitigation { get; set; }
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
