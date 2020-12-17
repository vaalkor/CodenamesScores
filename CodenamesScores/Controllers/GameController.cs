using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace TestRealtime.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        [HttpGet]
        [Route("state")]
        public async Task<ActionResult<StateResult>> GetState([FromQuery] string gameId, [FromQuery] string correlationId)
        {
            if (string.IsNullOrEmpty(gameId)) return new NotFoundResult(); 
            
            var foundGame = GameStates.Cache.TryGetValue(gameId, out GameState game);

            if (!foundGame) return new NotFoundResult();

            if (string.IsNullOrEmpty(correlationId))
            {
                return new StateResult() { CorellationId = game.CorrelationId, Values = game.GetValues() };
            }

            Task waitOn;

            if (correlationId != game.CorrelationId)
            {
                lock (game.Mutex) { return new StateResult() { CorellationId = game.CorrelationId, Values = game.GetValues() }; }
            }
            else 
            {
                var delayTask = Task.Delay(1000 * 60);
                waitOn = Task.WhenAny(delayTask, game.GameUpdated.Task);
            }

            await waitOn;
            lock (game.Mutex) { return new StateResult() { CorellationId = game.CorrelationId, Values = game.GetValues() }; }
        }

        [HttpPost]
        [Route("state/score")]
        public ActionResult UpdateScore([FromBody] UpdateScoreRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId)) return new NotFoundResult();

            var foundGame = GameStates.Cache.TryGetValue(request.GameId, out GameState game);

            if (!foundGame) return new NotFoundResult();

            game.UpdateScore(request.Team, request.Increment);

            return Ok();
        }

        [HttpPost]
        [Route("state/teaminfo")]
        public ActionResult UpdateTeamInfo([FromBody] UpdateTeamInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId)) return new NotFoundResult();

            var foundGame = GameStates.Cache.TryGetValue(request.GameId, out GameState game);

            if (!foundGame) return new NotFoundResult();

            game.UpdateTeamInfo(request);

            return Ok();
        }

        [HttpPost]
        [Route("state/player")]
        public ActionResult AddPlayer([FromBody] AddPlayerRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId)) return new NotFoundResult();

            var foundGame = GameStates.Cache.TryGetValue(request.GameId, out GameState game);

            if (!foundGame) return new NotFoundResult();

            var wasAdded = game.AddPlayer(request.Team, request.PlayerName);
            if (wasAdded) return Ok();
            else return BadRequest();
        }

        [HttpGet]
        [Route("new-game")]
        public ActionResult<string> CreateGame()
        {
            string id = Guid.NewGuid().ToString();

            GameState newState = new GameState();
            GameStates.Cache.TryAdd(id, newState);

            return id;
        }

    }
}
