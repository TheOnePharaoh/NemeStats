﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DataAccess;
using BusinessLogic.Logic.PlayedGames;
using BusinessLogic.Models;
using BusinessLogic.Models.PlayedGames;
using BusinessLogic.Models.User;
using NUnit.Framework;
using Rhino.Mocks;
using StructureMap.AutoMocking;

namespace BusinessLogic.Tests.UnitTests.LogicTests.PlayedGamesTests.PlayedGameRetrieverTests
{
    [TestFixture]
    public class SearchPlayedGamesTests
    {
        private RhinoAutoMocker<PlayedGameRetriever> autoMocker;
        private List<PlayedGame> playedGames;
        private const int PLAYED_GAME_ID_FOR_GAME_RECORDED_IN_MARCH = 1;
        private const int PLAYED_GAME_ID_FOR_GAME_RECORDED_IN_APRIL = 2;
        private const int EXPECTED_GAMING_GROUP_ID = 30;
        private const int EXPECTED_GAME_DEFINITION_ID = 51;
            
        [SetUp]
        public void SetUp()
        {
            autoMocker = new RhinoAutoMocker<PlayedGameRetriever>();

            playedGames = new List<PlayedGame>
            {
                new PlayedGame
                {
                    Id = PLAYED_GAME_ID_FOR_GAME_RECORDED_IN_MARCH,
                    DateCreated = new DateTime(2015, 3, 1, 4, 4, 4),
                    PlayerGameResults = new List<PlayerGameResult>(),
                    GameDefinition = new GameDefinition(),
                    GamingGroup = new GamingGroup(),
                    GamingGroupId = EXPECTED_GAMING_GROUP_ID,
                    GameDefinitionId = EXPECTED_GAME_DEFINITION_ID
                },
                new PlayedGame
                {
                    Id = PLAYED_GAME_ID_FOR_GAME_RECORDED_IN_APRIL,
                    DateCreated = new DateTime(2015, 4, 1, 5, 5, 5),
                    PlayerGameResults = new List<PlayerGameResult>(),
                    GameDefinition = new GameDefinition(),
                    GamingGroup = new GamingGroup(),
                    GamingGroupId = 135353
                }
            };

            autoMocker.Get<IDataContext>().Expect(mock => mock.GetQueryable<PlayedGame>()).Return(playedGames.AsQueryable());
        }

        [Test]
        public void ItSetsAllTheFields()
        {
            autoMocker = new RhinoAutoMocker<PlayedGameRetriever>();

            var playedGame = new PlayedGame
            {
                Id = 1,
                DateCreated = new DateTime(2015, 11, 1, 2, 2, 2),
                DatePlayed = new DateTime(2015, 10, 1, 3, 3, 3),
                GameDefinitionId = 2,
                GamingGroupId = 3,
                Notes = "some notes",
                GamingGroup = new GamingGroup
                {
                    Name = "some gaming group name"
                },
                GameDefinition = new GameDefinition
                {
                    Name = "some game definition name",
                    BoardGameGeekObjectId = 4
                }
            };

            var playerGameResult = new PlayerGameResult
            {
                GameRank = 1,
                PlayerId = 2,
                PointsScored = 50,
                Player = new Player
                {
                    Id = 100,
                    Name = "some player name"
                },
                PlayedGame = playedGame
            };

            var playerGameResults = new List<PlayerGameResult>
            {
               playerGameResult
            };

            playedGame.PlayerGameResults = playerGameResults;

            playedGames = new List<PlayedGame>
            {
                playedGame
            };
            autoMocker.Get<IDataContext>().Expect(mock => mock.GetQueryable<PlayedGame>()).Return(playedGames.AsQueryable());

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(new PlayedGameFilter()).First();

            Assert.That(results.PlayedGameId, Is.EqualTo(playedGame.Id));
            Assert.That(results.GameDefinitionName, Is.EqualTo(playedGame.GameDefinition.Name));
            Assert.That(results.GameDefinitionId, Is.EqualTo(playedGame.GameDefinitionId));
            Assert.That(results.DateLastUpdated, Is.EqualTo(playedGame.DateCreated));
            Assert.That(results.DatePlayed, Is.EqualTo(playedGame.DatePlayed));
            Assert.That(results.GamingGroupId, Is.EqualTo(playedGame.GamingGroupId));
            Assert.That(results.GamingGroupName, Is.EqualTo(playedGame.GamingGroup.Name));
            Assert.That(results.Notes, Is.EqualTo(playedGame.Notes));
            var actualPlayerResult = results.PlayerGameResults[0];
            var expectedPlayerGameResult = playedGame.PlayerGameResults[0];
            Assert.That(actualPlayerResult.GameRank, Is.EqualTo(expectedPlayerGameResult.GameRank));
            Assert.That(actualPlayerResult.NemeStatsPointsAwarded, Is.EqualTo(expectedPlayerGameResult.NemeStatsPointsAwarded));
            Assert.That(actualPlayerResult.PlayerId, Is.EqualTo(expectedPlayerGameResult.PlayerId));
            Assert.That(actualPlayerResult.PlayerName, Is.EqualTo(expectedPlayerGameResult.Player.Name));
            Assert.That(actualPlayerResult.PointsScored, Is.EqualTo(expectedPlayerGameResult.PointsScored));
            Assert.That(actualPlayerResult.PlayedGameId, Is.EqualTo(expectedPlayerGameResult.PlayedGameId));
            Assert.That(actualPlayerResult.DatePlayed, Is.EqualTo(expectedPlayerGameResult.PlayedGame.DatePlayed));
            Assert.That(actualPlayerResult.GameName, Is.EqualTo(expectedPlayerGameResult.PlayedGame.GameDefinition.Name));
            Assert.That(actualPlayerResult.GameDefinitionId, Is.EqualTo(expectedPlayerGameResult.PlayedGame.GameDefinitionId));
        }

        [Test]
        public void ItReturnsAnEmptyListIfThereAreNoSearchResults()
        {
            autoMocker = new RhinoAutoMocker<PlayedGameRetriever>();
            autoMocker.Get<IDataContext>().Expect(mock => mock.GetQueryable<PlayedGame>()).Return(new List<PlayedGame>().AsQueryable());
            var results = autoMocker.ClassUnderTest.SearchPlayedGames(new PlayedGameFilter());

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test, Ignore("This test no longer works because DbFunctions.TruncateTime is not unit testable. Bummer.")]
        public void ItFiltersOnStartDateGameLastUpdated()
        {
            var filter = new PlayedGameFilter
            {
                StartDateGameLastUpdated = "2015-04-01"
            };

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(filter);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.True(results.All(x => x.DateLastUpdated.Date >= new DateTime(2015, 4, 1)));
        }

        [Test, Ignore("This test no longer works because DbFunctions.TruncateTime is not unit testable. Bummer.")]
        public void ItFiltersOnEndDateGameLastUpdated()
        {
            var filter = new PlayedGameFilter
            {
                EndDateGameLastUpdated = "2015-03-01"
            };

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(filter);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.True(results.All(x => x.DateLastUpdated.Date <= new DateTime(2015, 3, 1)));
        }

        [Test]
        public void ItLimitsSearchResultsToTheMaximumSpecified()
        {
            const int MAX_RESULTS = 1;
            var filter = new PlayedGameFilter
            {
                MaximumNumberOfResults = MAX_RESULTS
            };

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(filter);

            Assert.That(results.Count, Is.EqualTo(MAX_RESULTS));
        }

        [Test]
        public void ItFiltersOnTheGamingGroupId()
        {
            var filter = new PlayedGameFilter
            {
                GamingGroupId = EXPECTED_GAMING_GROUP_ID
            };

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(filter);

            Assert.True(results.All(result => result.GamingGroupId == filter.GamingGroupId));
        }

        [Test]
        public void ItFiltersOnTheGameDefinitionId()
        {
            var filter = new PlayedGameFilter
            {
                GameDefinitionId = EXPECTED_GAME_DEFINITION_ID
            };

            var results = autoMocker.ClassUnderTest.SearchPlayedGames(filter);

            Assert.True(results.All(result => result.GameDefinitionId == filter.GameDefinitionId));
        }
    }
}
