﻿using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using NUnit.Framework;
using System.Xml.XPath;
using TestStack.FluentMVCTesting;
using WarmTransfer.Web.Controllers;
using WarmTransfer.Web.Domain;
using WarmTransfer.Web.Models.Repository;
using WarmTransfer.Web.Tests.Extensions;
using WarmTransfer.Web.Models;

namespace WarmTransfer.Web.Tests.Controllers
{
    public class ConferenceControllerTest
    {
        private Mock<ICallCreator> _mockCallCreator;
        private Mock<ICallsRepository> _mockCallsRepository;
        private ConferenceController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockCallCreator = new Mock<ICallCreator>();
            _mockCallsRepository = new Mock<ICallsRepository>();
            _mockCallsRepository.Setup(c => c.FindByAgentId("agent2"))
                .Returns(new Call("agent2", "conference-id"));

            _controller = new ConferenceController(
                _mockCallCreator.Object, _mockCallsRepository.Object)
            {
                ControllerContext = MockControllerContext(),
                Url = MockUrlHelper(),
            };
        }

        [Test]
        public void WhenConnectClient_ThenShouldCallAgent()
        {
            _controller.ConnectClient("conference-id");

            _mockCallCreator.Verify(c => c.CallAgent("agent1", "http://example.com/Home/ConnectAgent1?conferenceId=conference-id"), 
                Times.Once());
        }

        [Test]
        public void WhenConnectClient_ThenItShouldCreateACall()
        {
            _controller.ConnectClient("conference-id");

            _mockCallsRepository.Verify(c => c.CreateIfNotExists("agent1", "conference-id"),
                Times.Once());
        }

        [Test]
        public void WhenConnectClient_ThenItShouldGenerateConnectConference()
        {
            _controller.WithCallTo(c => c.ConnectClient("conference-id"))
            .ShouldReturnTwiMLResult(data =>
                {
                    Assert.That(data.XPathSelectElement("Response/Dial/Conference").Value, Is.EqualTo("conference-id"));
                });
        }

        [Test]
        public void WhenWait_ThenItShouldGenerateWait()
        {
            _controller.WithCallTo(c => c.Wait())
            .ShouldReturnTwiMLResult(data =>
            {
                StringAssert.Contains("Please wait", data.XPathSelectElement("Response/Say").Value);
                StringAssert.Contains("twilio.music", data.XPathSelectElement("Response/Play").Value);
            });

        }

        [Test]
        public void WhenConnectAgent1_ThenItShouldGenerateConnectConference()
        {
            _controller.WithCallTo(c => c.ConnectAgent1("conference-id"))
            .ShouldReturnTwiMLResult(data =>
            {
                var xElement = data.XPathSelectElement("Response/Dial/Conference");
                Assert.That(xElement.Value, Is.EqualTo("conference-id"));
                Assert.That(xElement.Attribute("waitUrl").Value, Is.EqualTo(ConferenceController.WaitUrl));
                Assert.That(xElement.Attribute("startConferenceOnEnter").Value, Is.EqualTo("false"));
                Assert.That(xElement.Attribute("endConferenceOnExit").Value, Is.EqualTo("true"));
            });

        }

        [Test]
        public void WhenConnectAgent2_ThenItShouldGenerateConnectConference()
        {
            _controller.WithCallTo(c => c.ConnectAgent2("conference-id"))
            .ShouldReturnTwiMLResult(data =>
            {
                var xElement = data.XPathSelectElement("Response/Dial/Conference");
                Assert.That(xElement.Value, Is.EqualTo("conference-id"));
                Assert.That(xElement.Attribute("waitUrl").Value, Is.EqualTo(ConferenceController.WaitUrl));
                Assert.That(xElement.Attribute("startConferenceOnEnter").Value, Is.EqualTo("true"));
                Assert.That(xElement.Attribute("endConferenceOnExit").Value, Is.EqualTo("true"));
            });

        }

        [Test]
        public void WhenCallAgent2_ThenItShouldFindByAgentId()
        {
            _controller.CallAgent2("agent2");

            _mockCallsRepository.Verify(c => c.FindByAgentId("agent2"),
                Times.Once());
        }

        [Test]
        public void WhenCallAgent2_ThenShouldInvokeCallAgent()
        {
            _controller.CallAgent2("agent2");

            _mockCallCreator.Verify(c => c.CallAgent("agent2", "http://example.com/Home/ConnectAgent2?conferenceId=conference-id"),
                Times.Once());
        }

        private static ControllerContext MockControllerContext()
        {

            var httpContextMock = new Mock<HttpContextBase>();
            httpContextMock.Setup(c => c.Request.ApplicationPath).Returns("/");

            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.Setup(r => r.ApplicationPath).Returns("/");
            httpContextMock.Setup(c => c.Response.ApplyAppPathModifier(It.IsAny<string>()))
                .Returns<string>(s => s);
            mockRequest.SetupGet(r => r.Url).Returns(new Uri("http://www.localhost.com"));
            var mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(x => x.HttpContext.Request).Returns(mockRequest.Object);
            return mockControllerContext.Object;
        }

        public static UrlHelper MockUrlHelper()
        {
            var mockHttpContext = new Mock<HttpContextBase>(MockBehavior.Loose);
            var mockHttpRequest = new Mock<HttpRequestBase>(MockBehavior.Loose);
            var mockHttpResponse = new Mock<HttpResponseBase>(MockBehavior.Strict);
            mockHttpContext.Setup(httpContext => httpContext.Request).Returns(mockHttpRequest.Object);
            mockHttpContext.Setup(httpContext => httpContext.Response).Returns(mockHttpResponse.Object);
            mockHttpRequest.Setup(httpRequest => httpRequest.Url).Returns(new Uri("http://example.com"));
            mockHttpRequest.Setup(httpRequest => httpRequest.ServerVariables).Returns(new NameValueCollection());

            string value = null;
            Action<string> saveValue = x =>
            {
                value = x;
            };
            Func<String> restoreValue = () => value;
            mockHttpResponse.Setup(httpResponse => httpResponse.ApplyAppPathModifier(It.IsAny<string>()))
                            .Callback(saveValue).Returns(restoreValue);
            var requestContext = new RequestContext(mockHttpContext.Object, new RouteData());
            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);
            return new UrlHelper(requestContext, routes);
        }

    }
}
