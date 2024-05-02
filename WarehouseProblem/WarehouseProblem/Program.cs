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
        var becost = 0;

        Console.WriteLine("Enter the file name:");
        string fileName = Console.ReadLine();
        string filePath = @"..\..\..\PublicInstances\" + fileName;

        Console.WriteLine("Enter the duration (in seconds):");
        int durationInSeconds = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter the possible intervals (separated by space):");
        int[] possibleInterval = Console.ReadLine().Split(' ').Select(int.Parse).ToArray();

        for (int k = 1; k <= 1; k++)
        {



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

                var proCapacity = 0;

                for (int i = 0; i < data.Warehouses; i++)
                {
                    proCapacity = proCapacity + data.Capacity[i];
                    warehouses.Add(new Warehouse { Id = i + 1, StartCapacity = data.Capacity[i], Capacity = data.Capacity[i], FixedCost = data.FixedCost[i], Open = false });
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

                //stores = stores.OrderByDescending(x => x.Request).ToList();
                //warehouses = warehouses.OrderBy(x => x.FixedCost).ThenByDescending(x => x.Capacity).ToList();

                var sol = intialSol(new ProSolution(stores: stores.ToList(),
                    warehouses: warehouses.ToList(), Procapacity: proCapacity
                    ));

                var cost = EvaluateSolution(sol.Stores, sol.Warehouses);


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
                    warehouses: sol.Warehouses.Select(w => new Warehouse
                    {
                        Id = w.Id,
                        Capacity = w.Capacity,
                        FixedCost = w.FixedCost,
                        supplyForStore = w.supplyForStore,
                        Open = w.Open,
                        StartCapacity = w.StartCapacity
                    }).ToList(),
                    incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                    Procapacity: sol.Procapacity
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
                    warehouses: sol.Warehouses.Select(w => new Warehouse
                    {
                        Id = w.Id,
                        Capacity = w.Capacity,
                        FixedCost = w.FixedCost,
                        supplyForStore = w.supplyForStore,
                        Open = w.Open,
                        StartCapacity = w.StartCapacity
                    }).ToList(),
                    incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                    Procapacity: sol.Procapacity
                );

                // sol.Stores.Clear();

                var bbestCost = EvaluateSolution(best.Stores, best.Warehouses);



                DateTime startTime = DateTime.Now;
                TimeSpan duration = TimeSpan.FromSeconds(durationInSeconds);
                Random rnd = new Random();

                var per = false;
                var bsol = false;
                var bestCost = 0.0;
                var R = new ProSolution(new List<Store>(), new List<Warehouse>());
                var tes = 1;

                var tweekedcost = 0.0;

                while (DateTime.Now - startTime < duration)
                {
                    tes--;
                    int randomIndex = rnd.Next(possibleInterval.Length);
                    int intervalLength = possibleInterval[randomIndex];
                    var currentCost = 0.0;
                    currentCost = cost;
                    for (int i = 0; i < intervalLength; i++)
                    {

                        int randomTweek = rnd.Next(10);

                        if (randomTweek == 0)
                        {
                            R = TweakWarehouse(new ProSolution(
                                stores: sol.Stores.Select(s => new Store
                                {
                                    Id = s.Id,
                                    Request = s.Request,
                                    Suppliers = s.Suppliers.ToList(),
                                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                    WarehousesSupply = s.WarehousesSupply.ToList(),
                                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                                }).ToList(),
                                warehouses: sol.Warehouses.Select(w => new Warehouse
                                {
                                    Id = w.Id,
                                    Capacity = w.Capacity,
                                    FixedCost = w.FixedCost,
                                    supplyForStore = w.supplyForStore,
                                    Open = w.Open,
                                    StartCapacity = w.StartCapacity
                                }).ToList(),
                                incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                                Procapacity: sol.Procapacity
                                ));
                        }
                        else if (randomTweek == 1 && i != 0)
                        {

                            var ts = TweakStore(new ProSolution(
                                stores: sol.Stores.Select(s => new Store
                                {
                                    Id = s.Id,
                                    Request = s.Request,
                                    Suppliers = s.Suppliers.ToList(),
                                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                    WarehousesSupply = s.WarehousesSupply.ToList(),
                                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                                }).ToList(),
                                warehouses: sol.Warehouses.Select(w => new Warehouse
                                {
                                    Id = w.Id,
                                    Capacity = w.Capacity,
                                    FixedCost = w.FixedCost,
                                    supplyForStore = w.supplyForStore,
                                    Open = w.Open,
                                    StartCapacity = w.StartCapacity
                                }).ToList(),
                                incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                                Procapacity: sol.Procapacity
                                ), tweekedcost);

                            R = ts.Item1;
                            tweekedcost = ts.Item2;
                        }
                        else
                        {
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
                                warehouses: sol.Warehouses.Select(w => new Warehouse
                                {
                                    Id = w.Id,
                                    Capacity = w.Capacity,
                                    FixedCost = w.FixedCost,
                                    supplyForStore = w.supplyForStore,
                                    Open = w.Open,
                                    StartCapacity = w.StartCapacity
                                }).ToList(),
                                incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                                Procapacity: sol.Procapacity
                                ));
                        }



                        //if (i == 0 || per || bsol )
                        //{
                        //    per = false;
                        //    bsol = false;
                        //    R = Tweak(new ProSolution(
                        //        stores: sol.Stores.Select(s => new Store
                        //        {
                        //            Id = s.Id,
                        //            Request = s.Request,
                        //            Suppliers = s.Suppliers.ToList(),
                        //            IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                        //            WarehousesSupply = s.WarehousesSupply.ToList(),
                        //            SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)

                        //        }).ToList(),
                        //        warehouses: sol.Warehouses.ToList(),
                        //        incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                        //    ));
                        //}
                        //else
                        //{
                        //    R = Tweak(new ProSolution(
                        //        stores: sol.Stores.Select(s => new Store
                        //        {
                        //            Id = s.Id,
                        //            Request = s.Request,
                        //            Suppliers = s.Suppliers.ToList(),
                        //            IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                        //            WarehousesSupply = s.WarehousesSupply.ToList(),
                        //            SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)

                        //        }).ToList(),
                        //        warehouses: sol.Warehouses.ToList(),
                        //        incompatiblePairs: sol.IncompatiblePairs.ToHashSet()
                        //        ));
                        //}

                        var Rcost = 0.0;

                        if (i == 0 || randomTweek != 1)
                        {
                            Rcost = EvaluateSolution(R.Stores, R.Warehouses);
                            tweekedcost = Rcost;
                        }
                        else
                        {
                            Rcost = tweekedcost;
                        }

                        //if (i == 0 && Rcost < bestCost)
                        //{
                        //    sol = best;
                        //    currentCost = bestCost;
                        //    bsol = true;
                        //}

                        if (Rcost < currentCost || currentCost == 0)
                        {
                            sol = R;
                            currentCost = Rcost;
                            bsol = true;
                        }

                    }

                    bestCost = EvaluateSolution(best.Stores, best.Warehouses);

                    if (bestCost > currentCost && currentCost != 0)
                    {
                        // Create a deep copy of sol to preserve the current state
                        best = new ProSolution(
                            stores: sol.Stores.Select(s => new Store
                            {
                                Id = s.Id,
                                Request = s.Request,
                                Suppliers = s.Suppliers.ToList(),
                                IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                WarehousesSupply = s.WarehousesSupply.ToList(),
                                SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                            }).ToList(),
                            warehouses: sol.Warehouses.Select(w => new Warehouse
                            {
                                Id = w.Id,
                                Capacity = w.Capacity,
                                FixedCost = w.FixedCost,
                                supplyForStore = w.supplyForStore,
                                Open = w.Open,
                                StartCapacity = w.StartCapacity
                            }).ToList(),
                            incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                            Procapacity: sol.Procapacity
                        );

                        bestCost = currentCost;

                    }


                    Console.WriteLine("Current best cost: " + bestCost);
                    //Console.WriteLine("Cost: " + bestCost);
                    var homeBaseCost = EvaluateSolution(homeBase.Stores, homeBase.Warehouses);
                    if (true)//homeBaseCost >= currentCost && currentCost != 0)
                    {
                        // Create a deep copy of sol to preserve the current state
                        homeBase = new ProSolution(
                            stores: sol.Stores.Select(s => new Store
                            {
                                Id = s.Id,
                                Request = s.Request,
                                Suppliers = s.Suppliers.ToList(),
                                IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                WarehousesSupply = s.WarehousesSupply.ToList(),
                                SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                            }).ToList(),
                            warehouses: sol.Warehouses.Select(w => new Warehouse
                            {
                                Id = w.Id,
                                Capacity = w.Capacity,
                                FixedCost = w.FixedCost,
                                supplyForStore = w.supplyForStore,
                                Open = w.Open,
                                StartCapacity = w.StartCapacity
                            }).ToList(),
                            incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                            Procapacity: sol.Procapacity
                        );
                    }

                    sol = Perturb(new ProSolution(
                            stores: homeBase.Stores.Select(s => new Store
                            {
                                Id = s.Id,
                                Request = s.Request,
                                Suppliers = s.Suppliers.ToList(),
                                IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                                WarehousesSupply = s.WarehousesSupply.ToList(),
                                SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                            }).ToList(),
                            warehouses: homeBase.Warehouses.ToList(),
                            incompatiblePairs: homeBase.IncompatiblePairs.ToHashSet(),
                            Procapacity: homeBase.Procapacity
                        ));
                    per = true;

                    var solPerCost = EvaluateSolution(sol.Stores, sol.Warehouses);
                    //Console.WriteLine("Cost: " + solPerCost);
                }



                //for(var i =0; i< best.Warehouses.Count*500; i++)
                //{

                //    var T = TweakWarehouse(new ProSolution(
                //                stores: best.Stores.Select(s => new Store
                //                {
                //                    Id = s.Id,
                //                    Request = s.Request,
                //                    Suppliers = s.Suppliers.ToList(),
                //                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                //                    WarehousesSupply = s.WarehousesSupply.ToList(),
                //                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                //                }).ToList(),
                //                warehouses: best.Warehouses.Select(w => new Warehouse
                //                {
                //                    Id = w.Id,
                //                    Capacity = w.Capacity,
                //                    FixedCost = w.FixedCost,
                //                    supplyForStore = w.supplyForStore,
                //                    Open = w.Open,
                //                    StartCapacity = w.StartCapacity
                //                }).ToList(),
                //                incompatiblePairs: best.IncompatiblePairs.ToHashSet(),
                //                Procapacity: best.Procapacity
                //                ));

                //    var Tcost = EvaluateSolution(T.Stores, T.Warehouses);

                //    if (Tcost < bestCost)
                //    {
                //        best = T;
                //        bestCost = Tcost;
                //    }

                //}




                Console.WriteLine("Cost" + k + ": " + bestCost);

                if (k == 1)
                {
                    becost = (int)bestCost;
                }

                if (becost >= (int)bestCost)
                {
                    becost = (int)bestCost;
                    // Save results to a text file in the "Solutions" folder
                    using (StreamWriter sw = new StreamWriter(Path.Combine(@"..\..\..\Solutions", $"sol-{Path.GetFileNameWithoutExtension(filePath)}.txt")))
                    {
                        sw.Write("{");
                        for (int i = 0; i < stores.Count; i++)
                        {
                            for (int j = 0; j < best.Stores[i].Suppliers.Count; j++)
                            {
                                sw.Write($"({best.Stores[i].Id},{best.Stores[i].Suppliers[j].Id},{best.Stores[i].WarehousesSupply.Where(x => x.warehouse.Id == best.Stores[i].Suppliers[j].Id).SingleOrDefault().supplyreq})");
                                if (j != best.Stores[i].Suppliers.Count - 1)
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
            }
            else
            {
                Console.WriteLine("No match found.");
            }

            Console.ReadLine();


        }





    }


    public static ProSolution Tweak(ProSolution sol)
    {

        var oldsol = new ProSolution(
                stores: sol.Stores.Select(s => new Store
                {
                    Id = s.Id,
                    Request = s.Request,
                    Suppliers = s.Suppliers.ToList(),
                    IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                    WarehousesSupply = s.WarehousesSupply.ToList(),
                    SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
                }).ToList(),
                warehouses: sol.Warehouses.Select(w => new Warehouse
                {
                    Id = w.Id,
                    Capacity = w.Capacity,
                    FixedCost = w.FixedCost,
                    supplyForStore = w.supplyForStore,
                    Open = w.Open,
                    StartCapacity = w.StartCapacity
                }).ToList(),
                incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
                Procapacity: sol.Procapacity
            );

        var stores = sol.Stores.ToList();
        Random rnd = new Random();
        int randomIndex = rnd.Next(stores.Count);
        var store = stores[randomIndex];
        var allreq = store.Request;



        if (store.Request <= sol.Procapacity)
        {




            // return cap 
            foreach (var w in store.Suppliers)
            {
                var supplyreq = store.WarehousesSupply.Where(x => x.warehouse.Id == w.Id).FirstOrDefault().supplyreq;
                var wr = sol.Warehouses.Where(x => x.Id == w.Id).SingleOrDefault();
                wr.Capacity = wr.Capacity + supplyreq;

                if (wr.Capacity == wr.StartCapacity)
                    wr.Open = false;

            }

            var storeinc = new HashSet<string>();

            foreach (var sinc in sol.IncompatiblePairs)
            {
                if (sinc.Contains($"{store.Id}"))
                    storeinc.Add(sinc);
            }

            sol.IncompatiblePairs = new HashSet<string>();

            // order and remove 
            foreach (var wrt in store.SupplyCosts)
            {
                var solwr = sol.Warehouses.Where(x => x.Id == wrt.Key).SingleOrDefault();
                solwr.supplyForStore = (int)wrt.Value;

            }

            var warehouses = sol.Warehouses
                .OrderBy(x => x.supplyForStore).ThenBy(x => x.FixedCost)
                .Where(warehouse => !store.Suppliers.Where(x => x.Id == warehouse.Id).Any() && warehouse.Open).ToList();

            //new sup

            store.Suppliers = new List<Warehouse>();
            store.WarehousesSupply = new List<SupplyReq>();



            foreach (var wrf in warehouses)
            {
                var wr = sol.Warehouses.Where(x => x == wrf).SingleOrDefault();

                if (allreq == 0)
                    break;
                var supInThatStore = 0;
                if (wr.Capacity != 0 && !storeinc.Contains($"{wr.Id},{store.Id}"))
                {
                    if (wr.Capacity >= allreq)
                    {
                        supInThatStore = allreq;
                    }
                    else
                    {
                        supInThatStore = wr.Capacity;
                    }
                    allreq = allreq - supInThatStore;

                    wr.Capacity = wr.Capacity - supInThatStore;

                    store.Suppliers.Add(wr);
                    store.WarehousesSupply.Add(new SupplyReq { warehouse = wr, supplyreq = supInThatStore });

                    // Add all incompatible pairs of the current store to the HashSet
                    foreach (var incompatibleStore in store.IncompatibleStores)
                    {
                        sol.IncompatiblePairs.Add($"{wr.Id},{incompatibleStore}");
                    }

                }

            }


            ///add again IncompatiblePairs
            foreach (var s in sol.Stores)
            {
                foreach (var wrs in s.Suppliers)
                {
                    foreach (var incompatibleStore in s.IncompatibleStores)
                    {
                        sol.IncompatiblePairs.Add($"{wrs.Id},{incompatibleStore}");
                    }
                }
            }


        }

        if (allreq == 0)
        {
            return sol;
        }
        else
        {
            return oldsol;
        }
    }

    public static ProSolution TweakWarehouse(ProSolution sol)
    {
        var oldsol = new ProSolution(
               stores: sol.Stores.Select(s => new Store
               {
                   Id = s.Id,
                   Request = s.Request,
                   Suppliers = s.Suppliers.ToList(),
                   IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                   WarehousesSupply = s.WarehousesSupply.ToList(),
                   SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
               }).ToList(),
               warehouses: sol.Warehouses.Select(w => new Warehouse
               {
                   Id = w.Id,
                   Capacity = w.Capacity,
                   FixedCost = w.FixedCost,
                   supplyForStore = w.supplyForStore,
                   Open = w.Open,
                   StartCapacity = w.StartCapacity
               }).ToList(),
               incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
               Procapacity: sol.Procapacity
           );


        var openWarehouses = sol.Warehouses.Where(x => x.Open).OrderByDescending(x => x.FixedCost).ThenBy(x => x.StartCapacity).ToList();


        Random rnd = new Random();
        int randomIndex = rnd.Next(openWarehouses.Count);
        var rndwr = openWarehouses[randomIndex];
        var oldwr = sol.Warehouses.Where(x => x.Id == rndwr.Id).FirstOrDefault();
        var stores = sol.Stores.Where(x => x.Suppliers.Any(x => x.Id == oldwr.Id)).ToList();
        var selectedwr = new Warehouse();
        var allwrreq = 0;

        foreach (var store in stores)
        {
            var allreq = store.Request;
            allwrreq = allwrreq + store.Request;
            if (store.Request <= sol.Procapacity)
            {


                // order and remove 
                foreach (var wrt in store.SupplyCosts)
                {
                    var solwr = sol.Warehouses.Where(x => x.Id == wrt.Key).SingleOrDefault();
                    solwr.supplyForStore = (int)wrt.Value;

                }

                var warehouses = sol.Warehouses
                    .OrderBy(x => x.supplyForStore).ThenBy(x => x.FixedCost)
                    .Where(warehouse => !store.Suppliers.Where(x => x.Id == warehouse.Id).Any() && warehouse.Open).ToList();

                //new sup

                store.Suppliers = new List<Warehouse>();
                store.WarehousesSupply = new List<SupplyReq>();



                foreach (var wrf in warehouses)
                {
                    var wr = sol.Warehouses.Where(x => x == wrf).SingleOrDefault();

                    if (allreq == 0)
                        break;
                    var supInThatStore = 0;
                    if (wr.Capacity != 0 && !sol.IncompatiblePairs.Contains($"{wr.Id},{store.Id}"))
                    {
                        if (wr.Capacity >= allreq)
                        {
                            supInThatStore = allreq;
                        }
                        else
                        {
                            supInThatStore = wr.Capacity;
                        }

                        allreq = allreq - supInThatStore;
                        allwrreq = allwrreq - supInThatStore;

                        wr.Capacity = wr.Capacity - supInThatStore;

                        store.Suppliers.Add(wr);
                        store.WarehousesSupply.Add(new SupplyReq { warehouse = wr, supplyreq = supInThatStore });

                        // Add all incompatible pairs of the current store to the HashSet
                        foreach (var incompatibleStore in store.IncompatibleStores)
                        {
                            sol.IncompatiblePairs.Add($"{wr.Id},{incompatibleStore}");
                        }

                    }

                }





            }

        }

        if (allwrreq == 0)
        {
            ///add again IncompatiblePairs
            foreach (var s in sol.Stores)
            {
                foreach (var wrs in s.Suppliers)
                {
                    foreach (var incompatibleStore in s.IncompatibleStores)
                    {
                        sol.IncompatiblePairs.Add($"{wrs.Id},{incompatibleStore}");
                    }
                }
            }

            ////Remove this warehouse IncompatiblePairs
            //foreach (var sinc in sol.IncompatiblePairs)
            //{
            //    if (sinc.Contains($"{oldwr.Id}"))
            //        sol.IncompatiblePairs.Remove(sinc);
            //}

            oldwr.Open = false;

            var opw = sol.Warehouses.Where(x => x.Open).OrderByDescending(x => x.FixedCost).ThenBy(x => x.StartCapacity).ToList();

            return sol;
        }
        else
        {
            return oldsol;
        }



    }

    static Tuple<ProSolution, Double> TweakStore(ProSolution sol, double cost)
    {

        var oldsol = new ProSolution(
            stores: sol.Stores.Select(s => new Store
            {
                Id = s.Id,
                Request = s.Request,
                Suppliers = s.Suppliers.ToList(),
                IncompatibleStores = s.IncompatibleStores.ToHashSet(),
                WarehousesSupply = s.WarehousesSupply.ToList(),
                SupplyCosts = new Dictionary<int, double>(s.SupplyCosts)
            }).ToList(),
            warehouses: sol.Warehouses.Select(w => new Warehouse
            {
                Id = w.Id,
                Capacity = w.Capacity,
                FixedCost = w.FixedCost,
                supplyForStore = w.supplyForStore,
                Open = w.Open,
                StartCapacity = w.StartCapacity
            }).ToList(),
            incompatiblePairs: sol.IncompatiblePairs.ToHashSet(),
            Procapacity: sol.Procapacity
        );


        Random rnd = new Random();
        int randomIndex = rnd.Next(sol.Stores.Count);
        var store = sol.Stores[randomIndex];

        var ncost = cost;

        // return cap 
        foreach (var w in store.Suppliers)
        {
            var supplyreq = store.WarehousesSupply.Where(x => x.warehouse.Id == w.Id).FirstOrDefault().supplyreq;
            var wr = sol.Warehouses.Where(x => x.Id == w.Id).SingleOrDefault();
            wr.Capacity = wr.Capacity + supplyreq;

            ncost = ncost - store.SupplyCosts[w.Id] * store.WarehousesSupply.Where(x => x.warehouse.Id == w.Id).SingleOrDefault().supplyreq;

            if (wr.Capacity == wr.StartCapacity)
            {
                wr.Open = false;
                ncost = ncost - sol.Warehouses.Where(x => x.Id == w.Id).SingleOrDefault().FixedCost;
            }

        }


        var storeinc = new HashSet<string>();

        //Leave just this stores IncompatiblePairs
        foreach (var sinc in sol.IncompatiblePairs)
        {
            if (sinc.Contains($"{store.Id}"))
                storeinc.Add(sinc);
        }

        sol.IncompatiblePairs = new HashSet<string>();




        //Order
        foreach (var wrt in store.SupplyCosts)
        {
            var solwr = sol.Warehouses.Where(x => x.Id == wrt.Key).SingleOrDefault();
            solwr.supplyForStore = (int)wrt.Value;

        }

        //Order and remove warehouses that already supply store
        var warehouses = sol.Warehouses
        .OrderBy(x => x.supplyForStore)
        .Where(warehouse => !store.Suppliers.Where(x => x.Id == warehouse.Id).Any() && warehouse.Open).ToList();


        var req = (int)store.Request;


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

            foreach (var wr in warehouses)
            {
                // Check if the warehouse-store pair is incompatible
                if (storeinc.Contains($"{wr.Id},{store.Id}"))
                {
                    continue;
                }

                if (wr.Capacity >= request)
                {
                    wr.Capacity -= request;
                    store.Supply = store.SupplyCosts[wr.Id];
                    store.Suppliers.Add(wr);
                    store.WarehousesSupply.Add(new SupplyReq { warehouse = wr, supplyreq = request });


                    sol.CountReq = sol.CountReq - request;
                    req = req - request;
                    // Add all incompatible pairs of the current store to the HashSet
                    foreach (var incompatibleStore in store.IncompatibleStores)
                    {
                        sol.IncompatiblePairs.Add($"{wr.Id},{incompatibleStore}");
                    }

                    warehouses.Remove(wr);

                    break;
                }
            }
        }

        ///add again IncompatiblePairs
        foreach (var s in sol.Stores)
        {
            foreach (var wrs in s.Suppliers)
            {
                foreach (var incompatibleStore in s.IncompatibleStores)
                {
                    sol.IncompatiblePairs.Add($"{wrs.Id},{incompatibleStore}");
                }
            }
        }

        if (req != 0)
        {
            return Tuple.Create(oldsol, cost);

        }
        else
        {

            for (int i = 0; i < store.Suppliers.Count; i++)
            {
                // Add the supply cost for the store
                ncost += store.SupplyCosts[store.Suppliers[i].Id] * store.WarehousesSupply.Where(x => x.warehouse.Id == store.Suppliers[i].Id).SingleOrDefault().supplyreq;
            }

            return Tuple.Create(sol, ncost);
        }


    }




    static ProSolution Perturb(ProSolution sol)
    {

        Random rnd = new Random();
        int randomIndex = rnd.Next(sol.Warehouses.Count);
        var wr = sol.Warehouses[randomIndex];
        var stores = sol.Stores.Where(x => x.Suppliers[0] == wr).ToList();
        var selectedwr = new Warehouse();

        sol.Warehouses = sol.Warehouses.OrderBy(x => x.FixedCost).ThenBy(x => x.Capacity).ToList();

        var found = false;

        foreach (var nwr in sol.Warehouses)
        {
            if (nwr.Capacity >= wr.Capacity && nwr != wr && !nwr.Open)
            {
                selectedwr = nwr;
                found = true;
                continue;
            }
        }
        if (found)
        {


            foreach (var store in stores)
            {
                store.Suppliers = new List<Warehouse>();
                store.WarehousesSupply = new List<SupplyReq>();
                store.Suppliers.Add(selectedwr);
                selectedwr.Capacity = selectedwr.Capacity - store.Request;

                store.WarehousesSupply.Add(new SupplyReq { warehouse = selectedwr, supplyreq = store.Request });

                wr.Capacity = wr.StartCapacity;
                wr.Open = false;

            }
        }





        return sol;
    }

    static ProSolution PerturbWarehouse(ProSolution sol)
    {

        Random rnd = new Random();
        int randomIndex = rnd.Next(sol.Warehouses.Count);
        //var wr = sol.Warehouses[randomIndex];
        //var stores = sol.Stores.Where(x => x.Suppliers[0] == wr).ToList();
        //var selectedwr = new Warehouse();

        var openWarehouses = sol.Warehouses.Where(x => x.Open).OrderByDescending(x => x.FixedCost).ThenBy(x => x.StartCapacity).ToList();

        var closedWarehouses = sol.Warehouses.Where(x => !x.Open).OrderBy(x => x.FixedCost).ThenByDescending(x => x.StartCapacity).ToList();

        var found = false;

        foreach (var oldw in openWarehouses)
        {
            foreach (var clwr in closedWarehouses)
            {
                if (clwr.StartCapacity >= oldw.StartCapacity - oldw.Capacity)
                {
                    var stores = sol.Stores.Where(x => x.Suppliers.Any(x => x.Id == oldw.Id)).ToList();
                    var selectedwr = sol.Warehouses.Where(x => x.Id == clwr.Id).FirstOrDefault();

                    var oldwr = sol.Warehouses.Where(x => x.Id == oldw.Id).FirstOrDefault();
                    foreach (var store in stores)
                    {


                        store.Suppliers = new List<Warehouse>();
                        store.WarehousesSupply = new List<SupplyReq>();
                        store.Suppliers.Add(clwr);
                        selectedwr.Capacity = selectedwr.Capacity - store.Request;

                        store.WarehousesSupply.Add(new SupplyReq { warehouse = selectedwr, supplyreq = store.Request });

                        oldwr.Capacity = oldwr.StartCapacity;
                        oldwr.Open = false;

                    }
                    found = true;
                    break;
                }
            }
            if (found)
                break;
        }




        //foreach (var nwr in sol.Warehouses)
        //{
        //    if (nwr.Capacity >= wr.Capacity && nwr != wr && !nwr.Open)
        //    {
        //        selectedwr = nwr;
        //        found = true;
        //        continue;
        //    }
        //}
        //if (found)
        //{


        //    foreach (var store in stores)
        //    {
        //        store.Suppliers = new List<Warehouse>();
        //        store.WarehousesSupply = new List<SupplyReq>();
        //        store.Suppliers.Add(selectedwr);
        //        selectedwr.Capacity = selectedwr.Capacity - store.Request;

        //        store.WarehousesSupply.Add(new SupplyReq { warehouse = selectedwr, supplyreq = store.Request });

        //        wr.Capacity = wr.StartCapacity;
        //        wr.Open = false;

        //    }
        //}





        return sol;
    }



    static ProSolution intialSol(ProSolution sol)
    {
        // Create a HashSet to store incompatible pairs
        var incompatiblePairs = new HashSet<string>();

        // Assign goods to stores
        foreach (var store in sol.Stores)
        {

            foreach (var wr in store.SupplyCosts)
            {
                var solwr = sol.Warehouses.Where(x => x.Id == wr.Key).SingleOrDefault();
                solwr.supplyForStore = (int)wr.Value;

            }

            sol.Warehouses = sol.Warehouses.OrderBy(x => x.supplyForStore).ToList();

            foreach (var warehouse in sol.Warehouses)
            {
                // Check if the warehouse-store pair is incompatible
                if (incompatiblePairs.Contains($"{warehouse.Id},{store.Id}"))
                {
                    continue;
                }

                if (warehouse.Capacity >= store.Request)
                {
                    warehouse.Open = true;
                    sol.Procapacity = sol.Procapacity - store.Request;
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
