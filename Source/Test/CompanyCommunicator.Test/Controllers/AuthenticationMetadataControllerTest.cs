// <copyright file="AuthenticationMetadataControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>
namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using FluentAssertions;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Authentication;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Moq;
    using Xunit;

    /// <summary>
    /// AuthenticationMetadataController test class.
    /// </summary>
    public class AuthenticationMetadataControllerTest
    {
        private readonly Mock<IOptions<AuthenticationOptions>> options = new Mock<IOptions<AuthenticationOptions>>();
        private readonly Mock<IAppConfiguration> appConfigurationMock = new Mock<IAppConfiguration>();

        private readonly string tenantId = "tenantId";
        private readonly string clientId = "clientId";

        /// <summary>
        /// Gets GetParams.
        /// </summary>
        public static IEnumerable<object[]> GetParams
        {
            get
            {
                return new[]
                {
                    new object[] { null/*loginHint*/, "windowLocationOriginDomain" },
                    new object[] { "loginHint", null /*windowLocationOriginDomain */ },
                };
            }
        }

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            this.options.Setup(x => x.Value).Returns(new AuthenticationOptions() { AzureAdTenantId = this.tenantId, AzureAdClientId = this.clientId });
            Action action = () => new AuthenticationMetadataController(this.options.Object, this.appConfigurationMock.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameter.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParameter_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new AuthenticationMetadataController(null /*authenticationOptions*/, null);

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
            var getInstance = this.GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "firstname.lastname@testname.com";

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
            var getInstance = this.GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "firstname.lastname@testname.com";
            var components = this.GetComponents();
            var allComponentsExists = true;

            // Act
            var consentUrlString = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            foreach (var component in components)
            {
                if (!consentUrlString.Contains(component))
                {
                    allComponentsExists = false;
                    break;
                }
            }

            // Assert
            Assert.True(allComponentsExists);
        }

        /// <summary>
        /// Test case to check for correct mapping of clientId, tenantId, loginHint and windowLocationOriginDomain.
        /// </summary>
        [Fact]
        public void Get_CorrectMapping_ReturnsConsentUrl()
        {
            // Arrange
            var getInstance = this.GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "firstname.lastname@testname.com";
            var consentUrlPrefix = $"https://login.microsoftonline.com/";

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            var components = result.Split('&');
            var redirect_uri = components.FirstOrDefault(x => x.Contains("redirect_uri")).Split('=')[1];
            var windowLocationOrigin_Domain = redirect_uri.Substring(14, windowLocationOriginDomain.Length);
            var client_id = components.FirstOrDefault(x => x.Contains("client_id")).Split('=')[1];
            var login_hint = HttpUtility.UrlDecode(components.FirstOrDefault(x => x.Contains("login_hint")).Split('=')[1]);
            var tenant_Id = components.FirstOrDefault(x => x.Contains("redirect_uri")).Substring(consentUrlPrefix.Length, this.tenantId.Length);

            // Assert
            Assert.Equal(client_id, this.clientId);
            Assert.Equal(tenant_Id, this.tenantId);
            Assert.Equal(login_hint, loginHint);
            Assert.Equal(windowLocationOrigin_Domain, windowLocationOriginDomain);
        }

        /// <summary>
        /// Test case to check if method handles null Parameters.
        /// </summary>
        /// <param name="loginHint">loginHint.</param>
        /// <param name="windowLocationOriginDomain">windowLocationOriginDomain.</param>
        [Theory]
        [MemberData(nameof(GetParams))]
        public void GetConsentUrl_NullParameters_ThrowsAgrumentNullException(string loginHint, string windowLocationOriginDomain)
        {
            // Arrange
            var getInstance = this.GetAuthenticationMetadataController();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint));
        }

        /// <summary>
        /// Test case to check if consentUrlString is not null and empty.
        /// </summary>
        [Fact]
        public void GetConsentUrl_ComponentCount_ReturnEigthComponents()
        {
            // Arrange
            var getInstance = this.GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "firstname.lastname@testname.com";
            var components = this.GetComponents();

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);
            var consentUrlcomponents = result.Split('&');

            // Assert
            Assert.Equal(components.Count(), consentUrlcomponents.Count());
        }

        /// <summary>
        /// Test case to check if consentUrlString join character is Ampersand.
        /// </summary>
        [Fact]
        public void Check_consentUrl_JoinCharaterAmpersand()
        {
            // Arrange
            var getInstance = this.GetAuthenticationMetadataController();
            string windowLocationOriginDomain = "windowLocationOriginDomain";
            string loginHint = "firstname.lastname@testname.com";

            // Act
            var result = getInstance.GetConsentUrl(windowLocationOriginDomain, loginHint);

            // Assert
            Assert.True(result.Split('&').Count() > 1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationMetadataController"/> class.
        /// </summary>
        /// <returns>return the instance of AuthenticationMetadataController.</returns>
        public AuthenticationMetadataController GetAuthenticationMetadataController()
        {
            this.options.Setup(x => x.Value).Returns(new AuthenticationOptions() { AzureAdTenantId = this.tenantId, AzureAdClientId = this.clientId });
            return new AuthenticationMetadataController(this.options.Object, new CommericalConfiguration("tenant Id"));
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
                "login_hint",
            };
        }
    }
}
