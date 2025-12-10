using UnityEngine;

public class UnscaledMoveEffect : MonoBehaviour
{
    private Vector3 _dir;
    private float _speed;
    private float _lifeTime;
    private float _elapsed;

    private bool _initialized;

    /// <summary>
    /// 방향, 속도, 생존시간 설정
    /// </summary>
    public void Init(Vector2 dir, float speed, float lifeTime)
    {
        _dir = dir.normalized;
        _speed = speed;
        _lifeTime = lifeTime;
        _elapsed = 0f;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized)
            return;

        float dt = Time.unscaledDeltaTime;

        // 타임스케일과 무관하게 이동
        transform.position += _dir * _speed * dt;

        // 수명 관리
        if (_lifeTime > 0f)
        {
            _elapsed += dt;
            if (_elapsed >= _lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}
