using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"..\..\..\PublicInstances\toy.dzn";

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

            var warehouses = new List<WarehouseClass>();
            for (int i = 0; i < data.Warehouses; i++)
            {
                warehouses.Add(new WarehouseClass { Capacity = data.Capacity[i] });
            }

            var stores = new List<Store>();
            for (int i = 0; i < data.Stores; i++)
            {
                stores.Add(new Store { Request = data.Goods[i] });
            }

            // Assign goods to stores
            foreach (var store in stores)
            {
                foreach (var warehouse in warehouses)
                {
                    if (warehouse.Capacity >= store.Request)
                    {
                        warehouse.Capacity -= store.Request;
                        store.Supply = store.Request;
                        store.Supplier = warehouse;
                        break;
                    }
                }
            }

            // Print results
            for (int i = 0; i < stores.Count; i++)
            {
                Console.WriteLine($"Store {i + 1} is supplied by Warehouse {warehouses.IndexOf(stores[i].Supplier) + 1} with quantity {stores[i].Supply}");
            }
        }
        else
        {
            Console.WriteLine("No match found.");
        }
    }
}

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

public class WarehouseClass
{
    public int Capacity { get; set; }
}

public class Store
{
    public int Request { get; set; }
    public int Supply { get; set; }
    public WarehouseClass Supplier { get; set; }
}
