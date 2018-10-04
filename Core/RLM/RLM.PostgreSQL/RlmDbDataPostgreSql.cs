using Dapper;
using Npgsql;
using NpgsqlTypes;
using RLM.Models;
using RLM.Models.Exceptions;
using RLM.Models.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RLM.PostgreSQLServer
{
    public class RlmDbDataPostgreSql : BaseRlmDbData
    {
        public RlmDbDataPostgreSql(string databaseName) : base(databaseName)
        {
        }

        public override void Create<T>(T entity)
        {
            try
            {
                var entityType = typeof(T);
                var sqlbuilder = new StringBuilder();
                EntityInfo einfo = Util.GetEntityInfo<T>();

                // exclude Id property if Identity
                var props = einfo.Properties;
                if (einfo.IsIdentity)
                    props = props.Where(a => a.Name != einfo.IDProperty);

                string columns = string.Join(",", props.Select(a => $"{EscapeStr(a.Name)}"));
                string parameters = string.Join(",", props.Select(a => $"@{a.Name}"));
                sqlbuilder.Append($"INSERT INTO {DatabaseName}.{entityType.Name} ({columns}) VALUES (");
                sqlbuilder.Append(parameters);

                string sql = sqlbuilder.Append(") ").ToString();

                using (IDbConnection conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
                {
                    conn.Execute(sql, entity);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override void Create<T>(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                Create(item);
            }
        }

        public override void CreateTable<T>()
        {
            try
            {
                string entityName = typeof(T).Name;
                EntityInfo einfo = Util.GetEntityInfo<T>();
                StringBuilder sqlBuilder = new StringBuilder();

                sqlBuilder.Append($@"CREATE TABLE IF NOT EXISTS {DatabaseName}.{entityName} (");

                StringBuilder foreignKeyBuilder = new StringBuilder();

                foreach (var pinfo in einfo.Properties)
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
                            foreignKeyBuilder.Append($"CONSTRAINT FK_{entityName}_{pinfo.Name} FOREIGN KEY ({EscapeStr(pinfo.Name)}) REFERENCES {DatabaseName}.{attr.Name} ({EscapeStr("ID")}),");
                            break;
                        }
                    }

                    string sqlType = GetColumnDataType(propType);
                    string column = EscapeStr(pinfo.Name);
                    sqlBuilder.Append($@"{column} {(einfo.IDProperty == pinfo.Name && einfo.IsIdentity ? $"SERIAL" : sqlType)},");
                }

                string sql = "";
                sqlBuilder.Append($"CONSTRAINT PK_{entityName} PRIMARY KEY ({EscapeStr("ID")}),");
                if (!string.IsNullOrEmpty(foreignKeyBuilder.ToString()))
                {
                    sqlBuilder.Append(foreignKeyBuilder.ToString().TrimEnd(',')).Append(")");
                    sql = sqlBuilder.ToString();
                }
                else
                {
                    sql = sqlBuilder.ToString().TrimEnd(',') + ")";
                }

                using (IDbConnection conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
                {
                    conn.Execute(sql);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override IEnumerable<T> FindAll<T>()
        {
            var entityType = typeof(T).Name;
            EntityInfo einfo = Util.GetEntityInfo<T>();

            string sql = $"SELECT * FROM {DatabaseName}.{entityType}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                using (var comm = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                T instance = Activator.CreateInstance<T>();
                                foreach (var item in einfo.Properties)
                                {
                                    if(reader[item.Name] != System.DBNull.Value)
                                        item.SetValue(instance, reader[item.Name]);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
            }
        }

        public override IEnumerable<TResult> FindAll<TBase, TResult>()
        {
            var entityType = typeof(TBase);
            EntityInfo einfo = Util.GetEntityInfo<TBase>();

            string sql = $"SELECT * FROM {DatabaseName}.{entityType.Name}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                using (var comm = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                TResult instance = Activator.CreateInstance<TResult>();
                                foreach (var item in einfo.Properties)
                                {
                                    if(reader[item.Name] != System.DBNull.Value)
                                        item.SetValue(instance, reader[item.Name]);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
            }
        }

        public override T FindByID<T>(long id)
        {
            var entityType = typeof(T);
            T retVal = default(T);

            string sql = $"SELECT * FROM {DatabaseName}.{entityType.Name} WHERE {EscapeStr("ID")} = @{nameof(id)}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.QuerySingleOrDefault<T>(sql, new { id });
            }

            return retVal;
        }

        public override TResult FindByID<TBase, TResult>(long id)
        {
            var entityType = typeof(TBase);
            TResult retVal = default(TResult);

            string sql = $"SELECT * FROM {DatabaseName}.{entityType.Name} WHERE {EscapeStr("ID")} = @{nameof(id)}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                sql = $"SELECT * FROM {DatabaseName}.{entityType.Name} WHERE {EscapeStr(nameProp.Name)} = @{nameof(name)}";

                using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                sql = $"SELECT * FROM {DatabaseName}.{entityType.Name} WHERE {EscapeStr(nameProp.Name)} = @{nameof(name)}";

                using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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

            sqlbuilder.Append($"UPDATE {DatabaseName}.{entityType.Name} SET ");
            sqlbuilder.Append(string.Join(", ", einfo.Properties.Where(a => a.Name != einfo.IDProperty).Select(a => $"{EscapeStr(a.Name)} = @{a.Name}")));
            sqlbuilder.Append($" WHERE {EscapeStr(einfo.IDProperty)} = @{einfo.IDProperty}");

            using (IDbConnection conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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

            string sql = $"SELECT COUNT(*) FROM {DatabaseName}.{entityType.Name}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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

            string sql = $"SELECT MAX({EscapeStr(propInfo.Name)}) FROM {DatabaseName}.{entityType.Name}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.QuerySingleOrDefault<TResult>(sql);
            }

            return retVal;
        }

        public override int CountBestSolutions()
        {
            int retVal = 0;
            string sql = $@"
                SELECT
                    COUNT(*) Cnt
                FROM
                (
                    SELECT
                        main.{EscapeStr("Rneuron_ID")},
                        main.{EscapeStr("Solution_ID")}
                    FROM {DatabaseName}.{nameof(_Case)} main
                    INNER JOIN {DatabaseName}.{nameof(_Session)} sess ON main.{EscapeStr("Session_ID")} = sess.{EscapeStr("ID")}

                    WHERE sess.{EscapeStr("Hidden")} = false
                    GROUP BY main.{EscapeStr("Rneuron_ID")}, main.{EscapeStr("Solution_ID")}
                ) a";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql);
            }

            return retVal;
        }

        public override IEnumerable<BestSolution> LoadBestSolutions()
        {
            EntityInfo einfo = Util.GetEntityInfo<BestSolution>();

            string sql = $@"
                SELECT
                    main.{EscapeStr("Rneuron_ID")} as RneuronId,
                    main.{EscapeStr("Solution_ID")} as SolutionId,
                    MAX(main.{EscapeStr("CycleScore")}) as CycleScore,
                    MAX(sess.{EscapeStr("SessionScore")}) as SessionScore,
                    MAX(main.{EscapeStr("Order")}) as CycleOrder
                FROM {DatabaseName}.{nameof(_Case)} main
                INNER JOIN {DatabaseName}.{nameof(_Session)} sess ON main.{EscapeStr("Session_ID")} = sess.{EscapeStr("ID")}
                WHERE sess.{EscapeStr("Hidden")} = false
                GROUP BY main.{EscapeStr("Rneuron_ID")}, main.{EscapeStr("Solution_ID")}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                using (var comm = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var instance = new BestSolution();
                                foreach (var item in einfo.Properties)
                                {
                                    if(reader[item.Name] != System.DBNull.Value)
                                        item.SetValue(instance, reader[item.Name]);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
            }
        }

        public override IEnumerable<Session> LoadVisibleSessions()
        {
            EntityInfo einfo = Util.GetEntityInfo<_Session>();

            string sql = $"SELECT * FROM {DatabaseName}.{nameof(_Session)} WHERE {EscapeStr("Hidden")} = false";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                using (var comm = new NpgsqlCommand(sql, (NpgsqlConnection)conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var instance = new Session();
                                foreach (var item in einfo.Properties)
                                {
                                    if(reader[item.Name] != System.DBNull.Value)
                                        item.SetValue(instance, reader[item.Name]);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
            }
        }

        protected override string DetermineSQLConnectionString()
        {
            string connStrPsqlNoPassword = $@"Server=localhost;Port=5432;User ID=postgres;Database={DBNAME_PLACEHOLDER}";
            string connStrPsqlWithDefaultPassword = $@"Server=localhost;Port=5432;User ID=postgres;Password=postgres;Database={DBNAME_PLACEHOLDER}";

            string retVal = connStrPsqlNoPassword;

            if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                // try NON SQLEXPRESS
                retVal = connStrPsqlWithDefaultPassword;
                if (!TryConnect(retVal.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
                {
                    throw new RlmDefaultConnectionStringException("Unable to connect to the SQL Server using the default connection strings. Please provide a SQL Connection String on the application config file to override the default.");
                }
            }

            return retVal;
        }

        protected override IDbConnection GetOpenDBConnection(string connStr)
        {
            var retVal = new NpgsqlConnection(connStr);
            retVal.Open();
            return retVal;
        }

        protected override bool DBExists(string dbName)
        {
            bool retVal = false;
            string sql = $@"SELECT EXISTS(SELECT datname FROM pg_database WHERE LOWER(datname) = LOWER('{dbName}'));";
            string connStr = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase());

            using (var conn = GetOpenDBConnection(connStr))
            {
                retVal = conn.QueryFirstOrDefault<bool>(sql);
            }

            return retVal;
        }

        protected override void CreateDBIfNotExists(string dbName)
        {
            string sql = $"CREATE SCHEMA IF NOT EXISTS {dbName}";
            string connStr = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase());

            using (var conn = GetOpenDBConnection(connStr))
            {
                conn.Execute(sql);
            }

            //if (DBExists(dbName))
            //    return;

            //string sql = $"CREATE DATABASE {dbName};";
            //string connStr = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase());

            //using (var conn = GetOpenDBConnection(connStr))
            //{
            //    conn.Execute(sql);
            //}
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
                return nameof(NpgsqlDbType.Integer);
            }

            switch (type.Name)
            {
                case "System.Int32":
                case "Int32":
                    dataType = nameof(NpgsqlDbType.Integer);
                    break;
                case "System.Int64":
                case "Int64":
                    dataType = nameof(NpgsqlDbType.Bigint);
                    break;
                case "System.Double":
                case "Double":
                    dataType = "Double Precision";
                    break;
                case "System.String":
                case "String":
                    dataType = nameof(NpgsqlDbType.Text);
                    break;
                case "System.DateTime":
                case "DateTime":
                    dataType = nameof(NpgsqlDbType.Timestamp);
                    break;
                case "System.Boolean":
                case "Boolean":
                    dataType = nameof(NpgsqlDbType.Boolean);
                    break;
                case "System.Int16":
                case "Int16":
                    dataType = nameof(NpgsqlDbType.Smallint);
                    break;
            }

            return dataType;
        }

        protected override string GetDefaultDatabase()
        {
            return "postgres";
        }

        private string EscapeStr(string column)
        {
            return $"\"{column}\"";
        }

        #region TODO: Visualizer Queries Not Working
        public override double GetVariance(long networkId, int top)
        {
            double retVal = 0;

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                string sql = $@" 
                                SELECT 
                                    {EscapeStr("SessionScore")} 
                                FROM {DatabaseName}.{nameof(_Session)} 
                                WHERE {EscapeStr("Rnetwork_ID")} = @p0 
                                ORDER BY {EscapeStr("DateTimeStop")} DESC LIMIT {top}";

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
            throw new NotImplementedException();
        }

        public override IEnumerable<Session> GetSessions(long networkId, int? skip = null, int? take = null, bool descending = false)
        {
            IEnumerable<Session> retVal = null;

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                string sql = $"SELECT * FROM {DatabaseName}.{nameof(_Session)} WHERE {EscapeStr("Rnetwork_ID")} = @p0";
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
            throw new NotImplementedException();
        }

        public override int GetSessionCount(long networkId)
        {
            int retVal = 0;

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                string sql = $"SELECT COUNT(*) FROM {DatabaseName}.{nameof(_Session)} WHERE {EscapeStr("Rnetwork_ID")} = @p0";
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = networkId });
            }

            return retVal;
        }

        public override IEnumerable<Case> GetCases(long sessionId, int? skip = null, int? take = null)
        {
            IEnumerable<Case> retVal = null;

            string sql = $@"SELECT * FROM {DatabaseName}.{nameof(_Case)} WHERE Session_ID = @p0";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.Query<Case>(sql, new { p0 = sessionId });
                //TODO: include Rneuron, Input_Values_Rneuron, Input
            }

            if (skip.HasValue && take.HasValue)
            {
                retVal = retVal.Skip(skip.Value)
                    .Take(take.Value);
            }

            return retVal.ToList();
        }

        public override int GetCaseCount(long sessionId)
        {
            int retVal = 0;
            string sql = $"SELECT COUNT(*) FROM {DatabaseName}.{nameof(_Case)} WHERE {EscapeStr("Session_ID")} = @p0";
            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = sessionId });
            }

            return retVal;
        }

        public override RlmStats GetStatistics(long networkId)
        {
            RlmStats retVal = null;
            string dateTimeStop = EscapeStr("DateTimeStop");
            string dateTimeStart = EscapeStr("DateTimeStart");

            string sql = $@"
                     
                    DROP TABLE IF EXISTS tmp_table_stats;
                    DO $$
                    DECLARE 
                        lastSessionId bigint;
                        declare lastSessionScore integer;
                        declare lastSessionTime integer;
                    BEGIN
                    CREATE TEMP TABLE tmp_table_stats ON COMMIT DROP AS
                    SELECT 
                        COALESCE(AVG({DateDiffStr("second")}),0) as AvgTimePerSessionInSeconds,
                        COALESCE(SUM({DateDiffStr("second")}), 0) as TotalSessionTimeInSeconds,
                        COUNT({EscapeStr("ID")}) as TotalSessions,
                        COALESCE(max({EscapeStr("SessionScore")}), 0) as MaxSessionScore,
                        COALESCE(avg({EscapeStr("SessionScore")}), 0) as AvgSessionScore,
                        COALESCE(lastSessionId, 0) as LastSessionId,
                        COALESCE(lastSessionScore, 0) as LastSessionScore,
                        COALESCE(lastSessionTime, 0) as LastSessionTimeInSeconds
                    FROM {DatabaseName}.{nameof(_Session)}
                    WHERE {EscapeStr("Rnetwork_ID")} = @p0 and {EscapeStr("Hidden")} = false and {EscapeStr("SessionScore")} <> @p1;
                    END $$;
                    SELECT * FROM tmp_table_stats; ";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.Query<RlmStats>(sql, new { p0 = networkId, p1 = int.MinValue }).FirstOrDefault();

                if (retVal != null)
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
                 
                SELECT 
                    COUNT(*) AS NumSessionSinceBestScore
                FROM
                (
	                SELECT
		                {EscapeStr("ID")},
                        {EscapeStr("SessionScore")},
		                ROW_NUMBER() OVER(ORDER BY {EscapeStr("ID")} DESC) as Num
                    FROM {DatabaseName}.{nameof(_Session)}
                    WHERE {EscapeStr("ID")} > (
                        SELECT 
                            {EscapeStr("ID")} 
                        FROM {DatabaseName}.{nameof(_Session)} 
                        ORDER BY {EscapeStr("SessionScore")} DESC, {EscapeStr("ID")} DESC LIMIT 1) AND {EscapeStr("Rnetwork_ID")} = @p0
                ) a";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
            {
                retVal = conn.QuerySingleOrDefault<int>(sql, new { p0 = rnetworkId });
            }

            return retVal;
        }

        public override IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = null;
            string sql = $@" SELECT 
                                {EscapeStr("ID")}, 
                                {EscapeStr("SessionScore")}, 
                                {EscapeStr("DateTimeStart")}, 
                                {EscapeStr("DateTimeStop")}, 
                                ROW_NUMBER() OVER (ORDER BY {EscapeStr("DateTimeStart")} ASC) AS SessionNumber 
                            FROM {DatabaseName}.{nameof(_Session)};";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
            string sql = $@"
                            SELECT 
                                a.{EscapeStr("ID")},
                                a.{EscapeStr("SessionScore")},
                                a.{EscapeStr("Order")} AS SessionNumber,
                                a.{EscapeStr("DateTimeStart")},
                                a.{EscapeStr("DateTimeStop")}
                            FROM(
                                SELECT 
                                    *, 
                                    ROW_NUMBER() OVER(ORDER BY {EscapeStr("DateTimeStart")} ASC) AS {EscapeStr("Order")} 
                                FROM {DatabaseName}.{nameof(_Session)}) a
                            INNER JOIN
                            (
                                SELECT
                                    {EscapeStr("SessionScore")},
                                    MIN({EscapeStr("Order")}) AS {EscapeStr("Order")}
                                FROM(
                                    SELECT 
                                        *, 
                                        ROW_NUMBER() OVER(ORDER BY {EscapeStr("DateTimeStart")} ASC) AS {EscapeStr("Order")}  
                                    FROM {DatabaseName}.{nameof(_Session)}) a
                                GROUP BY {EscapeStr("SessionScore")}
                            ) b ON a.{EscapeStr("SessionScore")} = b.{EscapeStr("SessionScore")} AND a.{EscapeStr("Order")} = b.{EscapeStr("Order")}
                            ORDER BY a.{EscapeStr("SessionScore")} DESC; ";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
            string query = $@" 
                            SELECT 
                                {EscapeStr("ID")}, 
                                ROW_NUMBER() OVER (ORDER BY {EscapeStr("Order")} ASC) AS RowNumber, 
                                {EscapeStr("CycleStartTime")} AS DateTimeStart, {EscapeStr("CycleEndTime")} AS DateTimeStop, 
                                {EscapeStr("CycleScore")}, {EscapeStr("Session_ID")} AS SessionId, 
                                {EscapeStr("Rneuron_ID")} AS RneuronId, {EscapeStr("Solution_ID")} AS SolutionId, 
                                {EscapeStr("Order")} AS {EscapeStr("CycleOrder")} 
                            FROM {DatabaseName}.{nameof(_Case)} WHERE {EscapeStr("Session_ID")} = {sessionId}; ";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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

            string queryIn = $@"
                                SELECT 
                                    inp.{EscapeStr("ID")} AS Id, 
                                    inp.{EscapeStr("Name")}, 
                                    ivr.{EscapeStr("Value")} 
                                FROM {nameof(_Input_Values_Rneuron)} ivr 
                                INNER JOIN {nameof(_Input)} inp ON inp.{EscapeStr("ID")} = ivr.{EscapeStr("Input_ID")} 
                                WHERE {EscapeStr("Rneuron_ID")} = @p0;";

            string queryOut = $@"
                                SELECT 
                                    outp.{EscapeStr("ID")} AS Id, 
                                    outp.{EscapeStr("Name")}, 
                                    ovs.{EscapeStr("Value")} 
                                FROM {nameof(_Output_Values_Solution)} ovs 
                                INNER JOIN {nameof(_Output)} outp ON outp.{EscapeStr("ID")} = ovs.{EscapeStr("Output_ID")} 
                                WHERE {EscapeStr("Solution_ID")} = @p0;";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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

            query.AppendLine($@"SELECT DISTINCT r.{EscapeStr("ID")} FROM {DatabaseName}.{nameof(_Rneuron)} r");

            int cnt = 0;
            foreach (var input in inputs)
            {
                SqlParameter input_name = new SqlParameter($"p{cnt++}", input.Key);
                SqlParameter input_val = new SqlParameter($"p{cnt++}", input.Value);

                string alias = $"i{cnt}";
                joins.AppendLine($@"INNER JOIN (
                                        SELECT 
                                            ivr.{EscapeStr("Rneuron_ID")}, 
                                            i.{EscapeStr("Name")}, 
                                            ivr.{EscapeStr("Value")} 
                                        FROM {DatabaseName}.{nameof(_Input_Values_Rneuron)} ivr 
                                        INNER JOIN {DatabaseName}.{nameof(_Input)} i 
                                        ON ivr.{EscapeStr("Input_ID")} = i.{EscapeStr("ID")}) {alias} 
                                        ON r.{EscapeStr("ID")} = {alias}.{EscapeStr("Rneuron_ID")}");

                where.AppendLine($@"({alias}.{EscapeStr("Name")} = @{input_name.ParameterName} AND {alias}.{EscapeStr("Value")} = @{input_val.ParameterName}) AND");

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

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
	                    ovs.{EscapeStr("Solution_ID")}
                    FROM {DatabaseName}.{nameof(_Output_Values_Solution)} ovs
                    INNER JOIN {DatabaseName}.{nameof(_Output)} o ON ovs.{EscapeStr("Output_ID")} = o.{EscapeStr("ID")} 
                    WHERE ";

            int cnt = 0;
            foreach (var output in outputs)
            {
                SqlParameter output_name = new SqlParameter($"p{cnt++}", output.Key);
                SqlParameter output_val = new SqlParameter($"p{cnt++}", output.Value);

                query += $"(o.{EscapeStr("Name")} = @{output_name.ParameterName} AND ovs.{EscapeStr("Value")} = @{output_val.ParameterName}) AND\n";

                parameters.Add(output_name);
                parameters.Add(output_val);

                paramVals[output_name.ParameterName] = output.Key;
                paramVals[output_val.ParameterName] = output.Value;
            }
            query = query.Substring(0, query.LastIndexOf("AND"));

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                WITH cte (ID, {EscapeStr("Time")}, Score)
                AS
                (
                    SELECT
                        c.{EscapeStr("ID")},
                        s.{EscapeStr("Time")},
                        MAX(s.{EscapeStr("SessionScore")}) OVER(ORDER BY c.{EscapeStr("ID")} ASC) Score
                    FROM {DatabaseName}.{nameof(_Case)} c
                    INNER JOIN(
                        SELECT
                            {EscapeStr("ID")},
                            SUM({DateDiffStr("millisecond")}) OVER(ORDER BY {EscapeStr("DateTimeStart")}) {EscapeStr("Time")},
                            {EscapeStr("SessionScore")}
                        FROM {DatabaseName}.{nameof(_Session)}
                    ) s ON c.{EscapeStr("Session_ID")} = s.{EscapeStr("ID")}
                    WHERE c.{EscapeStr("Rneuron_ID")} = @{ rneuronParam.ParameterName}
                    AND c.{EscapeStr("Solution_ID")} = @{ solutionParam.ParameterName}
                )
                SELECT
                    MIN(ID) CaseID,
	                MIN({EscapeStr("Time")}) {EscapeStr("Time")},
	                Score
                FROM cte
                GROUP BY Score
                ORDER BY Score DESC LIMIT(SELECT ROUND((SELECT COUNT(*) FROM(SELECT Score FROM cte GROUP BY Score)sss) * (@{ scaleParam.ParameterName}/100.00))::integer)";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                WITH cte (ID, {EscapeStr("Time")}, Score)
                AS
                (
                    SELECT

                        {EscapeStr("ID")},
                        {EscapeStr("Time")},
                        MAX({EscapeStr("SessionScore")}) OVER(ORDER BY {EscapeStr("Time")}) Score
                    FROM(
                        SELECT
                            {EscapeStr("ID")},
                            SUM({DateDiffStr("millisecond")}) OVER(ORDER BY {EscapeStr("DateTimeStart")}) {EscapeStr("Time")},
                            {EscapeStr("SessionScore")}
                        FROM {DatabaseName}.{nameof(_Session)} WHERE {EscapeStr("Hidden")} = false
                    ) s
                )
                SELECT
                    MIN(ID) SessionId,
	                MIN({EscapeStr("Time")}) {EscapeStr("Time")},
	                Score
                FROM cte
                GROUP BY Score
                ORDER BY Score ASC 
                LIMIT(SELECT ROUND((SELECT COUNT(*) FROM(SELECT Score FROM cte GROUP BY Score)sss) * (@{ scaleParam.ParameterName}/100.00))::integer)";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                DROP TABLE IF EXISTS temp_table_{tempTablePostfix};
                CREATE TEMP TABLE temp_table_{tempTablePostfix} (ID BIGINT, Score FLOAT);

                INSERT INTO temp_table_{tempTablePostfix}
                SELECT
	                c.{EscapeStr("ID")},

                    s.{EscapeStr("SessionScore")} as Score
                FROM
                (
                    SELECT
                        MIN(sub.Id) as ID,
                        sub.Score
                    FROM(
                        SELECT
                            c.{EscapeStr("ID")} as Id,
                            MAX(s.{EscapeStr("SessionScore")}) OVER(ORDER BY c.{EscapeStr("ID")} ASC) as Score
                        FROM {DatabaseName}.{ nameof(_Case)} c
                        INNER JOIN {DatabaseName}.{nameof(_Session)} s ON c.{EscapeStr("Session_ID")} = s.{EscapeStr("ID")}
                        WHERE c.{EscapeStr("Rneuron_ID")} = (
                            SELECT 
                                {EscapeStr("Rneuron_ID")} 
                            FROM {DatabaseName}.{nameof(_Case)}
                            WHERE {EscapeStr("ID")} = @{ caseParam.ParameterName}) 
                            AND c.{EscapeStr("Solution_ID")} = (
                                SELECT 
                                    {EscapeStr("Solution_ID")} 
                                FROM {DatabaseName}.{nameof(_Case)}
                                WHERE {EscapeStr("ID")} = @{ caseParam.ParameterName})
	                ) sub
                    GROUP BY sub.Score
                ) sub
                INNER JOIN {DatabaseName}.{nameof(_Case)} c ON sub.Id = c.{EscapeStr("ID")}
                INNER JOIN {DatabaseName}.{nameof(_Session)} s ON c.{EscapeStr("Session_ID")} = s.{EscapeStr("ID")};

                SELECT
                    s2.ID AS PreviousCaseId
                FROM(
                    SELECT *, 
                        ROW_NUMBER() OVER(ORDER BY Score DESC) ord 
                    FROM temp_table_{ tempTablePostfix}) s1
                    LEFT JOIN(
                        SELECT *, 
                            ROW_NUMBER() OVER(ORDER BY Score DESC) ord 
                        FROM temp_table_{ tempTablePostfix}) s2 ON s1.ord = s2.ord { (next ? "+" : "-")} 1
                        WHERE s1.ID = @{ caseParam.ParameterName};";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                            DROP TABLE IF EXISTS {DatabaseName}.temp_table_{tempTablePostfix};
                            CREATE TABLE {DatabaseName}.temp_table_{tempTablePostfix}(ID BIGINT, Score FLOAT);
                            INSERT INTO temp_table_{tempTablePostfix}
                            WITH cte (ID, {EscapeStr("Time")}, Score)
                            AS
                            (
	                            SELECT 
		                            {EscapeStr("ID")} as ID,
		                            {EscapeStr("Time")},
		                            MAX({EscapeStr("SessionScore")}) OVER (ORDER BY {EscapeStr("Time")}) Score
	                            FROM (
		                            SELECT
			                            {EscapeStr("ID")},
			                            SUM({DateDiffStr("millisecond")}) OVER (ORDER BY {EscapeStr("DateTimeStart")}) {EscapeStr("Time")},
			                            {EscapeStr("SessionScore")}
		                            FROM {DatabaseName}.{nameof(_Session)} WHERE {EscapeStr("Hidden")} = false
	                            ) s
                            )
                            SELECT 
	                            MIN(ID) ID,
	                            Score
                            FROM cte
                            GROUP BY Score;

                            SELECT
                                s2.ID AS PreviousSessionId
                            FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord FROM {DatabaseName}.temp_table_{tempTablePostfix}) s1
                            LEFT JOIN (SELECT *, ROW_NUMBER() OVER (ORDER BY Score DESC) ord {DatabaseName}.FROM temp_table_{tempTablePostfix}) s2 on s1.ord = s2.ord {(next ? "+" : "-")} 1
                            WHERE s1.ID = @{sessParam.ParameterName};";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                    {EscapeStr("ID")} SessionId,
                    {EscapeStr("SessionScore")} Score,
                    {DateDiffStr("millisecond")}  {EscapeStr("Time")},
                    ROW_NUMBER() OVER(ORDER BY {EscapeStr("DateTimeStart")}) As SessionNum
                FROM {DatabaseName}.{nameof(_Session)})
                SELECT * FROM TempSessions 
                WHERE SessionId in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                WITH cte ({EscapeStr("ID")}, {EscapeStr("SessionScore")}, {EscapeStr("Time")}, {EscapeStr("Order")})
                AS
                (	
                     SELECT
                          {EscapeStr("ID")},
                          {EscapeStr("SessionScore")},
                          SUM({DateDiffStr("millisecond")}) OVER (ORDER BY {EscapeStr("DateTimeStart")}) {EscapeStr("Time")},
                          ROW_NUMBER() OVER (ORDER BY {EscapeStr("DateTimeStart")}) {EscapeStr("Order")}
                     FROM {DatabaseName}.{nameof(_Session)}
                )
                SELECT
                     c.{EscapeStr("ID")} CaseId,
                     c.{EscapeStr("Order")} CycleNum,
                     c.{EscapeStr("CycleScore")} CycleScore,
                     c.{EscapeStr("Session_ID")} SessionId,
                     s.{EscapeStr("SessionScore")} SessionScore,
                     s.{EscapeStr("Time")} SessionTime,
                     s.{EscapeStr("Order")} SessionNum
                FROM {DatabaseName}.{nameof(_Case)} c
                INNER JOIN cte s ON c.{EscapeStr("Session_ID")} = s.{EscapeStr("ID")}
                WHERE c.{EscapeStr("ID")} in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                     i.{EscapeStr("ID")},
                     i.{EscapeStr("Name")},
                     i.{EscapeStr("Value")},
                     CAST(1 AS BIT) {EscapeStr("IsInput")},	
                     c.{EscapeStr("ID")} CaseId,
                     c.{EscapeStr("CycleScore")},
                     c.{EscapeStr("Session_ID")} SessionId,
                        c.{EscapeStr("Order")} CycleOrder
                FROM {DatabaseName}.{nameof(_Case)} c
                LEFT JOIN (
                     SELECT
                          i.{EscapeStr("ID")},
                          i.{EscapeStr("Name")},
                          ivr.{EscapeStr("Value")},
                          ivr.{EscapeStr("Rneuron_ID")}
                     FROM {DatabaseName}.{nameof(_Input_Values_Rneuron)} ivr
                     LEFT JOIN {DatabaseName}.{nameof(_Input)} i ON ivr.{EscapeStr("Input_ID")} = i.{EscapeStr("ID")}
                    ) i ON c.{EscapeStr("Rneuron_ID")} = i.{EscapeStr("Rneuron_ID")}
                    WHERE c.{EscapeStr("ID")} = @{caseParam.ParameterName}
                UNION
                SELECT
                     o.{EscapeStr("ID")},
                     o.{EscapeStr("Name")},
                     o.{EscapeStr("Value")},
                     CAST(0 AS BIT) {EscapeStr("IsInput")},
                     c.{EscapeStr("ID")} CaseId,
                     c.{EscapeStr("CycleScore")},
                     c.{EscapeStr("Session_ID")} SessionId,
                    c.{EscapeStr("Order")} CycleOrder
                FROM {DatabaseName}.{nameof(_Case)} c
                LEFT JOIN(
                SELECT
                      o.{EscapeStr("ID")},
                      o.{EscapeStr("Name")},
                      ovs.{EscapeStr("Value")},
                      ovs.{EscapeStr("Solution_ID")}
                 FROM {DatabaseName}.{nameof(_Output_Values_Solution)} ovs
                 LEFT JOIN {DatabaseName}.{nameof(_Output)} o on ovs.{EscapeStr("Output_ID")} = o.{EscapeStr("ID")}
                ) o ON c.{EscapeStr("Solution_ID")} = o.{EscapeStr("Solution_ID")}
                WHERE c.{EscapeStr("ID")} = @{caseParam.ParameterName}";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
                    i.{EscapeStr("ID")},
                    i.{EscapeStr("Name")},
                    i.{EscapeStr("Value")},
                    CAST(1 AS BIT) IsInput,	
                    c.{EscapeStr("ID")} CaseId,
                    c.{EscapeStr("CycleScore")},
                    c.{EscapeStr("Session_ID")} SessionId,
                    c.{EscapeStr("Order")} CycleOrder
                FROM {DatabaseName}.{nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                      i.{EscapeStr("ID")},
                      i.{EscapeStr("Name")},
                      ivr.{EscapeStr("Value")},
                      ivr.{EscapeStr("Rneuron_ID")}
                 FROM {DatabaseName}.{nameof(_Input_Values_Rneuron)} ivr
                 LEFT JOIN {DatabaseName}.{nameof(_Input)} i on ivr.{EscapeStr("Input_ID")} = i.{EscapeStr("ID")}
                ) i ON c.{EscapeStr("Rneuron_ID")} = i.{EscapeStr("Rneuron_ID")}
                WHERE c.{EscapeStr("Session_ID")} in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})
                UNION
                SELECT
                    o.{EscapeStr("ID")},
                    o.{EscapeStr("Name")},
                    o.{EscapeStr("Value")},
                    CAST(0 AS BIT) IsInput,
                    c.{EscapeStr("ID")} CaseId,
                    c.{EscapeStr("CycleScore")},
                    c.{EscapeStr("Session_ID")} SessionId,
                    c.{EscapeStr("Order")} CycleOrder
                FROM {DatabaseName}.{nameof(_Case)} c
                LEFT JOIN (
                 SELECT
                      o.{EscapeStr("ID")},
                      o.{EscapeStr("Name")},
                      ovs.{EscapeStr("Value")},
                      ovs.{EscapeStr("Solution_ID")}
                 FROM {DatabaseName}.{nameof(_Output_Values_Solution)} ovs
                 LEFT JOIN {DatabaseName}.{nameof(_Output)} o on ovs.{EscapeStr("Output_ID")} = o.{EscapeStr("ID")}
                ) o ON c.{EscapeStr("Solution_ID")} = o.{EscapeStr("Solution_ID")}
                WHERE c.{EscapeStr("Session_ID")} in ({string.Join(",", paramVals.Select(a => $"@{a.Key}").ToArray())})";

            using (var conn = GetOpenDBConnection(ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, GetDefaultDatabase())))
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
        #endregion

        private string DateDiffStr(string type)
        {
            string dateTimeStop = EscapeStr("DateTimeStop");
            string dateTimeStart = EscapeStr("DateTimeStart");
            string query = "";

            if (type == "millisecond")
            {
                query = $@"1000 * ( ((DATE_PART('day', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp) * 24 +
                DATE_PART('hour', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp)) * 60 +
                DATE_PART('minute', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp)) * 60 +
                DATE_PART('second', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp))";

            }
            else if(type == "second")
            {
                query = $@"((DATE_PART('day', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp) * 24 +
                DATE_PART('hour', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp)) * 60 +
                DATE_PART('minute', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp)) * 60 +
                DATE_PART('second', {dateTimeStop}::timestamp - {dateTimeStart}::timestamp)";
            }


            return query;
        }
    }
}
