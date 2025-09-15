using UnityEngine;

public class LobbyRotateObject : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 rotationAmount;
    [SerializeField] float smoothSpeed = 10f;
    [SerializeField] bool enablePlayerCollision = true; // �÷��̾�� �浹 �� ���׵� ó�� ����

    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // ȸ�� ���
        Vector3 rotateBy = targetRotation.eulerAngles + rotationAmount * Time.deltaTime;
        targetRotation = Quaternion.Euler(rotateBy);

        // �ε巯�� ȸ�� ����
        Quaternion smoothRotation = Quaternion.Lerp(transform.rotation, targetRotation,
            smoothSpeed * Time.deltaTime);

        // ���� ����
        if (rb != null)
            rb.MoveRotation(smoothRotation);
        else
            transform.rotation = smoothRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enablePlayerCollision) return;

        // �÷��̾�� �浹 üũ
        HandlePlayerCollision(collision);
    }

    private void HandlePlayerCollision(Collision collision)
    {
        // LobbyPlayer üũ
        LobbyPlayer lobbyPlayer = collision.transform.root.GetComponent<LobbyPlayer>();
        if (lobbyPlayer != null)
        {
            lobbyPlayer.OnPlayerBodyPartHit();
            return;
        }

        // NetworkPlayer üũ (Ȥ�� ���� ���� ���� ���)
        NetworkPlayer networkPlayer = collision.transform.root.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            networkPlayer.OnPlayerBodyPartHit();
            return;
        }

        // AIPlayer üũ (Ȥ�� ���� ���� ���� ���)  
        AIPlayer aiPlayer = collision.transform.root.GetComponent<AIPlayer>();
        if (aiPlayer != null)
        {
            aiPlayer.OnPlayerBodyPartHit();
            return;
        }
    }
}