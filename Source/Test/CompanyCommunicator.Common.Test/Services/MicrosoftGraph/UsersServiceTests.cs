// <copyright file="UsersServiceTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Graph;
    using Microsoft.Teams.App.CompanyCommunicator.Common.Test.Services.Mock;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Moq;
    using Xunit;

    /// <summary>
    /// Users Service unit tests.
    /// </summary>
    public class UsersServiceTests
    {
        /// <summary>
        /// Test case to check if Users service is instanticated successfully.
        /// </summary>
        [Fact]
        public void CreateInstance_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Mock<IGraphServiceClient> graphServiceClientMock = new Mock<IGraphServiceClient>();
            Action action = () => new UsersService(graphServiceClientMock.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Test case to check if ArgumentNullException is thrown for null parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new UsersService(null);

            // Act and Assert.
            action.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to check if ArgumentNullException is thrown for null parameter in GetBatchByUserIds method.
        /// </summary>
        [Fact]
        public async void GetBatch_NullParameter_ThrowsArgumentNullException()
        {
            // Arrange
            var usersServiceInstance = this.GetUsersService();

            // Act
            Func<Task> task = async () => await usersServiceInstance.GetBatchByUserIds(null);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to check if response is empty for empty parameters.
        /// </summary>
        [Fact]
        public async void GetBatch_EmptyParameter_ReturnsEmptyResponse()
        {
            // Arrange
            var usersServiceInstance = this.GetUsersService();
            var usersByGroups = new List<List<string>>();

            // Act
            var users = await usersServiceInstance.GetBatchByUserIds(usersByGroups);

            // Assert
            Assert.Empty(users);
        }

        /// <summary>
        /// Test case to check if InvalidOperationException is thrown if user batch count is over 15.
        /// </summary>
        [Fact]
        public async void GetBatch_UserBatchCountOver15_ShouldThrowInvalidOperationExcetpion()
        {
            // Arrange
            var usersService = this.GetUsersService();
            var usersByGroups = new List<List<string>>
            {
                new List<string>()
                { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17" },
            };

            // Act
            Func<Task> task = async () => await usersService.GetBatchByUserIds(usersByGroups);

            // Assert
            await task.Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// Test case to check if we get exact batch count of 8.
        /// </summary>
        [Fact]
        public async void GetBatch_UserBatchSizeOver20_ShouldMatchResponseCount()
        {
            // Arrange
            var usersService = this.GetUsersService();
            var userIdBatch = new List<string>() { "1" };

            // Adding 21 batches.
            var usersByGroups = new List<List<string>>() { userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch, userIdBatch };

            // Act
            var users = await usersService.GetBatchByUserIds(usersByGroups);

            // Assert
            // 1 Batch should have 4 user count, expected is 2 batch hence 8.
            Assert.Equal(8, users.Count());
        }

        /// <summary>
        /// Test case to check if we user mapping is correct.
        /// </summary>
        [Fact]
        public async void GetBatch_AllParameters_ShouldMapCorrectly()
        {
            // Arrange
            var usersService = this.GetUsersService();
            var users = new List<string>() { "1" };
            var usersByGroups = new List<List<string>>() { users };

            // Act
            var resp = await usersService.GetBatchByUserIds(usersByGroups);

            // Assert
            Assert.Equal("test-id-1", resp.FirstOrDefault().Id);
            Assert.Equal("test-display-id-1", resp.FirstOrDefault().DisplayName);
        }

        /// <summary>
        /// Test case to check if we get exact batch count of 4.
        /// </summary>
        [Fact]
        public async void GetBatch_UserBatchSizeOf1_ShouldMatchResponseCount()
        {
            // Arrange
            var usersService = this.GetUsersService();
            var users = new List<string>() { "1" };
            var usersByGroups = new List<List<string>>() { users };

            // Act
            var resp = await usersService.GetBatchByUserIds(usersByGroups);

            // Assert
            Assert.Equal(4, resp.Count());
        }

        private UsersService GetUsersService()
        {
            MockHttpProvider mockHttpProvider = new MockHttpProvider();
            mockHttpProvider.Responses.Add("POST:https://graph.microsoft.com/v1.0/$batch", new
            {
                responses = new List<object>()
                {
                    new
                    {
                        id = "1",
                        body = new
                        {
                          value = new List<User>()
                          {
                            new User()
                            {
                                DisplayName = "test-display-id-1",
                                Id = "test-id-1",
                            },
                            new User()
                            {
                                DisplayName = "test-display-id-2",
                                Id = "test-id-2",
                            },
                          },
                        },
                    },
                    new
                    {
                        id = "2",
                        body = new
                        {
                          value = new List<User>()
                          {
                            new User()
                            {
                                DisplayName = "test-display-id-3",
                                Id = "test-id-3",
                            },
                            new User()
                            {
                                DisplayName = "test-display-id-4",
                                Id = "test-id-4",
                            },
                          },
                        },
                    },
                },
            });

            GraphServiceClient client = new GraphServiceClient(new MockAuthenticationHelper(), mockHttpProvider);
            return new UsersService(client);
        }
    }
}
