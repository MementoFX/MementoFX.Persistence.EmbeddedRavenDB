using System;
using System.Collections.Generic;
using MementoFX.Domain;
using MementoFX.Persistence.EmbeddedRavenDB.Tests.Events;

namespace MementoFX.Persistence.EmbeddedRavenDB.Tests.Model
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
