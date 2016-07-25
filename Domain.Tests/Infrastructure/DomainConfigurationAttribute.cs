// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Its.Domain.Tests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class DomainConfigurationAttribute : Attribute, ITestAction
    {
        private Configuration configuration;
        private CompositeDisposable disposables;

        public void BeforeTest(ITest test)
        {
            if (ConfigurationContext.Current == null)
            {
                configuration = new Configuration()
                    .TraceScheduledCommands();

                disposables = new CompositeDisposable
                              {
                                  VirtualClock.Start(),
                                  ConfigurationContext.Establish(configuration),
                                  configuration
                              };
            }
            else
            {
                configuration = Configuration.Current;
            }

            BeforeTest(test, configuration);
        }

        protected abstract void BeforeTest(ITest test, Configuration configuration);

        public void AfterTest(ITest test)
        {
            disposables?.Dispose();
        }

        public Configuration Configuration => configuration;

        public ActionTargets Targets { get; } = ActionTargets.Test;
    }
}