using RLM.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class SessionCaseHistory
    {
        public string DatabaseName { get; private set; }
        public SessionCaseHistory(string databaseName)
        {
            this.DatabaseName = databaseName;
        }
        public IEnumerable<RlmSessionHistory> GetSessionHistory(int pageFrom = 0, int pageTo = 0)
        {
            var resultCount = pageTo - pageFrom;
            IEnumerable<RlmSessionHistory> retVal = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string query = "SELECT Id, SessionScore, DateTimeStart, DateTimeStop, ROW_NUMBER() over (order by DateTimeStart asc) as [SessionNumber] from [Sessions];";

                if (pageFrom == 0 && pageTo == 0)
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).ToList();
                }
                else
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).Skip(pageFrom).Take(resultCount).ToList();
                }
            }

            return retVal;
        }

        public IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int pageFrom = 0, int pageTo = 0)
        {
            var resultCount = pageTo - pageFrom;
            IEnumerable<RlmSessionHistory> retVal = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string query = @"SELECT 
                                    a.ID,
                                    a.SessionScore,
                                    a.[Order] AS [SessionNumber],
                                    a.DateTimeStart,
                                    a.DateTimeStop
                                FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY DateTimeStart ASC) AS [Order] FROM [Sessions]) a
                                INNER JOIN 
                                (
                                    SELECT 
                                    SessionScore, 
                                    MIN([Order]) AS [Order]
                                    FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY DateTimeStart ASC) AS [Order]  FROM [Sessions]) a
                                    GROUP BY SessionScore
                                ) b ON a.SessionScore = b.SessionScore AND a.[Order] = b.[order]
                                ORDER BY a.[SessionScore] DESC;";

                if (pageFrom == 0 && pageTo == 0)
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).ToList();
                }
                else
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).Skip(pageFrom).Take(resultCount).ToList();
                }
            }

            return retVal;
        }

        public IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int pageFrom = 0, int pageTo = 50)
        {
            var resultCount = pageTo - pageFrom;
            IEnumerable<RlmCaseHistory> retVal = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string query = $"SELECT Id, ROW_NUMBER() OVER (ORDER BY CycleStartTime ASC) AS [RowNumber], CycleStartTime AS DateTimeStart, CycleEndTime AS DateTimeStop, CycleScore, Session_Id AS SessionId, Rneuron_Id AS RneuronId, Solution_Id AS SolutionId FROM Cases where Session_ID = {sessionId};";

                if (pageFrom == 0 && pageTo == 0)
                {
                    retVal = db.Database.SqlQuery<RlmCaseHistory>(query).ToList();
                }
                else
                {
                    retVal = db.Database.SqlQuery<RlmCaseHistory>(query).Skip(pageFrom).Take(resultCount).ToList();
                }
            }

            return retVal;
        }

        public RlmCaseIOHistory GetCaseIOHistory(long caseId, long rneuronId, long solutionId)
        {
            RlmCaseIOHistory retVal = new RlmCaseIOHistory();
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string queryIn = $"SELECT Inputs.[ID] AS Id, Inputs.[Name], Input_Values_Rneuron.[Value] FROM Input_Values_Rneuron INNER JOIN Inputs ON Inputs.ID = Input_Values_Rneuron.Input_ID WHERE Rneuron_ID = {rneuronId};";
                string queryOut = $"SELECT Outputs.[ID] AS Id, Outputs.[Name], Output_Values_Solution.[Value] FROM Output_Values_Solution INNER JOIN Outputs ON Outputs.ID = Output_Values_Solution.Output_ID WHERE Solution_ID = {solutionId};";

                var resultsIn = db.Database.SqlQuery<RlmCaseInputOutput>(queryIn);
                var resultsOut = db.Database.SqlQuery<RlmCaseInputOutput>(queryOut);

                retVal.Id = caseId;

                retVal.Inputs = resultsIn.Select(a =>
                {
                    return a;
                }).ToList();

                retVal.Outputs = resultsOut.Select(a =>
                {
                    return a;
                }).ToList();
            }

            return retVal;
        }
    }
}
