using RLM.Enums;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    [DbConfigurationType(typeof(ContextConfiguration))]
    public class RlmDbEntities : DbContext
    {
        private const string DEFAULT_RLM_DB_NAME = "RyskampLearningMachines";
        public const string MASTER_DB = "master";

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

        private static string ConnStr
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMConnStr"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = "Server=.;Database={{dbName}};Integrated Security=True;";
                }
                return retVal;
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
            CreateRLMFoldersIfNotExists();
            CreateRLMTemplateDB();
        }

        public static void CreateRLMFoldersIfNotExists()
        {
            Directory.CreateDirectory(BackupLocation);
            Directory.CreateDirectory(DataLocation);
        }

        public static void CreateRLMTemplateDB()
        {
            using (RlmDbEntities db = new RlmDbEntities(DefaultRLMTemplateDb))
            {
                db.Database.CreateIfNotExists();

                // delete if backup already exists
                if (db.FileBackupExists(DefaultRLMTemplateDb))
                {
                    db.DeleteFileBackup(DefaultRLMTemplateDb);
                }

                db.BackupDB(DefaultRLMTemplateDb);
            }
        }
        #endregion

        public RlmDbEntities()
            : base(DEFAULT_RLM_DB_NAME)
        {
            DatabaseName = DEFAULT_RLM_DB_NAME;
            Initialize();
        }

        public RlmDbEntities(string databaseName)
            : base(ConnStr.Replace("{{dbName}}", databaseName))
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("The database name cannot be null or empty.");
            }

            DatabaseName = databaseName;
            bool isMasterDb = databaseName == MASTER_DB;
            Initialize(isMasterDb);
        }

        private void Initialize(bool isMasterDb = false)
        {
            try
            {
                if (isMasterDb)
                {
                    System.Data.Entity.Database.SetInitializer<RlmDbEntities>(null);
                }
                else
                {
                    System.Data.Entity.Database.SetInitializer<RlmDbEntities>(new RLMCreateDBIfNotExists());
                }
                //Database.SetInitializer<RnnDbEntities>(new MigrateDatabaseToLatestVersion<RnnDbEntities, Configuration>(true));
                //Database.SetInitializer<RnnDbEntities>(null);
                ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 0;
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex.ToString());
                throw;
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public virtual void SetDBRecoveryMode(string database, DBRecoveryMode mode)
        {
            string sql = $"ALTER DATABASE [{database}] SET RECOVERY {mode.ToString()}";
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql);
        }

        public virtual void BackupDB(string database)
        {
            string path = Path.Combine(BackupLocation, database + ".bak");
            string sql = $"BACKUP DATABASE [{database}] TO DISK = @p0 WITH COMPRESSION";
            try
            {
                Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql, path);
            }
            catch (SqlException e)
            {
                if (e.Number == 1844)
                {
                    sql = sql.Replace("WITH COMPRESSION", string.Empty);
                    Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql, path);
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
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql, path);
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
            Database.Connection.Open();
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, sql, path, dataPath, logPath); //, fileGroupPath);
                        
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

    public class RLMCreateDBIfNotExists : CreateDatabaseIfNotExists<RlmDbEntities>
    {
        public override void InitializeDatabase(RlmDbEntities context)
        {
            base.InitializeDatabase(context);
        }
    }

    public class ContextConfiguration : DbConfiguration
    {
        public ContextConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy(5, TimeSpan.FromSeconds(30)));
        }
    }

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