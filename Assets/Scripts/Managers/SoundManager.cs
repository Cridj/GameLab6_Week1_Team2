using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();


    private void Awake()
    {
        //사운드 긁어오기
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sound");
        foreach (AudioClip clip in clips)
        {
            audioClips[clip.name] = clip;
        }        
    }

    public void Play(string name)
    {
        if(audioClips.ContainsKey(name))
        {
            sfxSource.PlayOneShot(audioClips[name]);
        }
    }

    public void PlayBgm(string name)
    {
        if(audioClips.ContainsKey(name))
        {
            bgmSource.clip = audioClips[name];
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        bgmSource.Stop();
    }

    public void PlayComboSound()
    {
        int ran = Random.Range(0, 4);

        if (ran == 0)
            Play("Combo1");
        else if (ran == 1)
            Play("Combo2");
        else if (ran == 2)
            Play("Combo3");
        else
            Play("Combo4");
    }

}
