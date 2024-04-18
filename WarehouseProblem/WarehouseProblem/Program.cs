using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WarehouseProblem.Models;
using static System.Formats.Asn1.AsnWriter;

class Program
{
    static void Main(string[] args)
    {
        for (int k = 1; k <= 1; k++)
        {
            string filePath = "";// @"..\..\..\PublicInstances\toy.dzn";
            if (k < 10)
                filePath = @"..\..\..\PublicInstances\wlp0" + k + ".dzn";
            if (k >= 10)
                filePath = @"..\..\..\PublicInstances\wlp" + k + ".dzn";

            string fileContent = File.ReadAllText(filePath);

            // Use regex to match and extract data
            var match = Regex.Match(fileContent, @"Warehouses = (\d+);\s*Stores = (\d+);\s*Capacity = \[(.*?)\];\s*FixedCost = \[(.*?)\];\s*Goods = \[(.*?)\];\s*SupplyCost = \[\|(.*?)\|\];\s*Incompatibilities = (\d+);\s*IncompatiblePairs = \[\|(.*?)\|\];", RegexOptions.Singleline);

            if (match.Success)
            {
                var data = new DataModel
                {
                    Warehouses = int.Parse(match.Groups[1].Value),
                    Stores = int.Parse(match.Groups[2].Value),
                    Capacity = match.Groups[3].Value.Split(',').Select(int.Parse).ToList(),
                    FixedCost = match.Groups[4].Value.Split(',').Select(int.Parse).ToList(),
                    Goods = match.Groups[5].Value.Split(',').Select(int.Parse).ToList(),
                    SupplyCost = match.Groups[6].Value.Trim(),
                    Incompatibilities = int.Parse(match.Groups[7].Value),
                    IncompatiblePairs = match.Groups[8].Value.Trim()
                };

                var warehouses = new List<Warehouse>();
                for (int i = 0; i < data.Warehouses; i++)
                {
                    warehouses.Add(new Warehouse { Id = i + 1, Capacity = data.Capacity[i], FixedCost = data.FixedCost[i] });
                }

                // Create a dictionary to store the incompatible stores for each store
                var incompatibleStoresDict = new Dictionary<int, HashSet<int>>();

                // Parse the incompatible pairs string
                var incompatiblePairsList = data.IncompatiblePairs.Split('|').Select(pair => pair.Split(',').Select(int.Parse).ToList()).ToList();

                // Populate the incompatible stores dictionary
                foreach (var pair in incompatiblePairsList)
                {
                    int store1 = pair[0];
                    int store2 = pair[1];

                    if (!incompatibleStoresDict.ContainsKey(store1))
                    {
                        incompatibleStoresDict[store1] = new HashSet<int>();
                    }
                    if (!incompatibleStoresDict.ContainsKey(store2))
                    {
                        incompatibleStoresDict[store2] = new HashSet<int>();
                    }

                    incompatibleStoresDict[store1].Add(store2);
                    incompatibleStoresDict[store2].Add(store1);
                }

                // Parse the supply costs string
                var supplyCostsList = data.SupplyCost.Split('|').Select(pair => pair.Split(',').Select(double.Parse).ToList()).ToList();

                var stores = new List<Store>();
                for (int i = 0; i < data.Stores; i++)
                {
                    stores.Add(new Store
                    {
                        Id = i + 1,
                        Request = data.Goods[i],
                        IncompatibleStores = incompatibleStoresDict.ContainsKey(i + 1) ? incompatibleStoresDict[i + 1] : new HashSet<int>(),
                        SupplyCosts = Enumerable.Range(1, data.Warehouses).ToDictionary(j => j, j => supplyCostsList[i][j - 1])
                    });
                }

                stores = stores.OrderByDescending(x => x.Request).ToList();
                //warehouses = warehouses.OrderBy(x => x.FixedCost).ThenByDescending(x => x.Capacity).ToList();

                var sol = intialSol(new ProSolution(stores : stores.ToList(),
                    warehouses : warehouses.ToList()
                    ));

                var cost = EvaluateSolution(sol.Stores, sol.Warehouses);

                int[] possibleInterval = { 100, 200, 300, 400, 500, 600 };

                var homeBase = new ProSolution(
                    stores: sol.Stores.Select(s => new Store
                    {
                        Id = s.Id,
                        Request = s.Request,
                        Suppliers = s.Suppliers.ToList(),
                        IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                        WarehousesSupply = s.WarehousesSupply.ToList(),
                        SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                    }).ToList(),
                    warehouses: sol.Warehouses.ToList(),
                    incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                );


                var best = new ProSolution(
                    stores: sol.Stores.Select(s => new Store
                    {
                        Id = s.Id,
                        Request = s.Request,
                        Suppliers = s.Suppliers.ToList(),
                        IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                        WarehousesSupply = s.WarehousesSupply.ToList(),
                        SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                    }).ToList(),
                    warehouses: sol.Warehouses.ToList(),
                    incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                );

                // sol.Stores.Clear();

                var bbestCost = EvaluateSolution(best.Stores, best.Warehouses);

                

                DateTime startTime = DateTime.Now;
                TimeSpan duration = TimeSpan.FromSeconds(10);
                Random rnd = new Random();

                var per = false;

                var bestCost = 0.0;
                var R = new ProSolution(new List<Store>(), new List<Warehouse>());

                while (DateTime.Now - startTime < duration)
                {
                    int randomIndex = rnd.Next(possibleInterval.Length);
                    int intervalLength = possibleInterval[randomIndex];
                    var currentCost = 0.0;
                    for (int i = 0; i < 1; i++)
                    {

                        if (i == 0 || per )
                        {
                            per = false;
                            R = Tweak(new ProSolution(
                                stores: sol.Stores.Select(s => new Store
                                {
                                    Id = s.Id,
                                    Request = s.Request,
                                    Suppliers = s.Suppliers.ToList(),
                                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                    WarehousesSupply = s.WarehousesSupply.ToList(),
                                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)

                                }).ToList(),
                                warehouses: sol.Warehouses.ToList(),
                                incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                            ));
                        }
                        else
                        {
                            R = Tweak(new ProSolution(
                                stores: R.Stores.Select(s => new Store
                                {
                                    Id = s.Id,
                                    Request = s.Request,
                                    Suppliers = s.Suppliers.ToList(),
                                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                    WarehousesSupply = s.WarehousesSupply.ToList(),
                                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)

                                }).ToList(),
                                warehouses: R.Warehouses.ToList(),
                                incompatiblePairs: R.IncompatiblePairs.ToHashSet()
                                ));
                        }

                        var Rcost = EvaluateSolution(R.Stores, R.Warehouses);

                        if (Rcost < cost || Rcost < currentCost)
                        {
                            sol = R;
                            currentCost = Rcost;
                        }

                    }

                    bestCost = EvaluateSolution(best.Stores, best.Warehouses);

                    if (bestCost > currentCost && currentCost != 0)
                    {
                        // Create a deep copy of sol to preserve the current state
                        best = new ProSolution(
                            stores: sol.Stores.ToList(),
                            warehouses: sol.Warehouses.ToList(),
                            incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                        );

                        bestCost = currentCost;

                    }
                    Console.WriteLine("Cost: " + bestCost);
                    var homeBaseCost = EvaluateSolution(homeBase.Stores, homeBase.Warehouses);
                    if (homeBaseCost >= currentCost && currentCost != 0)
                    {
                        // Create a deep copy of sol to preserve the current state
                        homeBase = new ProSolution(
                            stores: sol.Stores.ToList(),
                            warehouses: sol.Warehouses.ToList(),
                            incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                        );
                    }

                    sol = Perturb(new ProSolution(
                            stores: sol.Stores.Select(s => new Store
                            {
                                Id = s.Id,
                                Request = s.Request,
                                Suppliers = s.Suppliers.ToList(),
                                IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                WarehousesSupply = s.WarehousesSupply.ToList(),
                                SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                            }).ToList(),
                            warehouses: sol.Warehouses.ToList(),
                            incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                        ));
                    per = true;

                }






                Console.WriteLine("Cost" + k + ": " + bestCost);

                // Save results to a text file in the "Solutions" folder
                using (StreamWriter sw = new StreamWriter(Path.Combine(@"..\..\..\Solutions", $"sol-{Path.GetFileNameWithoutExtension(filePath)}.txt")))
                {
                    sw.Write("{");
                    for (int i = 0; i < stores.Count; i++)
                    {
                        for (int j = 0; j < best.Stores[i].Suppliers.Count; j++)
                        {
                            sw.Write($"({best.Stores[i].Id},{best.Stores[i].Suppliers[j].Id},{best.Stores[i].WarehousesSupply.Where(x => x.warehouse.Id == best.Stores[i].Suppliers[j].Id).SingleOrDefault().supplyreq})");
                            if (j != best.Stores[i].Suppliers.Count-1)
                                sw.Write(", ");
                        }
                        if (i < stores.Count - 1)
                        {
                            sw.Write(", ");
                        }
                    }
                    sw.Write("}");
                }

            }
            else
            {
                Console.WriteLine("No match found.");
            }


        }

         ProSolution Tweak(ProSolution sol)
        {
            Random rnd = new Random();
            int randomIndex = rnd.Next(sol.Stores.Count);
            var store = sol.Stores[randomIndex];
            var wr = sol.Warehouses.Where(x => x == store.Suppliers[0]).SingleOrDefault();


            foreach (var wrt in store.SupplyCosts)
            {
                var solwr = sol.Warehouses.Where(x => x.Id == wrt.Key).SingleOrDefault();
                solwr.supplyForStore = (int)wrt.Value;

            }

            sol.Warehouses = sol.Warehouses.OrderBy(x => x.supplyForStore).ToList();

            // Create a HashSet to store incompatible pairs
            var alreadySupplies = new HashSet<string>();

            alreadySupplies.Add($"{wr.Id},{store.Id}");

            foreach (var warehouse in store.Suppliers)
            {
                foreach (var incompatibleStore in store.IncompatibleStores)
                {
                    var remove = true;
                    var incompatibleStoreobj = sol.Stores.Where(x => x.Id == incompatibleStore).SingleOrDefault();

                    foreach (var sr in incompatibleStoreobj.IncompatibleStores)
                    {

                        if (store.Id == sr)
                            continue;

                        var wrs = sol.Stores.Where(s => s.Id == sr).FirstOrDefault().Suppliers;

                        foreach (var s in wrs)
                        {
                            if (warehouse == s)
                                remove = false;
                        }

                    }



                    if (remove)
                        sol.IncompatiblePairs.Remove($"{warehouse.Id},{incompatibleStore}");
                }
                //warehouse.Capacity = warehouse.Capacity + (int)store.Request;//store.WarehousesSupply.Where(x => x.warehouse.Id == warehouse.Id).SingleOrDefault().supplyreq;
            }

            var req = (int)store.Request;

            //wr.Capacity += req;


            store.Suppliers = new List<Warehouse>();
            store.WarehousesSupply = new List<SupplyReq>();



            var requestList = new List<int>();
            var firstRequest = (int)store.Request; //rnd.Next((int)store.Request - 1);
            var secondRequest = (int)store.Request - firstRequest;



            sol.CountReq = (int)store.Request;

            requestList.Add(firstRequest);
            //requestList.Add(secondRequest);

            foreach (var request in requestList)
            {

                foreach (var warehouse in sol.Warehouses)
                {
                    // Check if the warehouse-store pair is incompatible
                    if (sol.IncompatiblePairs.Contains($"{warehouse.Id},{store.Id}") || alreadySupplies.Contains($"{warehouse.Id},{store.Id}"))
                    {
                        continue;
                    }

                    if (warehouse.Capacity >= request)
                    {
                        warehouse.Capacity -= request;
                        store.Supply = store.SupplyCosts[warehouse.Id];
                        store.Suppliers.Add(warehouse);
                        store.WarehousesSupply.Add(new SupplyReq { warehouse = warehouse, supplyreq = request });


                        sol.CountReq = sol.CountReq - request;
                        req = req - request;
                        // Add all incompatible pairs of the current store to the HashSet
                        foreach (var incompatibleStore in store.IncompatibleStores)
                        {
                            sol.IncompatiblePairs.Add($"{warehouse.Id},{incompatibleStore}");
                        }

                        break;
                    }
                }
            }

            if (req != 0)
            {
                wr.Capacity -= req;
                store.Suppliers.Add(wr);
                store.WarehousesSupply.Add(new SupplyReq { warehouse = wr, supplyreq = req });

                foreach (var incompatibleStore in store.IncompatibleStores)
                {
                    sol.IncompatiblePairs.Add($"{wr.Id},{incompatibleStore}");
                }

            }
            else
            {
                wr.Capacity += req;
            }



            return sol;

        }

        static ProSolution Perturb(ProSolution sol)
        {
            var preW = new Dictionary<int, Warehouse>();

            Random rnd = new Random();

            // Calculate the number of stores that represent 5% of the total
            int numStoresToSelect = (int)Math.Ceiling(sol.Stores.Count * 0.30);

            // Create a list to store the randomly selected stores
            var selectedStores = new List<Store>();

            // Randomly select 5% of stores
            while (selectedStores.Count < numStoresToSelect)
            {
                int randomIndex = rnd.Next(sol.Stores.Count);
                var store = sol.Stores[randomIndex];
                if (!selectedStores.Contains(store))
                {
                    selectedStores.Add(store);
                }
            }

            // Process the randomly selected stores
            foreach (var store in selectedStores)
            {
                var wr = sol.Warehouses.Where(x => x == store.Suppliers[0]).SingleOrDefault();
                wr.Capacity = wr.Capacity + store.Request;
                preW.Add(store.Id, wr);


                foreach (var warehouse in store.Suppliers)
                {
                    foreach (var incompatibleStore in store.IncompatibleStores)
                    {
                        var remove = true;
                        var incompatibleStoreobj = sol.Stores.Where(x => x.Id == incompatibleStore).SingleOrDefault();

                        foreach (var sr in incompatibleStoreobj.IncompatibleStores)
                        {

                            if (store.Id == sr)
                                continue;

                            var wrs = sol.Stores.Where(s => s.Id == sr).FirstOrDefault().Suppliers;

                            foreach (var s in wrs)
                            {
                                if (warehouse == s)
                                    remove = false;
                            }

                        }



                        if (remove)
                            sol.IncompatiblePairs.Remove($"{warehouse.Id},{incompatibleStore}");
                    }
                    //warehouse.Capacity = warehouse.Capacity + (int)store.Request;//store.WarehousesSupply.Where(x => x.warehouse.Id == warehouse.Id).SingleOrDefault().supplyreq;
                }



                store.Suppliers = new List<Warehouse>();
                store.WarehousesSupply = new List<SupplyReq>();


            }

            foreach (var store in selectedStores)
            {
                var req = (int)store.Request;
                var requestList = new List<int>();
                var firstRequest = (int)store.Request; //rnd.Next((int)store.Request - 1);
                var secondRequest = (int)store.Request - firstRequest;
                var wr = preW[store.Id];


                sol.CountReq = (int)store.Request;

                requestList.Add(firstRequest);
                //requestList.Add(secondRequest);

                foreach (var request in requestList)
                {

                    foreach (var warehouse in sol.Warehouses)
                    {
                        // Check if the warehouse-store pair is incompatible
                        if (sol.IncompatiblePairs.Contains($"{warehouse.Id},{store.Id}") || wr == warehouse)
                        {
                            continue;
                        }

                        if (warehouse.Capacity >= request)
                        {
                            warehouse.Capacity -= request;
                            store.Supply = store.SupplyCosts[warehouse.Id];
                            store.Suppliers.Add(warehouse);
                            store.WarehousesSupply.Add(new SupplyReq { warehouse = warehouse, supplyreq = request });


                            sol.CountReq = sol.CountReq - request;
                            req = req - request;
                            // Add all incompatible pairs of the current store to the HashSet
                            foreach (var incompatibleStore in store.IncompatibleStores)
                            {
                                sol.IncompatiblePairs.Add($"{warehouse.Id},{incompatibleStore}");
                            }

                            break;
                        }
                    }
                }

                if (req != 0)
                {
                    wr.Capacity -= req;
                    store.Suppliers.Add(wr);
                    store.WarehousesSupply.Add(new SupplyReq { warehouse = wr, supplyreq = req });

                    foreach (var incompatibleStore in store.IncompatibleStores)
                    {
                        sol.IncompatiblePairs.Add($"{wr.Id},{incompatibleStore}");
                    }

                }
                else
                {
                   // wr.Capacity += req;
                }


            }




            return new ProSolution(stores: sol.Stores, warehouses: sol.Warehouses, incompatiblePairs: sol.IncompatiblePairs);
        }

        static ProSolution intialSol(ProSolution sol)
        {
            // Create a HashSet to store incompatible pairs
            var incompatiblePairs = new HashSet<string>();

            // Assign goods to stores
            foreach (var store in sol.Stores)
            {

                //foreach(var wr in store.SupplyCosts)
                //{
                //    var solwr = sol.Warehouses.Where(x => x.Id == wr.Key).SingleOrDefault();
                //    solwr.supplyForStore = (int)wr.Value;
                        
                //}

                //sol.Warehouses = sol.Warehouses.OrderBy(x => x.supplyForStore).ToList();

                foreach (var warehouse in sol.Warehouses)
                {
                    // Check if the warehouse-store pair is incompatible
                    if (incompatiblePairs.Contains($"{warehouse.Id},{store.Id}"))
                    {
                        continue;
                    }

                    if (warehouse.Capacity >= store.Request)
                    {
                        warehouse.Capacity -= store.Request;
                        store.Supply = store.SupplyCosts[warehouse.Id];
                        store.Suppliers.Add(warehouse);
                        store.WarehousesSupply.Add(new SupplyReq { warehouse = warehouse, supplyreq = store.Request });
                        // Add all incompatible pairs of the current store to the HashSet
                        foreach (var incompatibleStore in store.IncompatibleStores)
                        {
                            incompatiblePairs.Add($"{warehouse.Id},{incompatibleStore}");
                        }

                        break;
                    }
                }
            }

            sol.IncompatiblePairs = incompatiblePairs;

            return sol;
        }

        static double EvaluateSolution(List<Store> stores, List<Warehouse> warehouses)
        {
            double totalCost = 0;
            var warehousesAdded = new HashSet<int>();
            foreach (var store in stores)
            {
                for (int i = 0; i < store.Suppliers.Count; i++)
                {
                    if (!warehousesAdded.Contains(store.Suppliers[i].Id))
                        // Add the fixed cost of the warehouse supplying the store
                        totalCost += warehouses.Where(x => x.Id == store.Suppliers[i].Id).SingleOrDefault().FixedCost;

                    warehousesAdded.Add(store.Suppliers[i].Id);
                    // Add the supply cost for the store
                    totalCost += store.SupplyCosts[store.Suppliers[i].Id] * store.WarehousesSupply.Where(x => x.warehouse.Id == store.Suppliers[i].Id).SingleOrDefault().supplyreq;
                }



            }

            return totalCost;
        }

    }
}
