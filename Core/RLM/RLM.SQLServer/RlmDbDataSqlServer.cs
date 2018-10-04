using Dapper;
using RLM.Models;
using RLM.Models.Exceptions;
using RLM.Models.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RLM.SQLServer
{
    public class RlmDbDataSQLServer : BaseRlmDbData
    {
        public RlmDbDataSQLServer(string databaseName) : base(databaseName)
        {
        }

        public override void Create<T>(T entity)
        {
            var entityType = typeof(T);
            var sqlbuilder = new StringBuilder();
            EntityInfo einfo = Util.GetEntityInfo<T>();

            sqlbuilder.Append($"INSERT INTO [{DatabaseName}].[dbo].[{entityType.Name}] VALUES (");

            // exclude Id property if Identity
            var props = einfo.Properties;
            if (einfo.IsIdentity)
                props = props.Where(a => a.Name != einfo.IDProperty);

            sqlbuilder.Append(string.Join(",", props.Select(a => $"@{a.Name}")));

            string sql = sqlbuilder.Append(") ").ToString();

            using (IDbConnection conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                conn.Execute(sql, entity);
            }
        }

        public override void Create<T>(IEnumerable<T> entities)
        {
            foreach(var item in entities)
            {
                Create(item);
            }
        }
        
        public override void CreateTable<T>()
        {
            string entityName = typeof(T).Name;
            EntityInfo einfo = Util.GetEntityInfo<T>();
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($@"USE {DatabaseName}; ");
            sqlBuilder.Append($"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = '{entityName}') ");
            sqlBuilder.Append($@"CREATE TABLE [dbo].[{entityName}] (");

            StringBuilder foreignKeyBuilder = new StringBuilder();

            foreach(var pinfo in einfo.Properties)
            {
                Type propType = pinfo.PropertyType;

                if (propType.IsClass && propType != typeof(string))
                {
                    continue;
                }

                foreach (var item in pinfo.GetCustomAttributes())
                {
                    if (item is ForeignKeyAttribute)
                    {
                        var attr = item as ForeignKeyAttribute;
                        foreignKeyBuilder.Append($"CONSTRAINT FK_{entityName}_{pinfo.Name} FOREIGN KEY ({pinfo.Name}) REFERENCES [{attr.Name}] (ID), ");
                        break;
                    }
                }

                string sqlType = GetColumnDataType(propType);
                string sqlTypeStr = sqlType == "NVarChar" ? $"[{sqlType}] (MAX) NULL" : $"[{sqlType}] NOT NULL";
                
                sqlBuilder.Append($@"[{pinfo.Name}] {sqlTypeStr} {(einfo.IDProperty == pinfo.Name && einfo.IsIdentity ? "IDENTITY" : string.Empty)}, ");
            }

            sqlBuilder.Append($"CONSTRAINT PK_{entityName} PRIMARY KEY (ID),\n");
            sqlBuilder.Append(foreignKeyBuilder.ToString().TrimEnd(',')).Append(")");

            using (IDbConnection conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                conn.Execute(sqlBuilder.ToString());
            }
        }

        public override IEnumerable<T> FindAll<T>()
        {
            var entityType = typeof(T).Name;
            EntityInfo einfo = Util.GetEntityInfo<T>();

            string sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType}]";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                using (var comm = new SqlCommand(sql, (SqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T instance = Activator.CreateInstance<T>();
                            foreach (var item in einfo.Properties)
                            {
                                item.SetValue(instance, reader[item.Name]);
                            }

                            yield return instance;
                        }
                    }
                }
            }
        }

        public override IEnumerable<TResult> FindAll<TBase, TResult>()
        {
            var entityType = typeof(TBase);
            EntityInfo einfo = Util.GetEntityInfo<TBase>();

            string sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType.Name}]";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                using (var comm = new SqlCommand(sql, (SqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TResult instance = Activator.CreateInstance<TResult>();
                            foreach (var item in einfo.Properties)
                            {
                                if(!(reader[item.Name] is DBNull))
                                    item.SetValue(instance, reader[item.Name]);
                            }

                            yield return instance;
                        }
                    }
                }
            }
        }

        public override T FindByID<T>(long id)
        {
            var entityType = typeof(T);
            T retVal = default(T);

            string sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType.Name}] WHERE ID = @{nameof(id)}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<T>(sql, new { id });
            }

            return retVal;
        }

        public override TResult FindByID<TBase, TResult>(long id)
        {
            var entityType = typeof(TBase);
            TResult retVal = default(TResult);

            string sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType.Name}] WHERE ID = @{nameof(id)}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<TResult>(sql, new { id });
            }

            return retVal;
        }

        public override T FindByName<T>(string name)
        {
            var entityType = typeof(T);
            T retVal = default(T);
            EntityInfo einfo = Util.GetEntityInfo<T>();
            string sql = string.Empty;

            PropertyInfo nameProp = einfo.Properties.FirstOrDefault(a => a.Name.ToLower() == "name");
            if (nameProp != null)
            {
                sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType.Name}] WHERE {nameProp.Name} = @{nameof(name)}";

                using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
                {
                    retVal = conn.QuerySingleOrDefault<T>(sql, new { name });
                }
            }

            return retVal;
        }

        public override TResult FindByName<TBase, TResult>(string name)
        {
            var entityType = typeof(TBase);
            TResult retVal = default(TResult);
            EntityInfo einfo = Util.GetEntityInfo<TBase>();
            string sql = string.Empty;

            PropertyInfo nameProp = einfo.Properties.FirstOrDefault(a => a.Name.ToLower() == "name");
            if (nameProp != null)
            {
                sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{entityType.Name}] WHERE {nameProp.Name} = @{nameof(name)}";

                using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
                {
                    retVal = conn.QuerySingleOrDefault<TResult>(sql, new { name });
                }
            }

            return retVal;
        }

        public override void Update<T>(T entity)
        {
            var entityType = typeof(T);
            var sqlbuilder = new StringBuilder();
            EntityInfo einfo = Util.GetEntityInfo<T>();

            sqlbuilder.Append($"UPDATE [{DatabaseName}].[dbo].[{entityType.Name}] SET ");
            sqlbuilder.Append(string.Join(", ", einfo.Properties.Where(a => a.Name != einfo.IDProperty).Select(a => $"[{a.Name}] = @{a.Name}")));
            sqlbuilder.Append($" WHERE {einfo.IDProperty} = @{einfo.IDProperty}");

            using (IDbConnection conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                conn.Execute(sqlbuilder.ToString(), entity);
            }
        }
        
        public override void DropDB(string dbName)
        {
            throw new NotImplementedException();
        }

        public override int Count<T>()
        {
            var entityType = typeof(T);
            int retVal = 0;

            string sql = $"SELECT COUNT(*) FROM [{DatabaseName}].[dbo].[{entityType.Name}]";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql);
            }

            return retVal;
        }
        
        public override TResult Max<TSource, TResult>(Expression<Func<TSource, TResult>> propertyExpr)
        {
            var entityType = typeof(TSource);
            TResult retVal = default(TResult);
            PropertyInfo propInfo = Util.GetPropertyInfo(propertyExpr);
            
            string sql = $"SELECT MAX([{propInfo.Name}]) FROM [{DatabaseName}].[dbo].[{entityType.Name}]";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<TResult>(sql);
            }

            return retVal;
        }

        public override int CountBestSolutions()
        {
            int retVal = 0;
            string sql = $@"SELECT
                    COUNT(*) [Cnt]
                FROM
                (
                    SELECT
                        main.Rneuron_ID,
                        main.Solution_ID
                    FROM [{DatabaseName}].[dbo].[{nameof(_Case)}] main
                    INNER JOIN [{DatabaseName}].[dbo].[{nameof(_Session)}] sess ON main.[Session_ID] = sess.[ID]
	                WHERE sess.[Hidden] = 0
                    GROUP BY main.[Rneuron_ID], main.[Solution_ID]
                ) a";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql);
            }

            return retVal;
        }

        public override IEnumerable<BestSolution> LoadBestSolutions()
        {
            EntityInfo einfo = Util.GetEntityInfo<BestSolution>();

            string sql = $@"SELECT
                    main.[Rneuron_ID] as [RneuronId],
                    main.[Solution_ID] as [SolutionId],
                    MAX(main.[CycleScore]) as [CycleScore],
                    MAX(sess.[SessionScore]) as [SessionScore],
                    MAX(main.[Order]) as [CycleOrder]
                FROM [{DatabaseName}].[dbo].[{nameof(_Case)}] main with (nolock)
                INNER JOIN [{DatabaseName}].[dbo].[{nameof(_Session)}] sess ON main.[Session_ID] = sess.[ID]
                WHERE sess.[Hidden] = 0
                GROUP BY main.[Rneuron_ID], main.[Solution_ID]";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                using (var comm = new SqlCommand(sql, (SqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var instance = new BestSolution();
                            foreach (var item in einfo.Properties)
                            {
                                item.SetValue(instance, reader[item.Name]);
                            }

                            yield return instance;
                        }
                    }
                }
            }
        }

        public override IEnumerable<Session> LoadVisibleSessions()
        {
            EntityInfo einfo = Util.GetEntityInfo<_Session>();

            string sql = $"SELECT * FROM [{DatabaseName}].[dbo].[{nameof(_Session)}] WHERE [Hidden] = 0";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                using (var comm = new SqlCommand(sql, (SqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var instance = new Session();
                            foreach (var item in einfo.Properties)
                            {
                                item.SetValue(instance, reader[item.Name]);
                            }

                            yield return instance;
                        }
                    }
                }
            }
        }

        protected override string DetermineSQLConnectionString()
        {
            string connStrSqlExpress = $"Server=.\\sqlexpress;Database={DBNAME_PLACEHOLDER};Integrated Security=True;";
            string connStrSql = $"Server=.;Database={DBNAME_PLACEHOLDER};Integrated Security=True;";

            // try SQLEXPRESS default connection string
            string retVal = connStrSqlExpress;
            if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                // try NON SQLEXPRESS
                retVal = connStrSql;
                if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
                {
                    throw new RlmDefaultConnectionStringException("Unable to connect to the SQL Server using the default connection strings. Please provide a SQL Connection String on the application config file to override the default.");
                }
            }

            return retVal;
        }

        protected override IDbConnection GetOpenDBConnection(string connStr)
        {
            var retVal = new SqlConnection(connStr);
            retVal.Open();
            return retVal;
        }

        protected override bool DBExists(string dbName)
        {
            bool retVal = false;
            string sql = $"SELECT CONVERT(BIT, 1) FROM SYS.DATABASES WHERE [Name] = @dbName";
            string connStr = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase());

            using (var conn = GetOpenDBConnection(connStr))
            {
                retVal = conn.QueryFirstOrDefault<bool>(sql, new { dbName });
            }

            return retVal;
        }

        protected override void CreateDBIfNotExists(string dbName)
        {
            if (DBExists(dbName))
                return;

            string sql = $"CREATE DATABASE [{dbName}]";
            string connStr = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase());

            using (var conn = GetOpenDBConnection(connStr))
            {
                conn.Execute(sql);
            }
        }

        public override string GetColumnDataType(Type type)
        {
            string dataType = null;

            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type.IsEnum)
            {
                return nameof(SqlDbType.Int);
            }

            switch (type.Name)
            {
                case "System.Int32":
                case "Int32":
                    dataType = nameof(SqlDbType.Int);
                    break;
                case "System.Int64":
                case "Int64":
                    dataType = nameof(SqlDbType.BigInt);
                    break;
                case "System.Double":
                case "Double":
                    dataType = nameof(SqlDbType.Float);
                    break;
                case "System.String":
                case "String":
                    dataType = nameof(SqlDbType.NVarChar);
                    break;
                case "System.DateTime":
                case "DateTime":
                    dataType = nameof(SqlDbType.DateTime);
                    break;
                case "System.Boolean":
                case "Boolean":
                    dataType = nameof(SqlDbType.Bit);
                    break;
                case "System.Int16":
                case "Int16":
                    dataType = nameof(SqlDbType.SmallInt);
                    break;
            }

            return dataType;
        }

        protected override string GetDefaultDatabase()
        {
            return "master";
        }

        //RLM Data Legacy
        public override double GetVariance(long networkId, int top)
        {
            double retVal = 0;

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                string sql = $@"SELECT TOP {top} SessionScore FROM _Session WHERE Rnetwork_ID = @p0 ORDER BY DateTimeStop DESC";

                var sessions = conn.Query<double>(sql, new { p0 = networkId });

                double max = sessions.Max();
                double min = sessions.Min();
                double diff = max - min;

                retVal = (diff <= 0) ? 0 : (diff / max);
            }

            return retVal;
        }

        public override int GetTotalSimulationInSeconds(long networkId)
        {
            int retVal = 0;

            string sql = $@"SELECT DATEDIFF(SECOND,'1900-01-01 00:00:00.0000000', 
                            CONVERT(DATETIME,SUM(CONVERT(FLOAT,DateTimeStop)-CONVERT(FLOAT,DateTimeStart)))) 
                            FROM [{nameof(_Session)}] WHERE Rnetwork_ID = @p0 and hidden = 0";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = networkId });
            }

            return retVal;
        }

        public override IEnumerable<Session> GetSessions(long networkId, int? skip = null, int? take = null, bool descending = false)
        {
            IEnumerable<Session> retVal = null;

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                string sql = $"SELECT * FROM {nameof(_Session)} WHERE Rnetwork_ID = @p0";
                retVal = conn.Query<Session>(sql, new { p0 = networkId });

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
            }

            return retVal;
        }

        public override IEnumerable<RlmSessionSummary> GetSessionSummary(long networkId, int groupBy, bool descending = false)
        {
            IEnumerable<RlmSessionSummary> retVal = null;

            string sql = $@"
                WITH T AS(
                  SELECT
                    *, RANK() OVER(ORDER BY[ID]) as [Row]
                  FROM [{nameof(_Session)}]
                  WHERE [Rnetwork_ID] = @p1 AND [Hidden] = 0 AND [SessionScore] <> @p2
                )
                SELECT
                    ([Row] - 1) / @p0 as [GroupId], 
                 coalesce(avg([SessionScore]), 0) as [Score],
                 coalesce(datediff(second, '1900-01-01 00:00:00.0000000', convert(datetime, avg(convert(float,[DateTimeStop]) - Convert(float,[DateTimeStart])))), 0) as [TimeInSeconds]
                FROM T
                GROUP BY (([Row] - 1) / @p0)
                ORDER BY [GroupId] {(descending ? "DESC" : "ASC")}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmSessionSummary>(sql, new { p0 = groupBy, p1 = networkId, p2 = int.MinValue });
            }

            return retVal;
        }

        public override int GetSessionCount(long networkId)
        {
            int retVal = 0;

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                string sql = $"SELECT COUNT(*) FROM {nameof(_Session)} WHERE Rnetwork_ID = @p0";
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = networkId });
            }

            return retVal;
        }

        public override IEnumerable<Case> GetCases(long sessionId, int? skip = null, int? take = null)
        {
            IEnumerable<Case> retVal = null;

            string sql = $@"SELECT * FROM {nameof(_Case)} WHERE Session_ID = @p0";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<Case>(sql, new { p0 = sessionId });
                //TODO: include Rneuron, Input_Values_Rneuron, Input
            }

            if(skip.HasValue && take.HasValue)
            {
                retVal = retVal.Skip(skip.Value)
                    .Take(take.Value);
            }

            return retVal.ToList();
        }

        public override int GetCaseCount(long sessionId)
        {
            int retVal = 0;
            string sql = $"SELECT COUNT(*) FROM {nameof(_Case)} WHERE Session_ID = @p0";
            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = sessionId });
            }

            return retVal;
        }

        public override RlmStats GetStatistics(long networkId)
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
                from [{nameof(_Session)}] 
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
                from [{nameof(_Session)}]                 
                where [Rnetwork_ID] = @p0 and [Hidden] = 0 and [SessionScore] <> @p1";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmStats>(sql, new { p0 = networkId, p1 =  int.MinValue }).FirstOrDefault();

                if(retVal != null)
                {
                    retVal.NumSessionsSinceLastBestScore = GetNumSessionSinceBestScore(networkId);
                }
            }

            return retVal;
        }

        public override int GetNumSessionSinceBestScore(long rnetworkId)
        {
            int retVal = 0;

            string sql = $@"
                SELECT COUNT(*) AS [NumSessionSinceBestScore]
                FROM
                (
	                SELECT
		                [ID],
		                [SessionScore],
		                ROW_NUMBER() OVER( ORDER BY [ID] DESC) as [Num]
	                FROM [{nameof(_Session)}]
	                WHERE ID > (SELECT TOP 1 [ID] FROM [{nameof(_Session)}] ORDER BY [SessionScore] DESC, [ID] DESC) AND [Rnetwork_ID] = @p0
                ) a";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = rnetworkId });
            }

            return retVal;
        }

        public override IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = null;
            string sql = $"SELECT Id, SessionScore, DateTimeStart, DateTimeStop, ROW_NUMBER() over (order by DateTimeStart asc) as [SessionNumber] from [{nameof(_Session)}];";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                if (pageFrom == null || pageTo == null)
                {
                    retVal = conn.Query<RlmSessionHistory>(sql);
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = conn.Query<RlmSessionHistory>(sql).Skip(pageFrom.Value).Take(resultCount.Value);
                }
            }

            return retVal;
        }

        public override IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = null;
            string sql = $@"SELECT 
                                    a.ID,
                                    a.SessionScore,
                                    a.[Order] AS [SessionNumber],
                                    a.DateTimeStart,
                                    a.DateTimeStop
                                FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY DateTimeStart ASC) AS [Order] FROM [{nameof(_Session)}]) a
                                INNER JOIN 
                                (
                                    SELECT 
                                    SessionScore, 
                                    MIN([Order]) AS [Order]
                                    FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY DateTimeStart ASC) AS [Order]  FROM [{nameof(_Session)}]) a
                                    GROUP BY SessionScore
                                ) b ON a.SessionScore = b.SessionScore AND a.[Order] = b.[order]
                                ORDER BY a.[SessionScore] DESC;";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                if (pageFrom == null || pageTo == null)
                {
                    retVal = conn.Query<RlmSessionHistory>(sql).ToList();
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = conn.Query<RlmSessionHistory>(sql).Skip(pageFrom.Value).Take(resultCount.Value).ToList();
                }
            }

            return retVal;
        }

        public override IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmCaseHistory> retVal = null;
            string query = $"SELECT Id, ROW_NUMBER() OVER (ORDER BY [Order] ASC) AS [RowNumber], CycleStartTime AS DateTimeStart, CycleEndTime AS DateTimeStop, CycleScore, Session_Id AS SessionId, Rneuron_Id AS RneuronId, Solution_Id AS SolutionId, [Order] AS CycleOrder FROM {nameof(_Case)} where Session_ID = {sessionId};";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {

                if (pageFrom == null || pageTo == null)
                {
                    retVal = conn.Query<RlmCaseHistory>(query);
                }
                else
                {
                    var resultCount = pageTo - pageFrom;
                    retVal = conn.Query<RlmCaseHistory>(query).Skip(pageFrom.Value).Take(resultCount.Value);
                }
            }

            return retVal;
        }

        public override RlmCaseIOHistory GetCaseIOHistory(long caseId, long rneuronId, long solutionId)
        {
            RlmCaseIOHistory retVal = new RlmCaseIOHistory();

            string queryIn = $"SELECT {nameof(_Input)}.[ID] AS Id, {nameof(_Input)}.[Name], {nameof(_Input_Values_Rneuron)}.[Value] FROM {nameof(_Input_Values_Rneuron)} INNER JOIN {nameof(_Input)} ON {nameof(_Input)}.ID = {nameof(_Input_Values_Rneuron)}.Input_ID WHERE Rneuron_ID = @p0;";
            string queryOut = $"SELECT {nameof(_Output)}.[ID] AS Id, {nameof(_Output)}.[Name], {nameof(_Output_Values_Solution)}.[Value] FROM {nameof(_Output_Values_Solution)} INNER JOIN {nameof(_Output)} ON {nameof(_Output)}.ID = {nameof(_Output_Values_Solution)}.Output_ID WHERE Solution_ID = @p0;";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                var resultsIn = conn.Query<RlmCaseInputOutput>(queryIn, new { p0 = rneuronId });
                var resultsOut = conn.Query<RlmCaseInputOutput>(queryOut, new { p0 = solutionId });

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

        public override long? GetRneuronIdFromInputs(KeyValuePair<string, string>[] inputs)
        {
            long? retVal = null;
            List<SqlParameter> parameters = new List<SqlParameter>();
            Dictionary<string, object> paramVals = new Dictionary<string, object>();

            StringBuilder query = new StringBuilder();
            StringBuilder joins = new StringBuilder();
            StringBuilder where = new StringBuilder();

            query.AppendLine($@"
                  select distinct
	                    r.ID
                  from {nameof(_Rneuron)} r");

            int cnt = 0;
            foreach (var input in inputs)
            {
                SqlParameter input_name = new SqlParameter($"p{cnt++}", input.Key);
                SqlParameter input_val = new SqlParameter($"p{cnt++}", input.Value);

                string alias = $"i{cnt}";
                joins.AppendLine($"inner join (select ivr.Rneuron_ID, i.Name, ivr.Value from {nameof(_Input_Values_Rneuron)} ivr inner join {nameof(_Input)} i on ivr.Input_ID = i.ID) {alias} on r.ID = {alias}.Rneuron_ID");

                where.AppendLine($"({alias}.Name = @{input_name.ParameterName} AND {alias}.Value = @{input_val.ParameterName}) AND");

                parameters.Add(input_name);
                parameters.Add(input_val);

                paramVals[input_name.ParameterName] = input.Key;
                paramVals[input_val.ParameterName] = input.Value;
            }

            var index = where.ToString().LastIndexOf("AND");
            if (index > 0)
            {
                where.Remove(index, 3);
            }

            query.AppendLine(joins.ToString());
            query.AppendLine("where");
            query.AppendLine(where.ToString());

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QueryFirstOrDefault<long?>(query.ToString(), paramVals);
            }

            return retVal;
        }

        public override long? GetSolutionIdFromOutputs(KeyValuePair<string, string>[] outputs)
        {
            long? retVal = null;
            List<SqlParameter> parameters = new List<SqlParameter>();
            Dictionary<string, object> paramVals = new Dictionary<string, object>();

            string query = $@"
                    SELECT DISTINCT
	                    ovs.Solution_ID
                    FROM {nameof(_Output_Values_Solution)} ovs
                    INNER JOIN {nameof(_Output)} o ON ovs.Output_ID = o.ID 
                    WHERE ";

            int cnt = 0;
            foreach (var output in outputs)
            {
                SqlParameter output_name = new SqlParameter($"p{cnt++}", output.Key);
                SqlParameter output_val = new SqlParameter($"p{cnt++}", output.Value);

                query += $"(o.Name = @{output_name.ParameterName} AND ovs.Value = @{output_val.ParameterName}) AND\n";

                parameters.Add(output_name);
                parameters.Add(output_val);

                paramVals[output_name.ParameterName] = output.Key;
                paramVals[output_val.ParameterName] = output.Value;
            }
            query = query.Substring(0, query.LastIndexOf("AND"));

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QueryFirstOrDefault<long?>(query, paramVals);
            }

            return retVal;
        }

        public override IEnumerable<RlmLearnedCase> GetLearnedCases(long rneuronId, long solutionId, double scale)
        {
            IEnumerable<RlmLearnedCase> retVal = null;

            var rneuronParam = new SqlParameter("p0", rneuronId);
            var solutionParam = new SqlParameter("p1", solutionId);
            var scaleParam = new SqlParameter("p2", scale);

            string query = $@"
                WITH cte (ID, [Time], Score)
                AS
                (
	                SELECT 
		                c.ID,
		                s.[Time],
		                MAX(s.SessionScore) OVER(ORDER BY c.ID ASC) Score
	                FROM {nameof(_Case)} c
	                INNER JOIN (
		                SELECT
			                ID,
			                SUM(DATEDIFF(ms, DateTimeStart, DateTimeStop)) OVER (ORDER BY DateTimeStart) [Time],
			                SessionScore
		                FROM [{nameof(_Session)}]
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

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmLearnedCase>(query, new { p0 = rneuronId, p1 = solutionId, p2 = scale }).ToList();
            }

            return retVal;
        }

        public override IEnumerable<RlmLearnedSession> GetLearnedSessions(double scale)
        {
            IEnumerable<RlmLearnedSession> retVal = null;

            var scaleParam = new SqlParameter("p0", scale);

            string query = $@"
                WITH cte (ID, [Time], Score)
                AS
                (
	                SELECT 
		                ID,
		                [Time],
		                MAX(SessionScore) OVER (ORDER BY [Time]) Score
	                FROM (
		                SELECT
			                ID,
			                SUM(DATEDIFF(ms, DateTimeStart, DateTimeStop)) OVER (ORDER BY DateTimeStart) [Time],
			                SessionScore
		                FROM [{nameof(_Session)}] WHERE [Hidden] < 1
	                ) s
                )
                SELECT TOP (@{scaleParam.ParameterName}) PERCENT
	                MIN(ID) SessionId,
	                MIN([Time]) [Time],
	                Score
                FROM cte
                GROUP BY Score
                ORDER BY Score ASC";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmLearnedSession>(query, new { p0 = scale }).ToList();
            }

            return retVal;
        }

        public override long? GetNextPreviousLearnedCaseId(long caseId, bool next = false)
        {
            long? retVal = null;

            string tempTablePostfix = Guid.NewGuid().ToString("N");
            var caseParam = new SqlParameter("p0", caseId);

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
		                FROM {nameof(_Case)} c
		                INNER JOIN [{nameof(_Session)}] s ON c.Session_ID = s.ID
		                WHERE c.Rneuron_ID = (select Rneuron_ID from {nameof(_Case)} where ID = @{caseParam.ParameterName}) AND c.Solution_ID = (select Solution_ID from {nameof(_Case)} where ID = @{caseParam.ParameterName})
	                ) sub
	                GROUP BY sub.Score
                ) sub
                INNER JOIN [{nameof(_Case)}] c ON sub.Id = c.ID
                INNER JOIN [{nameof(_Session)}] s ON c.Session_ID = s.ID

                SELECT
                    s2.ID AS PreviousCaseId
                FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s1
                LEFT JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s2 on s1.ord = s2.ord {(next ? "+" : "-")} 1
                WHERE s1.ID = @{caseParam.ParameterName}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QueryFirstOrDefault<long?>(query, new { p0 = caseId });
            }

            return retVal;
        }

        public override long? GetNextPreviousLearnedSessionId(long sessionId, bool next = false)
        {
            long? retVal = null;

            string tempTablePostfix = Guid.NewGuid().ToString("N");
            var sessParam = new SqlParameter("p0", sessionId);

            string query = $@"
                DECLARE @temp_table_{tempTablePostfix} TABLE(ID BIGINT, Score FLOAT);

                WITH cte (ID, [Time], Score)
                AS
                (
	                SELECT 
		                ID,
		                [Time],
		                MAX(SessionScore) OVER (ORDER BY [Time]) Score
	                FROM (
		                SELECT
			                ID,
			                SUM(DATEDIFF(ms, DateTimeStart, DateTimeStop)) OVER (ORDER BY DateTimeStart) [Time],
			                SessionScore
		                FROM [{nameof(_Session)}] WHERE [Hidden] < 1
	                ) s
                )
                INSERT INTO @temp_table_{tempTablePostfix}
                SELECT 
	                MIN(ID) ID,
	                Score
                FROM cte
                GROUP BY Score

                SELECT
                    s2.ID AS PreviousSessionId
                FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s1
                LEFT JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM @temp_table_{tempTablePostfix}) s2 on s1.ord = s2.ord {(next ? "+" : "-")} 1
                WHERE s1.ID = @{sessParam.ParameterName}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.QueryFirstOrDefault<long?>(query, new { p0 = sessionId });
            }

            return retVal;
        }

        public override IEnumerable<RlmLearnedSession> GetSessionDetails(params long[] sessionIds)
        {
            IEnumerable<RlmLearnedSession> retVal = null;
            Dictionary<string, object> paramVals = new Dictionary<string, object>();

            for (int i = 0; i < sessionIds.Length; i++)
            {
                paramVals[$"id_{i}"] = sessionIds[i];
            }

            string query = $@"
                 WITH TempSessions AS
                 (SELECT
                    ID [SessionId],
                    SessionScore [Score],
                    DATEDIFF(MILLISECOND, DateTimeStart, DateTimeStop)  [Time],
                    ROW_NUMBER() OVER(ORDER BY DateTimeStart) As SessionNum
                FROM [{nameof(_Session)}])
                SELECT * FROM TempSessions 
                WHERE SessionId in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmLearnedSession>(query, paramVals);
            }

            return retVal;
        }

        public override IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(params long[] caseIds)
        {
            IEnumerable<RlmLearnedCaseDetails> retVal = null;
            Dictionary<string, object> paramVals = new Dictionary<string, object>();

            for (int i = 0; i < caseIds.Length; i++)
            {
                paramVals[$"id_{i}"] = caseIds[i];
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
                 FROM [{nameof(_Session)}]
                )
                SELECT
                 c.ID CaseId,
                 c.[Order] CycleNum,
                 c.CycleScore CycleScore,
                 c.Session_ID SessionId,
                 s.[SessionScore] SessionScore,
                 s.[Time] SessionTime,
                 s.[Order] SessionNum
                FROM {nameof(_Case)} c
                INNER JOIN cte s ON c.Session_ID = s.ID
                WHERE c.ID in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                retVal = conn.Query<RlmLearnedCaseDetails>(query, paramVals);
            }

            return retVal;
        }

        public override IEnumerable<RlmIODetails>[] GetCaseIODetails(long caseId)
        {
            const int INPUT_INDEX = 0;
            const int OUTPUT_INDEX = 1;
            var retVal = new IEnumerable<RlmIODetails>[2];

            var caseParam = new SqlParameter("p0", caseId);

            string query = $@"
                SELECT
                 i.ID,
                 i.Name,
                 i.Value,
                 CAST(1 AS BIT) [IsInput],	
                 c.ID [CaseId],
                 c.CycleScore,
                 c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM {nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                  i.ID,
                  i.Name,
                  ivr.Value,
                  ivr.Rneuron_ID
                 FROM [{nameof(_Input_Values_Rneuron)}] ivr
                 LEFT JOIN [{nameof(_Input)}] i on ivr.Input_ID = i.ID
                ) i ON c.Rneuron_ID = i.Rneuron_ID
                WHERE c.ID = @{caseParam.ParameterName}
                UNION
                SELECT
                 o.ID,
                 o.Name,
                 o.Value,
                 CAST(0 AS BIT) [IsInput],
                 c.ID [CaseId],
                 c.CycleScore,
                 c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM {nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                  o.ID,
                  o.Name,
                  ovs.Value,
                  ovs.Solution_ID
                 FROM [{nameof(_Output_Values_Solution)}] ovs
                 LEFT JOIN [{nameof(_Output)}] o on ovs.Output_ID = o.ID
                ) o ON c.Solution_ID = o.Solution_ID
                WHERE c.ID = @{caseParam.ParameterName}";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                var results = conn.Query<RlmIODetails>(query, new { p0 = caseId });

                if (results != null)
                {
                    retVal[INPUT_INDEX] = results.Where(a => a.IsInput).ToList();
                    retVal[OUTPUT_INDEX] = results.Where(a => !a.IsInput).ToList();
                }
            }

            return retVal;
        }

        public override IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(params long[] sessionIds)
        {
            var retVal = new List<RlmLearnedSessionDetails>();
            Dictionary<string, object> paramVals = new Dictionary<string, object>();

            for (int i = 0; i < sessionIds.Length; i++)
            {
                paramVals[$"id_{i}"] = sessionIds[i];
            }

            string query = $@"
                SELECT
                 i.ID,
                 i.Name,
                 i.Value,
                 CAST(1 AS BIT) [IsInput],	
                 c.ID [CaseId],
                 c.CycleScore,
                 c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM {nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                  i.ID,
                  i.Name,
                  ivr.Value,
                  ivr.Rneuron_ID
                 FROM [{nameof(_Input_Values_Rneuron)}] ivr
                 LEFT JOIN [{nameof(_Input)}] i on ivr.Input_ID = i.ID
                ) i ON c.Rneuron_ID = i.Rneuron_ID
                WHERE c.Session_ID in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})
                UNION
                SELECT
                 o.ID,
                 o.Name,
                 o.Value,
                 CAST(0 AS BIT) [IsInput],
                 c.ID [CaseId],
                 c.CycleScore,
                 c.Session_ID [SessionId],
                    c.[Order] [CycleOrder]
                FROM {nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                  o.ID,
                  o.Name,
                  ovs.Value,
                  ovs.Solution_ID
                 FROM [{nameof(_Output_Values_Solution)}] ovs
                 LEFT JOIN [{nameof(_Output)}] o on ovs.Output_ID = o.ID
                ) o ON c.Solution_ID = o.Solution_ID
                WHERE c.Session_ID in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringWithDatabaseName))
            {
                var results = conn.Query<RlmIODetails>(query, paramVals);
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
