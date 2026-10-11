namespace MareReversa.Engine;

/// <summary>
/// Toda a lógica do jogo. Não depende de tela nenhuma: a interface (Jogo.razor) só lê o estado daqui.
/// </summary>
public sealed class GameEngine
{
    public const int W = 12, H = 8, LastLevel = 5;

    private readonly Random _rng = new();
    private readonly Dictionary<FishKind, int> _levelCatches = new();
    private int _levelBestCombo;
    private GameState _prevState = GameState.Playing;

    // ---- Estado visível para a interface ----
    public Tile[,] Grid { get; private set; } = new Tile[W, H];
    public double[,] Warn { get; private set; } = new double[W, H];
    public int PX { get; private set; }
    public int PY { get; private set; }
    public Facing Facing { get; private set; } = Facing.Down;
    public Cast? Cast { get; private set; }
    public GameState State { get; private set; } = GameState.Menu;

    public int Level { get; private set; } = 1;
    public int Hearts { get; private set; }
    public int MaxHearts { get; private set; } = 3;
    public int Coins { get; private set; }
    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int LandCreated { get; private set; }
    public int TotalCaught { get; private set; }
    public int CoinsEarnedLevel { get; private set; }
    public int CoinsEarnedTotal { get; private set; }

    public double Time { get; private set; }
    public double LevelTime { get; private set; }
    public double FreezeLeft { get; private set; }
    public double TideTimer { get; private set; }

    public List<Mission> Missions { get; private set; } = new();
    public List<Upgrade> Upgrades { get; private set; } = new();
    public List<ToastMsg> Toasts { get; } = new();
    public List<Splash> Splashes { get; } = new();

    public (int dx, int dy) Dir => Facing switch
    {
        Facing.Up => (0, -1),
        Facing.Down => (0, 1),
        Facing.Left => (-1, 0),
        _ => (1, 0)
    };

    public int Score =>
        (Level - 1 + (State == GameState.Won ? 1 : 0)) * 500
        + LandCreated * 10
        + CoinsEarnedTotal * 5
        + (int)Time;

    // ---- Dificuldade (melhora com as compras da loja, piora a cada fase) ----
    private int Up(string id) => Upgrades.FirstOrDefault(u => u.Id == id)?.Level ?? 0;
    public double TideInterval => Math.Max(0.7, (2.9 - 0.3 * Level) * (1 + 0.12 * Up("tide")));
    public double WarnTime => Math.Max(0.8, 1.5 - 0.08 * Level) + 0.25 * Up("boots");
    private double WaitTime => (0.8 + _rng.NextDouble() * 1.4) * (1 - 0.15 * Up("rod"));

    // ---- Controle do jogo ----
    public void Start()
    {
        Grid = new Tile[W, H];
        Warn = new double[W, H];
        for (var j = 3; j <= 5; j++)
            for (var i = 4; i <= 7; i++)
                Grid[i, j] = Tile.Grass;

        PX = 5; PY = 4;
        Facing = Facing.Down;
        Cast = null;
        Level = 1;
        MaxHearts = 3; Hearts = 3;
        Coins = 0; Combo = 0; BestCombo = 0;
        LandCreated = 0; TotalCaught = 0; CoinsEarnedTotal = 0;
        Time = 0;
        Toasts.Clear(); Splashes.Clear();

        Upgrades = new List<Upgrade>
        {
            new("rod",   "Vara Veloz",        "Os peixes mordem mais rápido",        8,  3),
            new("boots", "Botas de Borracha", "O aviso de alagamento dura mais",     10, 3),
            new("tide",  "Âncora do Tempo",   "A maré sobe mais devagar",            12, 3),
            new("heart", "Coração Extra",     "+1 vida máxima (e já enche uma)",     15, 2),
        };

        ResetLevelCounters();
        State = GameState.Playing;
        Say("Pesque terra antes que a maré engula a ilha!");
    }

    public void Act()
    {
        switch (State)
        {
            case GameState.Menu:
            case GameState.Won:
            case GameState.Lost:
                Start();
                break;
            case GameState.LevelDone:
                NextLevel();
                break;
            case GameState.Playing:
                CastOrReel();
                break;
        }
    }

    public void Move(Facing f)
    {
        if (State != GameState.Playing) return;

        if (f != Facing)
        {
            Facing = f;
            Cast = null;
        }

        var (dx, dy) = Dir;
        var nx = PX + dx;
        var ny = PY + dy;
        if (nx < 0 || ny < 0 || nx >= W || ny >= H || Grid[nx, ny] == Tile.Water) return;

        PX = nx; PY = ny;
        Cast = null;
    }

    private void CastOrReel()
    {
        if (Cast is null)
        {
            var (dx, dy) = Dir;
            var tx = PX + dx;
            var ty = PY + dy;
            if (tx >= 0 && ty >= 0 && tx < W && ty < H && Grid[tx, ty] == Tile.Water)
                Cast = new Cast { X = tx, Y = ty, Timer = WaitTime };
            else
                Say("Mire na água para pescar!");
        }
        else if (Cast.Bite)
        {
            var c = Cast;
            Cast = null;
            Reel(c);
        }
        else
        {
            Cast = null;
            Combo = 0;
            Say("Cedo demais! Combo perdido.");
        }
    }

    private FishKind RollFish()
    {
        var table = new (FishKind kind, int weight)[]
        {
            (FishKind.Terra, 55),
            (FishKind.Dourado, 12 + Level),
            (FishKind.Relogio, 9),
            (FishKind.Pedra, 12),
            (FishKind.Baiacu, 6 + Level * 2),
        };
        var roll = _rng.Next(table.Sum(t => t.weight));
        foreach (var (kind, weight) in table)
        {
            if (roll < weight) return kind;
            roll -= weight;
        }
        return FishKind.Terra;
    }

    private void Reel(Cast c)
    {
        Splashes.Add(new Splash { X = c.X, Y = c.Y });

        switch (c.Fish)
        {
            case FishKind.Terra:
                Grid[c.X, c.Y] = Tile.Grass;
                LandCreated++;
                break;
            case FishKind.Pedra:
                Grid[c.X, c.Y] = Tile.Rock;
                LandCreated++;
                break;
            case FishKind.Dourado:
                Gain(6);
                break;
            case FishKind.Relogio:
                FreezeLeft = 6;
                break;
            case FishKind.Baiacu:
                Combo = 0;
                FloodRandom(0.7);
                Say("Baiacu! A maré ficou furiosa!");
                return;
        }

        TotalCaught++;
        _levelCatches[c.Fish] = _levelCatches.GetValueOrDefault(c.Fish) + 1;
        Combo++;
        BestCombo = Math.Max(BestCombo, Combo);
        _levelBestCombo = Math.Max(_levelBestCombo, Combo);

        if (Combo >= 3)
        {
            var bonus = Combo / 3;
            Gain(bonus);
            Say($"{FishInfo.Name(c.Fish)}! Combo x{Combo}: +{bonus} moeda(s)");
        }
        else
        {
            Say($"{FishInfo.Name(c.Fish)}: {FishInfo.Effect(c.Fish)}");
        }
    }

    private void Gain(int n)
    {
        Coins += n;
        CoinsEarnedLevel += n;
        CoinsEarnedTotal += n;
    }

    // ---- Passagem do tempo ----
    public void Update(double dt)
    {
        for (var i = Toasts.Count - 1; i >= 0; i--)
        {
            Toasts[i].Left -= dt;
            if (Toasts[i].Left <= 0) Toasts.RemoveAt(i);
        }
        for (var i = Splashes.Count - 1; i >= 0; i--)
        {
            Splashes[i].Left -= dt;
            if (Splashes[i].Left <= 0) Splashes.RemoveAt(i);
        }

        if (State != GameState.Playing) return;

        Time += dt;
        LevelTime += dt;

        if (FreezeLeft > 0)
        {
            FreezeLeft = Math.Max(0, FreezeLeft - dt);
        }
        else
        {
            TideTimer -= dt;
            if (TideTimer <= 0)
            {
                TideTimer = TideInterval;
                FloodRandom(WarnTime);
            }

            for (var j = 0; j < H; j++)
                for (var i = 0; i < W; i++)
                    if (Warn[i, j] > 0)
                    {
                        Warn[i, j] -= dt;
                        if (Warn[i, j] <= 0) Flood(i, j);
                    }
        }

        if (State == GameState.Playing && Cast is { } c)
        {
            c.Timer -= dt;
            if (c.Timer <= 0)
            {
                if (!c.Bite)
                {
                    c.Bite = true;
                    c.Fish = RollFish();
                    c.Timer = 0.85;
                }
                else
                {
                    Cast = null;
                    if (c.Fish == FishKind.Baiacu)
                    {
                        Say("Você deixou o baiacu fugir. Esperto!");
                    }
                    else
                    {
                        Combo = 0;
                        Say("O peixe fugiu! Combo perdido.");
                    }
                }
            }
        }

        if (State == GameState.Playing && LandCount() == 0) Lose();
        if (State == GameState.Playing) CheckMissions();
    }

    private int LandCount()
    {
        var n = 0;
        for (var j = 0; j < H; j++)
            for (var i = 0; i < W; i++)
                if (Grid[i, j] != Tile.Water) n++;
        return n;
    }

    private bool TouchesWater(int x, int y)
    {
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            var nx = x + dx;
            var ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= W || ny >= H || Grid[nx, ny] == Tile.Water) return true;
        }
        return false;
    }

    private void FloodRandom(double warnSeconds)
    {
        var edge = new List<(int x, int y)>();
        var any = new List<(int x, int y)>();
        for (var j = 0; j < H; j++)
            for (var i = 0; i < W; i++)
            {
                if (Grid[i, j] != Tile.Grass || Warn[i, j] > 0) continue;
                any.Add((i, j));
                if (TouchesWater(i, j)) edge.Add((i, j));
            }

        var pool = edge.Count > 0 ? edge : any;
        if (pool.Count == 0) return;
        var (x, y) = pool[_rng.Next(pool.Count)];
        Warn[x, y] = warnSeconds;
    }

    private void Flood(int x, int y)
    {
        Warn[x, y] = 0;
        Grid[x, y] = Tile.Water;
        Splashes.Add(new Splash { X = x, Y = y });
        if (PX == x && PY == y) Fall();
    }

    private void Fall()
    {
        Hearts--;
        Combo = 0;
        Cast = null;
        if (Hearts <= 0) { Lose(); return; }

        var spot = NearestLand(PX, PY, safeOnly: true) ?? NearestLand(PX, PY, safeOnly: false);
        if (spot is null) { Lose(); return; }

        (PX, PY) = spot.Value;
        Say($"SPLASH! Você caiu na água. Vidas: {Hearts}");
    }

    private (int, int)? NearestLand(int x, int y, bool safeOnly)
    {
        (int, int)? best = null;
        var bestDist = int.MaxValue;
        for (var j = 0; j < H; j++)
            for (var i = 0; i < W; i++)
            {
                if (Grid[i, j] == Tile.Water) continue;
                if (safeOnly && Warn[i, j] > 0) continue;
                var d = Math.Abs(i - x) + Math.Abs(j - y);
                if (d < bestDist) { bestDist = d; best = (i, j); }
            }
        return best;
    }

    private void Lose()
    {
        Cast = null;
        State = GameState.Lost;
    }

    // ---- Missões e fases ----
    private void ResetLevelCounters()
    {
        LevelTime = 0;
        CoinsEarnedLevel = 0;
        FreezeLeft = 0;
        _levelCatches.Clear();
        _levelBestCombo = 0;
        Missions = BuildMissions(Level);
        TideTimer = TideInterval;
    }

    private static List<Mission> BuildMissions(int level) => level switch
    {
        1 => new()
        {
            new(MissionKind.Land, 14, 5),
            new(MissionKind.Catch, 6, 5, FishKind.Terra),
            new(MissionKind.Survive, 40, 5),
        },
        2 => new()
        {
            new(MissionKind.Land, 18, 8),
            new(MissionKind.Catch, 2, 8, FishKind.Dourado),
            new(MissionKind.Combo, 4, 8),
        },
        3 => new()
        {
            new(MissionKind.Catch, 2, 10, FishKind.Relogio),
            new(MissionKind.Catch, 3, 10, FishKind.Pedra),
            new(MissionKind.Survive, 60, 10),
        },
        4 => new()
        {
            new(MissionKind.Land, 24, 12),
            new(MissionKind.Combo, 6, 12),
            new(MissionKind.Earn, 45, 12),
        },
        _ => new()
        {
            new(MissionKind.Land, 30, 15),
            new(MissionKind.Catch, 4, 15, FishKind.Dourado),
            new(MissionKind.Survive, 90, 20),
        },
    };

    private void CheckMissions()
    {
        foreach (var m in Missions)
        {
            var p = m.Kind switch
            {
                MissionKind.Land => Math.Max(m.Progress, LandCount()),
                MissionKind.Catch => _levelCatches.GetValueOrDefault(m.Fish),
                MissionKind.Survive => (int)LevelTime,
                MissionKind.Combo => _levelBestCombo,
                MissionKind.Earn => CoinsEarnedLevel,
                _ => 0
            };
            m.Progress = Math.Min(m.Target, p);

            if (m.Done && !m.Paid)
            {
                m.Paid = true;
                Gain(m.Reward);
                Say($"Missão cumprida! +{m.Reward} moedas");
            }
        }

        if (Missions.All(m => m.Done))
        {
            Cast = null;
            Gain(10);
            State = Level >= LastLevel ? GameState.Won : GameState.LevelDone;
        }
    }

    private void NextLevel()
    {
        Level++;
        Hearts = Math.Min(MaxHearts, Hearts + 1);
        Warn = new double[W, H];
        Cast = null;
        Combo = 0;
        ResetLevelCounters();
        State = GameState.Playing;
        Say($"Fase {Level}! A maré está mais rápida.");
    }

    // ---- Loja ----
    public void ToggleShop()
    {
        if (State == GameState.Shop) CloseShop();
        else OpenShop();
    }

    public void OpenShop()
    {
        if (State is GameState.Playing or GameState.LevelDone)
        {
            _prevState = State;
            State = GameState.Shop;
        }
    }

    public void CloseShop()
    {
        if (State == GameState.Shop) State = _prevState;
    }

    public bool Buy(string id)
    {
        var u = Upgrades.FirstOrDefault(x => x.Id == id);
        if (u is null || u.Maxed || Coins < u.Cost) return false;

        Coins -= u.Cost;
        u.Level++;
        if (id == "heart") { MaxHearts++; Hearts++; }
        Say($"{u.Name} melhorou!");
        return true;
    }

    private void Say(string text)
    {
        Toasts.Add(new ToastMsg { Text = text });
        while (Toasts.Count > 3) Toasts.RemoveAt(0);
    }
}
