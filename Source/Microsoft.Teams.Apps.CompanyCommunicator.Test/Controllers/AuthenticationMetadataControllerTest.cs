// <copyright file="AuthenticationMetadataControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// </copyright>

using Castle.Core.Internal;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
using Microsoft.Graph;
using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    /// <summary>
    /// AuthenticationMetadataController test class.
    /// </summary>
    public class AuthenticationMetadataControllerTest
    {
        private readonly Mock<IOptions<AuthenticationOptions>> options = new Mock<IOptions<AuthenticationOptions>>();
        private readonly string tenantId = "tenantId";
        private readonly string clientId = "clientId";
        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            options.Setup(x => x.Value).Returns(new AuthenticationOptions() { AzureAdTenantId = tenantId, AzureAdClientId = clientId });
            Action action = () => new AuthenticationMetadataController(options.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        // <summary>
        /// Constructor test for null parameter.
        /// </summary> 
        [Fact]
        public void CreateInstance_NullParamter_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new AuthenticationMetadataController(null /*authenticationOptions*/);
            
            // Act and Assert.
            action.Should().Throw<ArgumentNullException>("authenticationOptions is null.");
        }

        /// <summary>
        /// Test case to check if consentUrlString is not null and empty.
        /// </summary>
        [Fact]
        public void GetConsentUrl_ValidInput_ReturnsValidConsentUrl()
        {
            // Arrange
            var getInstance = GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "loginHint";

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Test case to check if consenturl string should contain all the component as listed in method.
        /// </summary>
        [Fact]
        public void Get_ConsentUrl_ShouldContainAllComponents()
        {
            // Arrange
            var getInstance = GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "loginHint";
            var components = GetComponents();
            var AllComponentsExists = true;

            // Act
            var consentUrlString = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            foreach(var component in components)
            {
                if(!consentUrlString.Contains(component))
                {
                    AllComponentsExists = false;
                    break;
                }
            }

            // Assert
            Assert.True(AllComponentsExists);
        }

        /// <summary>
        /// Test case to check for correct mapping of clientId, tenantId, loginHint and windowLocationOriginDomain.
        /// </summary>
        [Fact]
        public void Get_CorrectMapping_ReturnsConsentUrl()
        {
            // Arrange
            var getInstance = GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "loginHint";
            var consentUrlPrefix = $"https://login.microsoftonline.com/";

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            var components = result.Split('&');
            var redirect_uri = components.FirstOrDefault(x => x.Contains("redirect_uri")).Split('=')[1];
            var windowLocationOrigin_Domain = redirect_uri.Substring(14, windowLocationOriginDomain.Length);
            var client_id = components.FirstOrDefault(x => x.Contains("client_id")).Split('=')[1];
            var login_hint = components.FirstOrDefault(x => x.Contains("login_hint")).Split('=')[1];
            var tenant_Id = components.FirstOrDefault(x => x.Contains("redirect_uri")).Substring(consentUrlPrefix.Length, tenantId.Length);

            // Assert
            Assert.Equal(client_id , clientId);
            Assert.Equal(tenant_Id, tenantId);
            Assert.Equal(login_hint, loginHint);
            Assert.Equal(windowLocationOrigin_Domain, windowLocationOriginDomain);
        }

        /// <summary>
        /// Test case to check if method handles null Parameters.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetParams))]
        public void GetConsentUrl_NullParameters_ThrowsAgrumentNullException(string loginHint, string windowLocationOriginDomain)
        {
            //Arrange
            var getInstance = GetAuthenticationMetadataController();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint));
        }

        public static IEnumerable<object[]> GetParams
        {
            get
            {
                return new[]
                {
                    new object[] {null/*loginHint*/, "windowLocationOriginDomain" },
                    new object[] { "loginHint", null /*windowLocationOriginDomain */}
                };
            }
        }

        /// <summary>
        /// Test case to check if consentUrlString is not null and empty.
        /// </summary>
        [Fact]
        public void GetConsentUrl_ComponentCount_ReturnEigthComponents()
        {
            // Arrange
            var getInstance = GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "loginHint";
            var components = GetComponents();

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            var ConsentUrlcomponents = result.Split('&');

            // Assert
            Assert.Equal(components.Count(),ConsentUrlcomponents.Count());
        }

        /// <summary>
        /// Test case to check if consentUrlString join character is '&'.
        /// </summary>
        [Fact]
        public void Check_consentUrl_JoinCharaterAmpersand()
        {
            // Arrange
            var getInstance = GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "loginHint";

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);

            // Assert
            Assert.True(result.Split('&').Count() > 1);
        }
        
        private List<string> GetComponents()
        {
            return new List<string>()
            {
                "redirect_uri",
                "client_id",
                "response_type",
                "response_mode",
                "scope",
                "nonce",
                "state",
                "login_hint"
            };
        }
        public AuthenticationMetadataController GetAuthenticationMetadataController()
        {
            options.Setup(x => x.Value).Returns(new AuthenticationOptions() { AzureAdTenantId = tenantId, AzureAdClientId = clientId });
            return new AuthenticationMetadataController(options.Object);
        }
    }
}
