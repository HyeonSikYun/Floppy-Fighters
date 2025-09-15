using Fusion;
using UnityEngine;

public class RockProjectile : NetworkBehaviour
{
    public float lifetime = 10f;
    public float hitForce = 5f;
    private float _spawnTime;

    public override void Spawned()
    {
        _spawnTime = Time.time;
    }

    public override void FixedUpdateNetwork()
    {
        // Lifetime 지난 뒤 처리
        if (Time.time - _spawnTime >= lifetime)
        {
            if (IsOutOfArena() && Object.HasStateAuthority)
                Runner.Despawn(Object);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Rigidbody 있는 경우 힘 적용
        if (collision.rigidbody != null)
        {
            Vector3 dir = (collision.transform.position - transform.position).normalized;
            collision.rigidbody.AddForce(dir * hitForce, ForceMode.Impulse);
        }

        Transform hitRoot = collision.transform.root;

        // 플레이어나 AI 맞았을 때 레그돌 처리
        NetworkPlayer hitNetworkPlayer = hitRoot.GetComponent<NetworkPlayer>();
        AIPlayer hitAIPlayer = hitRoot.GetComponent<AIPlayer>();

        if (hitNetworkPlayer != null)
        {
            // 기존 HandPunchHandler처럼 레그돌 상태
            hitNetworkPlayer.OnPlayerBodyPartHit();
        }
        else if (hitAIPlayer != null)
        {
            hitAIPlayer.OnPlayerBodyPartHit();
        }

        // 맞았으면 돌 삭제
        if (Object.HasStateAuthority)
            Runner.Despawn(Object);
    }

    bool IsOutOfArena()
    {
        // Ground 레이어 위면 유지
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                return false;
        }

        // y좌표가 낮으면 제거
        if (transform.position.y < -1f) return true;

        return true;
    }
}
