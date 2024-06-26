﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarehouseProblem.Models
{
    public class ProSolution
    {
        public List<Store> Stores { get; set; }
        public List<Warehouse> Warehouses { get; set; }
        public HashSet<string> IncompatiblePairs { get; set; }

        public int CountReq { get; set; }
        public ProSolution(List<Store> stores, List<Warehouse> warehouses, HashSet<string> incompatiblePairs, int Procapacity)
        {
            Stores = stores;
            Warehouses = warehouses;
            IncompatiblePairs = incompatiblePairs;
            this.Procapacity = Procapacity;
        }
        public ProSolution(List<Store> stores, List<Warehouse> warehouses)
        {
            Stores = stores;
            Warehouses = warehouses;
            IncompatiblePairs = new HashSet<string>();
        }
        public ProSolution(List<Store> stores, List<Warehouse> warehouses, int Procapacity)
        {
            Stores = stores;
            Warehouses = warehouses;
            IncompatiblePairs = new HashSet<string>();
            this.Procapacity = Procapacity;
        }


        public int Procapacity { get; set; }



    }
}
