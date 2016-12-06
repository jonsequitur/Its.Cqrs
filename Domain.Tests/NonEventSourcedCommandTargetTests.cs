// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using static Microsoft.Its.Domain.Tests.NonEventSourcedCommandTarget;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    public class NonEventSourcedCommandTargetTests
    {
        [Test]
        public async Task when_a_command_is_applied_directly_the_command_is_executed()
        {
            var target = new NonEventSourcedCommandTarget();
            await target.ApplyAsync(new TestCommand());
            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public void command_validations_are_checked()
        {
            Action applyCommand = () => new TestCommand { IsValid = false }
                                            .ApplyToAsync(new NonEventSourcedCommandTarget()).Wait();

            applyCommand.ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void target_validations_are_checked()
        {
            Action applyCommand = () => new TestCommand()
                                            .ApplyToAsync(new NonEventSourcedCommandTarget { IsValid = false }).Wait();

            applyCommand.ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void Commands_can_be_made_idempotent_by_setting_an_ETag()
        {
            var etag = Any.Guid().ToString().ToETag();

            var target = new NonEventSourcedIdempotentCommandTarget();

            var firstCommand = new TestCommand
            {
                ETag = etag
            };

            var secondCommand = new TestCommand
            {
                ETag = etag
            };

            target.Apply(firstCommand);
            target.Apply(secondCommand);

            target.CommandsEnacted
                  .Should()
                  .OnlyContain(c => c == firstCommand);
        }
    }
}