// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using Its.Validation;
using Its.Validation.Configuration;
using Moq;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using Microsoft.Its.Domain.Testing;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class AggregateCommandTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void SetUp()
        {
            // disable authorization
            Command<FakeAggregateWithEnactCommandConvention>.AuthorizeDefault = (o, c) => true;
            Command<FakeAggregateWithNestedCommandConvention>.AuthorizeDefault = (o, c) => true;
            Command<Order>.AuthorizeDefault = (o, c) => true;

            disposables = new CompositeDisposable
            {
                ConfigurationContext.Establish(new Configuration()
                                                   .IgnoreScheduledCommands())
            };
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void Default_CommandValidator_uses_DataAnnotations()
        {
            var command = new CommandWithDataAnnotations();

            var aggregate = new FakeAggregateWithEnactCommandConvention();
            var report = aggregate.Validate(command);

            report
                .Failures
                .Should()
                .Contain(f => f.MemberPath == "Name");
        }

        [Test]
        public void Default_CommandValidator_for_ConstructorCommand_uses_DataAnnotations()
        {
            var command = new ConstructorCommandWithDataAnnotations();

            var aggregate = new FakeAggregateWithEnactCommandConvention();
            var report = aggregate.Validate(command);

            report
                .Failures
                .Should()
                .Contain(f => f.MemberPath == "Name");
        }

        [Test]
        public void When_command_state_is_invalid_then_calling_ApplyTo_throws()
        {
            var command = new CommandWithAggregateValidator();

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldThrow<CommandValidationException>()
                   .And
                   .Message.Should().Contain("Validation error while applying CommandWithAggregateValidator to a FakeAggregateWithEnactCommandConvention")
                   .And
                   .Contain("The Name field is required");
        }

        [Test]
        public void When_aggregate_state_is_invalid_then_calling_ApplyTo_throws()
        {
            var command = new CommandWithAggregateValidator
            {
                Name = "foo"
            };

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldThrow<CommandValidationException>()
                   .And
                   .Message.Should().Contain("Ain't valid!");
        }

        [Test]
        public void An_aggregate_can_record_an_event_for_a_command_that_fails_during_validation_using_HandleCommandValidationFailure()
        {
            var order = new Order(new CreateOrder(Any.FullName()))
                .Apply(new AddItem
                {
                    Price = 10m,
                    ProductName = Any.CompanyName()
                })
                .Apply(new Ship())
                .Apply(new ChargeCreditCard
                {
                    Amount = 10m,
                    CallPaymentService = _ => { throw new ArgumentException("Insufficient funds!"); }
                });

            order.PendingEvents
                 .Last()
                 .Should()
                 .BeOfType<Order.CreditCardChargeRejected>();
        }

        [Test]
        public void A_command_can_call_a_domain_service_and_cache_the_value_and_set_its_ETag_on_success()
        {
            var chargeCreditCard = new ChargeCreditCard
            {
                Amount = 10m
            };
            var order = new Order(new CreateOrder(Any.FullName()))
                .Apply(new AddItem
                {
                    Price = 10m,
                    ProductName = Any.CompanyName()
                })
                .Apply(new Ship())
                .Apply(chargeCreditCard);

            var charged = order.PendingEvents
                               .Last()
                               .As<Order.CreditCardCharged>();

            charged.ETag.Should().Be(chargeCreditCard.ETag);
            charged.Amount.Should().Be(chargeCreditCard.Amount);
        }

        [Test]
        public void An_aggregate_can_choose_whether_to_throw_for_a_failed_command_using_HandleCommandValidationFailure()
        {
            Action charge = () => new Order(new CreateOrder(Any.FullName()))
                                      .Apply(new ChargeCreditCard());

            charge.ShouldThrow<CommandValidationException>()
                  .And
                  .Message
                  .Should()
                  .Contain("The field Amount must be between 0.01 and 1.79769313486232E+308.");
        }

        [Test]
        public void When_command_validator_is_not_of_the_correct_generic_type_then_an_informative_exception_is_thrown()
        {
            var command = new CommandWithBadCommandValidator();

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldThrow<InvalidOperationException>()
                   .And
                   .Message.Should().Contain("Property CommandValidator returned a validator of the wrong type.");
        }

        [Test]
        public void When_command_validator_is_IValidationrule_of_the_correct_generic_type_then_no_exception_is_thrown()
        {
            var command = new CommandWithCommandValidator(Validate.That<CommandWithCommandValidator>(s => true));

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldNotThrow();
        }

        [Test]
        public void Authorization_can_be_specified_in_the_command_class()
        {
            var command = new CommandWithCustomAuthorization
            {
                Authorized = () => false
            };

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldThrow<CommandAuthorizationException>();

            command.Authorized = () => true;

            command.Invoking(c => c.ApplyTo(new FakeAggregateWithEnactCommandConvention()))
                   .ShouldNotThrow();
        }

        [Test]
        public void When_AppliesToVersion_is_specified_on_command_and_does_not_match_aggregate_Version_then_Apply_throws()
        {
            var order = new Order();
            order.Apply(new AddItem { Price = 10, ProductName = "Widget" })
                 .Apply(new Ship())
                 .Apply(new ProvideCreditCardInfo
                 {
                     CreditCardCvv2 = "123",
                     CreditCardExpirationMonth = "09",
                     CreditCardExpirationYear = "2071",
                     CreditCardNumber = "2344123412341234",
                     CreditCardName = Any.FullName()
                 })
                 .Apply(new ChargeCreditCard
                 {
                     Amount = 10,
                 })
                 .Apply(new Deliver())
                 .ConfirmSave();

            var actualVersion = order.Version();

            var requiredVersion = actualVersion - 1;
            var command = new Cancel
            {
                AppliesToVersion = requiredVersion
            };

            Action applyCancel = () => order.Apply(command);

            applyCancel.ShouldThrow<ConcurrencyException>()
                       .WithMessage(string.Format("The command's AppliesToVersion value ({0}) does not match the aggregate's version ({1})", requiredVersion, actualVersion));
        }

        [Test]
        public void When_AppliesToVersion_is_specified_on_command_and_does_indeed_match_aggregate_Version_then_Apply_does_not_throw()
        {
            var order = new Order();
            order.ConfirmSave();

            var actualVersion = order.Version();

            var requiredVersion = actualVersion;
            var command = new Cancel
            {
                AppliesToVersion = requiredVersion
            };

            Action applyCancel = () => order.Apply(command);

            applyCancel.ShouldNotThrow("Because the AppliesToVersion value ({0}) matches the aggregate's version ({1})", requiredVersion, actualVersion);
        }

        [Test]
        public void Command_T_KnownTypes_returns_nested_types()
        {
            Command<FakeAggregateWithNestedCommandConvention>.KnownTypes
                                                             .Should()
                                                             .Contain(t => t == typeof (FakeAggregateWithNestedCommandConvention.CommandWithAggregateValidator));
        }

        [Test]
        public void Named_returns_nested_types()
        {
            Command<FakeAggregateWithNestedCommandConvention>.Named("CommandWithAggregateValidator")
                                                             .Should()
                                                             .Be(typeof (FakeAggregateWithNestedCommandConvention.CommandWithAggregateValidator));
        }

        [Test]
        public void CommandValidator_rules_based_on_data_annotations_attributes_are_cached()
        {
            var command1 = new CommandWithDataAnnotations();
            var command2 = new CommandWithDataAnnotations();

            command1.CommandValidator.Should().BeSameAs(command2.CommandValidator);
        }

        [Test]
        public void Custom_CommandValidator_rules_are_not_cached()
        {
            var command1 = new CommandWithCommandValidator(Validate.That<CommandWithCommandValidator>(t => true));
            var command2 = new CommandWithCommandValidator(Validate.That<CommandWithCommandValidator>(t => true));

            command1.CommandValidator.Should().NotBeSameAs(command2.CommandValidator);
        }

        [Test]
        public void Commands_can_be_made_idempotent_by_setting_an_ETag()
        {
            var newName = Any.FullName();
            var etag = Any.Word();
            var order = new Order(new CreateOrder(Any.FullName()));

            order.Apply(new ChangeCustomerInfo
            {
                CustomerName = newName,
                ETag = etag
            });

            order.ConfirmSave();

            order.Apply(new ChangeCustomerInfo
            {
                CustomerName = Any.FullName(),
                ETag = etag
            });

            order.CustomerName.Should().Be(newName);
        }

        [Test]
        public void ICommandHandler_implementations_have_dependencies_injected()
        {
            Configuration.Current.UseDependency<IPaymentService>(_ => new CreditCardPaymentGateway());

            var order = new Order(new CreateOrder(Any.FullName()))
                .Apply(new AddItem
                {
                    Price = 5m,
                    ProductName = Any.Word()
                })
                .Apply(new Ship())
                .Apply(new ChargeAccount
                {
                    AccountNumber = Any.PositiveInt().ToString()
                });

            order.Events()
                 .Last()
                 .Should()
                 .BeOfType<Order.PaymentConfirmed>();
        }

        [Test]
        public void A_command_handler_can_record_an_event_for_a_failed_domain_service_call()
        {
            var paymentService = new Mock<IPaymentService>();
            paymentService.Setup(m => m.Charge(It.IsAny<decimal>()))
                          .Throws(new InvalidOperationException("Account does not exist"));

            Configuration.Current.UseDependency(_ => paymentService.Object);

            var order = new Order(new CreateOrder(Any.FullName()))
                .Apply(new AddItem
                {
                    Price = 5m,
                    ProductName = Any.Word()
                })
                .Apply(new Ship())
                .Apply(new ChargeAccount
                {
                    AccountNumber = Any.PositiveInt().ToString()
                });

            order.PendingEvents
                 .Last()
                 .Should()
                 .BeOfType<Order.ChargeAccountChargeRejected>();
        }

        [Test]
        public async Task A_command_can_be_applied_asynchronously()
        {
            Configuration.Current.UseDependency<IPaymentService>(_ => new CreditCardPaymentGateway());
            var order = new Order(new CreateOrder(Any.FullName()));
            order.Apply(new AddItem
            {
                Price = 5m,
                ProductName = Any.Word()
            });

            await order.ApplyAsync(new Ship());

            order.Events()
                 .Last()
                 .Should()
                 .BeOfType<Order.Shipped>();
        }

        public class FakeAggregateWithEnactCommandConvention : EventSourcedAggregate<FakeAggregateWithEnactCommandConvention>
        {
            public FakeAggregateWithEnactCommandConvention(ConstructorCommand<FakeAggregateWithEnactCommandConvention> createCommand) : base(createCommand)
            {
            }

            public bool IsValid = true;

            public FakeAggregateWithEnactCommandConvention(Guid? id = null) : base(id)
            {
            }

            public FakeAggregateWithEnactCommandConvention(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
            {
            }

            public void EnactCommand(CommandWithAggregateValidator command)
            {
            }

            public void EnactCommand(CommandWithCommandValidator command)
            {
            }

            public void EnactCommand(CommandWithCustomAuthorization command)
            {
            }
        }

        public class FakeAggregateWithNestedCommandConvention : IEventSourced
        {
            public FakeAggregateWithNestedCommandConvention()
            {
            }

            public FakeAggregateWithNestedCommandConvention(Guid id, IEnumerable<IEvent> history)
            {
                Id = id;
                PendingEvents = history;
            }

            public Guid Id { get; private set; }

            public long Version
            {
                get
                {
                    return PendingEvents.Max(e => e.SequenceNumber);
                }
            }

            public IEnumerable<IEvent> PendingEvents { get; private set; }
            public void ConfirmSave()
            {
                throw new NotImplementedException();
            }

            public bool IsValid = true;

            public class CommandWithAggregateValidator : Command<FakeAggregateWithNestedCommandConvention>
            {
                [Required]
                public string Name { get; set; }

                public override IValidationRule<FakeAggregateWithNestedCommandConvention> Validator
                {
                    get
                    {
                        return Validate
                            .That<FakeAggregateWithNestedCommandConvention>(o => IsValid)
                            .WithErrorMessage("Ain't valid!");
                    }
                }

                public bool IsValid { get; set; }
            }
        }

        public class CommandWithCommandValidator : Command<FakeAggregateWithEnactCommandConvention>
        {
            private readonly IValidationRule commandValidator;

            public CommandWithCommandValidator(IValidationRule commandValidator)
            {
                this.commandValidator = commandValidator;
            }

            public override IValidationRule<FakeAggregateWithEnactCommandConvention> Validator
            {
                get
                {
                    return Validate.That<FakeAggregateWithEnactCommandConvention>(o => o.IsValid);
                }
            }

            public override IValidationRule CommandValidator
            {
                get
                {
                    return commandValidator;
                }
            }
        }

        public class CommandWithBadCommandValidator : Command<FakeAggregateWithEnactCommandConvention>
        {
            public override IValidationRule CommandValidator
            {
                get
                {
                    return Validate.That<string>(t => true);
                }
            }
        }

        public class CommandWithCustomAuthorization : Command<FakeAggregateWithEnactCommandConvention>
        {
            public override IValidationRule<FakeAggregateWithEnactCommandConvention> Validator
            {
                get
                {
                    return Validate.That<FakeAggregateWithEnactCommandConvention>(a => true);
                }
            }

            public override bool Authorize(FakeAggregateWithEnactCommandConvention aggregate)
            {
                return Authorized();
            }

            public Func<bool> Authorized = () => false;
        }

        public class CommandWithAggregateValidator : Command<FakeAggregateWithEnactCommandConvention>
        {
            [Required]
            public string Name { get; set; }

            public override IValidationRule<FakeAggregateWithEnactCommandConvention> Validator
            {
                get
                {
                    return Validate
                        .That<FakeAggregateWithEnactCommandConvention>(o => IsValid)
                        .WithErrorMessage("Ain't valid!");
                }
            }

            public bool IsValid { get; set; }
        }

        public class CommandWithDataAnnotations : Command<FakeAggregateWithEnactCommandConvention>
        {
            [Required]
            public string Name { get; set; }
        }

        public class ConstructorCommandWithDataAnnotations : ConstructorCommand<FakeAggregateWithEnactCommandConvention>
        {
            [Required]
            public string Name { get; set; }

            public override IValidationRule CommandValidator
            {
                get
                {
                    var baseValidations = (IValidationRule<ConstructorCommandWithDataAnnotations>) base.CommandValidator;

                    return new ValidationPlan<ConstructorCommandWithDataAnnotations>
                    {
                        baseValidations
                    };
                }
            }
        }

        public class NonEventSourcedCommand : Command<object>
        {
            public override bool Authorize(object aggregate)
            {
                return true;
            }
        }
    }
}
