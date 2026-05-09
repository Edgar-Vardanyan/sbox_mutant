using Sandbox;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum RoundState
{
    WaitingForPlayers,
    Warmup,
    InfectionStarted,
    RoundEnded
}

public sealed class GameManager : Component
{
    [Sync(SyncFlags.FromHost), Property]
    public RoundState State { get; private set; } = RoundState.WaitingForPlayers;

    [Sync(SyncFlags.FromHost), Property]
    public float StateTimeLeft { get; private set; }

    [Sync(SyncFlags.FromHost), Property]
    public int HumanCount { get; private set; }

    [Sync(SyncFlags.FromHost), Property]
    public int MutantCount { get; private set; }

    [Sync(SyncFlags.FromHost), Property]
    public string LastWinnerText { get; private set; } = "";

    [Property]
    public bool DebugLogs { get; set; } = true;

    [Property]
    public int MinimumPlayersToStart { get; set; } = 1;

    [Property]
    public float WarmupDuration { get; set; } = 10f;

    [Property]
    public float RoundDuration { get; set; } = 120f;

    [Property]
    public float RoundEndDuration { get; set; } = 8f;

    [Property]
    public float MutantRespawnDelay { get; set; } = 3f;

    private readonly HashSet<PlayerHealth> _registeredHealth = new();

    protected override void OnStart()
    {
        if (DebugLogs)
        {
            Log.Info("GameManager started.");
        }

        ChangeState(RoundState.WaitingForPlayers);
    }

    protected override void OnUpdate()
    {
        RegisterPlayerHealthEvents();
        UpdateTeamCounts();

        StateTimeLeft -= Time.Delta;

        switch (State)
        {
            case RoundState.WaitingForPlayers:
                UpdateWaitingForPlayers();
                break;

            case RoundState.Warmup:
                UpdateWarmup();
                break;

            case RoundState.InfectionStarted:
                UpdateInfectionStarted();
                break;

            case RoundState.RoundEnded:
                UpdateRoundEnded();
                break;
        }
    }

    private void RegisterPlayerHealthEvents()
    {
        foreach (var health in Scene.GetAllComponents<PlayerHealth>())
        {
            if (_registeredHealth.Contains(health))
                continue;

            health.OnDied += HandlePlayerDied;
            _registeredHealth.Add(health);

            if (DebugLogs)
            {
                Log.Info($"Registered death event for {health.GameObject.Name}");
            }
        }
    }

    private void HandlePlayerDied(PlayerHealth health)
    {
        var role = health.Components.Get<PlayerRole>();

        if (role is null)
            return;

        if (DebugLogs)
        {
            Log.Info($"GameManager handling death for {health.GameObject.Name}. Team: {role.Team}");
        }

        if (State != RoundState.InfectionStarted)
        {
            health.ResetHealth();
            return;
        }

        if (role.Team == PlayerTeam.Human)
        {
            role.SetTeam(PlayerTeam.Mutant);
            RespawnPlayer(role, SpawnPointType.Mutant);
            return;
        }

        if (role.Team == PlayerTeam.Mutant)
        {
            _ = RespawnMutantAfterDelay(role, health);
        }
    }

    private async Task RespawnMutantAfterDelay(PlayerRole role, PlayerHealth health)
    {
        await Task.DelaySeconds(MutantRespawnDelay);

        if (role is null || health is null)
            return;

        if (role.Team != PlayerTeam.Mutant)
            return;

        RespawnPlayer(role, SpawnPointType.Mutant);
        health.ResetHealth();

        if (DebugLogs)
        {
            Log.Info($"{role.GameObject.Name} respawned as Mutant.");
        }
    }

    private void RespawnPlayer(PlayerRole role, SpawnPointType preferredType)
    {
        var spawn = PickSpawnPoint(preferredType);

        if (spawn is null)
        {
            if (DebugLogs)
            {
                Log.Warning("No spawn point found for respawn.");
            }

            return;
        }

        var respawn = role.Components.Get<PlayerRespawn>();

        if (respawn is not null)
        {
            respawn.RespawnAt(spawn.WorldPosition, spawn.WorldRotation);
        }
        else
        {
            role.GameObject.WorldPosition = spawn.WorldPosition;
            role.GameObject.WorldRotation = spawn.WorldRotation;
        }

        if (DebugLogs)
        {
            Log.Info($"{role.GameObject.Name} respawned at {spawn.GameObject.Name}");
        }
    }

    private SpawnPoint PickSpawnPoint(SpawnPointType preferredType)
    {
        var allSpawns = Scene.GetAllComponents<SpawnPoint>().ToList();

        if (allSpawns.Count == 0)
            return null;

        var matchingSpawns = allSpawns
            .Where(x => x.Type == preferredType || x.Type == SpawnPointType.Any)
            .ToList();

        if (matchingSpawns.Count == 0)
            matchingSpawns = allSpawns;

        var randomIndex = Game.Random.Next(0, matchingSpawns.Count);
        return matchingSpawns[randomIndex];
    }

    private void UpdateWaitingForPlayers()
    {
        var playerCount = GetPlayers().Count;

        if (playerCount >= MinimumPlayersToStart)
        {
            ChangeState(RoundState.Warmup);
        }
    }

    private void UpdateWarmup()
    {
        SetAllPlayersHuman();

        if (StateTimeLeft <= 0f)
        {
            PickFirstMutant();
            ChangeState(RoundState.InfectionStarted);
        }
    }

    private void UpdateInfectionStarted()
    {
        CheckWinCondition();

        if (StateTimeLeft <= 0f)
        {
            EndRound(humansWon: true);
        }
    }

    private void UpdateRoundEnded()
    {
        if (StateTimeLeft <= 0f)
        {
            ChangeState(RoundState.WaitingForPlayers);
        }
    }

    private void ChangeState(RoundState newState)
    {
        State = newState;

        switch (State)
        {
            case RoundState.WaitingForPlayers:
                StateTimeLeft = 0f;
                LastWinnerText = "";
                SetAllPlayersHuman();
                break;

            case RoundState.Warmup:
                StateTimeLeft = WarmupDuration;
                LastWinnerText = "";
                SetAllPlayersHuman();
                break;

            case RoundState.InfectionStarted:
                StateTimeLeft = RoundDuration;
                break;

            case RoundState.RoundEnded:
                StateTimeLeft = RoundEndDuration;
                break;
        }

        if (DebugLogs)
        {
            Log.Info($"Round state changed to {State}. Time left: {StateTimeLeft:0.0}");
        }
    }

    private List<PlayerRole> GetPlayers()
    {
        return Scene.GetAllComponents<PlayerRole>().ToList();
    }

    [Button("Set All Human")]
    public void SetAllPlayersHuman()
    {
        foreach (var role in GetPlayers())
        {
            role.SetTeam(PlayerTeam.Human);
        }
    }

    [Button("Set All Mutant")]
    public void SetAllPlayersMutant()
    {
        foreach (var role in GetPlayers())
        {
            role.SetTeam(PlayerTeam.Mutant);
        }
    }

    [Button("Pick First Mutant")]
    public void PickFirstMutant()
    {
        var players = GetPlayers();

        if (players.Count == 0)
        {
            if (DebugLogs)
            {
                Log.Warning("No players found. Cannot pick first mutant.");
            }

            return;
        }

        foreach (var player in players)
        {
            player.SetTeam(PlayerTeam.Human);
        }

        var randomIndex = Game.Random.Next(0, players.Count);
        var randomPlayer = players[randomIndex];

        randomPlayer.SetTeam(PlayerTeam.Mutant);

        if (DebugLogs)
        {
            Log.Info($"{randomPlayer.GameObject.Name} was picked as first mutant.");
        }
    }

    private void CheckWinCondition()
    {
        var players = GetPlayers();

        if (players.Count == 0)
            return;

        var humanCount = players.Count(x => x.Team == PlayerTeam.Human);
        var mutantCount = players.Count(x => x.Team == PlayerTeam.Mutant);

        if (humanCount <= 0 && mutantCount > 0)
        {
            EndRound(humansWon: false);
        }
    }

    private void EndRound(bool humansWon)
    {
        if (State == RoundState.RoundEnded)
            return;

        LastWinnerText = humansWon ? "Humans Win!" : "Mutants Win!";

        if (DebugLogs)
        {
            Log.Info(LastWinnerText);
        }

        ChangeState(RoundState.RoundEnded);
    }

    private void UpdateTeamCounts()
    {
        var players = GetPlayers();

        HumanCount = players.Count(x => x.Team == PlayerTeam.Human);
        MutantCount = players.Count(x => x.Team == PlayerTeam.Mutant);
    }

    [Button("Force Warmup")]
    private void ForceWarmup()
    {
        ChangeState(RoundState.Warmup);
    }

    [Button("Force Infection Started")]
    private void ForceInfectionStarted()
    {
        PickFirstMutant();
        ChangeState(RoundState.InfectionStarted);
    }

    [Button("Force End Round")]
    private void ForceEndRound()
    {
        EndRound(humansWon: true);
    }
}