/// <summary>
/// Định nghĩa tất cả các event struct dùng trong toàn bộ game.
/// Dùng struct để tránh GC allocation khi publish event.
/// </summary>

// Phát khi trạng thái game thay đổi (MAIN_MENU, GAME_STARTED, PAUSE, GAME_OVER...)
public struct GameStateChangedEvent
{
    public GameManager.eStateGame NewState;
}

// Phát bởi LevelTime mỗi khi số giây thay đổi
public struct TimeUpdatedEvent
{
    public int RemainingSeconds;
}

// Phát bởi LevelMoves mỗi khi người chơi đi một nước
public struct MovesUpdatedEvent
{
    public int RemainingMoves;
}

// Phát khi điều kiện thắng/thua được thỏa mãn
public struct LevelConditionCompletedEvent
{
}
