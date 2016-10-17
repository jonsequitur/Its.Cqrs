// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for working with projectors.
    /// </summary>
    public static class ProjectorExtensions
    {
        static ProjectorExtensions()
        {
            ReadModelUpdate.ConfigureUnitOfWork();
        }

        /// <summary>
        /// Creates a unit of work for projection updates within an <see cref="IUpdateProjectionWhen{T}" /> implementation.
        /// </summary>
        /// <typeparam name="TProjector">The type of the projector.</typeparam>
        /// <param name="projector">The projector.</param>
        /// <returns>A <see cref="UnitOfWork{ReadModelUpdate}" /> containing an initialized <see cref="ReadModelDbContext" /> and database transaction.</returns>
        /// <remarks>The DbContext can be accessed within the unit of work. If the database update operation is successful, the projector code should call VoteCommit or the transaction will be rolled back when the outermost <see cref="UnitOfWork{ReadModelUpdate}" /> is disposed.
        /// <c>
        /// using (var unitOfWork = this.Update())
        /// {
        ///     var dbContext = unitOfWork.Resource&lt;ReadModelDbContext&gt;();
        ///     
        ///     // do work
        /// 
        ///     unitOfWork.VoteCommit();
        /// }
        /// </c>
        /// </remarks>
        public static UnitOfWork<ReadModelUpdate> Update<TProjector>(this TProjector projector)
            where TProjector : class
        {
            var unitOfWork = new UnitOfWork<ReadModelUpdate>();

            unitOfWork.Subject
                      .Projectors
                      .Add(projector);

            EnsureDbContextIsInitialized(unitOfWork, () => CreateDbContext(projector));

            return unitOfWork;
        }

        internal static UnitOfWork<ReadModelUpdate> EnsureDbContextIsInitialized(
            this UnitOfWork<ReadModelUpdate> unitOfWork,
            Func<DbContext> createDbContext)
        {
            if (unitOfWork.Resource<DbContext>() == null)
            {
                var dbContext = createDbContext();

                // index the resource under DbContext...
                unitOfWork.AddResource(dbContext, true);

                // ...and other the actual registered type, if different, since callers will access it this way
                var actualType = dbContext.GetType();
                if (actualType != typeof (DbContext))
                {
                    unitOfWork.AddResource(actualType, dbContext, false);
                }
            }

            return unitOfWork;
        }

        internal static DbContext CreateDbContext(object projector) =>
            projector.IfTypeIs<IEntityModelProjector>()
                     .Then(emp => emp.CreateDbContext())
                     .Else(() => Configuration.Current.Container.Resolve<ReadModelDbContext>());
    }
}
