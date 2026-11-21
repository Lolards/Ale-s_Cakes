using UnityEngine;

public class IsometricFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smooth = 5f;

    Vector3 offset;

    void Start()
    {
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
    }
}