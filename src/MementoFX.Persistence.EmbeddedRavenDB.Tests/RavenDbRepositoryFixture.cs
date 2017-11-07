using System;
using System.Linq;
using System.Threading;
using Xunit;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Moq;
using SharpTestsEx;
using MementoFX.Persistence.EmbeddedRavenDB.Tests.Events;
using MementoFX.Persistence.EmbeddedRavenDB.Tests.Model;
using Raven.Tests.Helpers;
using MementoFX.Messaging;
using MementoFX.Persistence.EmbeddedRavenDB.Indexes;
using MementoFX.Persistence.EmbeddedRavenDB.Listeners;

namespace MementoFX.Persistence.EmbeddedRavenDB.Tests
{
    
    public class RavenDbRepositoryFixture : RavenTestBase, IDisposable
    {
        private EmbeddableDocumentStore documentStore;
        private EmbeddedRavenDbEventStore EventStore;

        
        public RavenDbRepositoryFixture()
        {
            documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true
            };
            //documentStore.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
            documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;
            documentStore.Initialize();

            new DomainEvents_Stream().Execute(documentStore);
            new RavenDocumentsByEntityName().Execute(documentStore);

            var bus = new Mock<IEventDispatcher>().Object;
            var eventStore = new EmbeddedRavenDbEventStore(documentStore, bus);
            EventStore = eventStore;
        }

        void IDisposable.Dispose()
        {
            documentStore.Dispose();
        }

        [Fact]
        public void Ctor_should_throw_ArgumentNullException_on_null_eventStore_and_value_of_parameter_should_be_eventStore()
        {
            Executing.This(() => new Repository(null))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("eventStore");
        }

        //[Fact]
        //public void Test_EventCount()
        //{
        //    var currentAccountId = Guid.NewGuid();
        //    var accountOpening = new AccountOpenedEvent
        //    {
        //        CurrentAccountId = currentAccountId,
        //        Balance = 200
        //    };
        //    EventStore.Save(accountOpening);

        //    var withdrawal1 = new WithdrawalEvent()
        //    {
        //        CurrentAccountId = currentAccountId,
        //        Amount = 100
        //    };
        //    withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
        //    EventStore.Save(withdrawal1);

        //    WaitForIndexing(documentStore);
        //    var sut = new Repository(EventStore);
        //    var currentAccount = sut.GetById<CurrentAccount>(currentAccountId);
        //    Assert.Equal<int>(2, currentAccount.OccurredEvents.Count());
        //}


        //[Fact]
        //public void Test_EventCount_at_a_specific_date()
        //{
        //    var currentAccountId = Guid.NewGuid();
        //    var accountOpening = new AccountOpenedEvent
        //    {
        //        CurrentAccountId = currentAccountId,
        //        Balance = 200
        //    };
        //    EventStore.Save(accountOpening);

        //    var withdrawal1 = new WithdrawalEvent()
        //    {
        //        CurrentAccountId = currentAccountId,
        //        Amount = 100
        //    };
        //    withdrawal1.SetTimeStamp(DateTime.Now.AddMonths(1));
        //    EventStore.Save(withdrawal1);

        //    WaitForIndexing(documentStore);
        //    var sut = new Repository(EventStore);
        //    var currentAccount = sut.GetById<CurrentAccount>(currentAccountId, DateTime.Now.AddMonths(2));
        //    Assert.Equal<int>(2, currentAccount.OccurredEvents.Count());
        //}

        [Fact]
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
            var currentAccount = sut.GetById<CurrentAccount>(currentAccountId, DateTime.Now.AddMonths(2));
            Assert.Equal(100, currentAccount.Balance);
        }

        [Fact]
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
            var currentAccount = sut.GetById<CurrentAccount>(currentAccountId, DateTime.Now.AddMonths(2));

            Assert.Equal(100, currentAccount.Balance);
        }

        [Fact]
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
            var currentAccount = sut.GetById<CurrentAccount>(currentAccountId, DateTime.Now.AddMonths(3));
            Assert.Equal(100, currentAccount.Balance);
        }

        [Fact]
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
            var currentAccount = sut.GetById<CurrentAccount>(currentAccountId, timelineId, DateTime.Now.AddMonths(3));
            Assert.Equal(50, currentAccount.Balance);
        }
    }
}
