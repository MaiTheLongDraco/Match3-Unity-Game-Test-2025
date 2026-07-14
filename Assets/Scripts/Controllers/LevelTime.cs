using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelTime : LevelCondition
{
    private float m_time;
    private int m_lastDisplayedSeconds = -1;

    public override void Setup(float value, Text txt)
    {
        base.Setup(value, txt);
        m_time = value;
        UpdateText();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private bool m_isGameStarted = false;

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        m_isGameStarted = (evt.NewState == GameManager.eStateGame.GAME_STARTED);
    }

    private void Update()
    {
        if (m_conditionCompleted) return;
        if (!m_isGameStarted) return;

        m_time -= Time.deltaTime;

        // Chỉ update UI khi phần giây nguyên thay đổi — tránh string.Format mỗi frame
        int seconds = Mathf.Max(0, Mathf.CeilToInt(m_time));
        if (seconds != m_lastDisplayedSeconds)
        {
            m_lastDisplayedSeconds = seconds;
            UpdateText();
            EventBus.Publish(new TimeUpdatedEvent { RemainingSeconds = seconds });
        }

        if (m_time <= -1f)
        {
            OnConditionComplete();
        }
    }

    protected override void UpdateText()
    {
        if (m_time < 0f) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(m_time));
        // Dùng string ghép thủ công tránh string.Format allocation
        m_txt.text = "TIME:\n" + seconds.ToString();
    }
}
