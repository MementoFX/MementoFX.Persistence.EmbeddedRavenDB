using System;
using System.Threading;
using NUnit.Framework;
using Raven.Client.Embedded;
using Raven.Database.Server;
using Memento.Persistence.EmbeddedRavenDB.Tests.Events;
using Moq;
using SharpTestsEx;
using Memento.Persistence.EmbeddedRavenDB.Tests.Model;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using Memento.Messaging;

namespace Memento.Persistence.EmbeddedRavenDB.Tests
{
    [TestFixture]
    public class SelfRetrievingAggregatesFixture : RavenTestBase
    {
        private EmbeddableDocumentStore documentStore;
        private EmbeddedRavenDbEventStore EventStore;

        [SetUp]
        public void SetUp()
        {
            documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true
            };
            //documentStore.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
            documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;
            documentStore.Initialize();
            new RavenDocumentsByEntityName().Execute(documentStore);

            var bus = new Mock<IEventDispatcher>().Object;
            var eventStore = new EmbeddedRavenDbEventStore(documentStore, bus);
            EventStore = eventStore;
        }

        [TearDown]
        public void CleanUp()
        {
            documentStore.Dispose();
        }

        [Test]
        public void Test_EventReplaying_evaluating_CurrentAccountBalance_using_a_stream_containing_past_events_only()
        {
            var currentAccountId = Guid.NewGuid();
            var accountOpening = new AccountOpenedEvent
            {
                CurrentAccountId = currentAccountId,
                Balance = 200
            };
            EventStore.Save(accountOpening);

            var withdrawal1 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100
            };
            withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
            EventStore.Save(withdrawal1);

            WaitForIndexing(documentStore);
            var sut = new Repository(EventStore);
            var currentAccount = sut.GetById<SelfRetrievingCurrentAccount>(currentAccountId, DateTime.Now.AddMonths(2));
            Assert.AreEqual(100, currentAccount.Balance);
        }

        [Test]
        public void Test_EventReplaying_evaluating_CurrentAccountBalance_using_a_stream_containing_both_past_and_future_events()
        {
            var currentAccountId = Guid.NewGuid();
            var accountOpening = new AccountOpenedEvent
            {
                CurrentAccountId = currentAccountId,
                Balance = 200
            };
            EventStore.Save(accountOpening);

            var withdrawal1 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100
            };
            withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
            EventStore.Save(withdrawal1);

            var withdrawal2 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100
            };
            withdrawal2.SetTimeStamp(DateTime.Now.AddMonths(3));
            EventStore.Save(withdrawal2);

            WaitForIndexing(documentStore);
            var sut = new Repository(EventStore);
            var currentAccount = sut.GetById<SelfRetrievingCurrentAccount>(currentAccountId, DateTime.Now.AddMonths(2));
            Assert.AreEqual(100, currentAccount.Balance);
        }

        [Test]
        public void Test_EventReplaying_evaluating_CurrentAccountBalance_using_a_stream_containing_past_events_only_and_a_different_timeline()
        {
            var currentAccountId = Guid.NewGuid();
            var accountOpening = new AccountOpenedEvent
            {
                CurrentAccountId = currentAccountId,
                Balance = 200
            };
            EventStore.Save(accountOpening);

            var withdrawal1 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100
            };
            withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
            EventStore.Save(withdrawal1);

            var withdrawal2 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100,
                TimelineId = Guid.NewGuid()
            };
            withdrawal2.SetTimeStamp(DateTime.Now.AddMonths(3));
            EventStore.Save(withdrawal2);

            WaitForIndexing(documentStore);
            var sut = new Repository(EventStore);
            var currentAccount = sut.GetById<SelfRetrievingCurrentAccount>(currentAccountId, DateTime.Now.AddMonths(3));
            Assert.AreEqual(100, currentAccount.Balance);
        }

        [Test]
        public void Test_Timeline_specific_EventReplaying_evaluating_CurrentAccountBalance_using_a_stream_containing_both_past_and_future_events()
        {
            var currentAccountId = Guid.NewGuid();
            var timelineId = Guid.NewGuid();
            var accountOpening = new AccountOpenedEvent
            {
                CurrentAccountId = currentAccountId,
                Balance = 200,
                TimelineId = timelineId
            };
            EventStore.Save(accountOpening);

            var withdrawal1 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 100
            };
            withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
            EventStore.Save(withdrawal1);

            var withdrawal2 = new WithdrawalEvent()
            {
                CurrentAccountId = currentAccountId,
                Amount = 50,
                TimelineId = timelineId
            };
            withdrawal2.SetTimeStamp(DateTime.Now.AddMonths(2));
            EventStore.Save(withdrawal2);

            WaitForIndexing(documentStore);
            var sut = new Repository(EventStore);
            var currentAccount = sut.GetById<SelfRetrievingCurrentAccount>(currentAccountId, timelineId, DateTime.Now.AddMonths(3));
            Assert.AreEqual(50, currentAccount.Balance);
        }

    }
}
