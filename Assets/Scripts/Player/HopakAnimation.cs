using System;
using System.Collections;
using UnityEngine;

public class HopakAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isWindmill;

    public Action<float, bool> leftAction, rightAction;
    public Action<float, bool> windmill;

    public void PlayAnimation(bool left, float speed)
    {
        if (isWindmill)
            return;
        //animator.speed = 2.3f - speed * 4f;
        animator.speed = 1f;

        if (left)
        {
            animator.Play("HopakLeft");
            leftAction?.Invoke(2.3f - speed * 4f, left);
        }
        else
        {
            animator.Play("HopakRight");
            rightAction?.Invoke(2.3f - speed * 4f, left);
        }
    }

    public void PlayWindmill(float duration)
    {
        StartCoroutine(OnWindmill(duration));
    }

    IEnumerator OnWindmill(float duration)
    {
        isWindmill = true;
        windmill?.Invoke(animator.speed, true);
        animator.Play("Windmill");
        yield return new WaitForSeconds(duration);
        windmill?.Invoke(animator.speed, false);
        isWindmill = false;
    }

}