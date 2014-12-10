namespace Banking.Domain
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