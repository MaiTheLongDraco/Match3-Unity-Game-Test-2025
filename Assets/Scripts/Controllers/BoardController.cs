using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }
    [SerializeField]
    private Board m_board;
    [SerializeField]
    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;
    [SerializeField]
    private GameSettings m_gameSettings;
    [SerializeField]
    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private float _dragCheckInterval = 0.05f;
    private float _lastDragCheck;

    // ─── Reusable buffers (Fix GC Spike #1) ───────────────────────────────────
    private readonly List<Cell> _reusableMatchResult = new List<Cell>(16);
    private readonly List<Cell> _reusableGetMatchResult = new List<Cell>(16);

    // ─── Cached yield instructions (Fix GC Spike #3) ─────────────────────────
    private static readonly WaitForSeconds _wait02 = new WaitForSeconds(0.2f);
    private static readonly WaitForSeconds _wait03 = new WaitForSeconds(0.3f);

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(m_cam.ScreenToWorldPoint(Input.mousePosition));
            if (hit != null)
            {
                m_isDragging = true;
                m_hitCollider = hit;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            if (Time.time - _lastDragCheck >= _dragCheckInterval)
            {
                _lastDragCheck = Time.time;
                Collider2D hit = Physics2D.OverlapPoint(m_cam.ScreenToWorldPoint(Input.mousePosition));
                if (hit != null)
                {
                    if (m_hitCollider != null && m_hitCollider != hit)
                    {
                        StopHints();

                        Cell c1 = m_hitCollider.GetComponent<Cell>();
                        Cell c2 = hit.GetComponent<Cell>();
                        if (AreItemsNeighbor(c1, c2))
                        {
                            IsBusy = true;
                            SetSortingLayer(c1, c2);
                            m_board.Swap(c1, c2, () =>
                            {
                                FindMatchesAndCollapse(c1, c2);
                            });

                            ResetRayCast();
                        }
                    }
                }
                else
                {
                    ResetRayCast();
                }
            }
        }
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            // Sử dụng _reusableMatchResult thay vì new List (Zero GC)
            _reusableMatchResult.Clear();

            // Lấy matches của cell1
            GetMatches(cell1, _reusableGetMatchResult);
            for (int i = 0; i < _reusableGetMatchResult.Count; i++)
            {
                _reusableMatchResult.Add(_reusableGetMatchResult[i]);
            }

            // Lấy matches của cell2, gộp vào (không trùng)
            GetMatches(cell2, _reusableGetMatchResult);
            for (int i = 0; i < _reusableGetMatchResult.Count; i++)
            {
                if (!_reusableMatchResult.Contains(_reusableGetMatchResult[i]))
                {
                    _reusableMatchResult.Add(_reusableGetMatchResult[i]);
                }
            }

            if (_reusableMatchResult.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(_reusableMatchResult, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    /// <summary>
    /// Ghi kết quả matches vào list truyền vào (Zero GC — không new List)
    /// </summary>
    private void GetMatches(Cell cell, List<Cell> result)
    {
        result.Clear();

        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count >= m_gameSettings.MatchesMin)
        {
            for (int i = 0; i < listHor.Count; i++)
            {
                result.Add(listHor[i]);
            }
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count >= m_gameSettings.MatchesMin)
        {
            for (int i = 0; i < listVert.Count; i++)
            {
                if (!result.Contains(listVert[i]))
                {
                    result.Add(listVert[i]);
                }
            }
        }
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return _wait02;

        m_board.FillGapsWithNewItems();

        yield return _wait02;

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return _wait02;

        m_board.Fill();

        yield return _wait02;

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return _wait03;

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
