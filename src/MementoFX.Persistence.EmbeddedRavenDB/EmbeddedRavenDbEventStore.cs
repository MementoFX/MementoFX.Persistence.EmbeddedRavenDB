using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Embedded;
using Raven.Client.Linq;
using Raven.Database.Server;
using Raven.Abstractions.Data;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using MementoFX.Domain;
using MementoFX.Messaging;
using MementoFX.Persistence;
using MementoFX.Persistence.EmbeddedRavenDB.Listeners;
using MementoFX.Persistence.EmbeddedRavenDB.Indexes;

namespace MementoFX.Persistence.EmbeddedRavenDB
{
    /// <summary>
    /// Provides an implementation of a Memento event store
    /// using Embedded RavenDB as the storage
    /// </summary>
    public class EmbeddedRavenDbEventStore : EventStore
    {
        /// <summary>
        /// Gets or sets the reference to the document store instance
        /// </summary>
        public static EmbeddableDocumentStore DocumentStore { get; private set; }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public EmbeddedRavenDbEventStore(IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (eventDispatcher == null)
                throw new ArgumentNullException(nameof(eventDispatcher));

            if (DocumentStore == null)
            {
                lock(DocumentStore)
                {
                    NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(Config.EventStorePort);
                    DocumentStore = new EmbeddableDocumentStore
                    {
                        ConnectionStringName = "EventStore",
                        UseEmbeddedHttpServer = true
                    };
                    DocumentStore.Configuration.Port = Config.EventStorePort;
                    DocumentStore.Initialize();
                    SetupDocumentStore();
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="documentStore">The document store to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public EmbeddedRavenDbEventStore(EmbeddableDocumentStore documentStore, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));
            if (eventDispatcher == null)
                throw new ArgumentNullException(nameof(eventDispatcher));

            DocumentStore = documentStore;
            SetupDocumentStore();
        }

        private void SetupDocumentStore()
        {
            if(DocumentStore.Listeners.StoreListeners.OfType<PatchDomainEventsApplyingMementoMetadata>().Count()==0)
                DocumentStore.RegisterListener(new PatchDomainEventsApplyingMementoMetadata());
            new DomainEvents_Stream().Execute(DocumentStore);
            new RavenDocumentsByEntityName().Execute(DocumentStore);
        }

        /// <summary>
        /// Retrieves all events of a type which satisfy a requirement
        /// </summary>
        /// <typeparam name="T">The type of the event</typeparam>
        /// <param name="filter">The requirement</param>
        /// <returns>The events which satisfy the given requirement</returns>
        public override IEnumerable<T> Find<T>(Func<T, bool> filter)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var events = session.Query<T>().Where(filter);
                return events;
            }
        }

        /// <summary>
        /// Saves an event into the store
        /// </summary>
        /// <param name="event">The event to be saved</param>
        protected override void _Save(DomainEvent @event)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(@event);
                session.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves the desired events from the store
        /// </summary>
        /// <param name="aggregateId">The aggregate id</param>
        /// <param name="pointInTime">The point in time up to which the events have to be retrieved</param>
        /// <param name="eventDescriptors">The descriptors defining the events to be retrieved</param>
        /// <param name="timelineId">The id of the timeline from which to retrieve the events</param>
        /// <returns>The list of the retrieved events</returns>
        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var descriptors = eventDescriptors.ToList();
                var fullQuery = "";
                for (int i = 0; i < descriptors.Count; i++)
                {
                    var d = descriptors[i];
                    var tag = DocumentStore.Conventions.FindTypeTagName(d.EventType);

                    var descriptorQuery = $"({d.AggregateIdPropertyName}:{aggregateId} AND Tag:{tag} AND TimeStamp:[* TO {pointInTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}]";
                    if (!timelineId.HasValue)
                    {
                        descriptorQuery += " AND TimelineId:[[NULL_VALUE]]";
                    }
                    else
                    {
                        descriptorQuery += $" AND (TimelineId:[[NULL_VALUE]] OR TimelineId:{timelineId.Value})";
                    }

                    descriptorQuery += ")";

                    fullQuery += descriptorQuery;
                    if (i < descriptors.Count - 1)
                    {
                        fullQuery += " OR ";
                    }
                }

                QueryHeaderInformation qhi;
                var query = session.Advanced.DocumentStore
                    .DatabaseCommands
                    .StreamQuery("DomainEvents/Stream", new IndexQuery()
                    {
                        Query = fullQuery,
                        SortedFields = new SortedField[]
                        {
                            new SortedField("+TimeStamp")
                        }
                    }, out qhi);

                var serializer = DocumentStore.Conventions.CreateSerializer();
                var events = new List<DomainEvent>();
                while (query.MoveNext())
                {
                    var mtd = (RavenJObject)query.Current["@metadata"];
                    var type = Type.GetType(mtd["Raven-Clr-Type"].ToString());

                    var instance = serializer.Deserialize(
                        new RavenJTokenReader(query.Current), type);

                    events.Add((DomainEvent)instance);
                }
                return events;
            }
        }

        #region obsolete
        //internal IEnumerable<DomainEvent> GetEventsByAggregate<T>(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId) where T : IAggregate
        //{
        //    using (var session = DocumentStore.OpenSession())
        //    {
        //        var events = new List<DomainEvent>();
        //        foreach(var e in eventDescriptors)
        //        {
        //            var tag = DocumentStore.Conventions.GetTypeTagName(e.EventType);
        //            var rawEvents = session.Advanced
        //                            .DocumentQuery<dynamic, RavenDocumentsByEntityName>()
        //                            .WhereEquals("Tag", tag)
        //                            .ToList();
        //            if(rawEvents.Count() > 0)
        //            {
        //                var filteredEvents = rawEvents
        //                                .ToAnonymousList()
        //                                .Where(e.AggregateIdPropertyName + " == @0", aggregateId)
        //                                .Cast<DomainEvent>()
        //                                .Where(ev => ev.TimeStamp <= pointInTime);
        //                if(timelineId.HasValue)
        //                {
        //                    filteredEvents = filteredEvents.Where(ev => !ev.TimelineId.HasValue || ev.TimelineId == timelineId.Value);
        //                }
        //                else
        //                {
        //                    filteredEvents = filteredEvents.Where(ev => ev.TimelineId == null);
        //                }
        //                events.AddRange(filteredEvents);
        //            }

        //            #region spikes
        //            //var evs3 = session.Advanced
        //            //            .DocumentStore
        //            //            .DatabaseCommands.Query("dynamic", new IndexQuery()
        //            //            {
        //            //                Query = string.Format("{0}:{1}", e.AggregateIdPropertyName, aggregateId)
        //            //            }, null)
        //            //            .Results;
        //            //var evs4 = session.Query<dynamic>()
        //            //                    .ToList()
        //            //                    .Select(ev => Mapper.DynamicMap(ev, e.EventType, e.EventType))
        //            //                    .Where(ev => ev.GetType() == e.EventType)
        //            //                    .ToAnonymousList()
        //            //                    .Where(e.AggregateIdPropertyName, aggregateId);
        //            #endregion
        //        }
        //        return events.OrderBy(e => e.TimeStamp);
        //    }
        //}
        #endregion

    }
}
