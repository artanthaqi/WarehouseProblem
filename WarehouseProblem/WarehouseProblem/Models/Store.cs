using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseProblem.Models
{
    public class Store
    {
        public int Id { get; set; }
        public int Request { get; set; }
        public double Supply { get; set; }
        public HashSet<int> IncompatibleStores { get; set; }
        public Warehouse Supplier { get; set; }
        public Dictionary<int, double> SupplyCosts { get; internal set; }
    }
}
