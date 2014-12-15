// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Transactions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using log = Its.Log.Lite.Log;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Represents a single atomic update to a read model database.
    /// </summary>
    public class ReadModelUpdate
    {
        /// <summary>
        /// Configures <see cref="UnitOfWork{ReadModelUpdate}" /> to provide DbContext sharing and database transactions.
        /// </summary>
        public static void ConfigureUnitOfWork()
        {
            // creating a new unit of work starts a transaction
            UnitOfWork<ReadModelUpdate>.Create = CreateUnitOfWork;
            UnitOfWork<ReadModelUpdate>.Commit = CommitUnitOfWork;
            UnitOfWork<ReadModelUpdate>.Reject = RejectUnitOfWork;
        }

        private static void CreateUnitOfWork(
            UnitOfWork<ReadModelUpdate> unitOfWork,
            Action<ReadModelUpdate> setSubject)
        {
            var update = new ReadModelUpdate
            {
                Transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                })
            };
            setSubject(update);
        }

        private static void RejectUnitOfWork(UnitOfWork<ReadModelUpdate> unitOfWork)
        {
            var t = unitOfWork.Subject.Transaction;
            if (t != null)
            {
                t.Dispose();
            }

            var exception = unitOfWork.Exception;
            if (exception != null)
            {
                unitOfWork.Subject.Projectors.ForEach(p => ReportFailure(
                    new Domain.EventHandlingError(
                        exception,
                        handler: p,
                        @event: unitOfWork.Resource<IEvent>()),
                    () => ProjectorExtensions.CreateDbContext(p)));
            }
        }

        private static void CommitUnitOfWork(UnitOfWork<ReadModelUpdate> unitOfWork)
        {
            try
            {
                var transaction = unitOfWork.Subject.Transaction;

                using (transaction)
                {
                    unitOfWork.Resource<DbContext>().SaveChanges();
                    transaction.Complete();
                }
            }
            catch (Exception exception)
            {
                unitOfWork.RejectDueTo(exception);
            }
        }

        internal readonly HashSet<object> Projectors = new HashSet<object>();

        private TransactionScope Transaction { get; set; }

        internal static void ReportFailure(
            Domain.EventHandlingError error,
            Func<DbContext> createDbContext)
        {
            // add an EventHandlingError entry as well
            EventHandlingError sqlError = CreateEventHandlingError((dynamic) error);

            var errorText = new
            {
                error.Exception,
                Event = sqlError.SerializedEvent
            }.ToJson();

            log.Write(() => errorText);

            using (var transaction = new TransactionScope(TransactionScopeOption.Suppress))
            using (var db = createDbContext())
            {
                var dbSet = db.Set<ReadModelInfo>();

                var handler = error.Handler;

                string readModelInfoName = null;

                if (handler != null)
                {
                    // update the affected ReadModelInfo
                    readModelInfoName = ReadModelInfo.NameForProjector(handler);

                    var readModelInfo = dbSet.SingleOrDefault(i => i.Name == readModelInfoName);
                    if (readModelInfo == null)
                    {
                        readModelInfo = new ReadModelInfo { Name = readModelInfoName };
                        dbSet.Add(readModelInfo);
                    }

                    readModelInfo.Error = errorText;
                    readModelInfo.FailedOnEventId = sqlError.OriginalId;
                }

                sqlError.Error = error.Exception.ToJson();
                sqlError.Handler = readModelInfoName;
                db.Set<EventHandlingError>().Add(sqlError);

                db.SaveChanges();
                transaction.Complete();
            }
        }

        private static EventHandlingError CreateEventHandlingError(Domain.EventHandlingError e)
        {
            return new EventHandlingError
            {
                Actor = e.Event.Actor(),
                AggregateId = e.Event.AggregateId,
                SequenceNumber = e.Event.SequenceNumber,
                SerializedEvent = e.Event.ToJson(),
                StreamName = e.Event.EventStreamName(),
                EventTypeName = e.Event.EventName(),
                OriginalId = e.Event
                              .IfTypeIs<IHaveExtensibleMetada>()
                              .Then(ee => ee.Try(eee => eee.Metadata.AbsoluteSequenceNumber,
                                                 ignore: ex => true))
                              .Else(() => (long?) null)
            };
        }

        private static EventHandlingError CreateEventHandlingError(IEvent e)
        {
            return new EventHandlingError
            {
                Actor = e.Actor(),
                AggregateId = e.AggregateId,
                SequenceNumber = e.SequenceNumber,
                SerializedEvent = e.ToJson(),
                StreamName = e.EventStreamName(),
                EventTypeName = e.EventName(),
                OriginalId = null
            };
        }

        private static EventHandlingError CreateEventHandlingError(EventHandlingDeserializationError e)
        {
            return new EventHandlingError
            {
                Actor = e.Actor,
                AggregateId = e.AggregateId,
                SequenceNumber = e.SequenceNumber,
                SerializedEvent = e.Body,
                StreamName = e.StreamName,
                EventTypeName = e.Type,
                OriginalId = e.Try(ee => ee.Metadata.AbsoluteSequenceNumber, ignore: ex => true)
                              .Else(() => (long?) null)
            };
        }

        private static EventHandlingError CreateEventHandlingError(StorableEvent e)
        {
            return new EventHandlingError
            {
                Actor = e.Actor,
                AggregateId = e.AggregateId,
                SequenceNumber = e.SequenceNumber,
                SerializedEvent = e.Body,
                StreamName = e.StreamName,
                EventTypeName = e.Type,
                OriginalId = e.Id
            };
        }
    }
}