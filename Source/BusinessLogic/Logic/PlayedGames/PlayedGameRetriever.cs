﻿#region LICENSE
// NemeStats is a free website for tracking the results of board games.
//     Copyright (C) 2015 Jacob Gordon
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>
#endregion

using BusinessLogic.DataAccess;
using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using BusinessLogic.Models.Games;
using BusinessLogic.Models.PlayedGames;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace BusinessLogic.Logic.PlayedGames
{
    public class PlayedGameRetriever : IPlayedGameRetriever
    {
        private readonly IDataContext dataContext;

        public PlayedGameRetriever(IDataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public List<PlayedGame> GetRecentGames(int numberOfGames, int gamingGroupId)
        {
            List<PlayedGame> playedGames = dataContext.GetQueryable<PlayedGame>()
                .Where(game => game.GamingGroupId == gamingGroupId)
                .Include(playedGame => playedGame.GameDefinition)
                .Include(playedGame => playedGame.GamingGroup)
                .Include(playedGame => playedGame.PlayerGameResults
                    .Select(playerGameResult => playerGameResult.Player))
                    .OrderByDescending(orderBy => orderBy.DatePlayed)
                    .ThenByDescending(orderBy => orderBy.DateCreated)
                .Take(numberOfGames)
                .ToList();

            //TODO this seems ridiculous but I can't see how to order a related entity in Entity Framework :(
            foreach (PlayedGame playedGame in playedGames)
            {
                playedGame.PlayerGameResults = playedGame.PlayerGameResults.OrderBy(orderBy => orderBy.GameRank).ToList();
            }

            return playedGames;
        }


        public PlayedGame GetPlayedGameDetails(int playedGameId)
        {
            PlayedGame result = dataContext.GetQueryable<PlayedGame>()
                .Where(playedGame => playedGame.Id == playedGameId)
                    .Include(playedGame => playedGame.GameDefinition)
                    .Include(playedGame => playedGame.GamingGroup)
                    .Include(playedGame => playedGame.PlayerGameResults
                        .Select(playerGameResult => playerGameResult.Player))
                    .FirstOrDefault();

            if (result == null)
            {
                throw new EntityDoesNotExistException(typeof(PlayedGame), playedGameId);
            }

            result.PlayerGameResults = result.PlayerGameResults.OrderBy(playerGameResult => playerGameResult.GameRank).ToList();

            return result;
        }

        public List<PublicGameSummary> GetRecentPublicGames(int numberOfGames)
        {
            return (from playedGame in dataContext.GetQueryable<PlayedGame>()
                        .OrderByDescending(game => game.DatePlayed)
                        .ThenByDescending(game => game.DateCreated)
                    select new PublicGameSummary
             {
                 PlayedGameId = playedGame.Id,
                 GameDefinitionId = playedGame.GameDefinitionId,
                 GameDefinitionName = playedGame.GameDefinition.Name,
                 GamingGroupId = playedGame.GamingGroupId,
                 GamingGroupName = playedGame.GamingGroup.Name,
                 WinnerType = (playedGame.PlayerGameResults.All(x => x.GameRank == 1) ? WinnerTypes.TeamWin :
                                playedGame.PlayerGameResults.All(x => x.GameRank != 1) ? WinnerTypes.TeamLoss :
                                WinnerTypes.PlayerWin),
                 WinningPlayer = playedGame.PlayerGameResults.FirstOrDefault(player => player.GameRank == 1).Player,
                 DatePlayed = playedGame.DatePlayed
             }).Take(numberOfGames)
                                .ToList();
        }

        public List<PlayedGameSearchResult> SearchPlayedGames(PlayedGameFilter playedGameFilter)
        {
            var queryable = (from playedGame in dataContext.GetQueryable<PlayedGame>()
                                                           .OrderByDescending(game => game.DatePlayed)
                                                           .ThenByDescending(game => game.DateCreated)
                             select new PlayedGameSearchResult
                             {
                                 PlayedGameId = playedGame.Id,
                                 GameDefinitionId = playedGame.GameDefinitionId,
                                 GameDefinitionName = playedGame.GameDefinition.Name,
                                 BoardGameGeekObjectId = playedGame.GameDefinition.BoardGameGeekObjectId,
                                 GamingGroupId = playedGame.GamingGroupId,
                                 GamingGroupName = playedGame.GamingGroup.Name,
                                 Notes = playedGame.Notes,
                                 DatePlayed = playedGame.DatePlayed,
                                 DateLastUpdated = playedGame.DateCreated,
                                 PlayerGameResults = playedGame.PlayerGameResults.Select(x => new PlayerResult
                                 {
                                     GameRank = x.GameRank,
                                     NemeStatsPointsAwarded = x.NemeStatsPointsAwarded,
                                     PlayerId = x.PlayerId,
                                     PlayerName = x.Player.Name,
                                     PointsScored = x.PointsScored,
                                     DatePlayed = x.PlayedGame.DatePlayed,
                                     GameDefinitionId = x.PlayedGame.GameDefinitionId,
                                     GameName = x.PlayedGame.GameDefinition.Name,
                                     PlayedGameId = x.PlayedGameId
                                 }).ToList()
                             });


            queryable = AddSearchCriteria(playedGameFilter, queryable);

            var results = queryable.ToList();

            SortPlayerResultsWithinEachSearchResult(results);

            return results;
        }

        private static void SortPlayerResultsWithinEachSearchResult(List<PlayedGameSearchResult> results)
        {
            foreach (var playedGameSearchResults in results)
            {
                playedGameSearchResults.PlayerGameResults = playedGameSearchResults.PlayerGameResults.OrderBy(x => x.GameRank).ToList();
            }
        }

        private static IQueryable<PlayedGameSearchResult> AddSearchCriteria(PlayedGameFilter playedGameFilter, IQueryable<PlayedGameSearchResult> queryable)
        {
            if (playedGameFilter.GamingGroupId.HasValue)
            {
                queryable = queryable.Where(query => query.GamingGroupId == playedGameFilter.GamingGroupId.Value);
            }

            if (playedGameFilter.GameDefinitionId.HasValue)
            {
                queryable = queryable.Where(query => query.GameDefinitionId == playedGameFilter.GameDefinitionId.Value);
            }

            if (!string.IsNullOrEmpty(playedGameFilter.StartDateGameLastUpdated))
            {
                DateTime startDate = DateTime.ParseExact(playedGameFilter.StartDateGameLastUpdated, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                queryable = queryable.Where(query => DbFunctions.TruncateTime(query.DateLastUpdated) >= startDate.Date);
            }

            if (!string.IsNullOrEmpty(playedGameFilter.EndDateGameLastUpdated))
            {
                DateTime endDate = DateTime.ParseExact(playedGameFilter.EndDateGameLastUpdated, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                queryable = queryable.Where(query => DbFunctions.TruncateTime(query.DateLastUpdated) <= endDate.Date);
            }

            if (playedGameFilter.MaximumNumberOfResults.HasValue)
            {
                queryable = queryable.Take(playedGameFilter.MaximumNumberOfResults.Value);
            }
            return queryable;
        }
    }
}
