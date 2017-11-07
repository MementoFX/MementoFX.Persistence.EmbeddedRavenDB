using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MementoFX.Persistence.EmbeddedRavenDB.Tests.Events
{
    public class AccountOpenedEvent : UnitTestFriendlyDomainEvent
    {
        public Guid CurrentAccountId { get; set; }
        public decimal Balance { get; set; }
    }
}
