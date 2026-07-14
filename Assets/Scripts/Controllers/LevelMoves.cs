using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelMoves : LevelCondition
{
    private int m_moves;

    private BoardController m_board;

    public override void Setup(float value, Text txt, BoardController board)
    {
        base.Setup(value, txt);

        m_moves = (int)value;

        m_board = board;
        m_board.OnMoveEvent += OnMove;

        UpdateText();
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;

        m_moves--;

        UpdateText();
        EventBus.Publish(new MovesUpdatedEvent { RemainingMoves = m_moves });

        if (m_moves <= 0)
        {
            OnConditionComplete();
        }
    }

    protected override void UpdateText()
    {
        // Dùng string ghép tránh string.Format allocation
        m_txt.text = "MOVES:\n" + m_moves.ToString();
    }

    protected override void OnDestroy()
    {
        if (m_board != null) m_board.OnMoveEvent -= OnMove;
        base.OnDestroy();
    }
}
