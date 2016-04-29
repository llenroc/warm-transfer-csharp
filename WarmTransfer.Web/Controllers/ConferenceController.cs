﻿using System.Web.Mvc;
using Twilio;
using Twilio.TwiML.Mvc;
using WarmTransfer.Web.Domain;
using WarmTransfer.Web.Models.Repository;

namespace WarmTransfer.Web.Controllers
{
    public class ConferenceController : TwilioController
    {
        private readonly ICallCreator _callCreator;
        private readonly ICallsRepository _callsRepository;

        public static string WaitUrl = "http://twimlets.com/holdmusic?Bucket=com.twilio.music.classical";

        public ConferenceController() : this(
            new CallCreator(new TwilioRestClient(Config.AccountSID, Config.AuthToken)),
            new CallsRepository(new Models.WarmTransferContext())) { }

        public ConferenceController(
            ICallCreator callCreator, ICallsRepository callsRepository)
        {
            _callCreator = callCreator;
            _callsRepository = callsRepository;
        }

        [HttpPost]
        public ActionResult ConnectClient(string callSid)
        {
            const string agentOne = "agent1";
            var conferenceId = callSid;
            var callBackUrl = GetConnectConfereceUrlForAgent(agentOne, conferenceId);
            _callCreator.CallAgent(agentOne, callBackUrl);
            _callsRepository.CreateIfNotExists(agentOne, conferenceId);
            var response = TwiMLGenerator.GenerateConnectConference(conferenceId, WaitUrl, false, true);
            return TwiML(response);
        }

        [HttpPost]
        public ActionResult Wait()
        {
            return TwiML(TwiMLGenerator.GenerateWait());
        }

        [HttpPost]
        public ActionResult ConnectAgent1(string conferenceId)
        {
            var response = TwiMLGenerator.GenerateConnectConference(conferenceId, WaitUrl, false, true);
            return TwiML(response);
        }

        [HttpPost]
        public ActionResult ConnectAgent2(string conferenceId)
        {
            var response = TwiMLGenerator.GenerateConnectConference(conferenceId, WaitUrl, true, true);
            return TwiML(response);
        }

        [HttpPost]
        public ActionResult CallAgent2(string agentId)
        {
            var call = _callsRepository.FindByAgentId(agentId);
            var callBackUrl = GetConnectConfereceUrlForAgent(agentId, call.ConferenceId);
            _callCreator.CallAgent("agent2", callBackUrl);
            return null;
        }

        private string GetConnectConfereceUrlForAgent(string agentId, string conferenceId) {
            var action = agentId == "agent1" ? "ConnectAgent1" : "ConnectAgent2";
            var url = string.Format(
                "https://{0}{1}",
                Config.Domain,
                Url.Action(action, new {conferenceId}));

            return url;
        }
    }
}