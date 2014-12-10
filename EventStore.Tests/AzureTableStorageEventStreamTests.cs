using System;
using System.Linq;
using Microsoft.Its.Configuration;
using Microsoft.Its.EventStore.AzureTableStorage;
using Microsoft.Its.Log.Instrumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;

namespace Microsoft.Its.EventStore.Tests
{
    [TestFixture, TestClass]
    public class AzureTableStorageEventStreamTests : CommonEventStreamTests
    {
        public AzureTableStorageEventStreamTests()
        {
            // the actual credentials for Azure Table Storage are stored outside the source tree
            Settings.Sources = new ISettingsSource[] { new ConfigDirectorySettings(@"c:\dev\.config") }
                .Concat(Settings.Sources);
            Formatter<StoredEvent>.RegisterForAllMembers();
        }

        protected override EventStream GetEventStream()
        {
            return new EventStream(
                GetType().Name,
                CloudStorageAccount.Parse(Settings.Get<TableStorageSettings>().ConnectionString));
        }
    }
}