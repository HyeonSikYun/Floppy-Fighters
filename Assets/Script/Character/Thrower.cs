using System.Collections;
using UnityEngine;
using Fusion;

public class Thrower : NetworkBehaviour
{
    [Header("Target")]
    private Transform player;
    private Rigidbody playerRb;

    [Header("Throw Settings")]
    public float throwInterval = 10f;
    public float launchSpeed = 18f;
    public float maxRange = 0f;
    public float minRange = 0f;
    public Transform muzzle;
    public bool requireLineOfSight = false;
    public LayerMask losMask = ~0;

    [Header("Projectile")]
    public NetworkObject projectilePrefab;   // 반드시 NetworkObject 가진 프리팹
    public Vector3 projectileLocalRotation;
    public float projectileLifetime = 10f;
    public float projectileHitForce = 5f;

    [Header("Quality")]
    public bool leadTarget = true;
    public bool rotateToAim = true;
    public bool useLowArc = true;

    [Header("Animation")]
    public Animator anim;

    private Coroutine _loop;

    public override void Spawned()
    {
        // 서버/호스트에서만 던지기
        if (Object.HasStateAuthority)
            _loop = StartCoroutine(ThrowLoop());
    }

    IEnumerator ThrowLoop()
    {
        var wait = new WaitForSeconds(throwInterval);
        while (true)
        {
            FindClosestPlayer();
            TryThrowOnce();
            yield return wait;
        }
    }

    void FindClosestPlayer()
    {
        var players = FindObjectsOfType<NetworkPlayer>();
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                closest = p.transform;
                minDist = d;
            }
        }

        player = closest;
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
        else
            playerRb = null;
    }

    void TryThrowOnce()
    {
        if (!projectilePrefab || !player) return;

        Vector3 origin = muzzle ? muzzle.position : transform.position;

        float dist = Vector3.Distance(origin, player.position);
        if (maxRange > 0f && dist > maxRange) return;
        if (minRange > 0f && dist < minRange) return;

        if (requireLineOfSight)
        {
            if (Physics.Linecast(origin, player.position, out RaycastHit hit, losMask))
            {
                if (hit.transform != player && !hit.transform.IsChildOf(player))
                    return;
            }
        }

        Vector3 targetPos = player.position;
        Vector3 targetVel = (leadTarget && playerRb) ? playerRb.linearVelocity : Vector3.zero;

        Vector3 launchVel;
        bool ok = SolveBallistic(origin, targetPos, targetVel, Physics.gravity.y, launchSpeed, useLowArc, out launchVel);

        if (!ok)
            launchVel = (player.position - origin).normalized * launchSpeed;

        // 네트워크 발사체 생성
        NetworkObject proj = Runner.Spawn(projectilePrefab, origin, Quaternion.LookRotation(launchVel.normalized));
        if (proj.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = launchVel;

        if (proj.TryGetComponent(out RockProjectile rock))
        {
            rock.hitForce = projectileHitForce;
            rock.lifetime = projectileLifetime;
        }

        if (rotateToAim)
        {
            Vector3 look = new Vector3(launchVel.x, 0f, launchVel.z);
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        if (anim)
        {
            anim.SetBool("ThrowRock", true);
            StartCoroutine(ResetThrowAnim());
        }
    }

    IEnumerator ResetThrowAnim()
    {
        yield return new WaitForSeconds(0.3f);
        if (anim) anim.SetBool("ThrowRock", false);
    }

    // ================== 포물선 계산 ==================
    static bool SolveBallistic(Vector3 origin, Vector3 targetPos, Vector3 targetVel,
        float gravityY, float speed, bool lowArc, out Vector3 launchVel)
    {
        launchVel = Vector3.zero;
        if (speed <= 0.01f) return false;

        Vector3 g = new Vector3(0f, gravityY, 0f);
        Vector3 predictedTarget = targetPos;
        Vector3 bestV = Vector3.zero;
        bool hasSolution = false;

        for (int i = 0; i < 3; i++)
        {
            Vector3 toTarget = predictedTarget - origin;
            float vy2;
            Vector3 v0_a, v0_b;
            bool ok = SolveBallisticFixedSpeed(toTarget, g.y, speed, out v0_a, out v0_b, out vy2);
            if (!ok) break;

            Vector3 v0 = lowArc ? v0_a : v0_b;
            float t = EstimateFlightTime(toTarget, v0, g);
            if (t <= 0.02f) break;

            hasSolution = true;
            bestV = v0;
            predictedTarget = targetPos + targetVel * t;
        }

        if (hasSolution)
        {
            launchVel = bestV;
            return true;
        }
        return false;
    }

    static bool SolveBallisticFixedSpeed(Vector3 toTarget, float gy, float speed,
        out Vector3 vA, out Vector3 vB, out float vy2)
    {
        vA = vB = Vector3.zero;
        vy2 = 0f;
        Vector3 toXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float x = toXZ.magnitude;
        float y = toTarget.y;
        float s2 = speed * speed;
        float g = -gy;
        if (g <= 0.0001f) return false;
        float inside = s2 * s2 - g * (g * x * x + 2f * y * s2);
        if (inside < 0f) return false;
        float root = Mathf.Sqrt(Mathf.Max(0f, inside));
        float tLow = (s2 - root) / (g * x);
        float tHigh = (s2 + root) / (g * x);
        Vector3 dirXZ = x > 0.0001f ? (toXZ / x) : Vector3.forward;

        Vector2 makeV(float t)
        {
            float cos = 1f / Mathf.Sqrt(1 + t * t);
            float sin = t * cos;
            return new Vector2(cos, sin);
        }

        var low = makeV(tLow);
        var high = makeV(tHigh);

        vA = dirXZ * (speed * low.x) + Vector3.up * (speed * low.y);
        vB = dirXZ * (speed * high.x) + Vector3.up * (speed * high.y);

        vy2 = speed * speed * low.y * low.y;
        return true;
    }

    static float EstimateFlightTime(Vector3 toTarget, Vector3 v0, Vector3 g)
    {
        if (Mathf.Abs(g.y) < 0.001f) return 0f;
        float a = 0.5f * g.y;
        float b = v0.y;
        float c = -toTarget.y;
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0f) return 0f;
        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrtDisc) / (2 * a);
        float t2 = (-b - sqrtDisc) / (2 * a);
        float t = Mathf.Max(t1, t2);
        return t > 0f ? t : 0f;
    }
}
