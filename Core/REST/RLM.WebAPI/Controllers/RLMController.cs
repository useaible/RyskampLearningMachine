using RLM.Models;
using RLM.WebAPI.Filters;
using RLM.WebAPI.Manager;
using RLM.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace RLM.WebAPI.Controllers
{
    [EnableCors("*", "*", "*")]
    [RoutePrefix("api/rlm")]
    public class RLMController : ApiController
    {
        private RLMApiManager manager = new RLMApiManager();

        [HttpPost]
        [Route("create_or_load")]
        [ValidateRlmModel]
        public void CreateOrLoad(CreateLoadNetworkParams data)
        {
            try
            {
                manager.CreateOrLoadNetwork(data);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        [HttpPut]
        [Route("configure")]
        [ValidateRlmModel]
        public void ConfigureNetwork(RlmSettingsParams settings)
        {
            try
            {
                manager.SetRLMSettings(settings);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        [HttpPut]
        [Route("start_session")]
        [ValidateRlmModel]
        public void StartSession(RlmParams data)
        {
            try
            {
                manager.StartSession(data);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        [HttpPut]
        [Route("end_session")]
        [ValidateRlmModel]
        public void EndSession(SessionEndParams data)
        {
            try
            {
                manager.EndSession(data);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        [HttpPut]
        [Route("run_cycle")]
        [ValidateRlmModel]
        public CycleOutputParams RunCycle(RunCycleParams data)
        {
            CycleOutputParams result = null;

            try
            {
                result = manager.RunCycle(data);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpPut]
        [Route("score_cycle")]
        [ValidateRlmModel]
        public void ScoreCycle(ScoreCycleParams data)
        {
            try
            {
                manager.ScoreCycle(data);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        /// <summary>
        /// Gets the data of games played.
        /// </summary>
        /// <param name="dbname">The database name where the record shall be taken.</param>
        /// <param name="w_learning">Determines which resultset to return. With Learning(True) = Displays all games played with significant learning, Without Learning (False) = Displays all games played</param>
        /// <param name="skip">The number of records to exclude.</param>
        /// <param name="take">The number of records to take.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("sessions")]
        [ValidateRlmModel]
        public IEnumerable<RlmSessionHistory> GetSessions(string dbname, bool w_learning = false, int? skip = null, int? take = null)
        {
            IEnumerable<RlmSessionHistory> result = null;

            try
            {
                result = manager.GetSessions(w_learning, new RlmFilterResultParams { RlmName = dbname, Skip = skip, Take = take });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        /// <summary>
        /// Gets the events(cases) of the specified game(session) identified by its session_id.
        /// </summary>
        /// <param name="dbname">The database name where the record shall be taken.</param>
        /// <param name="session_id">The game where the events shall be taken.</param>
        /// <param name="skip">The number of records to exclude.</param>
        /// <param name="take">The number of records to take.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("sessions/{session_id}/cases")]
        [ValidateRlmModel]
        public IEnumerable<RlmCaseHistory> GetSessionCases(string dbname, long session_id, int? skip = null, int? take = null)
        {
            IEnumerable<RlmCaseHistory> result = null;

            try
            {
                result = manager.GetSessionCases(new RlmGetSessionCaseParams { RlmName = dbname, SessionId = session_id, Skip = skip, Take = take });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        /// <summary>
        /// Gets the inputs and outputs details of a certain case specified by its RNeuronID and SolutionId
        /// </summary>
        /// <param name="dbname">The database name where the record shall be taken.</param>
        /// <param name="rneuron_id">The unique identifier of a case along with its solution_id.</param>
        /// <param name="solution_id">The unique identifier of a case along with its rneuron_id.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("iodetails")]
        [ValidateRlmModel]
        public RlmCaseIOHistory GetCaseIO(string dbname, long rneuron_id, long solution_id)
        {
            RlmCaseIOHistory result = null;

            try
            {
                result = manager.GetCaseInputOutputDetails(new RlmGetCaseIOParams { RlmName = dbname, RneuronId = rneuron_id, SolutionId = solution_id});
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("next_prev_learned_case")]
        [ValidateRlmModel]
        public long? GetNextPreviousLearnedCaseId(string dbname, long case_id, bool is_next)
        {
            long? result = null;

            try
            {
                result = manager.GetNextPrevLearnedCaseId(new RlmGetNextPrevLearnedCaseIdParams { RlmName = dbname, CaseId = case_id, IsNext = is_next });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("session_iodetails")]
        [ValidateRlmModel]
        public IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(string dbname, long[] session_ids)
        {
            IEnumerable<RlmLearnedSessionDetails> result = null;

            try
            {
                result = manager.GetSessionIODetails(new RlmGetSessionDetailsParams { RlmName = dbname, SessionIds = session_ids });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("rneuron_id_from_inputs")]
        [ValidateRlmModel]
        public long? GetRneuronIdFromInputs(string dbname, KeyValuePair<string, string>[] input_values_pair)
        {
            long? result = null;

            try
            {
                result = manager.GetRneuronIdFromInputs(new RlmGetRneuronIdFromInputs { RlmName = dbname, InputValuesPair = input_values_pair });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("solution_id_from_outputs")]
        [ValidateRlmModel]
        public long? GetSolutionIdFromOutputs(string dbname, KeyValuePair<string, string>[] output_values_pair)
        {
            long? result = null;

            try
            {
                result = manager.GetSolutionIdFromOutputs(new RlmGetSolutionIdFromOutputs { RlmName = dbname, OutputValuesPair = output_values_pair });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("learned_cases")]
        [ValidateRlmModel]
        public IEnumerable<RlmLearnedCase> GetLearnedCases(string dbname, long rneuron_id, long solution_id, double scale)
        {
            IEnumerable<RlmLearnedCase> result = null;

            try
            {
                result = manager.GetLearnedCases(new RlmGetLearnedCasesParams { RlmName = dbname, RneuronId = rneuron_id, SolutionId = solution_id, Scale = scale });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("case_details")]
        [ValidateRlmModel]
        public IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(string dbname, long case_id)
        {
            IEnumerable<RlmLearnedCaseDetails> result = null;

            try
            {
                result = manager.GetCaseDetails(new RlmGetCaseDetailsParams { RlmName = dbname, CaseId = case_id});
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }

        [HttpGet]
        [Route("case_iodetails")]
        [ValidateRlmModel]
        public IEnumerable<RlmIODetails>[] GetCaseIODetails(string dbname, long case_id)
        {
            IEnumerable<RlmIODetails>[] result = null;

            try
            {
                result = manager.GetCaseIODetails(new RlmGetCaseIODetailsParams { RlmName = dbname, CaseId = case_id});
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Internal error: {e.Message}");
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, e));
            }

            return result;
        }
    }
}
