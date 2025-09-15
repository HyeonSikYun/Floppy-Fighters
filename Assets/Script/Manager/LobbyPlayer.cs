using UnityEngine;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator anim;

    // 입력 관련
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;
    bool isReviveButtonPressed = false;
    bool isGrabButtonPressed = false;

    float maxSpeed = 3f;
    bool isGrounded = false;
    bool isActiveRagdoll = true;
    public bool IsActiveRagdoll => isActiveRagdoll;
    bool isGrabingActive = false;
    public bool IsGrabingActive => isGrabingActive;
    public bool IsGrabPressed => isGrabButtonPressed;

    RaycastHit[] raycastHits = new RaycastHit[18];
    SyncPhysicsObject[] syncPhysicsObjects;

    //펀치 관련
    bool isPunchButtonPressed = false;
    bool isPunchActive = false;
    public bool IsPunchActive => isPunchActive;
    float lastPunchTime = 0f;
    float punchCooldown = 0.5f; // 펀치 쿨다운

    float startSlerpPositionSpring = 0.0f;
    float lastTimeBecameRagdol = 0;

    HandGrabHandler[] handGrabHandlers;
    HandPunchHandler[] handPunchHandlers;

    // 로비용 상태 관리 (네트워크 없이)
    public bool IsFallen { get; set; } = false;
    public bool IsRespawning { get; set; } = false;
    public bool IsAlive => !IsFallen && IsActiveRagdoll;

    private void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        handGrabHandlers = GetComponentsInChildren<HandGrabHandler>();
        handPunchHandlers = GetComponentsInChildren<HandPunchHandler>();
    }

    void Start()
    {
        startSlerpPositionSpring = mainJoint.slerpDrive.positionSpring;

        // 로비에서는 마우스 커서 설정 (디버깅용으로 주석처리)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        // 입력 처리 (R키 리스폰 포함)
        moveInputVector.x = Input.GetAxisRaw("Horizontal");
        moveInputVector.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.R))
            isReviveButtonPressed = true;

        if (Input.GetMouseButtonDown(0))
            isPunchButtonPressed = true;

        isGrabButtonPressed = Input.GetKey(KeyCode.G);
    }

    Vector3 GetMoveDirection()
    {
        // 로비에서는 카메라 회전 없이 월드 기준으로 움직임
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        return (forward * moveInputVector.y + right * moveInputVector.x).normalized;
    }

    void FixedUpdate()
    {
        Vector3 localVelocifyVsForward = Vector3.zero;
        float localForwardVelocity = 0;

        // Ground check
        isGrounded = false;
        int numberOfHits = Physics.SphereCastNonAlloc(rb.position, 0.1f, Vector3.down, raycastHits, 0.5f);

        for (int i = 0; i < numberOfHits; i++)
        {
            if (raycastHits[i].transform.root == transform) continue;
            isGrounded = true;
            break;
        }
        if (!isGrounded)
            rb.AddForce(Vector3.down * 10);

        localVelocifyVsForward = transform.forward * Vector3.Dot(transform.forward, rb.linearVelocity);
        localForwardVelocity = localVelocifyVsForward.magnitude;

        // 리스폰 처리
        if (IsFallen && isReviveButtonPressed)
        {
            Respawn();
        }

        float inputMagnitued = moveInputVector.magnitude;

        if (isActiveRagdoll && !IsFallen) // 떨어진 상태에서는 움직임 처리 안함
        {
            if (inputMagnitued != 0)
            {
                Vector3 moveDirection = GetMoveDirection();

                if (moveDirection != Vector3.zero)
                {
                    Quaternion desiredRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.fixedDeltaTime * 10f);
                    mainJoint.targetRotation = Quaternion.Inverse(transform.rotation);

                    if (localForwardVelocity < maxSpeed)
                    {
                        rb.AddForce(moveDirection * inputMagnitued * 30);
                    }
                }
            }
            else
            {
                // 입력이 없을 때는 targetRotation을 현재 rotation의 역방향으로 유지
                mainJoint.targetRotation = Quaternion.Inverse(transform.rotation);
            }

            if (isGrounded && isJumpButtonPressed)
            {
                rb.AddForce(Vector3.up * 15, ForceMode.Impulse);
            }

            isGrabingActive = isGrabButtonPressed;

            if (isPunchButtonPressed && Time.time - lastPunchTime > punchCooldown)
            {
                isPunchActive = true;
                lastPunchTime = Time.time;
            }
        }

        // 자동 액티브 레그돌 복귀
        if (!isActiveRagdoll && Time.time - lastTimeBecameRagdol > 3)
        {
            MakeActiveRagdoll();
        }

        // 애니메이션 업데이트
        anim.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            if (isActiveRagdoll)
            {
                syncPhysicsObjects[i].UpdateJointFromAnimation();
            }
        }

        // 낙사 처리
        if (!IsFallen && transform.position.y < -10)
        {
            FallOut();
        }

        foreach (HandGrabHandler handGrabHandler in handGrabHandlers)
            handGrabHandler.UpdateState();

        foreach (HandPunchHandler handPunchHandler in handPunchHandlers)
            handPunchHandler.UpdateState();

        if (isPunchActive && Time.time - lastPunchTime > 0.2f)
        {
            isPunchActive = false;
        }

        // 입력 플래그 초기화
        isJumpButtonPressed = false;
        isPunchButtonPressed = false;
        isReviveButtonPressed = false;
    }

    public void OnPlayerBodyPartHit()
    {
        if (!isActiveRagdoll) return;
        MakeRagdoll();
    }

    public void MakeRagdoll()
    {
        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = 0;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeRagdoll();

        lastTimeBecameRagdol = Time.time;
        isActiveRagdoll = false;
        isGrabingActive = false;
        isPunchActive = false;
    }

    void MakeActiveRagdoll()
    {
        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = startSlerpPositionSpring;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeActiveRagdoll();

        isActiveRagdoll = true;
        isGrabingActive = false;
        isPunchActive = false;
    }

    void FallOut()
    {
        IsFallen = true;
        // 로비에서는 텔레포트 대신 직접 위치 변경
        // transform.position = Vector3.zero;
    }

    void Respawn()
    {
        IsFallen = false;
        transform.position = Vector3.up * 2;
        MakeActiveRagdoll();
    }
}