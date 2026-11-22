using System;
using System.Collections;
using UnityEngine;

public class CustomerWalker : MonoBehaviour
{
    [SerializeField] float speed = 2f;
    [SerializeField] float turnSpeed = 8f;

    Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void WalkTo(Vector3 target, Action onArrived)
    {
        StopAllCoroutines();
        StartCoroutine(WalkRoutine(target, onArrived));
    }

    IEnumerator WalkRoutine(Vector3 target, Action onArrived)
    {
        SetSpeed(1f);

        while (true)
        {
            Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
            Vector3 dir = flat - transform.position;
            if (dir.magnitude < 0.05f) break;

            transform.position = Vector3.MoveTowards(transform.position, flat, speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

            yield return null;
        }

        SetSpeed(0f);
        if (onArrived != null) onArrived();
    }

    void SetSpeed(float value)
    {
        if (animator != null) animator.SetFloat("velocidad", value);
    }
}