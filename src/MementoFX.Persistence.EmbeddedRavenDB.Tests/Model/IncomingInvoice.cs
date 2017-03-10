using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.EmbeddedRavenDB.Tests.Model
{
    public class IncomingInvoice : Invoice
    {
        public string SupplierName { get; set; }
        
    }
}
