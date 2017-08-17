// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using RLM.Database;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmSessionCaseHistory
    {
        public string DatabaseName { get; private set; }
        public RlmSessionCaseHistory(string databaseName)
        {
            this.DatabaseName = databaseName;
        }
        public IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string query = "SELECT Id, SessionScore, DateTimeStart, DateTimeStop, ROW_NUMBER() over (order by DateTimeStart asc) as [SessionNumber] from [Sessions];";

                if (pageFrom == null || pageTo == null)
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).ToList();
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).Skip(pageFrom.Value).Take(resultCount.Value).ToList();
                }
            }

            return retVal;
        }

        public IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int? pageFrom = null, int? pageTo = null)
        {
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

                if (pageFrom == null || pageTo == null)
                {
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).ToList();
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = db.Database.SqlQuery<RlmSessionHistory>(query).Skip(pageFrom.Value).Take(resultCount.Value).ToList();
                }
            }

            return retVal;
        }

        public IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmCaseHistory> retVal = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                string query = $"SELECT Id, ROW_NUMBER() OVER (ORDER BY CycleStartTime ASC) AS [RowNumber], CycleStartTime AS DateTimeStart, CycleEndTime AS DateTimeStop, CycleScore, Session_Id AS SessionId, Rneuron_Id AS RneuronId, Solution_Id AS SolutionId FROM Cases where Session_ID = {sessionId};";

                if (pageFrom == null || pageTo == null)
                {
                    retVal = db.Database.SqlQuery<RlmCaseHistory>(query).ToList();
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = db.Database.SqlQuery<RlmCaseHistory>(query).Skip(pageFrom.Value).Take(resultCount.Value).ToList();
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

        public long? GetRneuronIdFromInputs(KeyValuePair<string, string>[] inputs)
        {
            long? retVal = null;
            List<SqlParameter> parameters = new List<SqlParameter>();

            StringBuilder query = new StringBuilder();
            StringBuilder joins = new StringBuilder();
            StringBuilder where = new StringBuilder();
            
            query.AppendLine($@"
                  select distinct
	                    r.ID
                  from Rneurons r");

            int cnt = 0;
            foreach(var input in inputs)
            {
                SqlParameter input_name = new SqlParameter($"p{cnt++}", input.Key);
                SqlParameter input_val = new SqlParameter($"p{cnt++}", input.Value);

                //query += $"(i.Name = @{input_name.ParameterName} AND ivr.Value = @{input_val.ParameterName}) AND\n";
                string alias = $"i{cnt}";
                joins.AppendLine($"inner join (select ivr.Rneuron_ID, i.Name, ivr.Value from Input_Values_Rneuron ivr inner join Inputs i on ivr.Input_ID = i.ID) {alias} on r.ID = {alias}.Rneuron_ID");

                where.AppendLine($"({alias}.Name = @{input_name.ParameterName} AND {alias}.Value = @{input_val.ParameterName}) AND");

                parameters.Add(input_name);
                parameters.Add(input_val);
            }

            var index = where.ToString().LastIndexOf("AND");
            if (index > 0)
            {
                where.Remove(index, 3);
            }

            query.AppendLine(joins.ToString());
            query.AppendLine("where");
            query.AppendLine(where.ToString());

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Database.SqlQuery<long?>(query.ToString(), parameters.ToArray()).FirstOrDefault();
            }

            return retVal;
        }

        public long? GetSolutionIdFromOutputs(KeyValuePair<string, string>[] outputs)
        {
            long? retVal = null;
            List<SqlParameter> parameters = new List<SqlParameter>();

            string query = $@"
                    SELECT DISTINCT
	                    ovs.Solution_ID
                    FROM Output_Values_Solution ovs
                    INNER JOIN Outputs o ON ovs.Output_ID = o.ID 
                    WHERE ";

            int cnt = 0;
            foreach (var output in outputs)
            {
                SqlParameter output_name = new SqlParameter($"p{cnt++}", output.Key);
                SqlParameter output_val = new SqlParameter($"p{cnt++}", output.Value);

                query += $"(o.Name = @{output_name.ParameterName} AND ovs.Value = @{output_val.ParameterName}) AND\n";

                parameters.Add(output_name);
                parameters.Add(output_val);
            }
            query = query.Substring(0, query.LastIndexOf("AND"));

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Database.SqlQuery<long?>(query, parameters.ToArray()).FirstOrDefault();
            }

            return retVal;
        }
        
        public IEnumerable<RlmLearnedCase> GetLearnedCases(long rneuronId, long solutionId, double scale)
        {
            IEnumerable<RlmLearnedCase> retVal = null;

            var rneuronParam = new SqlParameter("rneuron", rneuronId);
            var solutionParam = new SqlParameter("solution", solutionId);
            var scaleParam = new SqlParameter("scale", scale);

            string query = $@"
                WITH cte (ID, [Time], Score)
                AS
                (
	                SELECT 
		                c.ID,
		                s.[Time],
		                MAX(s.SessionScore) OVER(ORDER BY c.ID ASC) Score
	                FROM Cases c
	                INNER JOIN (
		                SELECT
			                ID,
			                SUM(DATEDIFF(ms, DateTimeStart, DateTimeStop)) OVER (ORDER BY DateTimeStart) [Time],
			                SessionScore
		                FROM [Sessions]
	                ) s ON c.Session_ID = s.ID
	                WHERE c.Rneuron_ID = @{rneuronParam.ParameterName} AND c.Solution_ID = @{solutionParam.ParameterName}
                )
                SELECT TOP (@{scaleParam.ParameterName}) PERCENT
	                MIN(ID) CaseID,
	                MIN([Time]) [Time],
	                Score
                FROM cte
                GROUP BY Score
                ORDER BY Score DESC";

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Database.SqlQuery<RlmLearnedCase>(query, rneuronParam, solutionParam, scaleParam).ToList();
            }

            return retVal;
        }

        public long? GetNextPreviousLearnedCaseId(long caseId, bool next = false)
        {
            long? retVal = null;

            string tempTablePostfix = Guid.NewGuid().ToString("N");
            var caseParam = new SqlParameter("case", caseId);

            string query = $@"
                DECLARE @temp_table_{tempTablePostfix} TABLE(ID BIGINT, Score FLOAT)

                INSERT INTO @temp_table_{tempTablePostfix}
                SELECT
	                c.ID,
	                s.SessionScore as Score
                FROM
                (
	                SELECT 
		                MIN(sub.Id) as ID,		
		                sub.Score
	                FROM (
		                SELECT 
			                c.ID as Id,
			                MAX(s.SessionScore) OVER(ORDER BY c.ID ASC) as Score
		                FROM Cases c
		                INNER JOIN [Sessions] s ON c.Session_ID = s.ID
		                WHERE c.Rneuron_ID = (select Rneuron_ID from Cases where ID = @{caseParam.ParameterName}) AND c.Solution_ID = (select Solution_ID from Cases where ID = @{caseParam.ParameterName})
	                ) sub
	                GROUP BY sub.Score
                ) sub
                INNER JOIN [Cases] c ON sub.Id = c.ID
                INNER JOIN [Sessions] s ON c.Session_ID = s.ID

                SELECT
                    s2.ID AS PreviousCaseId
                FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s1
                LEFT JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s2 on s1.ord = s2.ord {(next ? "+" : "-")} 1
                WHERE s1.ID = @{caseParam.ParameterName}";

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Database.SqlQuery<long?>(query, caseParam).FirstOrDefault();
            }

            return retVal;
        }
        
        public IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(params long[] caseIds)
        {
            IEnumerable<RlmLearnedCaseDetails> retVal = null;

            var sqlParams = new List<SqlParameter>();
            for (int i = 0; i < caseIds.Length; i++)
            {
                sqlParams.Add(new SqlParameter($"id_{i}", caseIds[i]));
            }

            string query = $@"
                WITH cte (ID, SessionScore, [Time], [Order])
                AS
                (	
	                SELECT
		                ID,
		                SessionScore,
		                SUM(DATEDIFF(ms, DateTimeStart, DateTimeStop)) OVER (ORDER BY DateTimeStart) [Time],
		                ROW_NUMBER() OVER (ORDER BY DateTimeStart) [Order]
	                FROM [Sessions]
                )
                SELECT
	                c.ID CaseId,
	                c.[Order] CycleNum,
	                c.CycleScore CycleScore,
	                c.Session_ID SessionId,
	                s.[SessionScore] SessionScore,
	                s.[Time] SessionTime,
	                s.[Order] SessionNum
                FROM Cases c
                INNER JOIN cte s ON c.Session_ID = s.ID
                WHERE c.ID in ({string.Join(",", sqlParams.Select(a => $"@{a.ParameterName}").ToArray())})";

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                retVal = db.Database.SqlQuery<RlmLearnedCaseDetails>(query, sqlParams.ToArray()).ToList();
            }

            return retVal;
        }

        public IEnumerable<RlmIODetails>[] GetCaseIODetails(long caseId)
        {
            const int INPUT_INDEX = 0;
            const int OUTPUT_INDEX = 1; 
            var retVal = new IEnumerable<RlmIODetails>[2];

            var caseParam = new SqlParameter("caseId", caseId);

            string query = $@"
                SELECT
	                i.ID,
	                i.Name,
	                ivr.Value,
	                CAST(1 AS BIT) [IsInput]
                FROM Cases c
                INNER JOIN Input_Values_Rneuron ivr on c.Rneuron_ID = ivr.Rneuron_ID
                INNER JOIN Inputs i on ivr.Input_ID = ivr.Input_ID
                WHERE c.ID = @{caseParam.ParameterName}
                UNION
                SELECT
	                o.ID,
	                o.Name,
	                ovs.Value,
	                CAST(0 AS BIT) [IsInput]
                FROM Cases c
                INNER JOIN Output_Values_Solution ovs on c.Solution_ID = ovs.Solution_ID
                INNER JOIN Outputs o on ovs.Output_ID = ovs.Output_ID
                WHERE c.ID = @{caseParam.ParameterName}";

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                var results = db.Database.SqlQuery<RlmIODetails>(query, caseParam).ToList();

                if (results != null)
                {
                    retVal[INPUT_INDEX] = results.Where(a => a.IsInput).ToList();
                    retVal[OUTPUT_INDEX] = results.Where(a => !a.IsInput).ToList();
                }
            }

            return retVal;
        }

        public IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(params long[] sessionIds)
        {
            var retVal = new List<RlmLearnedSessionDetails>();

            var sqlParams = new List<SqlParameter>();
            for (int i = 0; i < sessionIds.Length; i++)
            {
                sqlParams.Add(new SqlParameter($"id_{i}", sessionIds[i]));
            }

            string query = $@"
                SELECT
	                i.ID,
	                i.Name,
	                ivr.Value,
	                CAST(1 AS BIT) [IsInput],	
	                c.ID [CaseId],
	                c.CycleScore,
	                c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM Cases c
                INNER JOIN Input_Values_Rneuron ivr on c.Rneuron_ID = ivr.Rneuron_ID
                INNER JOIN Inputs i on ivr.Input_ID = ivr.Input_ID
                WHERE c.Session_ID in ({string.Join(",", sqlParams.Select(a => $"@{a.ParameterName}").ToArray())})
                UNION
                SELECT
	                o.ID,
	                o.Name,
	                ovs.Value,
	                CAST(0 AS BIT) [IsInput],
	                c.ID [CaseId],
	                c.CycleScore,
	                c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM Cases c
                LEFT JOIN Output_Values_Solution ovs on c.Solution_ID = ovs.Solution_ID
                LEFT JOIN Outputs o on ovs.Output_ID = ovs.Output_ID
                WHERE c.Session_ID in ({string.Join(",", sqlParams.Select(a => $"@{a.ParameterName}").ToArray())})";

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                var results = db.Database.SqlQuery<RlmIODetails>(query, sqlParams.ToArray()).ToList();
                if (results != null)
                {
                    var sessionIODetails = results.GroupBy(a => a.SessionId);
                    foreach (var item in sessionIODetails)
                    {
                        var learnedSess = new RlmLearnedSessionDetails();
                        learnedSess.SessionId = item.Key;
                        learnedSess.Inputs = item.Where(a => a.IsInput).OrderBy(a => a.CycleOrder).ToList();
                        learnedSess.Outputs = item.Where(a => !a.IsInput).OrderBy(a => a.CycleOrder).ToList();
                        retVal.Add(learnedSess);
                    }
                }
            }

            return retVal;
        }
    }
}
