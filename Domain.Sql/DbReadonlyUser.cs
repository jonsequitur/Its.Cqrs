using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Represents a login for a readonly user of a database.
    /// </summary>
    public class DbReadonlyUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbReadonlyUser"/> class.
        /// </summary>
        /// <param name="loginName">Name of the login.</param>
        /// <param name="userName">Name of the user.</param>
        /// <exception cref="System.ArgumentException">
        /// The loginName cannot be null, empty or contain only whitespace.
        /// or
        /// The userName cannot be null, empty or contain only whitespace.
        /// </exception>
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

        /// <summary>
        /// The database login name.
        /// </summary>
        public string LoginName { get; }

        /// <summary>
        /// The database username.
        /// </summary>
        public string UserName { get; }
    }
}