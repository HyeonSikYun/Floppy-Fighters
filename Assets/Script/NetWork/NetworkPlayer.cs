using Fusion;
using UnityEngine;
using Fusion.Addons.Physics;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; }

    [SerializeField] Rigidbody rb;
    [SerializeField] NetworkRigidbody3D networkRigidbody3D;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator anim;

    [Header("Camera Settings")]
    [SerializeField] float mouseSensitivity = 200f;
    [SerializeField] float camDistance = 5f;
    [SerializeField] float camHeight = 2f;
    [SerializeField] float verticalLookLimit = 80f;

    // �Է� ����
    Vector2 moveInputVector = Vector2.zero;
    Vector2 mouseInput = Vector2.zero;
    bool isJumpButtonPressed = false;
    bool isReviveButtonPressed = false;
    bool isGrabButtonPressed = false;

    float maxSpeed = 3f;
    bool isGrounded = false;
    bool isActiveRagdoll = true;
    public bool IsActiveRagdoll => isActiveRagdoll;
    bool isGrabingActive = false;
    public bool IsGrabingActive => isGrabingActive;

    RaycastHit[] raycastHits = new RaycastHit[18];
    SyncPhysicsObject[] syncPhysicsObjects;

    // ī�޶� ����
    Camera mainCamera;
    float xRotation = 0f;
    float yRotation = 0f;

    //��ġ ����
    bool isPunchButtonPressed = false;
    bool isPunchActive = false;
    public bool IsPunchActive => isPunchActive;
    float lastPunchTime = 0f;
    float punchCooldown = 0.5f; // ��ġ ��ٿ�

    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkPhysicsSyncedRotations { get; }

    float startSlerpPositionSpring = 0.0f;
    float lastTimeBecameRagdol = 0;

    HandGrabHandler[] handGrabHandlers;
    HandPunchHandler[] handPunchHandlers;

    // ? ��Ʈ��ũ ����ȭ�Ǵ� ���·� ����
    [Networked] public bool IsFallen { get; set; } = false;
    [Networked] public bool IsRespawning { get; set; } = false;
    public bool IsAlive => !IsFallen && IsActiveRagdoll;

    // ? ���� ī�޶� ���� ����
    private bool localCameraInSpectatorMode = false;
    private bool previousFallenState = false;

    private void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        handGrabHandlers = GetComponentsInChildren<HandGrabHandler>();
        handPunchHandlers = GetComponentsInChildren<HandPunchHandler>();
    }

    void Start()
    {
        startSlerpPositionSpring = mainJoint.slerpDrive.positionSpring;
    }

    void Update()
    {
        if (Local != this) return; // �׻� Local �÷��̾ �Է� ó��

        // ? ��Ʈ��ũ ���� ��ȭ ���� (Update���� ó��)
        if (IsFallen != previousFallenState)
        {
            previousFallenState = IsFallen;
            if (IsFallen && !localCameraInSpectatorMode)
            {
                SetSpectatorCamera();
                localCameraInSpectatorMode = true;
            }
            else if (!IsFallen && localCameraInSpectatorMode)
            {
                localCameraInSpectatorMode = false;
            }
        }

        // �Է� ó�� (RŰ ������ ����)
        moveInputVector.x = Input.GetAxisRaw("Horizontal");
        moveInputVector.y = Input.GetAxisRaw("Vertical");
        mouseInput.x = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseInput.y = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.R))
            isReviveButtonPressed = true;

        if (Input.GetMouseButtonDown(0))
            isPunchButtonPressed = true;

        isGrabButtonPressed = Input.GetKey(KeyCode.G);

        HandleCamera();
    }

    void HandleCamera()
    {
        if (mainCamera == null) return;

        // ? ���� �÷��̾��� ī�޶� ���� ���� ����
        if (IsFallen && !localCameraInSpectatorMode)
        {
            // ���������� ���� ��ȯ
            SetSpectatorCamera();
            localCameraInSpectatorMode = true;
        }
        else if (!IsFallen && localCameraInSpectatorMode)
        {
            // �÷��̾� ���󰡱� ���� ����
            localCameraInSpectatorMode = false;
        }

        if (IsFallen)
        {
            Vector3 arenaCenter = Vector3.zero;
            Vector3 spectatorOffset = new Vector3(-15, 12, -15);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,
                                                         arenaCenter + spectatorOffset,
                                                         Time.deltaTime * 5f);
            mainCamera.transform.LookAt(arenaCenter + Vector3.up * 2);
            return;
        }

        // �÷��̾� ���� ī�޶�
        yRotation += mouseInput.x;
        xRotation -= mouseInput.y;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);

        Quaternion camRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 targetPos = transform.position + Vector3.up * camHeight;
        Vector3 desiredPos = targetPos - camRotation * Vector3.forward * camDistance;

        mainCamera.transform.position = Vector3.Slerp(mainCamera.transform.position, desiredPos, Time.deltaTime * 15f);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, camRotation, Time.deltaTime * 15f);
    }

    Vector3 GetMoveDirection(float cameraYRotation)
    {
        Vector3 forward = new Vector3(Mathf.Sin(cameraYRotation * Mathf.Deg2Rad), 0f, Mathf.Cos(cameraYRotation * Mathf.Deg2Rad));
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        return (forward * moveInputVector.y + right * moveInputVector.x).normalized;
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 localVelocifyVsForward = Vector3.zero;
        float localForwardVelocity = 0;

        // Ground check (StateAuthority ����)
        if (Object.HasStateAuthority)
        {
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
        }

        // --- �Է� �������� ---
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (Object.HasStateAuthority)
            {
                // ? ������ ó���� StateAuthority���� ó��
                if (Object.HasStateAuthority && IsFallen && networkInputData.isRevivePressed)
                {
                    Respawn();
                }


                float inputMagnitued = networkInputData.movementInput.magnitude;

                if (isActiveRagdoll && !IsFallen) // ? ������ ���¿����� ������ ó�� ����
                {
                    if (inputMagnitued != 0)
                    {
                        float cameraYRotation = networkInputData.cameraYRotation;
                        Vector3 forward = new Vector3(Mathf.Sin(cameraYRotation * Mathf.Deg2Rad), 0f, Mathf.Cos(cameraYRotation * Mathf.Deg2Rad));
                        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                        Vector3 moveDirection = (forward * networkInputData.movementInput.y + right * networkInputData.movementInput.x).normalized;

                        if (moveDirection != Vector3.zero)
                        {
                            Quaternion desiredRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Runner.DeltaTime * 10f);
                            mainJoint.targetRotation = Quaternion.Inverse(transform.rotation);

                            if (localForwardVelocity < maxSpeed)
                            {
                                rb.AddForce(moveDirection * inputMagnitued * 30);
                            }
                        }
                    }

                    if (isGrounded && networkInputData.isJumpPressed)
                    {
                        rb.AddForce(Vector3.up * 15, ForceMode.Impulse);
                    }

                    isGrabingActive = networkInputData.isGrabPressed;

                    if (networkInputData.isPunchPressed && Runner.SimulationTime - lastPunchTime > punchCooldown)
                    {
                        isPunchActive = true;
                        lastPunchTime = Runner.SimulationTime;
                    }
                }
            }
        }

        // --- �ڵ� ��Ƽ�� ���׵� ���� ---
        if (!isActiveRagdoll && Runner.SimulationTime - lastTimeBecameRagdol > 3)
        {
            MakeActiveRagdoll();
        }

        // --- �ִϸ��̼� & ����ȭ ---
        if (Object.HasStateAuthority)
        {
            anim.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                if (isActiveRagdoll)
                {
                    syncPhysicsObjects[i].UpdateJointFromAnimation();
                }
                networkPhysicsSyncedRotations.Set(i, syncPhysicsObjects[i].transform.localRotation);
            }

            // ? ���� ó�� - �̹� ������ ���°� �ƴ� ���� üũ
            if (!IsFallen && transform.position.y < -10)
            {
                FallOut();
            }

            foreach (HandGrabHandler handGrabHandler in handGrabHandlers)
                handGrabHandler.UpdateState();

            foreach (HandPunchHandler handPunchHandler in handPunchHandlers)
                handPunchHandler.UpdateState();

            if (isPunchActive && Runner.SimulationTime - lastPunchTime > 0.2f)
            {
                isPunchActive = false;
            }
        }
    }

    public override void Render()
    {
        if (!Object.HasStateAuthority)
        {
            var interpolated = new NetworkBehaviourBufferInterpolator(this);
            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                syncPhysicsObjects[i].transform.localRotation =
                    Quaternion.Slerp(syncPhysicsObjects[i].transform.localRotation,
                                     networkPhysicsSyncedRotations.Get(i), interpolated.Alpha);
            }
        }
    }

    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData inputData = new NetworkInputData();

        inputData.movementInput = moveInputVector;
        inputData.cameraYRotation = yRotation;

        if (isJumpButtonPressed)
            inputData.isJumpPressed = true;
        if (isReviveButtonPressed)
            inputData.isRevivePressed = true;
        if (isGrabButtonPressed)
            inputData.isGrabPressed = true;
        if (isPunchButtonPressed)
            inputData.isPunchPressed = true;

        isJumpButtonPressed = false;
        isPunchButtonPressed = false;

        return inputData;
    }

    public void OnPlayerBodyPartHit()
    {
        if (!isActiveRagdoll) return;
        SoundManager.Instance.PlayHitSound();
        MakeRagdoll();
    }

    void MakeRagdoll()
    {
        if (!Object.HasStateAuthority) return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = 0;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeRagdoll();

        lastTimeBecameRagdol = Runner.SimulationTime;
        isActiveRagdoll = false;
        isGrabingActive = false;
        isPunchActive = false;
    }

    void MakeActiveRagdoll()
    {
        if (!Object.HasStateAuthority) return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = startSlerpPositionSpring;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeActiveRagdoll();

        isActiveRagdoll = true;
        isGrabingActive = false;
        isPunchActive = false;
    }

    // ? ���������� ī�޶� ���� �и�
    void SetSpectatorCamera()
    {
        if (Object.HasInputAuthority && mainCamera != null)
        {
            mainCamera.transform.SetParent(null);
        }
    }

    void FallOut()
    {
        if (!Object.HasStateAuthority) return;

        IsFallen = true;
        //networkRigidbody3D.Teleport(Vector3.zero, Quaternion.identity);

        // ? ���� �÷��̾��� ���� ī�޶� ó��
        if (Object.HasInputAuthority)
        {
            SetSpectatorCamera();
            localCameraInSpectatorMode = true;
        }
    }

    void Respawn()
    {
        if (!Object.HasStateAuthority) return;

        IsFallen = false;

        if (networkRigidbody3D != null)
            networkRigidbody3D.Teleport(Vector3.up * 2, Quaternion.identity);
        else
            transform.position = Vector3.up * 2;

        if (Object.HasInputAuthority)
        {
            localCameraInSpectatorMode = false;
            yRotation = 0f;
            xRotation = 0f;
        }

        MakeActiveRagdoll();
    }




    public override void Spawned()
    {
        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            Local = this;
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();

             //? ���콺 Ŀ�� ���� �ּ�ó��(������)
             Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (mainCamera != null)
            {
                yRotation = 0f;
                xRotation = 0f;
            }

            // ? �ʱ� ���� ����
            previousFallenState = IsFallen;
            localCameraInSpectatorMode = IsFallen;

            Utils.DebugLog("Spawned player with local input authority");
        }

        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.InputAuthority == player)
            Runner.Despawn(Object);
    }
}