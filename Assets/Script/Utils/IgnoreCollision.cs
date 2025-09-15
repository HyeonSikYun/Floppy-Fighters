using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    [SerializeField]
    Collider thisColider;

    [SerializeField]
    Collider[] colliderToIgnore;

    void Start()
    {
        foreach (Collider col in colliderToIgnore)
        {
            Physics.IgnoreCollision(thisColider, col, true);
        }
    }

}
