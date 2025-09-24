using DG.Tweening;
using UnityEngine;

[ExecuteInEditMode]
public class LoadingIconAnimation : MonoBehaviour
{
    public float TargetAngle = 180f; // Góc cần xoay
    public float RotateSpeed = 90f; // Tốc độ xoay (độ/giây)

    private Tween _tween;
    
    private void Start()
    {
        _tween ??= RotateTween();
    }

    private void OnEnable()
    {
        // Đảm bảo tween tồn tại
        if (_tween == null)
        {
            _tween = RotateTween();
        }

        // Khởi động lại tween
        _tween.Restart();
    }

    private void OnDisable()
    {
        // Dừng tween khi script bị vô hiệu hóa
        _tween?.Kill();
        _tween = null; // Giải phóng tween để tránh lỗi khi kích hoạt lại
    }

    private Tween RotateTween()
    {
        // Kiểm tra tốc độ hợp lệ
        if (Mathf.Approximately(RotateSpeed, 0))
        {
            Debug.LogError("RotateSpeed không thể bằng 0!");
            return null;
        }

        // Tính toán thời gian tween dựa trên góc và tốc độ
        float duration = Mathf.Abs(TargetAngle / RotateSpeed);

        // Tạo tween xoay
        return transform.DORotate(new Vector3(0, 0, TargetAngle), duration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart); // Lặp vô hạn
    }
}
