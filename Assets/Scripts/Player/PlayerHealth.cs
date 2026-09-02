using System;
using UnityEngine;
using DG.Tweening;

public class PlayerHealth : MonoBehaviour
{
    public int CurrentHP { get; private set; }
    [SerializeField] private int maxHp;

    [SerializeField] float hitFlashDuration = 1.25f;

    public event Action<int> OnHealthChanged;
    public event Action OnDied;

    [SerializeField] Renderer[] renderers;
    private Color originalColor;

    public Action GameOver;
    private bool isDie = false;

    void OnEnable()
    {

    }

    public void Init(int curHp)
    {
        maxHp = curHp;
        if (maxHp > 0)
        {
            CurrentHP = maxHp;
            OnHealthChanged?.Invoke(CurrentHP);
        }

        if (renderers.Length > 0)
            originalColor = renderers[0].material.color;
    }
    public void TakeDamage(int amount)
    {
        if (isDie)
        {
            return;
        }
        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, maxHp);
        OnHealthChanged?.Invoke(CurrentHP);

        HitFlash();

        if (CurrentHP <= 0f)
        {
            Die();
        }
    }

    public void GainHP(int amount)
    {
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, maxHp);
        OnHealthChanged?.Invoke(CurrentHP);
    }

    void Die()
    {
        isDie = true;
        GameOver?.Invoke();
    }

    void HitFlash()
    {
        foreach (Renderer r in renderers)
        {
            r.material.color = Color.white;
            r.material.DOColor(originalColor, hitFlashDuration);
        }
    }
}
