using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private Cell[,] m_cells;

    private Transform m_root;

    private int m_matchMin;

    private List<NormalItem.eNormalType> _reusableTypeList = new List<NormalItem.eNormalType>();
    private List<Cell> _reusableHorMatchList = new List<Cell>();
    private List<Cell> _reusableVertMatchList = new List<Cell>();
    private List<Cell> _reusableFirstMatchList = new List<Cell>();
    private List<Cell> _reusablePotentialMatchesList = new List<Cell>();
    private List<Item> _reusableShuffleList = new List<Item>();
    private List<Cell> _reusableBonusCheckList = new List<Cell>();

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];

        CreateBoard();
        PrewarmPool();

        try
        {
            ServiceLocator.Resolve<SkinManager>().RegisterBoard(this);
        }
        catch { }
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        
        GameObject prefabBG = ResourceCache.GetPrefab(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

    }

    /// <summary>
    /// Pre-instantiate các Item View vào pool ngay sau khi tạo board.
    /// Dựa trên board size để tính số lượng hợp lý, tránh auto-expand lag ở frame đầu.
    /// </summary>
    private void PrewarmPool()
    {
        int boardSize = boardSizeX * boardSizeY;

        // Phân bổ đều cho 7 loại NormalItem + buffer 2 để hấp thụ combo matches
        int countPerNormalType = Mathf.CeilToInt((float)boardSize / 7f) + 2;
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_ONE,   countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_TWO,   countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_THREE, countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_FOUR,  countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_FIVE,  countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_SIX,   countPerNormalType);
        ViewPool.Prewarm(Constants.PREFAB_NORMAL_TYPE_SEVEN, countPerNormalType);

        // BonusItem: xuất hiện ít hơn, prewarm ít
        ViewPool.Prewarm(Constants.PREFAB_BONUS_HORIZONTAL, 2);
        ViewPool.Prewarm(Constants.PREFAB_BONUS_VERTICAL,   2);
        ViewPool.Prewarm(Constants.PREFAB_BONUS_BOMB,       2);
    }

    internal void Fill()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();

                _reusableTypeList.Clear();
                if (cell.NeighbourBottom != null)
                {
                    NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
                    if (nitem != null)
                    {
                        _reusableTypeList.Add(nitem.ItemType);
                    }
                }

                if (cell.NeighbourLeft != null)
                {
                    NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
                    if (nitem != null)
                    {
                        _reusableTypeList.Add(nitem.ItemType);
                    }
                }

                item.SetType(Utils.GetRandomNormalTypeExcept(_reusableTypeList));
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(false);
            }
        }
    }

    internal void Shuffle()
    {
        _reusableShuffleList.Clear();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                _reusableShuffleList.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, _reusableShuffleList.Count);
                m_cells[x, y].Assign(_reusableShuffleList[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                _reusableShuffleList.RemoveAt(rnd);
            }
        }
    }


    // ─── Reusable buffers cho FillGapsWithNewItems (zero-alloc) ─────────────────
    private static readonly NormalItem.eNormalType[] _allNormalTypes =
        (NormalItem.eNormalType[])System.Enum.GetValues(typeof(NormalItem.eNormalType));
    private readonly int[] _typeCounts = new int[7]; // 7 loại NormalItem
    private readonly NormalItem.eNormalType[] _candidateBuffer = new NormalItem.eNormalType[7];

    internal void FillGapsWithNewItems()
    {
        // Bước 1: Đếm số lượng từng loại NormalItem hiện có trên board
        System.Array.Clear(_typeCounts, 0, _typeCounts.Length);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                NormalItem ni = m_cells[x, y].Item as NormalItem;
                if (ni != null)
                {
                    _typeCounts[(int)ni.ItemType]++;
                }
            }
        }

        // Bước 2: Duyệt các ô trống, sinh item mới
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty) continue;

                // Bước 2a: Thu thập type của 4 ô xung quanh để loại trừ
                _reusableTypeList.Clear();
                AddNeighbourType(cell.NeighbourUp);
                AddNeighbourType(cell.NeighbourBottom);
                AddNeighbourType(cell.NeighbourLeft);
                AddNeighbourType(cell.NeighbourRight);

                // Bước 2b: Lọc ra các type hợp lệ (không trùng neighbour)
                int candidateCount = 0;
                for (int i = 0; i < _allNormalTypes.Length; i++)
                {
                    bool excluded = false;
                    for (int j = 0; j < _reusableTypeList.Count; j++)
                    {
                        if (_allNormalTypes[i] == _reusableTypeList[j])
                        {
                            excluded = true;
                            break;
                        }
                    }
                    if (!excluded)
                    {
                        _candidateBuffer[candidateCount++] = _allNormalTypes[i];
                    }
                }

                // Fallback: nếu tất cả 7 type đều bị loại (gần như bất khả thi), dùng random
                if (candidateCount == 0)
                {
                    _candidateBuffer[0] = Utils.GetRandomNormalType();
                    candidateCount = 1;
                }

                // Bước 2c: Trong các candidate hợp lệ, chọn type có số lượng ÍT NHẤT trên board
                NormalItem.eNormalType chosenType = _candidateBuffer[0];
                int minCount = _typeCounts[(int)chosenType];

                for (int i = 1; i < candidateCount; i++)
                {
                    int c = _typeCounts[(int)_candidateBuffer[i]];
                    if (c < minCount)
                    {
                        minCount = c;
                        chosenType = _candidateBuffer[i];
                    }
                    else if (c == minCount)
                    {
                        // Nếu bằng nhau thì random 50/50 để tránh bias luôn chọn type đầu tiên
                        if (UnityEngine.Random.value > 0.5f)
                        {
                            chosenType = _candidateBuffer[i];
                        }
                    }
                }

                // Bước 3: Tạo item mới và cập nhật count
                NormalItem item = new NormalItem();
                item.SetType(chosenType);
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(true);

                _typeCounts[(int)chosenType]++;
            }
        }
    }

    private void AddNeighbourType(Cell neighbour)
    {
        if (neighbour == null) return;
        NormalItem ni = neighbour.Item as NormalItem;
        if (ni != null)
        {
            _reusableTypeList.Add(ni.ItemType);
        }
    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        // SwapAsync fire-and-forget, callback được gọi sau khi cả 2 hoàn tất
        SwapWithCallbackAsync(item, cell2, item2, cell1, callback).Forget();
    }

    private async UniTask SwapWithCallbackAsync(
        Item item, Cell target1, Item item2, Cell target2, Action callback)
    {
        if (AnimationManager.Instance != null)
        {
            await AnimationManager.Instance.SwapAsync(
                item.View,  target1.transform.position,
                item2.View, target2.transform.position);
        }
        callback?.Invoke();
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        _reusableHorMatchList.Clear();
        _reusableHorMatchList.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _reusableHorMatchList.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _reusableHorMatchList.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return _reusableHorMatchList;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        _reusableVertMatchList.Clear();
        _reusableVertMatchList.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _reusableVertMatchList.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                _reusableVertMatchList.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return _reusableVertMatchList;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetView();
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

        bool allSameX = true, allSameY = true;
        int firstX = matches[0].BoardX, firstY = matches[0].BoardY;
        
        for (int i = 1; i < matches.Count; i++)
        {
            if (matches[i].BoardX != firstX) allSameX = false;
            if (matches[i].BoardY != firstY) allSameY = false;
            if (!allSameX && !allSameY) break;
        }

        if (allSameX) return eMatchDirection.VERTICAL;
        if (allSameY) return eMatchDirection.HORIZONTAL;
        if (matches.Count > 5) return eMatchDirection.ALL;
        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        _reusableFirstMatchList.Clear();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    return listhor;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    return listvert;
                }
            }
        }

        return _reusableFirstMatchList;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        Cell bonus = null;
        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].Item is BonusItem)
            {
                bonus = matches[i];
                break;
            }
        }
        if(bonus == null)
        {
            return matches;
        }

        _reusableBonusCheckList.Clear();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        _reusableBonusCheckList.Add(matches[i]);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        _reusableBonusCheckList.Add(matches[i]);
                    }
                }
                break;
            case eMatchDirection.ALL:
                for (int i = 0; i < matches.Count; i++)
                {
                    BonusItem item = matches[i].Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        _reusableBonusCheckList.Add(matches[i]);
                    }
                }
                break;
        }

        return _reusableBonusCheckList;
    }

    internal List<Cell> GetPotentialMatches()
    {
        _reusablePotentialMatchesList.Clear();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */

                if (cell.NeighbourRight != null)
                {
                    GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight, _reusablePotentialMatchesList);
                    if (_reusablePotentialMatchesList.Count > 0)
                    {
                        break;
                    }
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp != null)
                {
                    GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp, _reusablePotentialMatchesList);
                    if (_reusablePotentialMatchesList.Count > 0)
                    {
                        break;
                    }
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom != null)
                {
                    GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom, _reusablePotentialMatchesList);
                    if (_reusablePotentialMatchesList.Count > 0)
                    {
                        break;
                    }
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft != null)
                {
                    GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft, _reusablePotentialMatchesList);
                    if (_reusablePotentialMatchesList.Count > 0)
                    {
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second != null)
                    {
                        _reusablePotentialMatchesList.Add(cell);
                        _reusablePotentialMatchesList.Add(neib.NeighbourRight);
                        _reusablePotentialMatchesList.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = null;
                neib = cell.NeighbourUp;
                if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second != null)
                    {
                        _reusablePotentialMatchesList.Add(cell);
                        _reusablePotentialMatchesList.Add(neib.NeighbourUp);
                        _reusablePotentialMatchesList.Add(second);
                        break;
                    }
                }
            }

            if (_reusablePotentialMatchesList.Count > 0) break;
        }

        return _reusablePotentialMatchesList;
    }

    private void GetPotentialMatch(Cell cell, Cell neighbour, Cell target, List<Cell> result)
    {
        result.Clear();

        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                AnimationManager.Instance?.MoveToCell(item.View, holder.transform.position);
            }
        }
    }

    public void Clear()
    {
        try
        {
            ServiceLocator.Resolve<SkinManager>().UnregisterBoard();
        }
        catch { }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }

    public void ReskinAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell != null && cell.Item != null)
                {
                    cell.Item.ApplySkin();
                }
            }
        }
    }
}
