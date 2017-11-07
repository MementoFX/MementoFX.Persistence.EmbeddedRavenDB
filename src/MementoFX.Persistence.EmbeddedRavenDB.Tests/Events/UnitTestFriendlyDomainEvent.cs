using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MementoFX.Persistence.EmbeddedRavenDB.Tests.Events
{
    public class UnitTestFriendlyDomainEvent : DomainEvent
    {
        public void SetTimeStamp(DateTime pointInTime)
        {
            var type = typeof(DomainEvent);
            var property = type.GetProperty("TimeStamp");
            property.SetValue(this, pointInTime);
        }
    }
}
