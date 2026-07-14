using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelCondition : MonoBehaviour
{
    public event Action ConditionCompleteEvent = delegate { };

    protected Text m_txt;

    protected bool m_conditionCompleted = false;

    // Setup chuẩn — cho cả LevelTime lẫn LevelMoves dùng chung
    public virtual void Setup(float value, Text txt)
    {
        m_txt = txt;
    }

    // Override cho LevelMoves
    public virtual void Setup(float value, Text txt, BoardController board)
    {
        m_txt = txt;
    }

    protected virtual void UpdateText() { }

    protected void OnConditionComplete()
    {
        m_conditionCompleted = true;

        // Publish event global để các class khác có thể lắng nghe nếu cần
        EventBus.Publish(new LevelConditionCompletedEvent());

        ConditionCompleteEvent();
    }

    protected virtual void OnDestroy() { }
}
