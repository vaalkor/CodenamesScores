using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestRealtime
{

    public class StateResult
    {
        public GameValues Values { get; set; }
        public string CorellationId { get; set; }
    }

    public class AddPlayerRequest
    {
        public string GameId { get; set; }
        public int Team { get; set; }
        public string PlayerName { get; set; }
    }

    public class UpdateTeamInfoRequest
    {
        public string GameId { get; set; }
        public int Team { get; set; }
        public List<string> RemovePlayers { get; set; }
        public string TeamName { get; set; }
    }

    public class UpdateScoreRequest
    {
        public string GameId { get; set; }
        public int Team { get; set; }
        public bool Increment { get; set; }
    }

    public struct GameValues
    { 
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }
        public string[] Team1Players { get; set; }
        public string[] Team2Players { get; set; }

        public string Team1Spymaster { get; set; }
        public string Team2Spymaster { get; set; }
        public static GameValues DefaultNew => new GameValues() { Team1Name = "Red Team", Team2Name = "Blue Team", Team1Players = new string[0], Team2Players = new string[0], Team1Spymaster = "", Team2Spymaster = "" };
    }

    public class GameState
    {
        public object Mutex { get; } = new object();
        public DateTime LastUpdate { get; private set; } = DateTime.Now;
        public TaskCompletionSource<GameValues> GameUpdated { get; private set; } = new TaskCompletionSource<GameValues>();
        public string CorrelationId { get; private set; } = Guid.NewGuid().ToString();
        private GameValues _values = GameValues.DefaultNew;

        public GameValues GetValues()
        {
            lock (Mutex)
            {
                return _values;
            }
        }

        public bool AddPlayer(int team, string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return false;
            lock (Mutex)
            {
                int delta = 0;
                if (team == 1 && !_values.Team1Players.Any(x => x.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                {
                    int currentCount = _values.Team1Players.Length;
                    _values.Team1Players = _values.Team1Players.Append(playerName).ToArray();
                    Update();
                    delta = _values.Team1Players.Length - currentCount;
                }

                if (team == 2 && !_values.Team2Players.Any(x => x.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                {
                    int currentCount = _values.Team2Players.Length;
                    _values.Team2Players = _values.Team2Players.Append(playerName).ToArray();
                    Update();
                    delta = _values.Team1Players.Length - currentCount;
                }
                return delta > 0;
            }
        }

        public void UpdateScore(int team, bool increment)
        {
            lock (Mutex)
            {
                if (team == 1)
                {
                    if (increment) _values.Team1Score += 1;
                    else _values.Team1Score -= 1;
                    Update();
                }

                if (team == 2)
                {
                    if (increment) _values.Team2Score += 1;
                    else _values.Team2Score -= 1;
                    Update();
                }
            }
        }

        public void UpdateTeamInfo(UpdateTeamInfoRequest request)
        {
            lock (Mutex)
            {
                if (request.Team == 1)
                {
                    _values.Team1Name = request.TeamName;
                    _values.Team1Players = _values.Team1Players.Where(x => !request.RemovePlayers.Any(y => y.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();
                    Update();
                }

                if (request.Team == 2)
                {
                    _values.Team2Name = request.TeamName;
                    _values.Team2Players = _values.Team2Players.Where(x => !request.RemovePlayers.Any(y => y.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();
                    Update();
                }
            }
        }

        public void SetValues(GameValues values)
        {
            lock (Mutex)
            {
                _values = values;
                Update();
            }
        }

        private void Update()
        {
            LastUpdate = DateTime.Now;
            GameUpdated.SetResult(_values);
            GameUpdated = new TaskCompletionSource<GameValues>();
            CorrelationId = Guid.NewGuid().ToString();
        }
    }

    public static class GameStates
    {
        public static object Mutex { get; } = new object();

        public static ConcurrentDictionary<string, GameState> Cache = new ConcurrentDictionary<string, GameState>();
    }
}
