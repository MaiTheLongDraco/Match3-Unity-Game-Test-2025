using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "themedatabase", menuName = "Match3/Theme Database")]
public class ThemeDatabase : ScriptableObject
{
    [Header("Theme Configuration")]
    public ThemeSprites[] ConfiguredThemes;

    private Dictionary<eGameTheme, ThemeSprites> _themeDict;

    private void OnEnable()
    {
        // Clear dictionary để đảm bảo data luôn được build lại khi reload/edit trong Editor
        _themeDict = null;
    }

    private void BuildDictionaryIfNeeded()
    {
        if (_themeDict != null) return;
        
        _themeDict = new Dictionary<eGameTheme, ThemeSprites>();
        if (ConfiguredThemes == null) return;

        for (int i = 0; i < ConfiguredThemes.Length; i++)
        {
            _themeDict[ConfiguredThemes[i].Theme] = ConfiguredThemes[i];
        }
    }

    public Sprite GetSpriteForNormalItem(eGameTheme theme, NormalItem.eNormalType type)
    {
        BuildDictionaryIfNeeded();
        
        if (_themeDict.TryGetValue(theme, out ThemeSprites sprites))
        {
            return sprites.GetNormalSprite(type);
        }
        return null;
    }

    public Sprite GetSpriteForBonusItem(eGameTheme theme, BonusItem.eBonusType type)
    {
        BuildDictionaryIfNeeded();
        
        if (_themeDict.TryGetValue(theme, out ThemeSprites sprites))
        {
            return sprites.GetBonusSprite(type);
        }
        return null;
    }
}

[System.Serializable]
public struct ThemeSprites
{
    public eGameTheme Theme;
        
    [Header("Normal Items (Index maps to eNormalType)")]
    [Tooltip("0: TYPE_ONE, 1: TYPE_TWO, ...")]
    public Sprite[] NormalSprites;

    [Header("Bonus Items (Index maps to eBonusType)")]
    [Tooltip("0: NONE, 1: HORIZONTAL, 2: VERTICAL, 3: ALL")]
    public Sprite[] BonusSprites;

    public Sprite GetNormalSprite(NormalItem.eNormalType type)
    {
        int index = (int)type;
        if (NormalSprites != null && index >= 0 && index < NormalSprites.Length)
        {
            return NormalSprites[index];
        }
        return null;
    }

    public Sprite GetBonusSprite(BonusItem.eBonusType type)
    {
        int index = (int)type;
        if (BonusSprites != null && index >= 0 && index < BonusSprites.Length)
        {
            return BonusSprites[index];
        }
        return null;
    }
}
