/// <summary>
/// Assets/Table/Enemy.xlsx
/// INDEX | EnemyName  | Health | Damage | ATTackRange | MoveSpeed | Type
///   1   | Slime      |   3    |   1    |      1      |     1     | Normal
///   2   | Big Slime  |   5    |   2    |      1      |     1     | Boss
/// </summary>
public static class EnemyDataTable
{
    public struct EnemyData
    {
        public int    Index;
        public string Name;
        public int    HP;
        public int    Damage;
        public int    AttackRange;
        public int    MoveSpeed;
        public bool   IsBoss;
    }

    public static readonly EnemyData[] All = new[]
    {
        new EnemyData { Index=1, Name="Slime",     HP=3, Damage=1, AttackRange=1, MoveSpeed=1, IsBoss=false },
        new EnemyData { Index=2, Name="Big Slime", HP=5, Damage=2, AttackRange=1, MoveSpeed=1, IsBoss=true  },
    };

    public static EnemyData Get(int index) => All[index - 1];
}
