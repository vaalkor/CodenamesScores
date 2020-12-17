'use strict'

var _gameId = "";
var _gameState = {};
var _teamMap = {1: "team1", 2: "team2"};
var _playersToRemove = [];
var _configTeam = 0;

function getState()
{
    let req = new XMLHttpRequest();
    req.setRequestHeader
    req.addEventListener("load", function x() {
        if (this.status == 200) {
            console.log(this.response);
            _gameState = JSON.parse(this.response);
            renderState();
        }

        if (this.status == 404)
        {
            alert("Cannot find game or something. You've messed it up. go home mate.");
        }
        else
        {
            setTimeout(() => { getState() }, 50);
        } 
    });
    req.open("GET", `game/state?gameId=${_gameId}&correlationId=${_gameState.corellationId || ""}`);
    req.send();
}

function renderState()
{
    function addPlayerList(playerListElement, playerList)
    {
        [...playerListElement.children].filter(x=>x.nodeName=="LI").forEach(n=>playerListElement.removeChild(n));
        var insertBeforeElem = playerListElement.querySelector(".input-group"); //Theres an element at the end that we don't want to remove. We'll insert before it...
        playerList.forEach((x) => {
            let newElem = document.createElement("li");
            newElem.classList.add("list-group-item");
            newElem.innerText = x;
            playerListElement.insertBefore(newElem, insertBeforeElem);
        });
    };

    document.getElementById("team1-score").innerText = `Score: ${_gameState.values.team1Score}`;
    document.getElementById("team2-score").innerText = `Score: ${_gameState.values.team2Score}`;
    document.getElementById("team1-name").innerText = _gameState.values.team1Name;
    document.getElementById("team2-name").innerText = _gameState.values.team2Name;
    addPlayerList(document.getElementById("team1-players"), _gameState.values.team1Players);
    addPlayerList(document.getElementById("team2-players"), _gameState.values.team2Players);
}

function onShowModal(team)
{
    let players = null
    if(!(team === 1 || team === 2)){alert("Team should be 1 or 2. Something wrong here. Get out."); return;} 
    if(team === 1) players = _gameState.values.team1Players;
    if(team === 2) players = _gameState.values.team2Players;
    _configTeam = team;
    _playersToRemove = [];
    
    var teamName = document.getElementById(`team${team}-name`).innerText;
    document.getElementById( "configure-team-name").value = teamName;
    var removePlayersElem = document.getElementById( "remove-players");
    removePlayersElem.innerHTML = "";
    players.forEach((x)=>{
        let elem = document.createElement("div");
        elem.innerHTML = `<button type="button" onclick="onRemoveButtonClicked(this, '${x}')" class="btn btn-danger mr-1">${x}</button>`;
        removePlayersElem.appendChild(elem);
    });

    $(`#configTeamModal`).modal('show')
}

function onSubmitConfigChanges()
{
    let req = new XMLHttpRequest();
    req.open("POST", `game/state/teaminfo`);
    req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    req.send(JSON.stringify({GameId: _gameId, Team: _configTeam, RemovePlayers: _playersToRemove, TeamName: document.getElementById( "configure-team-name").value}));

    $(`#configTeamModal`).modal('hide')
}

function onRemoveButtonClicked(button, playerName)
{
    _playersToRemove.push(playerName);
    button.parentElement.parentElement.removeChild(button.parentElement);
}

function onAddPlayer(team)
{
    if(!(team === 1 || team === 2)){alert("Team should be 1 or 2. Something wrong here. Get out."); return;} 
    var playerName = document.getElementById(`${_teamMap[team]}-addplayer`).value;

    let req = new XMLHttpRequest();
    req.open("POST", `game/state/player`);
    req.addEventListener("load", function x() {
        if (this.status == 200) {
            document.getElementById(`team${team}-addplayer`).value = "";
        }
    });
    req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    req.send(JSON.stringify({GameId: _gameId, Team: team, PlayerName: playerName}));
}

function onUpdateScore(team, increment)
{
    increment = !!increment;
    if(!(team === 1 || team === 2)){alert("Team should be 1 or 2. Something wrong here. Get out."); return;} 

    let req = new XMLHttpRequest();
    req.open("POST", `game/state/score`);

    req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    req.send(JSON.stringify({ GameId: _gameId, Team: team, Increment: increment}));
}

function parseQuery(url){
    let object = {};
    let split = url.split("?");
    if(split.length == 1) return object;

    let queryFields = url.split("?")[1].split("&");
    
    for(let i=0;i<queryFields.length; i++)
    {
        let splitField = queryFields[i].split("=");
        object[splitField[0]] = splitField[1];
    }
    return object;
}

function onPageReady()
{
    let parsedQuery = parseQuery(document.URL);
    if(parsedQuery.gameId)
    {
        _gameId = parsedQuery.gameId;
        getState();
        document.getElementById("new-game-button").style.display = "none";
        document.getElementById("teams").style.display = "flex";
    }
}

function onNewGame()
{
    let req = new XMLHttpRequest();
    req.setRequestHeader
    req.addEventListener("load", function x() {
        window.location.href = `index.html?gameId=${this.response}`;
    });
    req.open("GET", `game/new-game`);
    req.send();
}

window.onload = onPageReady;
