using System.Collections;
using UnityEngine;

public class HopakAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] public Animator[] hopakJuniors;

    private bool isWindmill;

    public void PlayAnimation(bool left, float speed)
    {
        if (isWindmill)
            return;
        animator.speed = 2.3f - speed * 4f;
        if (left)
        {
            animator.Play("HopakLeft");
        }
        else
        {
            animator.Play("HopakRight");
        }
    }

    public void PlayWindmill(float duration)
    {
        StartCoroutine(OnWindmill(duration));
    }

    IEnumerator OnWindmill(float duration)
    {
        isWindmill = true;
        animator.Play("Windmill");
        yield return new WaitForSeconds(duration);
        isWindmill = false;
    }
}