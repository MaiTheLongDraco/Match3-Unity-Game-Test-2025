using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusItem : Item
{
    public enum eBonusType
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    public eBonusType ItemType;

    public void SetType(eBonusType type)
    {
        ItemType = type;
    }

    protected override string GetPrefabName()
    {
        string prefabname = string.Empty;
        switch (ItemType)
        {
            case eBonusType.NONE:
                break;
            case eBonusType.HORIZONTAL:
                prefabname = Constants.PREFAB_BONUS_HORIZONTAL;
                break;
            case eBonusType.VERTICAL:
                prefabname = Constants.PREFAB_BONUS_VERTICAL;
                break;
            case eBonusType.ALL:
                prefabname = Constants.PREFAB_BONUS_BOMB;
                break;
        }

        return prefabname;
    }

    internal override bool IsSameType(Item other)
    {
        BonusItem it = other as BonusItem;

        return it != null && it.ItemType == this.ItemType;
    }

    internal override void ExplodeView()
    {
        ActivateBonus();

        base.ExplodeView();
    }

    private void ActivateBonus()
    {
        switch (ItemType)
        {
            case eBonusType.HORIZONTAL:
                ExplodeHorizontalLine();
                break;
            case eBonusType.VERTICAL:
                ExplodeVerticalLine();
                break;
            case eBonusType.ALL:
                ExplodeBomb();
                break;

        }
    }

    private static List<Cell> _reusableExplodeList = new List<Cell>();

    private void ExplodeBomb()
    {
        _reusableExplodeList.Clear();
        if (Cell.NeighbourBottom) _reusableExplodeList.Add(Cell.NeighbourBottom);
        if (Cell.NeighbourUp) _reusableExplodeList.Add(Cell.NeighbourUp);
        if (Cell.NeighbourLeft)
        {
            _reusableExplodeList.Add(Cell.NeighbourLeft);
            if (Cell.NeighbourLeft.NeighbourUp)
            {
                _reusableExplodeList.Add(Cell.NeighbourLeft.NeighbourUp);
            }
            if (Cell.NeighbourLeft.NeighbourBottom)
            {
                _reusableExplodeList.Add(Cell.NeighbourLeft.NeighbourBottom);
            }
        }
        if (Cell.NeighbourRight)
        {
            _reusableExplodeList.Add(Cell.NeighbourRight);
            if (Cell.NeighbourRight.NeighbourUp)
            {
                _reusableExplodeList.Add(Cell.NeighbourRight.NeighbourUp);
            }
            if (Cell.NeighbourRight.NeighbourBottom)
            {
                _reusableExplodeList.Add(Cell.NeighbourRight.NeighbourBottom);
            }
        }

        for (int i = 0; i < _reusableExplodeList.Count; i++)
        {
            _reusableExplodeList[i].ExplodeItem();
        }
    }

    private void ExplodeVerticalLine()
    {
        _reusableExplodeList.Clear();

        Cell newcell = Cell;
        while (true)
        {
            Cell next = newcell.NeighbourUp;
            if (next == null) break;

            _reusableExplodeList.Add(next);
            newcell = next;
        }

        newcell = Cell;
        while (true)
        {
            Cell next = newcell.NeighbourBottom;
            if (next == null) break;

            _reusableExplodeList.Add(next);
            newcell = next;
        }


        for (int i = 0; i < _reusableExplodeList.Count; i++)
        {
            _reusableExplodeList[i].ExplodeItem();
        }
    }

    private void ExplodeHorizontalLine()
    {
        _reusableExplodeList.Clear();

        Cell newcell = Cell;
        while (true)
        {
            Cell next = newcell.NeighbourRight;
            if (next == null) break;

            _reusableExplodeList.Add(next);
            newcell = next;
        }

        newcell = Cell;
        while (true)
        {
            Cell next = newcell.NeighbourLeft;
            if (next == null) break;

            _reusableExplodeList.Add(next);
            newcell = next;
        }


        for (int i = 0; i < _reusableExplodeList.Count; i++)
        {
            _reusableExplodeList[i].ExplodeItem();
        }

    }
}
