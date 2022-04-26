// <copyright file="DataStreamFacadeTest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Test.Export.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Localization;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    /// <summary>
    /// DataStreamFacade test class.
    /// </summary>
    public class DataStreamFacadeTest
    {
        private readonly Mock<ISentNotificationDataRepository> sentNotificationDataRepository = new Mock<ISentNotificationDataRepository>();
        private readonly Mock<ITeamDataRepository> teamDataRepository = new Mock<ITeamDataRepository>();
        private readonly Mock<IUserDataRepository> userDataRepository = new Mock<IUserDataRepository>();
        private readonly Mock<IUserTypeService> userTypeService = new Mock<IUserTypeService>();
        private readonly Mock<IUsersService> usersService = new Mock<IUsersService>();
        private readonly Mock<IStringLocalizer<Strings>> localizer = new Mock<IStringLocalizer<Strings>>();
        private readonly string notificationId = "notificationId";
        private readonly string notificationStatus = "status";
        private readonly IEnumerable<List<SentNotificationDataEntity>> sentNotificationDataList = new List<List<SentNotificationDataEntity>>()
            {
                new List<SentNotificationDataEntity>()
                {
                    new SentNotificationDataEntity()
                    {
                        ConversationId = "conversationId", DeliveryStatus = "Succeeded", RowKey = "RowKey", ErrorMessage = string.Empty, Exception = "error",
                    },
                },
            };

        private readonly IEnumerable<List<SentNotificationDataEntity>> sentNotificationDataWithRecipientNotFound = new List<List<SentNotificationDataEntity>>()
            {
                new List<SentNotificationDataEntity>()
                {
                    new SentNotificationDataEntity()
                    {
                        ConversationId = "conversationId", DeliveryStatus = "RecipientNotFound", RowKey = "RowKey", StatusCode = 0, ErrorMessage = string.Empty,
                    },
                },
            };

        private readonly IEnumerable<List<SentNotificationDataEntity>> sentNotificationDataWithErrorList = new List<List<SentNotificationDataEntity>>()
            {
                new List<SentNotificationDataEntity>()
                {
                    new SentNotificationDataEntity()
                    {
                        ConversationId = "conversationId", DeliveryStatus = "Failed", RowKey = "RowKey", StatusCode = 500,
                        ErrorMessage = "{\"Error\": { \"Message\":\"Internal Server error\", \"Code\" : \"500\" } }",
                    },
                },
            };

        private readonly IEnumerable<List<SentNotificationDataEntity>> sentNotificationDataEmptyList = new List<List<SentNotificationDataEntity>>();

        /// <summary>
        /// Constructor test for all parameters.
        /// </summary>
        [Fact]
        public void DataStreamFacadeInstanceCreation_AllParameters_ShouldBeSuccess()
        {
            // Arrange
            Action action = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, this.userDataRepository.Object, this.userTypeService.Object, this.usersService.Object, this.localizer.Object);

            // Act and Assert.
            action.Should().NotThrow();
        }

        /// <summary>
        /// Constructor test for null parameters.
        /// </summary>
        [Fact]
        public void CreateInstance_NullParamters_ThrowsArgumentNullException()
        {
            // Arrange
            Action action1 = () => new DataStreamFacade(null /*sentNotificationDataRepository*/, this.teamDataRepository.Object, this.userDataRepository.Object, this.userTypeService.Object, this.usersService.Object, this.localizer.Object);
            Action action2 = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, null /*teamDataRepository*/, this.userDataRepository.Object, this.userTypeService.Object, this.usersService.Object, this.localizer.Object);
            Action action3 = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, this.userDataRepository.Object, this.userTypeService.Object, null /*usersService*/, this.localizer.Object);
            Action action4 = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, this.userDataRepository.Object, this.userTypeService.Object, this.usersService.Object, null /*localizer*/);
            Action action5 = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, null /*userDataRepository*/, this.userTypeService.Object, this.usersService.Object, this.localizer.Object);
            Action action6 = () => new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, this.userDataRepository.Object, null /*userTypeService*/, this.usersService.Object, this.localizer.Object);

            // Act and Assert.
            action1.Should().Throw<ArgumentNullException>("sentNotificationDataRepository is null.");
            action2.Should().Throw<ArgumentNullException>("teamDataRepository is null.");
            action3.Should().Throw<ArgumentNullException>("usersService is null.");
            action4.Should().Throw<ArgumentNullException>("localizer is null.");
            action5.Should().Throw<ArgumentNullException>("userDataRepository is null.");
            action6.Should().Throw<ArgumentNullException>("userTypeService is null.");
        }

        /// <summary>
        /// Test case to check if method handles null parameters.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetUserData_NullParameter_ThrowsAgrumentNullException()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();

            // Act
            Func<Task> task = async () => await activityInstance.GetTeamDataStreamAsync(null /*notificationId*/, "foo" /*notificationStatus*/).ForEachAsync(x => x.ToList());
            Func<Task> task1 = async () => await activityInstance.GetTeamDataStreamAsync("123" /*notificationId*/, null /*notificationStatus*/).ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().ThrowAsync<ArgumentNullException>("notificationId is null");
            await task1.Should().ThrowAsync<ArgumentNullException>("notificationStatus is null");
        }

        /// <summary>
        /// Test case to check if GetBatchByUserIds method is called at-least once based on GetStreamsService Response.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_BatchByUserIdsSevice_ShouldInvokeAtleastOnce()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userData = this.GetUserDataList();

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UserDataEntity());

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userData);

            // Act
            var userDataStream = activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus);

            await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            this.usersService.Verify(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Test case to check if GetBatchByUserIds method is never called as GetStreamsService returns empty response.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_BatchByUserIdsSevice_ShouldNeverBeInvokedForEmptysentNotificationData()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userData = this.GetUserDataList();

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataEmptyList.ToAsyncEnumerable());

            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userData);

            // Act
            var userDataStream = activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus);

            await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            this.usersService.Verify(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()), Times.Never);
        }

        /// <summary>
        /// Test case to check if GetBatchByUserIds method is never called as there is only recipientnotfound status in response.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetUserData_RecipientNotFound_ShouldNeverInvokeBatchByUserIds()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userData = this.GetUserDataList();

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithRecipientNotFound.ToAsyncEnumerable());

            // Act
            var userDataStream = activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus);

            await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            this.usersService.Verify(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()), Times.Never);
        }

        /// <summary>
        /// Test case to check if userdata object mapping is correct.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetUsersData_CorrectMapping_ReturnsUserDataObject()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userDataList = this.GetUserDataList();

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());
            var sendNotificationData = this.sentNotificationDataList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UserDataEntity());
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userDataList);
            string adminConsentError = "AdminConsentError";
            var localizedString = new LocalizedString(adminConsentError, adminConsentError);
            this.localizer.Setup(_ => _[adminConsentError]).Returns(localizedString);

            string succeeded = "Succeeded";
            var deliveryStatus = new LocalizedString(succeeded, succeeded);
            this.localizer.Setup(_ => _[succeeded]).Returns(deliveryStatus);

            string ok = "OK";
            var result = new LocalizedString(ok, ok);
            this.localizer.Setup(_ => _[ok]).Returns(result);

            var userType = "Member";
            var userTypeString = new LocalizedString(userType, userType);
            this.localizer.Setup(_ => _[userType]).Returns(userTypeString);

            // Act
            var userDataStream = await activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var userData = userDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();
            var user = userDataList.FirstOrDefault(user => user != null && user.Id.Equals(sendNotificationData.RowKey));

            // Assert
            Assert.Equal(userData.Id, sendNotificationData.RowKey);
            Assert.Equal(userData.Name, user.DisplayName);
            Assert.Equal(userData.Upn, user.UserPrincipalName);
            Assert.Equal(userData.UserType, userTypeString.Value);
            Assert.Equal(userData.DeliveryStatus, deliveryStatus.Value);
            Assert.Equal(userData.StatusReason, $"{sendNotificationData.StatusCode} : {result.Value}");
            Assert.Equal(userData.Error, sendNotificationData.Exception);
        }

        /// <summary>
        /// Test case to check that return userData object contains AdminConsentError for name and upn.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_ForbiddenGraphPermission_ReturnsAdminConsentError()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userDataList = new List<User>();
            var error = new Graph.Error()
            {
                Code = HttpStatusCode.Forbidden.ToString(),
                Message = "UnAuthorized",
            };
            var forbiddenException = new ServiceException(error, null, HttpStatusCode.Forbidden);

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UserDataEntity());
            var sendNotificationData = this.sentNotificationDataWithErrorList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ThrowsAsync(forbiddenException);
            string adminConsentError = "AdminConsentError";
            var localizedString = new LocalizedString(adminConsentError, adminConsentError);
            this.localizer.Setup(_ => _[adminConsentError]).Returns(localizedString);

            // Act
            var userDataStream = await activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var userData = userDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.Equal(userData.Name, adminConsentError);
            Assert.Equal(userData.Upn, adminConsentError);
            Assert.Equal(userData.UserType, adminConsentError);
        }

        /// <summary>
        /// Test case to check when user is deleted from the Tenant and from the User Data Table should return emty record.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_UserDeletedFromTenant_ReturnsEmptyRecord()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userDataList = new List<User>();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(default(UserDataEntity));
            var sendNotificationData = this.sentNotificationDataWithErrorList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userDataList);

            // Act
            var userDataStream = await activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var userData = userDataStream.FirstOrDefault().FirstOrDefault();

            // Assert
            Assert.Null(userData.Name);
            Assert.Null(userData.Upn);
            Assert.Null(userData.UserType);
        }

        /// <summary>
        /// Test case to check when user is deleted from the Tenant and not from the User Data Table should return emty record.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_UserResponseNullFromGraph_ReturnsEmptyRecord()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            List<User> userDataList = null;
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UserDataEntity() { UserType = UserType.Member });
            var sendNotificationData = this.sentNotificationDataWithErrorList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userDataList);
            string userType = UserType.Member;
            var localizedString = new LocalizedString(userType, userType);
            this.localizer.Setup(_ => _[userType]).Returns(localizedString);

            // Act
            var userDataStream = await activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var userData = userDataStream.FirstOrDefault().FirstOrDefault();

            // Assert
            Assert.Null(userData.Name);
            Assert.Null(userData.Upn);
            Assert.Equal(userData.UserType, UserType.Member);
        }

        /// <summary>
        /// Test case to check that return userdata object's statusReason is with error from sendNotificationData.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_UserStatusReason_withErrorStatus()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var userDataList = this.GetUserDataList();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());
            var sendNotificationData = this.sentNotificationDataWithErrorList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.userDataRepository
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UserDataEntity());
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ReturnsAsync(userDataList);

            // Get ErrorMessage
            var rootMessage = JsonConvert.DeserializeObject<RootErrorMessage>(sendNotificationData.ErrorMessage);
            var result = rootMessage.Error.Message;

            // Act
            var userDataStream = await activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var userData = userDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.Equal(userData.StatusReason, $"{sendNotificationData.StatusCode} : {result}");
        }

        /// <summary>
        /// Test case to check if GetAsync method(to get team data) is called atleast once based on response from GetStreamsAsync.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_TeamDataSevice_ShouldInvokeAtleastOnce()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var teamData = this.GetTeamDataEntity();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());

            string succeeded = "Succeeded";
            var deliveryStatusString = new LocalizedString(succeeded, succeeded);
            this.localizer.Setup(_ => _[succeeded]).Returns(deliveryStatusString);
            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamData);

            // Act
            var teamDataStream = activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus);
            await teamDataStream.ForEachAsync(x => x.ToList());

            // Assert
            this.teamDataRepository.Verify(x => x.GetAsync(It.Is<string>(x => x.Equals(TeamDataTableNames.TeamDataPartition)), It.IsAny<string>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Test case to check if GetAsync method(to get team data) is never called as GetStreamsService returns empty list.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_TeamDataSevice_ShouldNeverBeInvokedForEmptysentNotificationData()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var teamData = new TeamDataEntity();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataEmptyList.ToAsyncEnumerable());

            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamData);

            // Act
            var teamDataStream = activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus);
            await teamDataStream.ForEachAsync(x => x.ToList());

            // Assert
            this.teamDataRepository.Verify(x => x.GetAsync(It.Is<string>(x => x.Equals(TeamDataTableNames.TeamDataPartition)), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test case to check if teamdata object mapping is correct.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task GetTeamData_CorrectMapping_ReturnsTeamDataObject()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var teamDataEntity = this.GetTeamDataEntity();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());
            var sendNotificationData = this.sentNotificationDataList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();

            string notificationDeliveryStatus = sendNotificationData.DeliveryStatus;
            var deliveryStatus = new LocalizedString(notificationDeliveryStatus, notificationDeliveryStatus);
            this.localizer.Setup(_ => _[notificationDeliveryStatus]).Returns(deliveryStatus);

            string ok = "OK";
            var result = new LocalizedString(ok, ok);
            this.localizer.Setup(_ => _[ok]).Returns(result);
            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamDataEntity);

            // Act
            var teamDataStream = await activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var teamData = teamDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.Equal(teamData.Id, sendNotificationData.RowKey);
            Assert.Equal(teamData.Name, teamDataEntity.Name);
            Assert.Equal(teamData.DeliveryStatus, deliveryStatus.Value);
            Assert.Equal(teamData.StatusReason, $"{sendNotificationData.StatusCode} : {result.Value}");
            Assert.Equal(teamData.Error, sendNotificationData.Exception);
        }

        /// <summary>
        /// Test case to check that return teamdata object is not null and contains deleveryStatus from sendNotificationData.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_TeamDeliveryStatus_SucceededFromNotificationData()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var teamDataEntity = this.GetTeamDataEntity();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());
            var sendNotificationData = this.sentNotificationDataList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();

            string deliveryStatus = sendNotificationData.DeliveryStatus;
            var notificationDeliveryStatus = new LocalizedString(deliveryStatus, deliveryStatus);
            this.localizer.Setup(_ => _[deliveryStatus]).Returns(notificationDeliveryStatus);

            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamDataEntity);

            // Act
            var teamDataStream = await activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var teamData = teamDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.NotNull(teamData);
            Assert.Equal(teamData.DeliveryStatus, deliveryStatus);
        }

        /// <summary>
        /// Test case to check that return teamdata object's statusReason is with error from sendNotificationData.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_TeamStatusReason_ReturnsErrorWithStatusReasonFromNotificationData()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            var teamDataEntity = this.GetTeamDataEntity();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());
            var sendNotificationData = this.sentNotificationDataWithErrorList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(teamDataEntity);

            // Get ErrorMessage
            var rootMessage = JsonConvert.DeserializeObject<RootErrorMessage>(sendNotificationData.ErrorMessage);
            var result = rootMessage.Error.Message;

            // Act
            var teamDataStream = await activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var teamData = teamDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.NotNull(teamData);
            Assert.Equal(teamData.StatusReason, $"{sendNotificationData.StatusCode} : {result}");
        }

        /// <summary>
        /// Test case to check that return teamdata object contains name as null.
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_NullFromDownStream_ReturnsNullForTeamName()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();
            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataWithErrorList.ToAsyncEnumerable());

            var sendNotificationData = this.sentNotificationDataList.Select(x => x.Where(y => y.RowKey == "RowKey").FirstOrDefault()).FirstOrDefault();
            this.teamDataRepository.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(default(TeamDataEntity)));

            // Act
            var userDataStream = await activityInstance.GetTeamDataStreamAsync(this.notificationId, this.notificationStatus).ToListAsync();
            var teamData = userDataStream.Select(x => x.Where(y => y.Id == "RowKey").FirstOrDefault()).FirstOrDefault();

            // Assert
            Assert.Null(teamData.Name);
        }

        /// <summary>
        /// Test case to check if service exception is thrown when received GetUser service error which is not 403 (Forbidden).
        /// </summary>
        /// <returns>A task that represents the work queued to execute.</returns>
        [Fact]
        public async Task Get_CallBatchByUserIdsSevice_ThrowsServiceException()
        {
            // Arrange
            var activityInstance = this.GetDataStreamFacadeInstance();

            this.sentNotificationDataRepository
                .Setup(x => x.GetStreamsAsync(this.notificationId, null))
                .Returns(this.sentNotificationDataList.ToAsyncEnumerable());
            var serviceException = new ServiceException(null, null, HttpStatusCode.Unauthorized);
            this.usersService
                .Setup(x => x.GetBatchByUserIds(It.IsAny<IEnumerable<IEnumerable<string>>>()))
                .ThrowsAsync(serviceException);

            // Act
            var userDataStream = activityInstance.GetUserDataStreamAsync(this.notificationId, this.notificationStatus);
            Func<Task> task = async () => await userDataStream.ForEachAsync(x => x.ToList());

            // Assert
            await task.Should().ThrowAsync<ServiceException>();
        }

        private TeamDataEntity GetTeamDataEntity()
        {
            return new TeamDataEntity() { Name = "teamName" };
        }

        private IEnumerable<User> GetUserDataList()
        {
            return new List<User>()
            {
                new User()
                {
                    Id = "RowKey", DisplayName = "UserDisplyName", UserPrincipalName = "UserPrincipalName", UserType = "Member",
                },
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamFacade"/> class.
        /// </summary>
        /// <returns>return the instance of DataStreamFacade.</returns>
        private DataStreamFacade GetDataStreamFacadeInstance()
        {
            return new DataStreamFacade(this.sentNotificationDataRepository.Object, this.teamDataRepository.Object, this.userDataRepository.Object, this.userTypeService.Object, this.usersService.Object, this.localizer.Object);
        }
    }
}