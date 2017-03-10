using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Domain;

namespace Memento.Persistence.EmbeddedRavenDB.Tests.Model
{
    public class Invoice : Aggregate
    {
        public string Number { get; set; }
        public DateTime DateOfIssue { get; set; }
        public decimal Price { get; set; }
        public decimal Taxes { get; set; }

        public Invoice()
        {
            Id = Guid.NewGuid();
        }
    }
}
