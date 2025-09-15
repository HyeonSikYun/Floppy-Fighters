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
        // Lifetime ���� �� ó��
        if (Time.time - _spawnTime >= lifetime)
        {
            if (IsOutOfArena() && Object.HasStateAuthority)
                Runner.Despawn(Object);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Rigidbody �ִ� ��� �� ����
        if (collision.rigidbody != null)
        {
            Vector3 dir = (collision.transform.position - transform.position).normalized;
            collision.rigidbody.AddForce(dir * hitForce, ForceMode.Impulse);
        }

        Transform hitRoot = collision.transform.root;

        // �÷��̾ AI �¾��� �� ���׵� ó��
        NetworkPlayer hitNetworkPlayer = hitRoot.GetComponent<NetworkPlayer>();
        AIPlayer hitAIPlayer = hitRoot.GetComponent<AIPlayer>();

        if (hitNetworkPlayer != null)
        {
            // ���� HandPunchHandleró�� ���׵� ����
            hitNetworkPlayer.OnPlayerBodyPartHit();
        }
        else if (hitAIPlayer != null)
        {
            hitAIPlayer.OnPlayerBodyPartHit();
        }

        // �¾����� �� ����
        if (Object.HasStateAuthority)
            Runner.Despawn(Object);
    }

    bool IsOutOfArena()
    {
        // Ground ���̾� ���� ����
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                return false;
        }

        // y��ǥ�� ������ ����
        if (transform.position.y < -1f) return true;

        return true;
    }
}
