using ChallengerLib.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChallengerLib.Data
{
    public class ChallengerInitializer : CreateDatabaseIfNotExists<ChallengerDbEntities>
    {
        public override void InitializeDatabase(ChallengerDbEntities context)
        {
            context.BlockTemplates.Add(new BlockTemplate() { Name = "Start Simulation", Icon = Path.Combine("Icons", "robot.png") });
            context.BlockTemplates.Add(new BlockTemplate() { Name = "End Simulation", Icon = Path.Combine("Icons", "finish.png"), IsEndSimulation = true });
            context.BlockTemplates.Add(new BlockTemplate() { Name = "Basic", Icon = Path.Combine("Icons", "money.png") });

            base.InitializeDatabase(context);
        }
    }
}
