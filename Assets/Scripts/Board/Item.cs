using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

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

        AnimationManager.Instance?.MoveToCell(View, Cell.transform.position);
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

        AnimationManager.Instance?.PlayAppear(View);
    }

    internal virtual bool IsSameType(Item other)
    {
        return false;
    }

    internal virtual UniTask ExplodeViewAsync()
    {
        if (!View) return UniTask.CompletedTask;

        // Capture reference trước khi null hoá View để tránh stale closure bug.
        Transform viewRef = View;
        string    poolKey = _cachedPrefabName;
        View = null;

        return AnimationManager.Instance != null
            ? AnimationManager.Instance.PlayExplodeAsync(viewRef, poolKey)
            : UniTask.CompletedTask;
    }

    // Backward-compatible fire-and-forget wrapper
    internal virtual void ExplodeView()
    {
        ExplodeViewAsync().Forget();
    }



    internal void AnimateForHint()
    {
        if (View == null) return;
        _hintOriginalScale = View.localScale;
        AnimationManager.Instance?.PlayHintLoop(View);
    }

    internal void StopAnimateForHint()
    {
        if (View == null) return;
        AnimationManager.Instance?.StopHint(View, _hintOriginalScale);
    }

    private Vector3 _hintOriginalScale = Vector3.one;

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
