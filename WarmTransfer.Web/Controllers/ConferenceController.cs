﻿using System.Web.Mvc;
using Twilio.TwiML.Mvc;
using WarmTransfer.Web.Domain;
using WarmTransfer.Web.Models.Repository;

using static WarmTransfer.Web.Domain.TwiMLGenerator;

namespace WarmTransfer.Web.Controllers
{
    public class ConferenceController : TwilioController
    {
        private readonly ICallCreator _callCreator;
        private readonly ICallsRepository _callsRepository;

        public ConferenceController(
            ICallCreator callCreator, ICallsRepository callsRepository)
        {
            _callCreator = callCreator;
            _callsRepository = callsRepository;
        }

        public ActionResult ConnectClient(string conferenceId)
        {
            const string agentOne = "agent1";
            const string callBackUrl = "callback-url"; //TODO extract it to a config file
            _callCreator.CallAgent(agentOne, callBackUrl);
            _callsRepository.CreateIfNotExists(agentOne, conferenceId);
            var response = GenerateConnectConference(conferenceId, "wait-url", false, true);
            return TwiML(response);
        }

        public ActionResult Wait()
        {
            return TwiML(GenerateWait());
        }

        public ActionResult ConnectAgent1(string conferenceId)
        {
            var response = GenerateConnectConference(conferenceId, "wait-url", false, true);
            return TwiML(response);
        }
    }
}