using RLM.Database;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmNetworkLegacy : RlmNetwork
    {
        public RlmNetworkLegacy() : base() { }
        public RlmNetworkLegacy(string dbName) : base(dbName) { }

        public override long SessionStart()
        {
            long retVal =  base.SessionStart();

            // TEMPORARY for benchmark only
            MemoryManager.GetRneuronTimes.Clear();
            MemoryManager.RebuildCacheboxTimes.Clear();
            CurrentCycleCount = 0;
            MemoryManager.CacheBoxCount = 0;
            // TEMPORARY for benchmark only

            return retVal;
        }
        

        public int CurrentCycleCount { get; set; }
        public IEnumerable<Session> GetSessions()
        {
            return MemoryManager.Sessions.Values;
        }
        public double GetCacheBoxCnt()
        {
            return MemoryManager.CacheBoxCount;
        }
        public void SetCachBoxTolerance(double val, double bufferSize = 0, bool useMomentumAvgVal = false)
        {
            MemoryManager.MomentumAdjustment = val;
            MemoryManager.CacheBoxMargin = bufferSize;
            MemoryManager.UseMomentumAvgValue = useMomentumAvgVal;
        }
        public double GetSumGetRneuronTime()
        {
            return MemoryManager.GetRneuronTimes.Sum(a => a.Ticks);
        }
        public double GetSumRebuildCacheTime()
        {
            return MemoryManager.RebuildCacheboxTimes.Sum(a => a.Ticks);
        }

        public double GetBumpedIntoWallsCount(long networkId, long sessionId)
        {
            double retVal = 0;
            //using (RnnDbEntities db = new RnnDbEntities(DatabaseName))
            //{
            //    retVal = rnn_utils.GetBumpedIntoWallsPercentage(db, networkId, sessionId);
            //}
            return retVal;
        }

        public double GetVariance(int top)
        {
            double retVal = 0D;

            if (SessionCount > 0)
            {
                using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
                {
                    RlmUtils.GetVariance(db, CurrentNetworkID, top);
                }
            }

            return retVal;
        }

        public int GetTotalSimulationInSeconds()
        {
            int retVal = 0;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = RlmUtils.GetTotalSimulationInSeconds(db, CurrentNetworkID);
            }

            return retVal;
        }

        public IEnumerable<Session> GetSessions(int? skip = null, int? take = null, bool descending = false)
        {
            IEnumerable<Session> retVal = null;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = RlmUtils.GetSessions(db, CurrentNetworkID, skip, take, descending);
            }

            return retVal;
        }

        public IEnumerable<RlmSessionSummary> GetSessionSummary(int groupBy, bool descending = false)
        {
            IEnumerable<RlmSessionSummary> retVal = null;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = RlmUtils.GetSessionSummary(db, CurrentNetworkID, groupBy, descending);
            }

            return retVal;
        }

        public int GetSessionCount()
        {
            int retVal = 0;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Sessions.Where(a => a.Rnetwork.ID == CurrentNetworkID).Count();
            }

            return retVal;
        }

        public IEnumerable<Case> GetCases(long sessionId, int? skip = null, int? take = null)
        {
            IEnumerable<Case> retVal = null;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = RlmUtils.GetCases(db, sessionId, skip, take);
            }

            return retVal;
        }

        public int GetCasesCount(long sessionId)
        {
            int retVal = 0;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Cases.Where(a => a.Session.ID == sessionId).Count();
            }

            return retVal;
        }

        public RlmStats GetStatistics()
        {
            RlmStats retVal = null;

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = RlmUtils.GetRNetworkStatistics(db, CurrentNetworkID);
            }

            return retVal;
        }
    }
}
