// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Domain.Banking
{
    public partial class CheckingAccount
    {
        public void EnactCommand(DepositFunds command)
        {
            RecordEvent(new FundsDeposited
            {
                Amount = command.Amount
            });
        }

        public void EnactCommand(WithdrawFunds command)
        {
            RecordEvent(new FundsWithdrawn
            {
                Amount = command.Amount
            });

            if (IsOverdrawn)
            {
                RecordEvent(new Overdrawn
                {
                    Balance = Balance
                });
            }
        }

        public void EnactCommand(CloseCheckingAccount command)
        {
            RecordEvent(new Closed());
        }
    }
}
