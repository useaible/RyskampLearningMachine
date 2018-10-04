using RLM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    public class RlmDbBatchProcessor
    {
        private const string VALUES_PLACEHOLDER = "{{values}}";
        private readonly string SQL_RNEURON_INSERT = $"INSERT INTO [Rneurons] (ID, HashedKey, RandomizationFactor, Rnetwork_ID) VALUES {VALUES_PLACEHOLDER}";
        private readonly string SQL_IVR_INSERT = $"INSERT INTO [Input_Values_Rneuron] (Value, Input_ID, Rneuron_ID) VALUES {VALUES_PLACEHOLDER}";
        private readonly string SQL_SOLUTION_INSERT = $"INSERT INTO [Solutions] (ID) VALUES {VALUES_PLACEHOLDER}";
        private readonly string SQL_OVS_INSERT = $"INSERT INTO [Output_Values_Solution] (ID, Value, Output_ID, Solution_ID) VALUES {VALUES_PLACEHOLDER}";
        private readonly string SQL_CASE_INSERT = $"INSERT INTO [Cases] (CycleStartTime, CycleEndTime, CycleScore, CurrentRFactor, CurrentMFactor, ResultCompletelyRandom, SequentialMFactorSuccessesCount, Rneuron_ID, Solution_ID, Session_ID, [Order]) VALUES {VALUES_PLACEHOLDER}";

        private string connStr = RlmDbEntities.ConnStr;

        public RlmDbBatchProcessor(string databaseName)
        {
            connStr = connStr.Replace(RlmDbEntities.DBNAME_PLACEHOLDER, databaseName);
        }

        public async Task InsertCases(IEnumerable<Case> cases)
        {
            if (cases == null || cases.Count() == 0)
                throw new ArgumentException("Cases list cannot be null or empty");

            var sqlParams = new List<SqlParameter>();

            var rneuronValues = new StringBuilder();
            var ivrValues = new StringBuilder();
            var solutionValues = new StringBuilder();
            var ovsValues = new StringBuilder();
            var caseValues = new StringBuilder();

            // generic values for some required columns
            double g_double_val = 0D;
            short g_sint_val = 0;
            long g_bint_val = 0;
            var g_double = new SqlParameter($"@g_double", g_double_val);
            var g_sint = new SqlParameter($"@g_sint", g_sint_val);            
            var g_bint = new SqlParameter($"@g_bint", g_bint_val);
            sqlParams.Add(g_double);
            sqlParams.Add(g_sint);
            sqlParams.Add(g_bint);

            int cnt = 0;
            foreach(var c in cases)
            {
                if (c.Rneuron != null)
                {
                    var r_id = new SqlParameter($"@r_id_{cnt}", c.Rneuron.ID);
                    var r_rnet = new SqlParameter($"@r_rnet_{cnt}", c.Rneuron.Rnetwork_ID);
                    sqlParams.AddRange(new[] { r_id, r_rnet });

                    rneuronValues.Append($"{(rneuronValues.Length == 0 ? "" : ",")} ({r_id.ParameterName}, {g_bint.ParameterName}, {g_double.ParameterName}, {r_rnet.ParameterName})");

                    int ivrCnt = 0;
                    foreach(var ivr in c.Rneuron.Input_Values_Rneurons)
                    {
                        //var ivr_id = new SqlParameter($"@ivr_id_{cnt}_{ivrCnt}", ivr.ID);
                        var ivr_val = new SqlParameter($"@ivr_val_{cnt}_{ivrCnt}", ivr.Value);
                        var ivr_inp = new SqlParameter($"@ivr_inp_{cnt}_{ivrCnt}", ivr.Input_ID);
                        var ivr_rn = new SqlParameter($"@ivr_rn_{cnt}_{ivrCnt}", ivr.Rneuron_ID);
                        sqlParams.AddRange(new[] { /*ivr_id,*/ ivr_val, ivr_inp, ivr_rn });

                        ivrValues.Append($"{(ivrValues.Length == 0 ? "" : ",")} ({ivr_val.ParameterName}, {ivr_inp.ParameterName}, {ivr_rn.ParameterName})");
                        ivrCnt++;
                    }
                }

                if (c.Solution != null)
                {
                    var s_id = new SqlParameter($"@s_id_{cnt}", c.Solution.ID);
                    sqlParams.Add(s_id);
                    solutionValues.Append($"{(solutionValues.Length == 0 ? "" : ",")} ({s_id.ParameterName})");

                    int ovsCnt = 0;
                    foreach(var ovs in c.Solution.Output_Values_Solutions)
                    {
                        var ovs_id = new SqlParameter($"@ovs_id_{cnt}_{ovsCnt}", ovs.ID);
                        var ovs_val = new SqlParameter($"@ovs_val_{cnt}_{ovsCnt}", ovs.Value);
                        var ovs_out = new SqlParameter($"@ovs_out_{cnt}_{ovsCnt}", ovs.Output_ID);
                        var ovs_sol = new SqlParameter($"@ovs_sol_{cnt}_{ovsCnt}", ovs.Solution_ID);
                        sqlParams.AddRange(new[] { ovs_id, ovs_val, ovs_out, ovs_sol });

                        ovsValues.Append($"{(ovsValues.Length == 0 ? "" : ",")} ({ovs_id.ParameterName}, {ovs_val.ParameterName}, {ovs_out.ParameterName}, {ovs_sol.ParameterName})");
                        ovsCnt++;
                    }
                }

                var c_start = new SqlParameter($"@c_start_{cnt}", c.CycleStartTime);
                var c_end = new SqlParameter($"@c_end_{cnt}", c.CycleEndTime);
                var c_score = new SqlParameter($"@c_score_{cnt}", c.CycleScore);
                var c_random = new SqlParameter($"@c_random_{cnt}", c.ResultCompletelyRandom);
                var c_rn = new SqlParameter($"@c_rn_{cnt}", (c.Rneuron == null) ? c.Rneuron_ID : c.Rneuron.ID);
                var c_sol = new SqlParameter($"@c_sol_{cnt}", (c.Solution == null) ? c.Solution_ID : c.Solution.ID);
                var c_sess = new SqlParameter($"@c_sess_{cnt}", (c.Session == null) ? c.Session_ID : c.Session.ID);
                var c_order = new SqlParameter($"@c_order_{cnt}", c.Order);
                sqlParams.AddRange(new[] { c_start, c_end, c_score, c_random, c_rn, c_sol, c_sess, c_order });

                caseValues.Append($"{(caseValues.Length == 0 ? "" : ",")} ({c_start.ParameterName}, {c_end.ParameterName}, {c_score.ParameterName}, {g_double.ParameterName}, {g_sint.ParameterName}, {c_random.ParameterName}, {g_sint.ParameterName}, {c_rn.ParameterName}, {c_sol.ParameterName}, {c_sess.ParameterName}, {c_order.ParameterName})");
                cnt++;
            }
            
            var mainSql = new StringBuilder();
            if (rneuronValues.Length > 0)
            {
                mainSql.AppendLine(SQL_RNEURON_INSERT.Replace(VALUES_PLACEHOLDER, rneuronValues.ToString()));
            }
            if (ivrValues.Length > 0)
            {
                mainSql.AppendLine(SQL_IVR_INSERT.Replace(VALUES_PLACEHOLDER, ivrValues.ToString()));
            }
            if (solutionValues.Length > 0)
            {
                mainSql.AppendLine(SQL_SOLUTION_INSERT.Replace(VALUES_PLACEHOLDER, solutionValues.ToString()));
            }
            if (ovsValues.Length > 0)
            {
                mainSql.AppendLine(SQL_OVS_INSERT.Replace(VALUES_PLACEHOLDER, ovsValues.ToString()));
            }
            mainSql.AppendLine(SQL_CASE_INSERT.Replace(VALUES_PLACEHOLDER, caseValues.ToString()));

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand comm = new SqlCommand(mainSql.ToString(), conn))
                {
                    comm.Parameters.AddRange(sqlParams.ToArray());
                    //comm.Parameters.Add("@g_double", Syste);

                    //comm.Parameters.Add("@g_double", System.Data.SqlDbType.Float);
                    //comm.Parameters["@g_double"].Value = 0D;

                    try
                    {
                        conn.Open();
                        var rowsAffected = await comm.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"Rows affected: {rowsAffected}");
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: {e.Message}");
                    }
                }
            }
        }
    }
}
