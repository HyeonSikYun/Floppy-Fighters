using UnityEngine;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator anim;

    // �Է� ����
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

    //��ġ ����
    bool isPunchButtonPressed = false;
    bool isPunchActive = false;
    public bool IsPunchActive => isPunchActive;
    float lastPunchTime = 0f;
    float punchCooldown = 0.5f; // ��ġ ��ٿ�

    float startSlerpPositionSpring = 0.0f;
    float lastTimeBecameRagdol = 0;

    HandGrabHandler[] handGrabHandlers;
    HandPunchHandler[] handPunchHandlers;

    // �κ�� ���� ���� (��Ʈ��ũ ����)
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

        // �κ񿡼��� ���콺 Ŀ�� ���� (���������� �ּ�ó��)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        // �Է� ó�� (RŰ ������ ����)
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
        // �κ񿡼��� ī�޶� ȸ�� ���� ���� �������� ������
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

        // ������ ó��
        if (IsFallen && isReviveButtonPressed)
        {
            Respawn();
        }

        float inputMagnitued = moveInputVector.magnitude;

        if (isActiveRagdoll && !IsFallen) // ������ ���¿����� ������ ó�� ����
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
                // �Է��� ���� ���� targetRotation�� ���� rotation�� ���������� ����
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

        // �ڵ� ��Ƽ�� ���׵� ����
        if (!isActiveRagdoll && Time.time - lastTimeBecameRagdol > 3)
        {
            MakeActiveRagdoll();
        }

        // �ִϸ��̼� ������Ʈ
        anim.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            if (isActiveRagdoll)
            {
                syncPhysicsObjects[i].UpdateJointFromAnimation();
            }
        }

        // ���� ó��
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

        // �Է� �÷��� �ʱ�ȭ
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
        // �κ񿡼��� �ڷ���Ʈ ��� ���� ��ġ ����
        // transform.position = Vector3.zero;
    }

    void Respawn()
    {
        IsFallen = false;
        transform.position = Vector3.up * 2;
        MakeActiveRagdoll();
    }
}