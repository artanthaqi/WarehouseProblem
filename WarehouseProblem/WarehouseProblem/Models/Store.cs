using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseProblem.Models
{
    public class Store
    {
        public Store()
        {
            Suppliers = new List<Warehouse>();
            WarehousesSupply = new List<SupplyReq>();
        }
        public int Id { get; set; }
        public int Request { get; set; }
        public double Supply { get; set; }
        public HashSet<int> IncompatibleStores { get; set; }
        public List<Warehouse> Suppliers { get; set; }
        public Dictionary<int, double> SupplyCosts { get; internal set; }
        public List<SupplyReq> WarehousesSupply { get; set; }
    }
}
