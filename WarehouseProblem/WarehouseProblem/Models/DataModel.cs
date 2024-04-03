using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseProblem.Models
{
    public class DataModel
    {
        public int Warehouses { get; set; }
        public int Stores { get; set; }
        public List<int> Capacity { get; set; }
        public List<int> FixedCost { get; set; }
        public List<int> Goods { get; set; }
        public string SupplyCost { get; set; }
        public int Incompatibilities { get; set; }
        public string IncompatiblePairs { get; set; }
    }
}
