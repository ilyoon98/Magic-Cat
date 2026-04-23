using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Unit : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 3;
    public int currentHp;
    public int attackDamage = 1;
    public int attackRange = 1;

    public Vector2Int GridPos { get; protected set; }
    public bool IsAlive => currentHp > 0;
    public bool IsBoss { get; private set; }
    public void SetAsBoss() { IsBoss = true; }

    private bool placed = false;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    public virtual void PlaceOnBoard(Vector2Int pos)
    {
        Tile oldTile = BoardManager.Instance.GetTile(GridPos);
        oldTile?.ClearUnit();

        GridPos = pos;
        Vector3 targetPos = BoardManager.Instance.GridToWorld(pos);

        Tile newTile = BoardManager.Instance.GetTile(pos);
        newTile?.SetUnit(this);

        if (!placed)
        {
            transform.position = targetPos;
            placed = true;
            // 초기 배치 시에도 바닥 오브젝트 체크
            FloorObjectManager.Instance?.OnUnitEnterTile(this, pos);
        }
        else
        {
            StartCoroutine(SmoothMoveAndCheck(targetPos, pos));
        }
    }

    private IEnumerator SmoothMoveAndCheck(Vector3 target, Vector2Int gridPos)
    {
        float duration = 0.15f;
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
        // 이동 완료 후 바닥 오브젝트 발동
        FloorObjectManager.Instance?.OnUnitEnterTile(this, gridPos);
    }

    public virtual void TakeDamage(int amount, bool isCritical = false)
    {
        // 치트: 무적 (플레이어만)
        if (this is PlayerUnit &&
            CheatManager.Instance != null && CheatManager.Instance.Invincible)
        {
            DamagePopup.Create(transform.position, 0, isHeal: false);
            return;
        }

        currentHp -= amount;
        currentHp = Mathf.Max(0, currentHp);

        // 피격 이펙트
        if (this is PlayerUnit)
            EffectManager.Instance?.PlayPlayerHit(transform.position);
        else
            EffectManager.Instance?.PlayBlood(transform.position);

        // 데미지 팝업
        DamagePopup.Create(transform.position, amount, isHeal: false, isCritical: isCritical);

        // 피격 플래시
        StartCoroutine(HitFlash());

        OnHpChanged();
        if (!IsAlive) OnDeath();
    }

    public virtual void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Min(maxHp, currentHp);
        DamagePopup.Create(transform.position, amount, isHeal: true);
        OnHpChanged();
    }

    private IEnumerator HitFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = original;
        yield return new WaitForSeconds(0.06f);
        sr.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        sr.color = original;
    }

    protected virtual void OnHpChanged() { }

    protected virtual void OnDeath()
    {
        Debug.Log($"[Unit] {name} 사망");
        BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
        gameObject.SetActive(false);
    }

    // 기본 공격 (이펙트 포함)
    public virtual void Attack(Unit target)
    {
        if (target == null || !target.IsAlive) return;
        int dist = Mathf.Abs(GridPos.x - target.GridPos.x) + Mathf.Abs(GridPos.y - target.GridPos.y);
        if (dist > attackRange) return;

        EffectManager.Instance?.PlayAttack(target.transform.position);
        target.TakeDamage(attackDamage);
    }

    /// <summary>
    /// 강제 즉시 이동 (넉백 전용) — 애니메이션·바닥오브젝트 트리거 없음
    /// </summary>
    public void ForceMove(Vector2Int pos)
    {
        BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
        GridPos = pos;
        transform.position = BoardManager.Instance.GridToWorld(pos);
        BoardManager.Instance.GetTile(pos)?.SetUnit(this);
    }

    /// <summary>
    /// 부드러운 넉백 애니메이션 — 경로를 따라 타일마다 Lerp 이동 후 onComplete 콜백
    /// </summary>
    public void StartSmoothKnockback(List<Vector2Int> path, System.Action onComplete)
    {
        StartCoroutine(SmoothKnockbackCoroutine(path, onComplete));
    }

    private IEnumerator SmoothKnockbackCoroutine(List<Vector2Int> path, System.Action onComplete)
    {
        const float stepDuration = 0.07f; // 타일 1칸당 이동 시간
        foreach (var pos in path)
        {
            Vector3 startPos  = transform.position;
            Vector3 targetPos = BoardManager.Instance.GridToWorld(pos);

            // 그리드 위치 선점 (시각적 이동 전에 점유 처리)
            BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
            GridPos = pos;
            BoardManager.Instance.GetTile(pos)?.SetUnit(this);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
        }
        onComplete?.Invoke();
    }

    public bool IsInAttackRange(Unit target)
    {
        int dist = Mathf.Abs(GridPos.x - target.GridPos.x) + Mathf.Abs(GridPos.y - target.GridPos.y);
        if (dist > attackRange) return false;
        // 같은 행/열이면 벽 관통 여부 확인 (땅 원소 벽 등)
        return GridUtil.HasClearLine(GridPos, target.GridPos);
    }
}
