using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class HopakAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void PlayAnimation(bool left, float speed)
    {
        animator.speed = 2.3f - speed * 4f;
        if (left)
            animator.Play("HopakLeft");
        else
            animator.Play("HopakRight");
    }
}