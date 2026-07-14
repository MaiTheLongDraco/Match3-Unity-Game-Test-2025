using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeUI : MonoBehaviour
{
    [SerializeField] private Button selectThemeBtnPrefab;
    [SerializeField]private Transform selectThemeBtnParent;
    // Start is called before the first frame update
    void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        var listTheme= Enum.GetValues(typeof(eGameTheme));
        ClearChild();
        for (int i = 0; i < listTheme.Length; i++) 
        {
           var go= CreateSelectThemeBtn((eGameTheme)listTheme.GetValue(i));
            go.gameObject.SetActive(true);
        }
    }   
    private Button CreateSelectThemeBtn(eGameTheme theme)
    {
        var btn = Instantiate(selectThemeBtnPrefab, selectThemeBtnParent);
        btn.GetComponentInChildren<Text>().text = theme.ToString();
        btn.onClick.AddListener(() => OnClickSelectTheme(theme));
        return btn;
    }

    private void OnClickSelectTheme(eGameTheme theme)
    {
      ServiceLocator.Resolve<SkinManager>().ApplyTheme(theme);
    }

    private void ClearChild()
    {
        for (int i = selectThemeBtnParent.childCount - 1; i > 0; i--)
        {
            Destroy(selectThemeBtnParent.GetChild(i).gameObject);
        }
    }
}
