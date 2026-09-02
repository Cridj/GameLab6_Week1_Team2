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
            foreach (var junior in hopakJuniors)
            {
                if (junior.gameObject.activeSelf)
                    junior.Play("HopakLeft");
            }
            animator.Play("HopakLeft");

        }
        else
        {
            foreach (var junior in hopakJuniors)
            {
                if (junior.gameObject.activeSelf)
                    junior.Play("HopakRight");
            }
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
        foreach (var junior in hopakJuniors)
        {
            if (junior.gameObject.activeSelf)
                junior.Play("Windmill");
        }
        yield return new WaitForSeconds(duration);
        isWindmill = false;
    }
}