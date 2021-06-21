// <copyright file="UserTypeServiceTest.cs" company="Microsoft">
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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Moq;
    using Xunit;

    /// <summary>
    /// User Type service test.
    /// </summary>
    public class UserTypeServiceTest
    {
        private readonly Mock<IUsersService> usersService = new Mock<IUsersService>();
        private readonly Mock<IUserDataRepository> userRespositoryService = new Mock<IUserDataRepository>();

        /// <summary>
        /// Test case to check if ArgumentNullException is thrown if parameters are null.
        /// </summary>
        [Fact]
        public void UserTypeService_NullParameters_ShouldThrowException()
        {
            Action action1 = () => new UserTypeService(null, this.usersService.Object);
            Action action2 = () => new UserTypeService(this.userRespositoryService.Object, null);
            Action action3 = () => new UserTypeService(this.userRespositoryService.Object, this.usersService.Object);

            action1.Should().Throw<ArgumentNullException>();
            action2.Should().Throw<ArgumentNullException>();
            action3.Should().NotThrow();
        }

        /// <summary>
        /// Test case to verify that no action is taken when user data is null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NullUserData_ShouldNotSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserAsync(null, null);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify that exception is thrown when user type parameter is null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NullUserType_ShouldThrowException()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            var userData = new UserDataEntity() { AadId = "userId", UserType = null };

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserAsync(userData, null);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Test case to verify that no action is taken when user data has user type assigned.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_ExistingUserType_ShouldNotSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            var userData = new UserDataEntity() { AadId = "userId", UserType = UserType.Member };

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserAsync(userData, null);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to check that if data is saved when the parameters is correct.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_CorrectParameters_ShouldSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            var userData = new UserDataEntity() { AadId = "userId", UserType = null };
            var userType = UserType.Member;

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserAsync(userData, userType);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => x.UserType.Equals(userType))), Times.Once);
        }

        /// <summary>
        /// Test case to check that no action is taken when there is null user data entities.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NullParameters_ShouldNotSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = null;

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify that no data is saved when all users have user type assigned.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_UserTypeAssigned_ShouldNotSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = new List<UserDataEntity>()
            {
                new UserDataEntity() { AadId = "1", UserType = UserType.Member },
                new UserDataEntity() { AadId = "2", UserType = UserType.Member },
            };

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify that data is saved when there is no assignment for some user type.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_SomeUserTypeAssigned_ShouldSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = new List<UserDataEntity>()
            {
                new UserDataEntity() { AadId = "1", UserType = UserType.Member },
                new UserDataEntity() { AadId = "2", UserType = null },
            };
            var users = new List<User>()
            {
                new User() { Id = "2", UserType = UserType.Member },
            };

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(users);

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => this.Compare(x, users.FirstOrDefault()))), Times.Once);
        }

        /// <summary>
        /// Test case to verify that data is saved when there is no user type.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NoUserTypeAssigned_ShouldSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = new List<UserDataEntity>()
            {
                new UserDataEntity() { AadId = "1", UserType = null },
            };
            var users = new List<User>()
            {
                new User() { Id = "1", UserType = UserType.Member },
            };

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(users);

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.Is<UserDataEntity>(x => this.Compare(x, users.FirstOrDefault()))), Times.Once);
        }

        /// <summary>
        /// Test case to verify that no action is taken on null data response from graph.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NullResponseFromGraph_ShouldNotSaveData()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = new List<UserDataEntity>()
            {
                new UserDataEntity() { AadId = "1", UserType = null },
            };
            IEnumerable<User> users = null;

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(users);

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().NotThrowAsync();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        /// <summary>
        /// Test case to verify that exception is thrown when user is null in the response from graph.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task UserTypeService_NullResponseInUserListFromGraph_ShouldThrowException()
        {
            // Arrange
            var serviceContext = this.GetUserTypeService();
            IEnumerable<UserDataEntity> userDataEntities = new List<UserDataEntity>()
            {
                new UserDataEntity() { AadId = "1", UserType = null },
            };
            IEnumerable<User> users = new List<User>()
            { null };

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(users);

            // Act
            Func<Task> task = async () => await serviceContext.UpdateUserTypeForExistingUserListAsync(userDataEntities);

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>();
            this.userRespositoryService.Verify(x => x.InsertOrMergeAsync(It.IsAny<UserDataEntity>()), Times.Never);
        }

        private bool Compare(UserDataEntity userDataEntity, User user)
        {
            return userDataEntity.PartitionKey.Equals(UserDataTableNames.UserDataPartition)
                && userDataEntity.RowKey.Equals(user.Id)
                && userDataEntity.AadId.Equals(user.Id)
                && userDataEntity.UserType.Equals(user.UserType);
        }

        private UserTypeService GetUserTypeService()
        {
            return new UserTypeService(this.userRespositoryService.Object, this.usersService.Object);
        }
    }
}
