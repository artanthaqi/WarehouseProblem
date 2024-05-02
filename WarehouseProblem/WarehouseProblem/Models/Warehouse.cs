using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseProblem.Models
{
    public class Warehouse
    {
        public int Capacity { get; set; }
        public int Id { get; set; }
        public double FixedCost { get; set; }

        public int supplyForStore { get; set; }

        public bool Open { get; set; }
        public int StartCapacity { get; set; }
        public List<Incompatible> Incompatible { get; set; }
    }
}
