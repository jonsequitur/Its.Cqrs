using System;

namespace Microsoft.Its.Domain.Sql
{
    public class DbReadonlyUser
    {
        public string LoginName { get; private set; }
        public string UserName { get; private set; }

        public DbReadonlyUser(string loginName, string userName)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The loginName cannot be null, empty or contain only whitespace.");
            }
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("The userName cannot be null, empty or contain only whitespace.");
            }

            LoginName = loginName;
            UserName = userName;
        }
    }
}