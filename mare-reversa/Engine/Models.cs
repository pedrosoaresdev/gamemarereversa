namespace MareReversa.Engine;

public enum Tile { Water, Grass, Rock }
public enum FishKind { Terra, Dourado, Relogio, Pedra, Baiacu }
public enum Facing { Up, Down, Left, Right }
public enum GameState { Menu, Playing, Shop, LevelDone, Won, Lost }
public enum MissionKind { Land, Catch, Survive, Combo, Earn }

public static class FishInfo
{
    public static string Name(FishKind k) => k switch
    {
        FishKind.Terra => "Peixe-Terra",
        FishKind.Dourado => "Peixe-Dourado",
        FishKind.Relogio => "Peixe-Relógio",
        FishKind.Pedra => "Peixe-Pedra",
        _ => "Baiacu Furioso"
    };

    public static string Color(FishKind k) => k switch
    {
        FishKind.Terra => "#8fd14f",
        FishKind.Dourado => "#ffd23f",
        FishKind.Relogio => "#6fd6ff",
        FishKind.Pedra => "#aab0b8",
        _ => "#e8453c"
    };

    public static string Effect(FishKind k) => k switch
    {
        FishKind.Terra => "Transforma o mar em grama",
        FishKind.Dourado => "Rende 6 moedas",
        FishKind.Relogio => "Congela a maré por 6 segundos",
        FishKind.Pedra => "Cria uma rocha que nunca alaga",
        _ => "PERIGO! Faz um bloco alagar. Deixe ele fugir!"
    };
}

public sealed class Mission
{
    public MissionKind Kind { get; }
    public FishKind Fish { get; }
    public int Target { get; }
    public int Reward { get; }
    public int Progress { get; set; }
    public bool Paid { get; set; }
    public bool Done => Progress >= Target;
    public int Percent => Target == 0 ? 100 : Math.Min(100, Progress * 100 / Target);

    public Mission(MissionKind kind, int target, int reward, FishKind fish = FishKind.Terra)
    {
        Kind = kind;
        Target = target;
        Reward = reward;
        Fish = fish;
    }

    public string Title => Kind switch
    {
        MissionKind.Land => $"Ter {Target} blocos de terra ao mesmo tempo",
        MissionKind.Catch => $"Pescar {Target}x {FishInfo.Name(Fish)}",
        MissionKind.Survive => $"Sobreviver {Target} segundos nesta fase",
        MissionKind.Combo => $"Combo de {Target} pescas seguidas sem errar",
        MissionKind.Earn => $"Ganhar {Target} moedas nesta fase",
        _ => ""
    };
}

public sealed class Upgrade
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int BaseCost { get; }
    public int MaxLevel { get; }
    public int Level { get; set; }
    public int Cost => BaseCost * (Level + 1);
    public bool Maxed => Level >= MaxLevel;

    public Upgrade(string id, string name, string description, int baseCost, int maxLevel)
    {
        Id = id;
        Name = name;
        Description = description;
        BaseCost = baseCost;
        MaxLevel = maxLevel;
    }
}

public sealed class Cast
{
    public int X { get; init; }
    public int Y { get; init; }
    public double Timer { get; set; }
    public bool Bite { get; set; }
    public FishKind Fish { get; set; }
}

public sealed class ToastMsg
{
    public string Text { get; init; } = "";
    public double Left { get; set; } = 3.2;
}

public sealed class Splash
{
    public int X { get; init; }
    public int Y { get; init; }
    public double Left { get; set; } = 0.6;
}
