using System;
using Xunit;
using Moq;
using FluentAssertions;
using P21IntegrationWindowsService.Models;
using AutoFixture;
using System.Net.Http;
using System.Net;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Diagnostics;
using static P21IntegrationWindowsService.Models.ProkeepServiceModel;

namespace P21IntegrationWindowsService.Test
{
    public class SyncTriggerTest : SyncTrigger
    {
        private readonly SyncTrigger _sut;
        private readonly IFixture _fixture;
        private readonly Mock<object> _contact;
        private readonly Mock<P21Objects.Contact> _p21Contact;

        public SyncTriggerTest(EventLog eventLog) : base(eventLog)
        {
            _fixture = new Fixture();
            _sut = new SyncTrigger(eventLog);
            _contact = _fixture.Freeze<Mock<object>>();
            _p21Contact = _fixture.Freeze<Mock<P21Objects.Contact>>();
        }

        private readonly HttpResponseMessage response = new HttpResponseMessage();

        [Fact]
        public async void SendContact_ShouldReturnSucces_WhenContactPost()
        {
            // Arrange
            Mock<HttpMessageHandler> handlerMock = new Mock<HttpMessageHandler>();

            object requestBody = _fixture.Create<object>();

            string mediaType = _fixture.Create<string>();
            string url = _fixture.Create<Uri>().ToString();
            string authType = _fixture.Create<string>();
            string encodedString = _fixture.Create<string>();
            string contactId = null;
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(_contact.ToString());

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            HttpClient httpClient = new HttpClient(handlerMock.Object);

            var post = await _sut.SendRequest(httpClient, requestBody, mediaType, url, authType, encodedString, contactId);

            // Assert
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());
            post.Should().NotBeNull();
            requestBody.Should().NotBeNull();
        }

        [Fact]
        public async void SendContact_ShouldReturnSucces_WhenContactPut()
        {
            // Arrange
            Mock<HttpMessageHandler> handlerMock = new Mock<HttpMessageHandler>();

            object requestBody = _fixture.Create<object>();
            string mediaType = _fixture.Create<string>();
            string url = _fixture.Create<Uri>().ToString();
            string authType = _fixture.Create<string>();
            string encodedString = _fixture.Create<string>();
            string contactId = _fixture.Create<string>();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(_contact.ToString());

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            HttpClient httpClient = new HttpClient(handlerMock.Object);
            HttpResponseMessage post = await _sut.SendRequest(httpClient, requestBody, mediaType, url, authType, encodedString, contactId);

            // Assert
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>());
            post.Should().NotBeNull();
            requestBody.Should().NotBeNull();
        }
        
        [Fact]
        public async void SendContactToP21_ShouldReturnSucces_WhenContactPost()
        {
            // Arrange
            Mock<HttpMessageHandler> handlerMock = new Mock<HttpMessageHandler>();
            Contact requestBody = _fixture.Create<Contact>();
            string contactId = null;
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(_p21Contact.ToString());

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            HttpClient httpClient = new HttpClient(handlerMock.Object);
            HttpResponseMessage post = await _sut.SendContactToP21(requestBody, httpClient, contactId);

            // Assert
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());
           // post.Should().NotBeNull();
            requestBody.Should().NotBeNull();
        }
    }
}
