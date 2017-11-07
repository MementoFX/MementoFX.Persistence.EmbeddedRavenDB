using System;
using System.Linq;
using Xunit;
using SharpTestsEx;
using Moq;
using Raven.Client.Embedded;
using MementoFX.Messaging;
using MementoFX.Persistence.EmbeddedRavenDB.Indexes;
using Raven.Client.Indexes;
using MementoFX.Persistence.EmbeddedRavenDB.Listeners;
using MementoFX.Persistence.EmbeddedRavenDB;

namespace MementoFX.Persistence.EmbeddedRavenDB.Tests
{
    public class RavenDbEventStoreFixture : IDisposable
    {
        private EmbeddableDocumentStore documentStore;

        public RavenDbEventStoreFixture()
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
        }

        void IDisposable.Dispose()
        {
            documentStore.Dispose();
        }


        [Fact]
        public void Ctor_should_throw_ArgumentNullException_on_null_documentStore_and_value_of_parameter_should_be_documentStore()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            Executing.This(() => new EmbeddedRavenDbEventStore(null, bus))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("documentStore");
        }

        [Fact]
        public void Ctor_should_throw_ArgumentNullException_on_null_bus_and_value_of_parameter_should_be_eventDispatcher()
        {
            var documentstore = new Mock<EmbeddableDocumentStore>().Object;
            Executing.This(() => new EmbeddedRavenDbEventStore(documentstore, null))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("eventDispatcher");
        }

        [Fact]
        public void Ctor_should_set_DocumentStore_field()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            var mock = new Mock<EmbeddableDocumentStore>().Object;
            var sut = new EmbeddedRavenDbEventStore(mock, bus);
            Assert.Equal(mock, EmbeddedRavenDbEventStore.DocumentStore);
        }

        [Fact]
        public void Ctor_should_not_register_PatchDomainEventsApplyingMementoMetadata_listener_more_than_once()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            var store1 = new EmbeddedRavenDbEventStore(documentStore, bus);
            var store2 = new EmbeddedRavenDbEventStore(documentStore, bus);
            Assert.Single(documentStore.Listeners.StoreListeners.OfType<PatchDomainEventsApplyingMementoMetadata>());
        }
    }
}
