using UnityEngine;

public class LobbyRotateObject : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 rotationAmount;
    [SerializeField] float smoothSpeed = 10f;
    [SerializeField] bool enablePlayerCollision = true; // 플레이어와 충돌 시 레그돌 처리 여부

    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // 회전 계산
        Vector3 rotateBy = targetRotation.eulerAngles + rotationAmount * Time.deltaTime;
        targetRotation = Quaternion.Euler(rotateBy);

        // 부드러운 회전 적용
        Quaternion smoothRotation = Quaternion.Lerp(transform.rotation, targetRotation,
            smoothSpeed * Time.deltaTime);

        // 최종 적용
        if (rb != null)
            rb.MoveRotation(smoothRotation);
        else
            transform.rotation = smoothRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enablePlayerCollision) return;

        // 플레이어와 충돌 체크
        HandlePlayerCollision(collision);
    }

    private void HandlePlayerCollision(Collision collision)
    {
        // LobbyPlayer 체크
        LobbyPlayer lobbyPlayer = collision.transform.root.GetComponent<LobbyPlayer>();
        if (lobbyPlayer != null)
        {
            lobbyPlayer.OnPlayerBodyPartHit();
            return;
        }

        // NetworkPlayer 체크 (혹시 같은 씬에 있을 경우)
        NetworkPlayer networkPlayer = collision.transform.root.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            networkPlayer.OnPlayerBodyPartHit();
            return;
        }

        // AIPlayer 체크 (혹시 같은 씬에 있을 경우)  
        AIPlayer aiPlayer = collision.transform.root.GetComponent<AIPlayer>();
        if (aiPlayer != null)
        {
            aiPlayer.OnPlayerBodyPartHit();
            return;
        }
    }
}