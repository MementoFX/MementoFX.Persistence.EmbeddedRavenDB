using System;
using System.Collections.Generic;
using Memento.Domain;
using Memento.Persistence.EmbeddedRavenDB.Tests.Events;

namespace Memento.Persistence.EmbeddedRavenDB.Tests.Model
{
    public class CurrentAccount : Aggregate
    {
        public decimal Balance { get; private set; }

        public void ApplyEvent(AccountOpenedEvent @event)
        {
            this.Id = @event.CurrentAccountId;
            this.Balance = @event.Balance;
        }
        public void ApplyEvent(WithdrawalEvent @event)
        {
            this.Balance -= @event.Amount;
        }
    }
}
