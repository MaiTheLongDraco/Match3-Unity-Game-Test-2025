using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelPause : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnSetting;
    [SerializeField] private Button btnRestart;

    private UIMainManager m_mngr;

    private void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(OnClickClose);
        if (btnSetting) btnSetting.onClick.AddListener(OnClickSetting);
        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
    }

    private void OnDestroy()
    {
        if (btnClose) btnClose.onClick.RemoveAllListeners();
        if (btnSetting) btnSetting.onClick.RemoveAllListeners();
        if (btnRestart) btnRestart.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickClose()
    {
        m_mngr.ShowGameMenu();
    }
    private void OnClickSetting()
    {
        m_mngr.ShowMenuPublic<SettingUI>();
    }
    private void OnClickRestart()
    {
        m_mngr.RestartLevel();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
