// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class NonEventSourcedAggregateCommandTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void Setup()
        {
            Command<Target>.AuthorizeDefault = (account, command) => true;

            disposables = new CompositeDisposable
            {
                ConfigurationContext.Establish(new Configuration()
                                                   .UseInMemoryEventStore(traceEvents: true))
            };
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task when_a_command_is_applied_directly_the_command_is_executed()
        {
            var target = new Target();
            await target.ApplyAsync(new CommandOnTarget());
            target.CommandsAppliedCount.Should().Be(1);
        }

        [Test]
        public async Task when_a_command_is_applied_directly_with_an_etag_the_command_is_executed()
        {
            var target = new Target();
            await new CommandOnTarget { ETag = Any.Guid().ToString() }.ApplyToAsync(target);
            target.CommandsAppliedCount.Should().Be(1);
        }

        [Test]
        public void command_validations_are_checked()
        {
            Action applyCommand = () => (new CommandOnTarget { FailCommandValidation = true }.ApplyToAsync(new Target())).Wait();

            applyCommand.ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void target_validations_are_checked()
        {
            Action applyCommand = () => (new CommandOnTarget().ApplyToAsync(new Target { FailCommandApplications = true })).Wait();

            applyCommand.ShouldThrow<CommandValidationException>();
        }
    }

    public class Target
    {
        public bool FailCommandApplications { get; set; }
        public int CommandsAppliedCount { get; set; }
    }

    public class CommandOnTargetCommandHandler : ICommandHandler<Target, CommandOnTarget>
    {
        private readonly IEventSourcedRepository<MarcoPoloPlayerWhoIsIt> repository;

        public CommandOnTargetCommandHandler(IEventSourcedRepository<MarcoPoloPlayerWhoIsIt> repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository;
        }

        public async Task EnactCommand(Target target, CommandOnTarget command)
        {
            target.CommandsAppliedCount++;
        }

        public async Task HandleScheduledCommandException(Target target, CommandFailed<CommandOnTarget> command)
        {
        }
    }

    public class CommandOnTarget : Command<Target>
    {
        public bool FailCommandValidation { get; set; }

        public override IValidationRule<Target> Validator
        {
            get
            {
                return Validate.That<Target>(t => !t.FailCommandApplications).WithMessage("Target failed command validation");
            }
        }

        public override IValidationRule CommandValidator
        {
            get
            {
                return Validate.That<CommandOnTarget>(t => !t.FailCommandValidation).WithMessage("Command failed validation");
            }
        }
    }
}