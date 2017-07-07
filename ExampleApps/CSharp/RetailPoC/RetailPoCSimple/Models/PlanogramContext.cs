using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;

namespace RetailPoCSimple.Models
{
    public class PlanogramContext : DbContext
    {
        // Your context has been configured to use a 'PlanogramContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'RetailPoC.Models.PlanogramContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'PlanogramContext' 
        // connection string in the application configuration file.
        public PlanogramContext()
            : base ("name=PlanogramContext")
        {
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