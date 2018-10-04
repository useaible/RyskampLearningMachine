using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;

namespace RLM.Models
{
    public abstract class BaseRlmDbData : IRlmDbData
    {
        public const string DEFAULT_RLM_DBNAME = "RyskampLearningMachines";
        public const string DBNAME_PLACEHOLDER = "{{dbName}}";

        public string DatabaseName { get; set; }

        public BaseRlmDbData(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("The database name cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(ConnectionStringTemplate))
            {
                connectionStringTemplate = DetermineSQLConnectionString();
            }

            DatabaseName = databaseName;

            //Initialize();
        }
        
        public void Initialize()
        {
            try
            {
                CreateDBIfNotExists(DatabaseName);
                //ConnectionStringTemplate = ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, DatabaseName);

                CreateTable<_Rnetwork>();
                CreateTable<_RnetworkSetting>();
                CreateTable<_Input_Output_Type>();
                CreateTable<_Input>();
                CreateTable<_Output>();
                CreateTable<_Rneuron>();
                CreateTable<_Solution>();
                CreateTable<_Session>();
                CreateTable<_Case>();
                CreateTable<_Idea_Module>();
                CreateTable<_Idea_Implementation>();
                CreateTable<_Input_Values_Rneuron>();
                CreateTable<_Output_Values_Solution>();
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex.ToString());
                throw;
            }
        }

        private string connectionStringTemplate;
        public string ConnectionStringTemplate
        {
            get
            {

                var retVal = string.Empty;

                // tries to get the connection string set in the config file
                retVal = ConfigurationManager.AppSettings["RLMConnStr"];

                if (string.IsNullOrEmpty(retVal))
                {
                    // otherwise, we use the default
                    retVal = connectionStringTemplate;
                }
                else
                {
                    connectionStringTemplate = retVal;
                }

                return retVal;
            }
            set
            {
                connectionStringTemplate = value;
            }
        }

        public string ConnectionStringWithDatabaseName
        {
            get
            {
                return ConnectionStringTemplate.Replace(DBNAME_PLACEHOLDER, DatabaseName);
            }
        }

        protected bool TryConnect(string connString)
        {
            try
            {
                string connStringWithTimeout = connString + "Connect Timeout=5;";
                using (var conn = GetOpenDBConnection(connString))
                {
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        protected abstract string GetDefaultDatabase();
        protected abstract string DetermineSQLConnectionString();
        protected abstract IDbConnection GetOpenDBConnection(string connStr);
        protected abstract bool DBExists(string dbName);
        protected abstract void CreateDBIfNotExists(string dbName);

        public abstract void CreateTable<T>();
        public abstract void Create<T>(T entity);
        public abstract void Create<T>(IEnumerable<T> entity);
        public abstract void Update<T>(T entity);
        public abstract IEnumerable<T> FindAll<T>();
        public abstract IEnumerable<TResult> FindAll<TBase, TResult>();
        public abstract T FindByID<T>(long id);
        public abstract TResult FindByID<TBase, TResult>(long id);
        public abstract T FindByName<T>(string name);
        public abstract TResult FindByName<TBase, TResult>(string name);
        public abstract void DropDB(string dbName);
        public abstract int Count<T>();
        public abstract TResult Max<TSource, TResult>(Expression<Func<TSource, TResult>> propertyExpr);
        public abstract int CountBestSolutions();
        public abstract IEnumerable<BestSolution> LoadBestSolutions();
        public abstract IEnumerable<Session> LoadVisibleSessions();
        public abstract string GetColumnDataType(Type type);

        //RLM Data Legacy
        public abstract double GetVariance(Int64 networkId, int top);
        public abstract int GetTotalSimulationInSeconds(Int64 networkId);
        public abstract IEnumerable<Session> GetSessions(Int64 networkId, int? skip = null, int? take = null, bool descending = false);
        public abstract IEnumerable<RlmSessionSummary> GetSessionSummary(Int64 networkId, int groupBy, bool descending = false);
        public abstract int GetSessionCount(Int64 networkId);
        public abstract IEnumerable<Case> GetCases(long sessionId, int? skip = null, int? take = null);
        public abstract int GetCaseCount(long sessionId);
        public abstract RlmStats GetStatistics(Int64 networkId);
        public abstract int GetNumSessionSinceBestScore(long rnetworkId);

        //RLV
        public abstract IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null);
        public abstract IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int? pageFrom = null, int? pageTo = null);
        public abstract IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int? pageFrom = null, int? pageTo = null);
        public abstract RlmCaseIOHistory GetCaseIOHistory(long caseId, long rneuronId, long solutionId);
        public abstract long? GetRneuronIdFromInputs(KeyValuePair<string, string>[] inputs);
        public abstract long? GetSolutionIdFromOutputs(KeyValuePair<string, string>[] outputs);
        public abstract IEnumerable<RlmLearnedCase> GetLearnedCases(long rneuronId, long solutionId, double scale);
        public abstract IEnumerable<RlmLearnedSession> GetLearnedSessions(double scale);
        public abstract long? GetNextPreviousLearnedCaseId(long caseId, bool next = false);
        public abstract long? GetNextPreviousLearnedSessionId(long sessionId, bool next = false);
        public abstract IEnumerable<RlmLearnedSession> GetSessionDetails(params long[] sessionIds);
        public abstract IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(params long[] caseIds);
        public abstract IEnumerable<RlmIODetails>[] GetCaseIODetails(long caseId);
        public abstract IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(params long[] sessionIds);
    }
}
