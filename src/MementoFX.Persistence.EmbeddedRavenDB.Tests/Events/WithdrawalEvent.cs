using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.EmbeddedRavenDB.Tests.Events
{
    public class WithdrawalEvent : UnitTestFriendlyDomainEvent
    {
        public Guid CurrentAccountId { get; set; }

        public decimal Amount { get; set; }

    }
}
