using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Pos_System_CS.Data
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class Sale
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public List<SaleItem> Items { get; set; } = new List<SaleItem>();
    }

    internal static class DataAccess
    {
        private static string GetConnectionString()
        {
            try
            {
                // Try to read the runtime config file first
                string configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                if (string.IsNullOrEmpty(configPath) || !System.IO.File.Exists(configPath))
                {
                    // Fallback to App.config in base directory
                    configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App.config");
                }

                if (!System.IO.File.Exists(configPath))
                    throw new InvalidOperationException($"Configuration file not found: {configPath}");

                var doc = System.Xml.Linq.XDocument.Load(configPath);
                var add = doc.Root.Element("connectionStrings")?.Element("add");
                var cs = add?.Attribute("connectionString")?.Value;
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Connection string 'PosSystemDB' not found in config.");
                return cs;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to read connection string", ex);
                throw;
            }
        }

        public static List<Item> GetItems()
        {
            var result = new List<Item>();
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name, Price FROM dbo.Items ORDER BY Name";
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            result.Add(new Item
                            {
                                Id = rdr.GetInt32(0),
                                Name = rdr.GetString(1),
                                Price = rdr.GetDecimal(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("GetItems failed", ex);
            }
            return result;
        }

        public static int AddItem(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO dbo.Items (Name, Price) VALUES (@name, @price); SELECT CAST(SCOPE_IDENTITY() AS int);";
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    conn.Open();
                    var id = cmd.ExecuteScalar();
                    return Convert.ToInt32(id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AddItem failed", ex);
                return -1;
            }
        }

        public static bool UpdateItem(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE dbo.Items SET Name = @name, Price = @price WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    cmd.Parameters.AddWithValue("@id", item.Id);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("UpdateItem failed", ex);
                return false;
            }
        }

        public static bool DeleteItem(int id)
        {
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM dbo.Items WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("DeleteItem failed", ex);
                return false;
            }
        }

        public static int CreateSale(decimal total, IEnumerable<SaleItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO dbo.Sales (Total) VALUES (@total); SELECT CAST(SCOPE_IDENTITY() AS int);";
                        cmd.Parameters.AddWithValue("@total", total);
                        var saleIdObj = cmd.ExecuteScalar();
                        int saleId = Convert.ToInt32(saleIdObj);

                        // Insert items
                        cmd.Parameters.Clear();
                        cmd.CommandText = "INSERT INTO dbo.SaleItems (SaleId, ItemId, Quantity, Amount) VALUES (@saleId, @itemId, @qty, @amount)";
                        foreach (var si in items)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.Parameters.AddWithValue("@itemId", si.ItemId);
                            cmd.Parameters.AddWithValue("@qty", si.Quantity);
                            cmd.Parameters.AddWithValue("@amount", si.Amount);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return saleId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("CreateSale failed", ex);
                return -1;
            }
        }

        public static List<Sale> GetSales()
        {
            var result = new List<Sale>();
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    // First get sales
                    cmd.CommandText = "SELECT Id, SaleDate, Total FROM dbo.Sales ORDER BY SaleDate DESC";
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            result.Add(new Sale
                            {
                                Id = rdr.GetInt32(0),
                                SaleDate = rdr.GetDateTime(1),
                                Total = rdr.GetDecimal(2)
                            });
                        }
                    }

                    if (result.Count == 0) return result;

                    // Load sale items for all sales
                    var saleIds = string.Join(",", result.ConvertAll(s => s.Id.ToString()));
                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.CommandText = $"SELECT Id, SaleId, ItemId, Quantity, Amount FROM dbo.SaleItems WHERE SaleId IN ({saleIds})";
                        using (var rdr2 = cmd2.ExecuteReader())
                        {
                            while (rdr2.Read())
                            {
                                var si = new SaleItem
                                {
                                    Id = rdr2.GetInt32(0),
                                    SaleId = rdr2.GetInt32(1),
                                    ItemId = rdr2.GetInt32(2),
                                    Quantity = rdr2.GetInt32(3),
                                    Amount = rdr2.GetDecimal(4)
                                };
                                var sale = result.Find(s => s.Id == si.SaleId);
                                sale?.Items.Add(si);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("GetSales failed", ex);
            }
            return result;
        }
    }
}
