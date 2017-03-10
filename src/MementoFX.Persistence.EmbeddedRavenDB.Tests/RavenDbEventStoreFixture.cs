using System;
using System.Linq;
using NUnit.Framework;
using SharpTestsEx;
using Moq;
using Raven.Client.Embedded;
using Memento.Messaging;
using Memento.Persistence.EmbeddedRavenDB.Indexes;
using Raven.Client.Indexes;
using Memento.Persistence.EmbeddedRavenDB.Listeners;

namespace Memento.Persistence.EmbeddedRavenDB.Tests
{
    [TestFixture]
    public class RavenDbEventStoreFixture
    {
        private EmbeddableDocumentStore documentStore;


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

            new DomainEvents_Stream().Execute(documentStore);
            new RavenDocumentsByEntityName().Execute(documentStore);
        }

        [TearDown]
        public void CleanUp()
        {
            documentStore.Dispose();
        }


        [Test]
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

        [Test]
        public void Ctor_should_throw_ArgumentNullException_on_null_bus_and_value_of_parameter_should_be_bus()
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

        [Test]
        public void Ctor_should_set_DocumentStore_field()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            var mock = new Mock<EmbeddableDocumentStore>().Object;
            var sut = new EmbeddedRavenDbEventStore(mock, bus);
            //Assert.AreEqual(mock, sut.DocumentStore);
            Assert.AreEqual(mock, EmbeddedRavenDbEventStore.DocumentStore);
        }

        [Test]
        public void Ctor_should_not_register_PatchDomainEventsApplyingMementoMetadata_listener_more_than_once()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            var store1 = new EmbeddedRavenDbEventStore(documentStore, bus);
            var store2 = new EmbeddedRavenDbEventStore(documentStore, bus);
            Assert.AreEqual(1, documentStore.Listeners.StoreListeners.OfType<PatchDomainEventsApplyingMementoMetadata>().Count());
        }
    }
}
