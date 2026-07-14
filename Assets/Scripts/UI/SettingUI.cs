using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour,IMenu
{
    [SerializeField]private Button btnClose;
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Setup(UIMainManager mngr)
    {
       
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnClickClose()
    {
        var mngr = ServiceLocator.Resolve<GameManager>();
        if(mngr.State==GameManager.eStateGame.PAUSE)
        {
            mngr.SetState(GameManager.eStateGame.GAME_STARTED);
        }
        gameObject.SetActive(false);
        
    }

    private void OnDestroy()
    {
        btnClose.onClick.RemoveAllListeners();
    }
}
