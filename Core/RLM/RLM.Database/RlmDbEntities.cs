using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RLM.Enums;
using RLM.Models;
using RLM.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    //[DbConfigurationType(typeof(ContextConfiguration))]
    public class RlmDbEntities : DbContext
    {
        public const string DEFAULT_RLM_DBNAME = "RyskampLearningMachines";
        public const string MASTER_DB = "master";
        public const string DBNAME_PLACEHOLDER = "{{dbName}}";
        public string DatabaseName { get; private set; }

        public virtual DbSet<Rnetwork> Rnetworks { get; set; }
        public virtual DbSet<Rneuron> Rneurons { get; set; }
        public virtual DbSet<Session> Sessions { get; set; }
        public virtual DbSet<Case> Cases { get; set; }
        public virtual DbSet<Solution> Solutions { get; set; }
        public virtual DbSet<Idea_Implementation> Idea_Implementations { get; set; }
        public virtual DbSet<Idea_Module> Idea_Modules { get; set; }
        public virtual DbSet<RnetworkSetting> RnetworkSettings { get; set; }
        public virtual DbSet<Input_Output_Type> Input_Output_Types { get; set; }
        public virtual DbSet<Input> Inputs { get; set; }
        public virtual DbSet<Input_Values_Rneuron> Input_Values_Reneurons { get; set; }
        public virtual DbSet<Output> Outputs { get; set; }
        public virtual DbSet<Output_Values_Solution> Output_Values_Solutions { get; set; }

        #region Static
        static RlmDbEntities()
        {
            //if (string.IsNullOrEmpty(ConnStr))
            //{
            //    connStr = DetermineSQLConnectionString();
            //}

            //CreateRLMFoldersIfNotExists();
            //CreateRLMTemplateDB();
            //TODO: Migrate to EF Core

            // So that front end apps no longer need to reference EF
            // via http://stackoverflow.com/a/29743758/6223318
            //var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            //if (type == null)
            //    throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
        }

        public static string DefaultRLMTemplateDb
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMTemplateDb"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = "RLM_TEMPLATE";
                }
                return retVal;
            }
        }

        private static string connStr;
        public static string ConnStr
        {
            get
            {
                var retVal = string.Empty;

                // tries to get the connection string set in the config file
                retVal = ConfigurationManager.AppSettings["RLMConnStr"];

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

        public static string BackupLocation
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMBackupLocation"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = @"C:\RLM\Backup";
                }
                return retVal;
            }
        }

        public static string DataLocation
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMDataLocation"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = @"C:\RLM\Data";
                }
                return retVal;
            }
        }

        public static string BCPLocation
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMBcpLocation"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = @"C:\RLM\BCP_temporary_files";
                }
                return retVal;
            }
        }
        
        public static void CreateRLMFoldersIfNotExists()
        {
            Directory.CreateDirectory(BackupLocation);
            Directory.CreateDirectory(DataLocation);
            Directory.CreateDirectory(BCPLocation);
        }

        public static void CreateRLMTemplateDB()
        {
            using (RlmDbEntities db = new RlmDbEntities(DefaultRLMTemplateDb))
            {
                // delete if backup already exists
                if (db.FileBackupExists(DefaultRLMTemplateDb))
                {
                    db.DeleteFileBackup(DefaultRLMTemplateDb);
                }

                db.BackupDB(DefaultRLMTemplateDb);
            }
        }

        public static string DetermineDbName()
        {
            string retVal = DEFAULT_RLM_DBNAME;
            string sqlExistDb = "select name from sys.databases where name like @p0 + '%'";
            string sqlConnCount = @"
                SELECT 
                    DB_NAME(dbid) as Name, 
                    COUNT(dbid) as NumConnections
                FROM
                    sys.sysprocesses
                WHERE
                    dbid > 0 and DB_NAME(dbid) like @p0 + '%'
                GROUP BY
                    dbid ";

            //using (RlmDbEntities db = new RlmDbEntities(MASTER_DB))
            //{
            //    var existDbs = db.Database.SqlQuery<string>(sqlExistDb, DEFAULT_RLM_DBNAME).ToList();

            //    if (existDbs.Count > 0)
            //    {
            //        var dbConnections = db.Database.SqlQuery<RlmDBInfo>(sqlConnCount, DEFAULT_RLM_DBNAME).ToList();

            //        if (dbConnections.Count > 0)
            //        {
            //            // check for none existing connections
            //            List<string> dbWithConnections = dbConnections.Select(a => a.Name).ToList();
            //            List<string> dbWithoutConnections = existDbs.Except(dbWithConnections).ToList();

            //            if (dbWithoutConnections.Count > 0)
            //            {
            //                retVal = dbWithoutConnections.First();
            //                DropDb(db.Database, retVal);
            //            }
            //            else
            //            {
            //                retVal = GetPostfixedDbName(DEFAULT_RLM_DBNAME);
            //            }
            //        }
            //        else if (dbConnections.Count == 0 && existDbs.Count > 0)
            //        {
            //            retVal = existDbs.First();
            //            DropDb(db.Database, retVal);
            //        }
            //    }
            //}

            throw new Exception("TODO migrate to ef core");

            return retVal;
        }

        private static string GetPostfixedDbName(string dbName)
        {
            return dbName + "_" + Guid.NewGuid().ToString("N");
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
                    throw new RlmDefaultConnectionStringException("Unable to connect to the SQL Server using the default connection strings. Please provide a SQL Connection String on the application config file to override the default.");
                }
            }

            return retVal;
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

        private static void DropDb(DatabaseFacade databaseContext, string databaseName)
        {
            string sql = $@"
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];";
            databaseContext.ExecuteSqlCommand(sql);
        }

        #endregion

        public RlmDbEntities()
        {
            Database.EnsureCreated();
            DatabaseName = DEFAULT_RLM_DBNAME;
            Initialize();
        }

        public RlmDbEntities(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("The database name cannot be null or empty.");
            }
            
            DatabaseName = databaseName;
            bool isMasterDb = databaseName == MASTER_DB;
            Initialize(isMasterDb);

            //Database.EnsureCreated();
        }

        private void Initialize(bool isMasterDb = false)
        {
            try
            {
                string connString = ConnStr;
                var connection = Database.GetDbConnection();
                if (ConnStr.Contains(DBNAME_PLACEHOLDER))
                {
                    connection.ConnectionString = connString.Replace(DBNAME_PLACEHOLDER, DatabaseName);
                }
                else
                {
                    connection.ConnectionString = connString;
                }

                if (isMasterDb)
                {
                    //System.Data.Entity.Database.SetInitializer<RlmDbEntities>(null);
                }
                else
                {
                    //System.Data.Entity.Database.SetInitializer<RlmDbEntities>(new RLMCreateDBIfNotExists());
                }
                //Database.SetInitializer<RnnDbEntities>(new MigrateDatabaseToLatestVersion<RnnDbEntities, Configuration>(true));
                //Database.SetInitializer<RnnDbEntities>(null);
                //((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 0;

                //this.Configuration.AutoDetectChangesEnabled = false;
                //this.Configuration.ProxyCreationEnabled = false;
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex.ToString());
                throw;
            }
        }

        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //}

        public virtual void SetDBRecoveryMode(string database, DBRecoveryMode mode)
        {
            string sql = $"ALTER DATABASE [{database}] SET RECOVERY {mode.ToString()}";
            Database.ExecuteSqlCommand(sql);
        }

        public virtual void BackupDB(string database)
        {
            string path = Path.Combine(BackupLocation, database + ".bak");
            string sql = $"BACKUP DATABASE [{database}] TO DISK = @p0 WITH COMPRESSION";
            try
            {
                Database.ExecuteSqlCommand(sql, path);
            }
            catch (SqlException e)
            {
                if (e.Number == 1844)
                {
                    sql = sql.Replace("WITH COMPRESSION", string.Empty);
                    Database.ExecuteSqlCommand(sql, path);
                }
                else
                {
                    throw;
                }
            }
        }

        public virtual void RestoreDB(string database)
        {
            string path = Path.Combine(BackupLocation, database + ".bak");
            string sql = $"RESTORE DATABASE [{database}] FROM DISK = @p0";
            Database.ExecuteSqlCommand(sql, path);
        }

        public virtual void RestoreDBFromTemplate(string database)
        {
            string path = Path.Combine(BackupLocation, DefaultRLMTemplateDb + ".bak");
            string dataPath = Path.Combine(DataLocation, database + "_data.mdf");
            string logPath = Path.Combine(DataLocation, database + "_log.ldf");
            //string fileGroupPath = Path.Combine(DataLocation, database + "_dir");

            string sql = $@"RESTORE DATABASE [{database}] 
                FROM DISK = @p0
                WITH RECOVERY,
                MOVE '{DefaultRLMTemplateDb}' TO @p1,
                MOVE '{DefaultRLMTemplateDb}_log' TO @p2";
            //MOVE '{DefaultRLMTemplateDb}_dir' TO @p3";

            // restore db using the RNN_TEMPLATE backup
            Database.OpenConnection();
            Database.ExecuteSqlCommand(sql, path, dataPath, logPath); //, fileGroupPath);
                        
            // Modify logical names
            // data file
            //Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, $"ALTER DATABASE [{database}] MODIFY FILE (NAME='{DefaultRLMTemplateDb}', NEWNAME='{database}')");

            //// log file
            //Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, $"ALTER DATABASE [{database}] MODIFY FILE (NAME='{DefaultRLMTemplateDb}_log', NEWNAME='{database}_log')");

            //// in-mem file
            //Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, $"ALTER DATABASE [{database}] MODIFY FILE (NAME='{DefaultRLMTemplateDb}_dir', NEWNAME='{database}_dir')");

            //// filegroup
            //Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, $"ALTER DATABASE [{database}] MODIFY FILEGROUP [{DefaultRLMTemplateDb}_fg] NAME=[{database}_fg]");
        }

        public virtual void DropDB(string database)
        {
            string sql = $@"
                ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{database}];";
            Database.ExecuteSqlCommand(sql);
        }

        public virtual bool DBExists(string database)
        {
            bool retVal = false;

            string sql = $"SELECT CONVERT(BIT, 1) FROM SYS.DATABASES WHERE [Name] = @p0";
            //var result = Database.SqlQuery<bool?>(sql, database);

            var connection = Database.GetDbConnection();

            DataTable dt_databases = new DataTable();
            SqlDataAdapter dataAdapter = new SqlDataAdapter(sql, connection.ConnectionString);
            dataAdapter.Fill(dt_databases);

            if (dt_databases.Rows.Count > 0)
            {
                retVal = true;
            }

            //if (result != null)
            //{
            //    var resultItem = result.FirstOrDefault();
            //    if (resultItem.HasValue)
            //    {
            //        retVal = resultItem.Value;
            //    }
            //}

            return retVal;
        }

        public virtual bool FileBackupExists(string database)
        {
            string path = Path.Combine(BackupLocation, database + ".bak");
            return File.Exists(path);
        }

        public virtual void DeleteFileBackup(string database)
        {
            string path = Path.Combine(BackupLocation, database + ".bak");
            File.Delete(path);
        }
    }

    //public class RLMCreateDBIfNotExists : CreateDatabaseIfNotExists<RlmDbEntities>
    //{
    //    public override void InitializeDatabase(RlmDbEntities context)
    //    {
    //        base.InitializeDatabase(context);
    //    }
    //}

    //public class ContextConfiguration : DbConfiguration
    //{
    //    public ContextConfiguration()
    //    {
    //        SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy(5, TimeSpan.FromSeconds(30)));
    //    }
    //}

    #region Replaced by RnnCreateDBIfNotExists
    /*
    public class Configuration : DbMigrationsConfiguration<RnnDbEntities>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(RnnDbEntities context)
        {
            base.Seed(context);

            // checks if function already exists
            var result = context.Database.SqlQuery<int>(@"SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE [SPECIFIC_NAME] = @func_name", new SqlParameter("@func_name", "ConvertToDouble"));
            // creates a SQL Function for the Convert functions needed in our EF queries
            if (result != null && result.First() == 0)
            {
                context.Database.ExecuteSqlCommand(@"
                    CREATE FUNCTION [dbo].[ConvertToDouble] (@value nvarchar(100)) RETURNS float AS
                    BEGIN RETURN CONVERT(float, @value) END");
            }
        }
    }
    */
    #endregion
}