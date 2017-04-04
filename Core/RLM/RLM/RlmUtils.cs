// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Data.SqlClient;
using RLM.Models;
using RLM.Database;
using RLM.Enums;
using System.Reflection;

namespace RLM
{
    public static class RlmUtils
    {
        public static RlmDatatypeDescription GetRlmEnumDescription(this RlmInputDataType value)
        {
            RlmDatatypeDescription retVal = null;
            FieldInfo fi = value.GetType().GetField(value.ToString());

            RlmEnumDescriptionAttribute[] attributes = (RlmEnumDescriptionAttribute[])fi.GetCustomAttributes(typeof(RlmEnumDescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                retVal = new RlmDatatypeDescription();
                retVal.Description = attributes[0].Description;
                retVal.SystemType = attributes[0].SystemType;
            }

            return retVal;
        }

        public static RlmInputDataType GetRlmInputDataType(string systemType)
        {
            if (string.IsNullOrEmpty(systemType))
            {
                throw new ArgumentNullException("systemType");
            }

            RlmInputDataType retVal = RlmInputDataType.String;
            foreach(RlmInputDataType item in Enum.GetValues(typeof(RlmInputDataType)))
            {
                var rnnDataTypeDesc = item.GetRlmEnumDescription();
                if (rnnDataTypeDesc.SystemType.ToString() == systemType)
                {
                    retVal = item;
                    break;
                }
            }

            return retVal;
        }

        public static void ResetTrainingData(RlmDbEntities db, long rnetworkId)
        {
            string sql = $@"
                DELETE FROM [Cases] WHERE [Session_ID] IN (SELECT [ID] FROM [Sessions] WHERE [Rnetwork_ID] = @p0);
                DELETE FROM [Sessions] WHERE [Rnetwork_ID] = @p0;";

            db.Database.ExecuteSqlCommand(sql, rnetworkId);
        }

        #region used by Legacy RlmNetwork functions
        public static double GetVariance(RlmDbEntities db, long rnetworkId, int top)
        {
            double retVal = 0;

            var sessions = db.Sessions
                .Where(a => a.Rnetwork.ID == rnetworkId)
                .OrderByDescending(a => a.DateTimeStop)
                .Select(a => a.SessionScore)
                .Take(top);

            double max = sessions.Max();
            double min = sessions.Min();
            double diff = sessions.Max() - sessions.Min();

            retVal = (diff <= 0) ? 0 : (diff / max);

            return retVal;
        }

        public static int GetTotalSimulationInSeconds(RlmDbEntities db, long rnetworkId)
        {
            int retVal = 0;

            //var total = db.Sessions
            //    .Where(a => a.Rnetwork.ID == rnetworkId && (!a.Hidden || a.SessionScore != Int32.MinValue))
            //    .Select(a => (a.DateTimeStop - a.DateTimeStart))
            //    .Sum(a => a.TotalSeconds);

            string sql = "select datediff(second,'1900-01-01 00:00:00.0000000', convert(datetime,sum(convert(float,DateTimeStop)-Convert(float,DateTimeStart)))) from [Sessions] where Rnetwork_ID = @p0 and hidden = 0";
            var result = db.Database.SqlQuery<int?>(sql, rnetworkId);
            var resultVal = result.FirstOrDefault();
            if (resultVal.HasValue)
            {
                retVal = resultVal.Value;
            }

            return retVal;
        }

        public static RlmStats GetRNetworkStatistics(RlmDbEntities db, long rnetworkId)
        {
            RlmStats retVal = null;

            string sql = $@"
                 declare @lastSessionId bigint,
                 @lastSessionScore float,
                 @lastSessionTime int;

                select top 1 
	                @lastSessionId = [ID],
	                @lastSessionScore = [SessionScore],
	                @lastSessionTime = coalesce(datediff(second,'1900-01-01 00:00:00.0000000', convert(datetime,convert(float,DateTimeStop)-Convert(float,DateTimeStart))),0)
                from [Sessions] 
                where [Rnetwork_ID] = @p0 and [Hidden] = 0 and [SessionScore] <> @p1
                order by [ID] desc;

                select 
                    coalesce(datediff(second,'1900-01-01 00:00:00.0000000', convert(datetime,avg(convert(float,DateTimeStop)-Convert(float,DateTimeStart)))),0) as [AvgTimePerSessionInSeconds],
                    coalesce(datediff(second,'1900-01-01 00:00:00.0000000', convert(datetime,sum(convert(float,DateTimeStop)-Convert(float,DateTimeStart)))),0) as [TotalSessionTimeInSeconds],
                    count([ID]) as [TotalSessions],
                    coalesce(max([SessionScore]),0) as [MaxSessionScore],
                    coalesce(avg([SessionScore]),0) as [AvgSessionScore],
                    coalesce(@lastSessionId,0) as [LastSessionId],
                    coalesce(@lastSessionScore,0) as [LastSessionScore],
                    coalesce(@lastSessionTime,0) as [LastSessionTimeInSeconds]
                from [Sessions]                 
                where [Rnetwork_ID] = @p0 and [Hidden] = 0 and [SessionScore] <> @p1";

            retVal = db.Database.SqlQuery<RlmStats>(sql, rnetworkId, int.MinValue).FirstOrDefault();

            if (retVal != null)
            {
                retVal.NumSessionsSinceLastBestScore = GetNumSessionSinceBestScore(db, rnetworkId);
            }

            return retVal;
        }

        public static int GetNumSessionSinceBestScore(RlmDbEntities db, long rnetworkId)
        {
            int retVal = 0;

            string sql = @"
                select count(*) as [NumSessionSinceBestScore]
                from
                (
	                select
		                [ID],
		                [SessionScore],
		                ROW_NUMBER() OVER( ORDER BY [ID] DESC) as [Num]
	                from [Sessions]
	                where ID > (select top 1 [ID] from [Sessions] order by [SessionScore] desc, [ID] desc) and [Rnetwork_ID] = @p0
                ) a";

            int? result = db.Database.SqlQuery<int?>(sql, rnetworkId).FirstOrDefault();
            if (result.HasValue)
            {
                retVal = result.Value;
            }

            return retVal;
        }

        
        public static IEnumerable<Session> GetSessions(RlmDbEntities db, long rnetworkId, int? skip = null, int? take = null, bool descending = false)
        {
            IEnumerable<Session> retVal = db.Sessions
                .Where(a => a.Rnetwork.ID == rnetworkId);

            if (descending)
            {
                retVal = retVal.OrderByDescending(a => a.DateTimeStart);
            }
            else
            {
                retVal = retVal.OrderBy(a => a.DateTimeStart);
            }

            if (skip.HasValue && take.HasValue)
            {
                retVal = retVal.Skip(skip.Value)
                    .Take(take.Value);
            }

            return retVal.ToList();
        }

        public static IEnumerable<RlmSessionSummary> GetSessionSummary(RlmDbEntities db, long rnetworkId, int groupBy, bool descending = false)
        {
            IEnumerable<RlmSessionSummary> retVal = null;

            string sql = $@"
                WITH T AS(
                  SELECT
                    *, RANK() OVER(ORDER BY[ID]) as [Row]
                  FROM [Sessions]
                  WHERE [Rnetwork_ID] = @p1 AND [Hidden] = 0 AND [SessionScore] <> @p2
                )
                SELECT
                    ([Row] - 1) / @p0 as [GroupId], 
	                coalesce(avg([SessionScore]), 0) as [Score],
	                coalesce(datediff(second, '1900-01-01 00:00:00.0000000', convert(datetime, avg(convert(float,[DateTimeStop]) - Convert(float,[DateTimeStart])))), 0) as [TimeInSeconds]
                FROM T
                GROUP BY (([Row] - 1) / @p0)
                ORDER BY [GroupId] {(descending ? "DESC" : "ASC")}";

            retVal = db.Database.SqlQuery<RlmSessionSummary>(sql, groupBy, rnetworkId, int.MinValue).ToList();

            return retVal;
        }

        public static IEnumerable<Case> GetCases(RlmDbEntities db, long sessionId, int? skip = null, int? take = null)
        {
            IEnumerable<Case> retVal = db.Cases
                .Include(a => a.Rneuron.Input_Values_Reneurons.Select(b => b.Input))
                .Include(a => a.Solution.Output_Values_Solutions.Select(b => b.Output))
                .Where(a => a.Session.ID == sessionId)
                .OrderBy(a => a.ID);

            if (skip.HasValue && take.HasValue)
            {
                retVal = retVal.Skip(skip.Value)
                    .Take(take.Value);
            }

            return retVal.ToList();
        }

        public static bool RLMHasTrainingData(string dbName)
        {
            bool retVal = false;

            using (RlmDbEntities master = new RlmDbEntities("master"))
            {
                if (master.DBExists(dbName))
                {
                    using (RlmDbEntities db = new RlmDbEntities(dbName))
                    {
                        if (db.Sessions.Count() > 0)
                        {
                            if (db.Cases.Count() > 0)
                            {
                                retVal = true;
                            }
                        }
                    }
                }
            }

            return retVal;
        }
        #endregion
    }
}
