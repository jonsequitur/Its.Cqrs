// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class RequestUserNameCommandHandler : ICommandHandler<CustomerAccount, RequestUserName>
        {
            public async Task EnactCommand(CustomerAccount customerAccount, RequestUserName command)
            {
                customerAccount.RecordEvent(new UserNameAcquired
                {
                    UserName = command.UserName
                });
            }

            public async Task HandleScheduledCommandException(CustomerAccount aggregate, ScheduledCommandFailure<RequestUserName> command)
            {
                if (command.Exception is ConcurrencyException)
                {
                    command.Retry(TimeSpan.Zero);
                }
            }
        }

        public void EnactCommand(ChangeEmailAddress command)
        {
            RecordEvent(new EmailAddressChanged
            {
                NewEmailAddress = command.NewEmailAddress
            });
        }

        public void EnactCommand(SendMarketingEmailOn command)
        {
            var sendMarketingEmail = new SendMarketingEmail();
            ScheduleCommand(sendMarketingEmail, command.Date);
        }

        public class CustomerAccountSendMarketingEmailCommandHandler : ICommandHandler<CustomerAccount, SendMarketingEmail>
        {
            private readonly ICommandScheduler<Order> scheduler;

            public CustomerAccountSendMarketingEmailCommandHandler(ICommandScheduler<Order> scheduler)
            {
                if (scheduler == null)
                {
                    throw new ArgumentNullException("scheduler");
                }
                this.scheduler = scheduler;
            }

            public async Task EnactCommand(CustomerAccount customerAccount, SendMarketingEmail command)
            {
                var now = Clock.Now();

                customerAccount.RecordEvent(new MarketingEmailSent
                {
                    EmailSubject = new EmailSubject(string.Format("Weekly Specials ({0})", now.ToString("MM d, yyyy")))
                });

                // schedule the next email if one is not already scheduled
                if (!AggregateExtensions.Events(customerAccount)
                    .OfType<CommandScheduled<CustomerAccount>>()
                    .Where(e => e.Command is SendMarketingEmail)
                    .Any(e => e.DueTime > now))
                {
                    try
                    {
                        await scheduler.Schedule(Guid.NewGuid(),
                            new AddItem
                            {
                                ProductName = "Appreciation stickers",
                                Price = 0m
                            });
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }

                    customerAccount.Apply(new SendMarketingEmailOn(now.AddDays(7)));
                }
            }

            public async Task HandleScheduledCommandException(CustomerAccount aggregate, ScheduledCommandFailure<SendMarketingEmail> command)
            {
            }
        }

        public void EnactCommand(SendOrderConfirmationEmail command)
        {
            RecordEvent(new OrderShipConfirmationEmailSent());
        }

        public void EnactCommand(RequestNoSpam command)
        {
            RecordEvent(new RequestedNoSpam());
        }

        public void EnactCommand(RequestSpam command)
        {
            RecordEvent(new RequestedSpam());
        }

        public class OrderEmailConfirmer : ICommandHandler<CustomerAccount, NotifyOrderCanceled>
        {
            public async Task EnactCommand(CustomerAccount aggregate, NotifyOrderCanceled command)
            {
                await SendOrderConfirmationEmail(aggregate.EmailAddress);

                // then...
                aggregate.RecordEvent(new OrderCancelationConfirmationEmailSent());
            }

            public async Task HandleScheduledCommandException(
                CustomerAccount order,
                ScheduledCommandFailure<NotifyOrderCanceled> command)
            {
            }

            public Func<dynamic, Task> SendOrderConfirmationEmail = _ => Task.Run(() => { });
        }
    }
}