using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[Serializable]
public class Item
{
    public Cell Cell { get; private set; }

    public Transform View { get; private set; }

    // Lưu prefab key để có thể Return đúng pool khi object bị huỷ
    private string _cachedPrefabName;

    public virtual void SetView()
    {
        string prefabname = GetPrefabName();

        if (!string.IsNullOrEmpty(prefabname))
        {
            GameObject go = ViewPool.Get(prefabname);
            if (go != null)
            {
                _cachedPrefabName = prefabname;
                View = go.transform;
                ApplySkin();
            }
        }
    }

    public virtual void ApplySkin()
    {
        // Subclasses can override this to fetch correct sprite from SkinManager
    }

    public void SetSprite(Sprite sprite)
    {
        if (View != null && sprite != null)
        {
            SpriteRenderer sr = View.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
            }
        }
    }

    protected virtual string GetPrefabName() { return string.Empty; }

    public virtual void SetCell(Cell cell)
    {
        Cell = cell;
    }

    internal void AnimationMoveToPosition()
    {
        if (View == null) return;

        View.DOMove(Cell.transform.position, 0.2f);
    }

    public void SetViewPosition(Vector3 pos)
    {
        if (View)
        {
            View.position = pos;
        }
    }

    public void SetViewRoot(Transform root)
    {
        if (View)
        {
            View.SetParent(root);
        }
    }

    public void SetSortingLayerHigher()
    {
        if (View == null) return;

        SpriteRenderer sp = View.GetComponent<SpriteRenderer>();
        if (sp)
        {
            sp.sortingOrder = 1;
        }
    }


    public void SetSortingLayerLower()
    {
        if (View == null) return;

        SpriteRenderer sp = View.GetComponent<SpriteRenderer>();
        if (sp)
        {
            sp.sortingOrder = 0;
        }

    }

    internal void ShowAppearAnimation()
    {
        if (View == null) return;

        Vector3 scale = View.localScale;
        View.localScale = Vector3.one * 0.1f;
        View.DOScale(scale, 0.1f);
    }

    internal virtual bool IsSameType(Item other)
    {
        return false;
    }

    internal virtual void ExplodeView()
    {
        if (View)
        {
            // Capture reference trước khi null hoá View để tránh stale closure bug.
            // View = null ngay lập tức để các lệnh gọi khác (Clear, StopHint)
            // không tương tác với object đang trong animation.
            Transform viewRef   = View;
            string    poolKey   = _cachedPrefabName;
            View = null;

            viewRef.DOScale(0.1f, 0.1f).OnComplete(() =>
            {
                ViewPool.Return(poolKey, viewRef.gameObject);
            });
        }
    }



    internal void AnimateForHint()
    {
        if (View)
        {
            View.DOPunchScale(View.localScale * 0.1f, 0.1f).SetLoops(-1);
        }
    }

    internal void StopAnimateForHint()
    {
        if (View)
        {
            View.DOKill();
        }
    }

    internal void Clear()
    {
        Cell = null;

        if (View)
        {
            // Return về pool ngay (không có animation) — DOKill được gọi bên trong Return
            ViewPool.Return(_cachedPrefabName, View.gameObject);
            View = null;
        }
    }
}
