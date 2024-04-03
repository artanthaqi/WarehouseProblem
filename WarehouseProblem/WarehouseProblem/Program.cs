using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WarehouseProblem.Models;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"..\..\..\PublicInstances\wlp01.dzn";

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




            // Create a HashSet to store incompatible pairs
            var incompatiblePairs = new HashSet<string>();

            // Assign goods to stores
            foreach (var store in stores)
            {
                foreach (var warehouse in warehouses)
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
                        store.Supplier = warehouse;

                        // Add all incompatible pairs of the current store to the HashSet
                        foreach (var incompatibleStore in store.IncompatibleStores)
                        {
                            incompatiblePairs.Add($"{warehouse.Id},{incompatibleStore}");
                        }

                        break;
                    }
                }
            }

            var cost = EvaluateSolution(stores, warehouses);
            Console.WriteLine("Cost: " + cost);

            // Save results to a text file in the "Solutions" folder
            using (StreamWriter sw = new StreamWriter(Path.Combine(@"..\..\..\Solutions", $"sol-{Path.GetFileNameWithoutExtension(filePath)}.txt")))
            {
                sw.Write("{");
                for (int i = 0; i < stores.Count; i++)
                {
                    sw.Write($"({i + 1},{warehouses.IndexOf(stores[i].Supplier) + 1},{stores[i].Request})");
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

    static double EvaluateSolution(List<Store> stores, List<Warehouse> warehouses)
    {
        double totalCost = 0;
        var warehousesAdded = new HashSet<int>();
        foreach (var store in stores)
        {
            if (!warehousesAdded.Contains(store.Supplier.Id))
                // Add the fixed cost of the warehouse supplying the store
                totalCost += warehouses[store.Supplier.Id - 1].FixedCost;

            warehousesAdded.Add(store.Supplier.Id);

            // Add the supply cost for the store
            totalCost += store.Supply * store.Request;
        }

        return totalCost;
    }

}
