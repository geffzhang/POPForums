﻿using System;
using Moq;
using PopForums.Feeds;
using PopForums.Models;
using PopForums.Repositories;
using PopForums.ScoringGame;
using PopForums.Services;
using Xunit;

namespace PopForums.Test.ScoringGame
{
	public class EventPublisherTests
	{
		private EventPublisher GetPublisher()
		{
			_eventDefService = new Mock<IEventDefinitionService>();
			_pointLedgerRepo = new Mock<IPointLedgerRepository>();
			_feedService = new Mock<IFeedService>();
			_awardCalc = new Mock<IAwardCalculator>();
			_profileService = new Mock<IProfileService>();
			return new EventPublisher(_eventDefService.Object, _pointLedgerRepo.Object, _feedService.Object, _awardCalc.Object, _profileService.Object);
		}

		private Mock<IEventDefinitionService> _eventDefService;
		private Mock<IPointLedgerRepository> _pointLedgerRepo;
		private Mock<IFeedService> _feedService;
		private Mock<IAwardCalculator> _awardCalc;
		private Mock<IProfileService> _profileService;

		[Fact]
		public void ProcessEventPublishesToLedger()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition {EventDefinitionID = "blah", PointValue = 42};
			const string message = "msg";
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			var entry = new PointLedgerEntry();
			_pointLedgerRepo.Setup(x => x.RecordEntry(It.IsAny<PointLedgerEntry>())).Callback<PointLedgerEntry>(x => entry = x);
			publisher.ProcessEvent(message, user, eventDef.EventDefinitionID, false);
			Assert.Equal(user.UserID, entry.UserID);
			Assert.Equal(eventDef.EventDefinitionID, entry.EventDefinitionID);
			Assert.Equal(eventDef.PointValue, entry.Points);
		}

		[Fact]
		public void ProcessEventPublishesToFeedService()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42, IsPublishedToFeed = true };
			const string message = "msg";
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent(message, user, eventDef.EventDefinitionID, false);
			_feedService.Verify(x => x.PublishToFeed(user, message, eventDef.PointValue, It.IsAny<DateTime>()), Times.Once());
		}

		[Fact]
		public void ProcessEventPublishesToFeedServiceForActivity()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42, IsPublishedToFeed = true };
			const string message = "msg";
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent(message, user, eventDef.EventDefinitionID, false);
			_feedService.Verify(x => x.PublishToActivityFeed(message), Times.Once());
		}

		[Fact]
		public void ProcessEventDoesNotPublishToFeedServiceForActivityWhenEventDefSaysNo()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42, IsPublishedToFeed = false };
			const string message = "msg";
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent(message, user, eventDef.EventDefinitionID, false);
			_feedService.Verify(x => x.PublishToActivityFeed(message), Times.Never());
		}

		[Fact]
		public void ProcessEventDoesNotPublishToFeedServiceWhenEventDefSaysNo()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42, IsPublishedToFeed = false };
			const string message = "msg";
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent(message, user, eventDef.EventDefinitionID, false);
			_feedService.Verify(x => x.PublishToFeed(user, message, eventDef.PointValue, It.IsAny<DateTime>()), Times.Never());
		}

		[Fact]
		public void ProcessEventCallsCalculator()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42 };
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent("msg", user, eventDef.EventDefinitionID, false);
			_awardCalc.Verify(x => x.QueueCalculation(user, eventDef), Times.Once());
		}

		[Fact]
		public void ProcessEventUpdatesProfilePointTotal()
		{
			var user = new User(123, DateTime.MinValue);
			var eventDef = new EventDefinition { EventDefinitionID = "blah", PointValue = 42 };
			var publisher = GetPublisher();
			_eventDefService.Setup(x => x.GetEventDefinition(eventDef.EventDefinitionID)).Returns(eventDef);
			publisher.ProcessEvent("msg", user, eventDef.EventDefinitionID, false);
			_profileService.Verify(x => x.UpdatePointTotal(user), Times.Once());
		}

		[Fact]
		public void ProcessManualEventPublishesToLedger()
		{
			var user = new User(123, DateTime.MinValue);
			const string message = "msg";
			const int points = 252;
			var publisher = GetPublisher();
			var entry = new PointLedgerEntry();
			_pointLedgerRepo.Setup(x => x.RecordEntry(It.IsAny<PointLedgerEntry>())).Callback<PointLedgerEntry>(x => entry = x);
			publisher.ProcessManualEvent(message, user, points);
			Assert.Equal(user.UserID, entry.UserID);
			Assert.Equal("Manual", entry.EventDefinitionID);
			Assert.Equal(points, entry.Points);
		}

		[Fact]
		public void ProcessManualEventPublishesToFeedService()
		{
			var user = new User(123, DateTime.MinValue);
			const string message = "msg";
			const int points = 252;
			var publisher = GetPublisher();
			publisher.ProcessManualEvent(message, user, points);
			_feedService.Verify(x => x.PublishToFeed(user, message, points, It.IsAny<DateTime>()), Times.Once());
		}

		[Fact]
		public void ProcessManualEventUpdatesProfilePointTotal()
		{
			var user = new User(123, DateTime.MinValue);
			var publisher = GetPublisher();
			publisher.ProcessManualEvent("msg", user, 252);
			_profileService.Verify(x => x.UpdatePointTotal(user), Times.Once());
		}
	}
}
