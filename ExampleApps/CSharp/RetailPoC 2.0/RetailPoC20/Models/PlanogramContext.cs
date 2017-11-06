using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace RetailPoC.Models
{
    public class PlanogramContext : DbContext
    {
        public const string MASTER_DB = "master";
        public const string DEFAULT_DB_NAME = "Retail_PoC";
        public const string DBNAME_PLACEHOLDER = "{{dbName}}";

        static PlanogramContext()
        {
            if (string.IsNullOrEmpty(ConnStr))
            {
                connStr = DetermineSQLConnectionString();
            }
        }

        private static string connStr;
        public static string ConnStr
        {
            get
            {
                var retVal = string.Empty;

                // tries to get the connection string set in the config file
                retVal = ConfigurationManager.AppSettings["PlanogramConnStr"];

                if (string.IsNullOrEmpty(retVal))
                {
                    // otherwise, we use the default
                    retVal = connStr;
                }

                return retVal;
            }
            set
            {
                connStr = value;
            }
        }

        private static bool TryConnect(string connString)
        {
            try
            {
                string connStringWithTimeout = connString + "Connect Timeout=5;";
                using (var conn = new SqlConnection(connStringWithTimeout))
                {
                    conn.Open();
                }
            }
            catch (SqlException)
            {
                return false;
            }

            return true;
        }
        private static string DetermineSQLConnectionString()
        {
            string connStrSqlExpress = $"Server=.\\sqlexpress;Database={DBNAME_PLACEHOLDER};Integrated Security=True;";
            string connStrSql = $"Server=.;Database={DBNAME_PLACEHOLDER};Integrated Security=True;";

            // try SQLEXPRESS default connection string
            string retVal = connStrSqlExpress;
            if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, MASTER_DB)))
            {
                // try NON SQLEXPRESS
                retVal = connStrSql;
                if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, MASTER_DB)))
                {
                    throw new Exception("Unable to connect to the SQL Server using the default connection strings. Please provide a SQL Connection String on the application config file to override the default.");
                }
            }

            return retVal;
        }

        // Your context has been configured to use a 'PlanogramContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'RetailPoC.Models.PlanogramContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'PlanogramContext' 
        // connection string in the application configuration file.
        public PlanogramContext()
        {
            string connString = ConnStr;
            if (ConnStr.Contains(DBNAME_PLACEHOLDER))
            {
                Database.Connection.ConnectionString = connString.Replace(DBNAME_PLACEHOLDER, DEFAULT_DB_NAME);
            }
            else
            {
                Database.Connection.ConnectionString = connString;
            }

            Database.SetInitializer(new CreateDatabaseIfNotExists<PlanogramContext>());
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<Attributes> ItemAttributes { get; set; }


        public virtual void DropDB(string database)
        {
            string sql = $@"
                ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{database}];";
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql);
        }

        public virtual bool DBExists(string database)
        {
            bool retVal = false;

            string sql = $"SELECT CONVERT(BIT, 1) FROM SYS.DATABASES WHERE [Name] = @p0";
            var result = Database.SqlQuery<bool?>(sql, database);
            if (result != null)
            {
                var resultItem = result.FirstOrDefault();
                if (resultItem.HasValue)
                {
                    retVal = resultItem.Value;
                }
            }

            return retVal;
        }
    }



    //public class MyEntity
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}
}