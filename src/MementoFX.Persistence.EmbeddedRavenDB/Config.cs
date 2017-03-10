using System.Configuration;

namespace Memento.Persistence.EmbeddedRavenDB
{
    static class Config
    {
        public readonly static int EventStorePort = int.Parse(ConfigurationManager.AppSettings["Memento/Persistence/EmbeddedRavenDB/EventStore/Port"]);
    }
}
