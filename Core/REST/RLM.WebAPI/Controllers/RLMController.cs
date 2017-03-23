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
    }
}
