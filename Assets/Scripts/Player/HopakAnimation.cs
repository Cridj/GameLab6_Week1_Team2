using UnityEngine;

public class HopakAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private Animator[] hopakJuniors;

    public void PlayAnimation(bool left, float speed)
    {
        animator.speed = 2.3f - speed * 4f;
        if (left)
        {
            animator.Play("HopakLeft");
            foreach(var junior in hopakJuniors)
            {
                if(junior.gameObject.activeSelf)
                    junior.Play("HopakLeft");
            }

        }
        else
        {
            animator.Play("HopakRight");
            foreach (var junior in hopakJuniors)
            {
                if (junior.gameObject.activeSelf)
                    junior.Play("HopakRight");
            }
        }
    }
}