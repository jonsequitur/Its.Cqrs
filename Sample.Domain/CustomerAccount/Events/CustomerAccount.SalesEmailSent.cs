namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class MarketingEmailSent : EmailSent
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.CommunicationsSent.Add(EmailSubject);
            }

            public EmailSubject EmailSubject { get; set; }
        }
    }
}