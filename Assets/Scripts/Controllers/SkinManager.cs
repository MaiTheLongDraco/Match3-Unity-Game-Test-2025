using System;
using UnityEngine;

/// <summary>
/// SkinManager — quản lý thay đổi Sprite của item dựa trên GameTheme.
/// Lắng nghe ThemeChangedEvent và reskin tất cả item trên board.
/// Đọc data từ GameSettings ScriptableObject (Data-Driven).
/// </summary>
public class SkinManager : MonoBehaviour
{
    private const string PREFS_THEME_KEY = "SelectedThemeID";

    /// <summary>Theme đang active — Item sẽ đọc giá trị này khi ApplySkin().</summary>
    public static eGameTheme ActiveTheme { get; private set; } = eGameTheme.Food;
    [SerializeField]
    private Board _activeBoard;
    [SerializeField]
    private GameSettings _gameSettings;
    [SerializeField]
    private ThemeDatabase _themeDatabase;

    private void Awake()
    {
        _gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        _themeDatabase = Resources.Load<ThemeDatabase>(Constants.THEME_DATABASE_PATH);

        // Load theme đã lưu từ lần chơi trước
        int savedId = PlayerPrefs.GetInt(PREFS_THEME_KEY, (int)eGameTheme.Food);
        ActiveTheme = (eGameTheme)savedId;

        // Sync với ScriptableObject để inspector hiển thị đúng
        if (_gameSettings != null) _gameSettings.GameTheme = ActiveTheme;

        ServiceLocator.Register<SkinManager>(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ThemeChangedEvent>(OnThemeChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ThemeChangedEvent>(OnThemeChanged);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SkinManager>();
    }

    public void RegisterBoard(Board board)
    {
        _activeBoard = board;
    }

    public void UnregisterBoard()
    {
        _activeBoard = null;
    }

    /// <summary>
    /// API công khai để UI gọi khi người dùng chọn theme mới.
    /// Lưu vào PlayerPrefs ngay lập tức và publish event.
    /// </summary>
    public void ApplyTheme(eGameTheme newTheme)
    {
        if (ActiveTheme == newTheme) return;

        ActiveTheme = newTheme;

        // Lưu vào PlayerPrefs (persist qua session) và ScriptableObject (cho Editor)
        PlayerPrefs.SetInt(PREFS_THEME_KEY, (int)newTheme);
        PlayerPrefs.Save();

        if (_gameSettings != null) _gameSettings.GameTheme = newTheme;

        EventBus.Publish(new ThemeChangedEvent { NewTheme = newTheme });
    }

    // ─── Lấy Sprite theo Theme đang active ──────────────────────────────────────

    public Sprite GetSpriteForNormalItem(NormalItem.eNormalType type)
    {
        if (_themeDatabase == null) return null;
        return _themeDatabase.GetSpriteForNormalItem(ActiveTheme, type);
    }

    public Sprite GetSpriteForBonusItem(BonusItem.eBonusType type)
    {
        if (_themeDatabase == null) return null;
        return _themeDatabase.GetSpriteForBonusItem(ActiveTheme, type);
    }

    // ─── Handler ───────────────────────────────────────────────────────────────

    private void OnThemeChanged(ThemeChangedEvent evt)
    {
        if (_activeBoard == null) return;

        // Reskin toàn bộ Item đang sống trên board
        _activeBoard.ReskinAllItems();
    }
}
