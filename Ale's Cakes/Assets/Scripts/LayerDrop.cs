using UnityEngine;

public class LayerDrop : MonoBehaviour
{
    public bool IsFalling { get; private set; }
    public Vector3 TargetLocalPos { get; private set; }

    float velocity;
    float gravity;
    ParticleSystem trail;
    System.Action onLanded;

    public void Begin(Vector3 startLocalPos, Vector3 targetLocalPos, float gravity,
                      ParticleSystem trail, System.Action onLanded)
    {
        transform.localPosition = startLocalPos;
        TargetLocalPos = targetLocalPos;
        this.gravity = gravity;
        this.trail = trail;
        this.onLanded = onLanded;
        velocity = 0f;
        IsFalling = true;

        if (trail != null)
        {
            trail.transform.position = transform.position;
            trail.Clear();
            trail.Play();
        }
    }

    void Update()
    {
        if (!IsFalling) return;

        velocity += gravity * Time.deltaTime;
        Vector3 pos = transform.localPosition;
        pos.y -= velocity * Time.deltaTime;

        if (pos.y <= TargetLocalPos.y)
        {
            transform.localPosition = TargetLocalPos;
            IsFalling = false;
            if (trail != null) trail.Stop();
            if (onLanded != null) onLanded();
            return;
        }

        transform.localPosition = pos;
        if (trail != null) trail.transform.position = transform.position;
    }

    void OnDestroy()
    {
        // Si la capa se quita mientras cae, apaga la estela
        if (IsFalling && trail != null) trail.Stop();
    }
}