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

    // 입력 관련
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

    // 카메라 관련
    Camera mainCamera;
    float xRotation = 0f;
    float yRotation = 0f;

    //펀치 관련
    bool isPunchButtonPressed = false;
    bool isPunchActive = false;
    public bool IsPunchActive => isPunchActive;
    float lastPunchTime = 0f;
    float punchCooldown = 0.5f; // 펀치 쿨다운

    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkPhysicsSyncedRotations { get; }

    float startSlerpPositionSpring = 0.0f;
    float lastTimeBecameRagdol = 0;

    HandGrabHandler[] handGrabHandlers;
    HandPunchHandler[] handPunchHandlers;

    // ? 네트워크 동기화되는 상태로 변경
    [Networked] public bool IsFallen { get; set; } = false;
    [Networked] public bool IsRespawning { get; set; } = false;
    public bool IsAlive => !IsFallen && IsActiveRagdoll;

    // ? 로컬 카메라 상태 추적
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
        if (Local != this) return; // 항상 Local 플레이어만 입력 처리

        // ? 네트워크 상태 변화 감지 (Update에서 처리)
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

        // 입력 처리 (R키 리스폰 포함)
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

        // ? 로컬 플레이어의 카메라 상태 관리 개선
        if (IsFallen && !localCameraInSpectatorMode)
        {
            // 스펙테이터 모드로 전환
            SetSpectatorCamera();
            localCameraInSpectatorMode = true;
        }
        else if (!IsFallen && localCameraInSpectatorMode)
        {
            // 플레이어 따라가기 모드로 복원
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

        // 플레이어 따라 카메라
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

        // Ground check (StateAuthority 전용)
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

        // --- 입력 가져오기 ---
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (Object.HasStateAuthority)
            {
                // ? 리스폰 처리를 StateAuthority에서 처리
                if (Object.HasStateAuthority && IsFallen && networkInputData.isRevivePressed)
                {
                    Respawn();
                }


                float inputMagnitued = networkInputData.movementInput.magnitude;

                if (isActiveRagdoll && !IsFallen) // ? 떨어진 상태에서는 움직임 처리 안함
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

        // --- 자동 액티브 레그돌 복귀 ---
        if (!isActiveRagdoll && Runner.SimulationTime - lastTimeBecameRagdol > 3)
        {
            MakeActiveRagdoll();
        }

        // --- 애니메이션 & 동기화 ---
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

            // ? 낙사 처리 - 이미 떨어진 상태가 아닐 때만 체크
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

    // ? 스펙테이터 카메라 설정 분리
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

        // ? 로컬 플레이어일 때만 카메라 처리
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

             //? 마우스 커서 설정 주석처리(디버깅용)
             Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (mainCamera != null)
            {
                yRotation = 0f;
                xRotation = 0f;
            }

            // ? 초기 상태 설정
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