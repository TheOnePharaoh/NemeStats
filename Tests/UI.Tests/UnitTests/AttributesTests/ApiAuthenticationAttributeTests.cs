﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using BusinessLogic.Logic.Users;
using BusinessLogic.Models.User;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.DataProtection;
using NUnit.Framework;
using Rhino.Mocks;
using UI.Attributes;

namespace UI.Tests.UnitTests.AttributesTests
{
    [TestFixture]
    public class ApiAuthenticationAttributeTests
    {
        private ApiAuthenticationAttribute _attribute;
        private HttpActionContext _actionContext;
        private HttpControllerContext _controllerContext;
        private HttpRequestMessage _request;
        private ApplicationUserManager _applicationUserManager;
        private IUserStore<ApplicationUser> _userStoreMock;
        private IDataProtectionProvider _dataProtectionProviderMock;

        [SetUp]
        public void SetUp()
        {
            var dataProtector = MockRepository.GenerateMock<IDataProtector>();
            _request = new HttpRequestMessage();
            _request.SetConfiguration(new HttpConfiguration());
            _userStoreMock = MockRepository.GenerateMock<IUserStore<ApplicationUser>>();
            _dataProtectionProviderMock = MockRepository.GenerateMock<IDataProtectionProvider>();
            _dataProtectionProviderMock.Expect(mock => mock.Create(Arg<string>.Is.Anything)).Return(dataProtector);
            _applicationUserManager = MockRepository.GenerateMock<ApplicationUserManager>(_userStoreMock, _dataProtectionProviderMock);
            _attribute = new ApiAuthenticationAttribute(_applicationUserManager);
            _controllerContext = new HttpControllerContext {Request = _request};
            _actionContext = new HttpActionContext(_controllerContext, new ReflectedHttpActionDescriptor());
        }

        [Test]
        public void ShouldReturnBadRequestWhenNoTokenProvided()
        {
            _attribute.OnActionExecuting(_actionContext);
            Assert.AreEqual(HttpStatusCode.BadRequest, _actionContext.Response.StatusCode);
            string actualContent = ((ObjectContent<string>)_actionContext.Response.Content).Value.ToString();
            Assert.AreEqual("This action requires an X-Auth-Token header.", actualContent);
        }

        [Test]
        public void ShouldReturnBadRequestWhenTokenHasEmptyValue()
        {
            _request.Headers.Add(ApiAuthenticationAttribute.AUTH_HEADER, new [] { string.Empty });
            _attribute.OnActionExecuting(_actionContext);
            Assert.AreEqual(HttpStatusCode.BadRequest, _actionContext.Response.StatusCode);
            string actualContent = ((ObjectContent<string>)_actionContext.Response.Content).Value.ToString();
            Assert.AreEqual("Invalid X-Auth-Token", actualContent);
        }

        [Test]
        public void ShouldGetTheUsersToken()
        {
            const string expectedToken = "TEST";
            _request.Headers.Add(ApiAuthenticationAttribute.AUTH_HEADER, new[] { expectedToken });
            var expectedUsers = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    AuthenticationToken = expectedToken,
                    AuthenticationTokenExpirationDate = DateTime.UtcNow.AddMonths(2)
                }
            };

            _applicationUserManager.Expect(x => x.Users)
                .Repeat.Any()
                .Return(expectedUsers.AsQueryable());

            _attribute.OnActionExecuting(_actionContext);
            _applicationUserManager.AssertWasCalled(x => x.Users);
        }

        [Test]
        public void ShouldReturnUnauthorizedHttpStatusWhenWrongToken()
        {
            const string expectedToken = "TEST";
            const string actualToken = "DIFFERENT";
            _request.Headers.Add(ApiAuthenticationAttribute.AUTH_HEADER, new[] { expectedToken });
            var expectedUsers = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    AuthenticationToken = actualToken,
                    AuthenticationTokenExpirationDate = DateTime.UtcNow.AddMonths(2)
                }
            };

            _applicationUserManager.Expect(x => x.Users)
                .Repeat.Any()
                .Return(expectedUsers.AsQueryable());

            _attribute.OnActionExecuting(_actionContext);
            Assert.AreEqual(HttpStatusCode.Unauthorized, _actionContext.Response.StatusCode);
            string actualContent = ((ObjectContent<string>)_actionContext.Response.Content).Value.ToString();
            Assert.AreEqual("Invalid X-Auth-Token", actualContent);
        }

        [Test]
        public void ShouldReturnUnauthorizedHttpStatusWhenTokenExpired()
        {
            const string expectedToken = "TEST";
            const string actualToken = "DIFFERENT";
            _request.Headers.Add(ApiAuthenticationAttribute.AUTH_HEADER, new[] { expectedToken });
            var expectedUsers = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    AuthenticationToken = actualToken,
                    AuthenticationTokenExpirationDate = DateTime.UtcNow.AddMonths(-2)
                }
            };

            _applicationUserManager.Expect(x => x.Users)
                .Repeat.Any()
                .Return(expectedUsers.AsQueryable());

            _attribute.OnActionExecuting(_actionContext);
            Assert.AreEqual(HttpStatusCode.Unauthorized, _actionContext.Response.StatusCode);
            string actualContent = ((ObjectContent<string>)_actionContext.Response.Content).Value.ToString();
            Assert.AreEqual("Invalid X-Auth-Token", actualContent);
        }
    }
}