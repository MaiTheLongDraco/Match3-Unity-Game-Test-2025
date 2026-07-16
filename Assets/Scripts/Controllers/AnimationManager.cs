using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// AnimationManager — Quản lý tập trung tất cả animation trong game.
/// 
/// Nguyên tắc thiết kế:
///   • DOTween  — Chịu trách nhiệm nội suy giá trị (tweening engine).
///   • UniTask  — Chịu trách nhiệm await (không dùng coroutine, không gây GC allocation).
///   • Class này chỉ cung cấp API tĩnh/instance, KHÔNG giữ state của từng item.
///   • Mỗi tween trả về UniTask để caller có thể await hoặc fire-and-forget tuỳ ý.
/// </summary>
public class AnimationManager : MonoBehaviour
{
    // ─── Cấu hình duration (có thể expose ra Inspector hoặc GameSettings sau) ────

    [Header("Duration (seconds)")]
    [SerializeField] private float _moveDuration     = 0.3f;
    [SerializeField] private float _swapDuration     = 0.2f;
    [SerializeField] private float _appearDuration   = 0.1f;
    [SerializeField] private float _explodeDuration  = 0.1f;
    [SerializeField] private float _hintPunchScale   = 0.1f;
    [SerializeField] private float _hintDuration     = 0.5f;

    // ─── Singleton ────────────────────────────────────────────────────────────────

    public static AnimationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ServiceLocator.Register<AnimationManager>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<AnimationManager>();
        if (Instance == this) Instance = null;
    }

    // ─── Board Animations ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dịch chuyển item đến vị trí cell đang giữ nó.
    /// Fire-and-forget: dùng khi không cần chờ xong.
    /// </summary>
    public void MoveToCell(Transform view, Vector3 targetPos)
    {
        if (view == null) return;
        view.DOKill(false);
        view.DOMove(targetPos, _moveDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Dịch chuyển item đến vị trí — trả về UniTask để caller có thể await.
    /// </summary>
    public UniTask MoveToCellAsync(Transform view, Vector3 targetPos)
    {
        if (view == null) return UniTask.CompletedTask;
        view.DOKill(false);
        return view.DOMove(targetPos, _moveDuration)
                   .SetEase(Ease.OutQuad)
                   .ToUniTask();
    }

    /// <summary>
    /// Hoán đổi vị trí 2 item cùng lúc — await cho đến khi cả 2 hoàn tất.
    /// </summary>
    public async UniTask SwapAsync(Transform view1, Vector3 target1,
                                   Transform view2, Vector3 target2)
    {
        if (view1 == null || view2 == null) return;

        view1.DOKill(false);
        view2.DOKill(false);
        
        // Chạy song song, await cả 2 hoàn tất (Zero GC khi dùng ToUniTask)
        await UniTask.WhenAll(
            view1.DOMove(target1, _swapDuration).SetEase(Ease.OutQuad).ToUniTask(),
            view2.DOMove(target2, _swapDuration).SetEase(Ease.OutQuad).ToUniTask()
        );
    }

    /// <summary>
    /// Animation xuất hiện của item mới: scale từ nhỏ → bình thường.
    /// </summary>
    public void PlayAppear(Transform view)
    {
        if (view == null) return;

        Vector3 originalScale = view.localScale;
        view.localScale = Vector3.one * 0.1f;
        view.DOKill(false);
        view.DOScale(originalScale, _appearDuration).SetEase(Ease.OutBack);
    }

    public UniTask PlayAppearAsync(Transform view)
    {
        if (view == null) return UniTask.CompletedTask;

        Vector3 originalScale = view.localScale;
        view.localScale = Vector3.one * 0.1f;
        view.DOKill(false);
        return view.DOScale(originalScale, _appearDuration)
                   .SetEase(Ease.OutBack)
                   .ToUniTask().ContinueWith(() =>
                   {
                       view.localScale = Vector3.one;
                   });
    }

    /// <summary>
    /// Animation nổ của item: scale về 0, sau đó trả về pool.
    /// </summary>
    public async UniTask PlayExplodeAsync(Transform view, string poolKey)
    {
        if (view == null) return;

        view.DOKill(false);

        // Await trực tiếp ToUniTask() (Zero GC) thay vì dùng OnComplete callback sinh ra closure
        await view.DOScale(0.1f, _explodeDuration)
                  .SetEase(Ease.InBack)
                  .ToUniTask();

        // Sau khi nổ xong thì trả về pool
        ViewPool.Return(poolKey, view.gameObject);
    }
    /// <summary>
    /// Animation hint: lắc nhẹ scale theo vòng lặp.
    /// Trả về Tween để caller có thể Kill() khi cần.
    /// </summary>
    public Tween PlayHintLoop(Transform view)
    {
        if (view == null) return null;

        view.DOKill(false);
        return view.DOPunchScale(view.localScale * _hintPunchScale, _hintDuration)
                   .SetLoops(-1, LoopType.Restart);
    }

    /// <summary>
    /// Dừng hint animation và reset scale về bình thường.
    /// </summary>
    public void StopHint(Transform view, Vector3 originalScale)
    {
        if (view == null) return;

        view.DOKill(false);
        view.localScale = originalScale;
    }

    // ─── Batch Animations ─────────────────────────────────────────────────────────

    /// <summary>
    /// Chạy nhiều animation xuất hiện song song, await cho đến khi tất cả xong.
    /// Dùng khi Fill board hoặc FillGapsWithNewItems.
    /// </summary>
    public async UniTask PlayAllAppearsAsync(List<Transform> views)
    {
        if (views == null || views.Count == 0) return;

        var tasks = new UniTask[views.Count];

        for (int i = 0; i < views.Count; i++)
        {
            tasks[i] = PlayAppearAsync(views[i]);
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// Nổ một danh sách item, await đến khi tất cả xong, rồi trả về pool.
    /// Dùng trong BoardController khi xử lý match.
    /// </summary>
    public async UniTask PlayAllExplodesAsync(List<(Transform view, string poolKey)> targets)
    {
        if (targets == null || targets.Count == 0) return;

        var tasks = new UniTask[targets.Count];

        for (int i = 0; i < targets.Count; i++)
        {
            tasks[i] = PlayExplodeAsync(targets[i].view, targets[i].poolKey);
        }

        await UniTask.WhenAll(tasks);
    }

    // ─── DOTween Global Controls ──────────────────────────────────────────────────

    public void PauseAll()  => DOTween.PauseAll();
    public void ResumeAll() => DOTween.PlayAll();
    public void KillAll()   => DOTween.KillAll(false);
}
