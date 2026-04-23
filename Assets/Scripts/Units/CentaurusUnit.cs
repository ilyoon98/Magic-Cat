using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Centaurus (보스) — 2턴 주기 돌진 패턴
///
/// [홀수 턴] 예고 or 이동
///   플레이어가 일직선(같은 행/열)에 있으면 → 돌진 예고 (Danger 하이라이트 + 방향 저장)
///   일직선이 아니면 → BFS 1칸 이동
///
/// [짝수 턴] 돌진 or 이동
///   예고가 있었으면(chargeDir != zero) → 저장된 방향으로 돌진
///   예고가 없었으면 → BFS 1칸 이동
///
/// [돌진]
///   저장된 방향으로 벽/맵 끝까지 고속 슬라이드.
///   - 경로에 플레이어 있으면 충돌 데미지
///   - 경로에 Earth 원소 벽 있으면 그 앞에서 멈춤
///   - 경로에 Water 원소 있으면 해당 타일 도달 시 물 효과 발동
/// </summary>
public class CentaurusUnit : EnemyUnit
{
    private int        centaurusTurn = 0;
    private Vector2Int chargeDir     = Vector2Int.zero;

    // 예고 하이라이트 타일 목록 (warningTiles와 별도 관리)
    private readonly List<Vector2Int> chargeDangerTiles = new List<Vector2Int>();

    protected override void Awake() { base.Awake(); }

    // ── 턴 진입점 ────────────────────────────────────────────────────────
    public override void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;
        centaurusTurn++;

        if (centaurusTurn % 2 == 1)   // 홀수 턴: 예고 or 이동
            ExecuteWarningOrMoveTurn(player);
        else                           // 짝수 턴: 돌진 or 이동
            ExecuteChargeOrMoveTurn(player);
    }

    // ── 홀수 턴: 일직선이면 예고, 아니면 이동 ────────────────────────────
    private void ExecuteWarningOrMoveTurn(PlayerUnit player)
    {
        ClearChargeDanger();
        chargeDir = Vector2Int.zero;

        // 일직선 여부 확인 (같은 행 또는 같은 열)
        bool inLine = player.GridPos.x == GridPos.x || player.GridPos.y == GridPos.y;
        if (!inLine)
        {
            // 일직선 아님 → BFS 1칸 이동
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next != null)
            {
                Tile tile = BoardManager.Instance.GetTile(next.Value);
                if (tile != null && !tile.IsOccupied && !tile.IsWall)
                    PlaceOnBoard(next.Value);
            }
            return;
        }

        // 일직선 → 돌진 예고
        chargeDir = GridUtil.SnapToCardinal(player.GridPos - GridPos);

        // 직선 경로 Danger 하이라이트
        Vector2Int pos = GridPos + chargeDir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            Tile tile = BoardManager.Instance.GetTile(pos);
            if (tile == null || tile.IsWall) break;

            tile.SetHighlight(Tile.HighlightType.Danger);
            chargeDangerTiles.Add(pos);

            if (pos == player.GridPos) break;
            if (tile.IsOccupied) break;

            pos += chargeDir;
        }

        GameUI.Instance?.ShowNotify($"⚡ {name} 돌진 예고!", 1.5f);
    }

    // ── 짝수 턴: 예고 있으면 돌진, 없으면 이동 ──────────────────────────
    private void ExecuteChargeOrMoveTurn(PlayerUnit player)
    {
        if (chargeDir == Vector2Int.zero)
        {
            // 예고 없었음 → BFS 1칸 이동
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next != null)
            {
                Tile tile = BoardManager.Instance.GetTile(next.Value);
                if (tile != null && !tile.IsOccupied && !tile.IsWall)
                    PlaceOnBoard(next.Value);
            }
            return;
        }

        // 돌진 실행
        ClearChargeDanger();

        var  chargePath  = new List<Vector2Int>();
        bool hitPlayer   = false;
        FloorObject waterObj = null;

        Vector2Int cur = GridPos;
        const int  maxSearch = 32;

        for (int i = 0; i < maxSearch; i++)
        {
            Vector2Int next     = cur + chargeDir;
            Tile       nextTile = BoardManager.Instance.GetTile(next);

            if (nextTile == null) break;
            if (nextTile.IsWall) break;

            var floorObj = FloorObjectManager.Instance?.GetAt(next);
            if (floorObj != null
                && floorObj.Type    == FloorObject.ObjectType.Element
                && floorObj.Element == FloorObject.ElementType.Water)
            {
                chargePath.Add(next);
                waterObj = floorObj;
                cur = next;
                break;
            }

            if (next == player.GridPos)
            {
                hitPlayer = true;
                break;
            }

            if (nextTile.IsOccupied) break;

            chargePath.Add(next);
            cur = next;
        }

        GameUI.Instance?.ShowNotify($"⚡ {name} 돌진!", 1.0f);

        bool        finalHitPlayer = hitPlayer;
        FloorObject finalWaterObj  = waterObj;
        PlayerUnit  capturedPlayer = player;

        StartCoroutine(ChargeCoroutine(chargePath, () =>
        {
            if (finalHitPlayer && capturedPlayer != null && capturedPlayer.IsAlive)
            {
                capturedPlayer.TakeDamage(attackDamage);
                EffectManager.Instance?.PlayExplosion(capturedPlayer.transform.position);
                GameUI.Instance?.ShowNotify($"💥 {name} 돌진 충돌! -{attackDamage}", 1.2f);
            }
            if (finalWaterObj != null)
                finalWaterObj.TryActivate(this);
        }));

        chargeDir = Vector2Int.zero;
    }

    private IEnumerator ChargeCoroutine(List<Vector2Int> path, System.Action onComplete)
    {
        const float stepDuration = 0.065f;
        Vector2Int prevPos = GridPos;
        foreach (var pos in path)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            UpdateFacing(prevPos, pos);

            Vector3 startPos  = transform.position;
            Vector3 targetPos = BoardManager.Instance.GridToWorld(pos);

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
            prevPos = pos;
        }
        onComplete?.Invoke();
    }

    // ── 예고 하이라이트 관리 ─────────────────────────────────────────────
    private void ClearChargeDanger()
    {
        foreach (var pos in chargeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        chargeDangerTiles.Clear();
    }

    /// <summary>TurnManager.RefreshAllEnemyWarnings에서 호출 — 예고 경로 재표시</summary>
    public override void RefreshWarning()
    {
        base.RefreshWarning();
        foreach (var pos in chargeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.Danger);
    }

    protected override void OnDeath()
    {
        ClearChargeDanger();
        base.OnDeath();
    }
}
