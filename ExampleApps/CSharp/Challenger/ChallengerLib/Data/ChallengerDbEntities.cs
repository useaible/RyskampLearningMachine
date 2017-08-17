using ChallengerLib.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChallengerLib.Data
{
    public class ChallengerDbEntities : DbContext
    {
        public ChallengerDbEntities()
        {
            Database.SetInitializer(new ChallengerInitializer());
        }

        public DbSet<BlockTemplate> BlockTemplates { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<Block>();
        }
    }
}
