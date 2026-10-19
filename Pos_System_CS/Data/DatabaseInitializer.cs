using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace Pos_System_CS.Data
{
    internal static class DatabaseInitializer
    {
        public static void Initialize()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataDir = Path.Combine(baseDir, "Data");
                Directory.CreateDirectory(dataDir);
                // Set DataDirectory so connection strings with |DataDirectory| work
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDir);

                string mdfPath = Path.Combine(dataDir, "PosSystemDB.mdf");
                string ldfPath = Path.Combine(dataDir, "PosSystemDB_log.ldf");

                if (File.Exists(mdfPath))
                {
                    return; // already created
                }

                // Connect to master to create the database file
                var masterConn = new SqlConnectionStringBuilder
                {
                    DataSource = @"(localdb)\MSSQLLocalDB",
                    IntegratedSecurity = true,
                    InitialCatalog = "master",
                    ConnectTimeout = 30
                }.ToString();

                using (var conn = new SqlConnection(masterConn))
                {
                    conn.Open();
                    string safeMdf = mdfPath.Replace("'", "''");
                    string safeLdf = ldfPath.Replace("'", "''");
                    string createDb = $"CREATE DATABASE [PosSystemDB] ON (NAME = N'PosSystemDB', FILENAME = '{safeMdf}'), (NAME = N'PosSystemDB_log', FILENAME = '{safeLdf}')";
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createDb;
                        cmd.ExecuteNonQuery();
                    }
                }

                // Wait until the MDF file is accessible
                int attempts = 0;
                while (!File.Exists(mdfPath) && attempts < 10)
                {
                    Thread.Sleep(300);
                    attempts++;
                }

                // Create default schema (Items, Sales, SaleItems)
                var dbConn = new SqlConnectionStringBuilder
                {
                    DataSource = @"(localdb)\MSSQLLocalDB",
                    IntegratedSecurity = true,
                    InitialCatalog = "PosSystemDB",
                    ConnectTimeout = 30
                }.ToString();

                using (var conn = new SqlConnection(dbConn))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"IF OBJECT_ID('dbo.Items') IS NULL
BEGIN
    CREATE TABLE dbo.Items(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) NOT NULL
    );
END
IF OBJECT_ID('dbo.Sales') IS NULL
BEGIN
    CREATE TABLE dbo.Sales(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SaleDate DATETIME NOT NULL DEFAULT(GETDATE()),
        Total DECIMAL(18,2) NOT NULL
    );
END
IF OBJECT_ID('dbo.SaleItems') IS NULL
BEGIN
    CREATE TABLE dbo.SaleItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SaleId INT NOT NULL,
        ItemId INT NOT NULL,
        Quantity INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(Id),
        CONSTRAINT FK_SaleItems_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items(Id)
    );
END";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but do not crash the app
                try { Logger.LogError("Database initialization failed", ex); } catch { }
            }
        }
    }
}
