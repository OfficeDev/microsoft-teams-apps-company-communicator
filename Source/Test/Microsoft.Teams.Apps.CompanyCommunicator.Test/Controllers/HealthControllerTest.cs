// <copyright file="HealthControllerTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Test.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Apps.CompanyCommunicator.Controllers;
    using Xunit;

    /// <summary>
    /// HealthController test class. 
    /// </summary>
    public class HealthControllerTest
    {
        /// <summary>
        /// Test method to verify status code 200 for IndexAction.
        /// </summary>
        [Fact]
        public void Call_IndexAction_ReturnsStausCodeOk()
        {
            //Arrage
            var controller = getHealthControllerInstance();
            var statusCodeOk = 200;

            //Act
            var result = controller.Index();
            var statusCode = ((StatusCodeResult)result).StatusCode;

            // Assert
            Assert.Equal(statusCode, statusCodeOk);
        }

        private HealthController getHealthControllerInstance()
        {
            return new HealthController();
        }
    }
}
