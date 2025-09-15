using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player1 : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform cam;   // 메인 카메라 Transform 할당

    [Header("Movement Settings")]
    public float speed = 3f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Camera Settings")]
    public float mouseSensitivity = 200f; // 마우스 감도
    public float camDistance = 5f;        // 플레이어와 카메라 거리
    public float camHeight = 2f;          // 카메라 높이
    private float xRotation = 0f;
    private float yRotation = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (cam == null)
            cam = Camera.main.transform; // 메인 카메라 자동 할당

        // groundCheck가 할당되지 않았다면 자동 생성
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, 0, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    private void Update()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        HandleCamera();

        // ESC로 마우스 잠금 해제 (테스트용)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // 땅에 닿았고 떨어지고 있다면 velocity 리셋
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 약간의 음수값으로 설정하여 땅에 붙어있도록
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + yRotation;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

            anim.SetBool("Walk", true);
        }
        else
        {
            anim.SetBool("Walk", false);
        }
    }

    void HandleJump()
    {
        // 스페이스바로 점프
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // 점프 애니메이션 트리거
            anim.SetTrigger("Jump");
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;

        // 수직 이동 적용
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCamera()
    {
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -30f, 60f); // 위아래 제한

        // 카메라 위치 계산
        Quaternion camRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 camPosition = transform.position - camRotation * Vector3.forward * camDistance + Vector3.up * camHeight;

        cam.position = camPosition;
        cam.rotation = camRotation;
    }

    // Gizmo로 Ground Check 영역 표시 (Scene 뷰에서 확인용)
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}