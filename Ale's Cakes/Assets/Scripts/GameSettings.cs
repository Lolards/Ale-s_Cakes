public enum GameMode { Easy, Normal, Hard, Free }

public static class GameSettings
{
    public static GameMode Mode = GameMode.Normal;

    public static bool HasTimer => Mode != GameMode.Free;

    // El modo libre no termina nunca
    public static bool EndsAfterOrders => Mode != GameMode.Free;

    // Pasteles que hay que entregar para ganar
    public static int RequiredCakes => Mode == GameMode.Hard ? 5 : 3;

    // Multiplicador del tiempo de cada pedido
    public static float TimeMultiplier
    {
        get
        {
            switch (Mode)
            {
                case GameMode.Easy: return 1.5f;
                case GameMode.Hard: return 0.6f;
                default: return 1f;
            }
        }
    }
}