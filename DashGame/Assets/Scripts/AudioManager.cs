﻿using UnityEngine.Audio;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public Sound[] sounds;

    Sound currentLvlSound;
    Sound nextLvlSound;
    Sound currentMusic;
    Sound nextMusic;

    float t1,t2,t3;
    bool fade2LvlSound = false;
    bool clearLvlSounds = false;
    float time4SoundFade = 1.5f;
    float time4MusicFade = 2.5f;
    bool fade2Music = false;
    bool clearMusic = false;
    bool go2Basement = false;
    bool comeBackFromBasement = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < sounds.Length; i++)
        {
            sounds[i].source = gameObject.AddComponent<AudioSource>();
            sounds[i].source.clip = sounds[i].clip;

            sounds[i].source.volume = sounds[i].volume;
            sounds[i].source.pitch = sounds[i].pitch;
            sounds[i].source.loop = sounds[i].loop;
        }
    }

    public void Fade2LvlSound(string lvlSoundName)
    {
        try
        {
            Sound s = Array.Find(sounds, sound => sound.name == lvlSoundName);
            s.source.volume = 0;
            s.source.Play();

            t1 = 0;

            nextLvlSound = s;

            fade2LvlSound = true;
        }
        catch (NullReferenceException)
        {
            Debug.LogError("Sound " + lvlSoundName + " not found!");
            return;
        }
    }

    public void Fade2Music(string musicName)
    {
        try
        {
            Sound s = Array.Find(sounds, sound => sound.name == musicName);
            s.source.volume = 0;
            s.source.Play();

            t2 = 0;

            nextMusic = s;

            fade2Music = true;
        }
        catch (NullReferenceException)
        {
            Debug.LogError("Sound " + musicName + " not found!");
            return;
        }
    }

    private void Update()
    {
        if (fade2LvlSound)
        {
            t1 += Time.deltaTime / time4SoundFade;

            currentLvlSound.source.volume = Mathf.Lerp(currentLvlSound.volume, 0, t1);
            nextLvlSound.source.volume = Mathf.Lerp(0, nextLvlSound.volume, t1);

            if(currentLvlSound.source.volume == 0 && nextLvlSound.source.volume == nextLvlSound.volume)
            {
                currentLvlSound.source.Stop();
                currentLvlSound = nextLvlSound;
                nextLvlSound = null;

                fade2LvlSound = false;
            }
        }

        if (clearLvlSounds)
        {
            t1 += Time.deltaTime / time4SoundFade;

            currentLvlSound.source.volume = Mathf.Lerp(currentLvlSound.volume, 0, t1);

            if(currentLvlSound.source.volume == 0)
            {
                currentLvlSound.source.Stop();
                currentLvlSound = null;

                clearLvlSounds = false;
            }
        }

        if (fade2Music)
        {
            t2 += Time.deltaTime / time4MusicFade;

            currentMusic.source.volume = Mathf.Lerp(currentMusic.volume, 0, t2);
            nextMusic.source.volume = Mathf.Lerp(0, nextMusic.volume, t2);

            if (currentMusic.source.volume == 0 && nextMusic.source.volume == nextMusic.volume)
            {
                currentMusic.source.Stop();
                currentMusic = nextMusic;
                nextMusic = null;

                fade2Music = false;
            }
        }

        if (clearMusic)
        {
            t2 += Time.deltaTime / time4MusicFade;

            currentMusic.source.volume = Mathf.Lerp(currentMusic.volume, 0, t2);
            if(currentMusic.source.volume == 0)
            {
                currentMusic.source.Stop();
                currentMusic = null;

                clearMusic = false;
            }
        }

        if (go2Basement)
        {
            t3 += Time.deltaTime;

            currentLvlSound.source.volume = Mathf.Lerp(currentLvlSound.volume, currentLvlSound.volume - .15f, t3);

            if (currentLvlSound.source.volume == currentLvlSound.volume - .15f)
            {
                go2Basement = false;
            }
        }

        if (comeBackFromBasement)
        {
            t3 += Time.deltaTime;

            currentLvlSound.source.volume = Mathf.Lerp(currentLvlSound.volume - .15f, currentLvlSound.volume, t3);

            if (currentLvlSound.source.volume == currentLvlSound.volume)
            {
                comeBackFromBasement = false;
            }
        }
    }

    public void PlayLvlSound(string lvlSoundName)
    {
        try
        {
            currentLvlSound = Array.Find(sounds, sound => sound.name == lvlSoundName);

            currentLvlSound.source.volume = currentLvlSound.volume;
            currentLvlSound.source.Play();
        }
        catch (System.NullReferenceException)
        {
            Debug.LogError("Sound " + lvlSoundName + " not found!");
            return;
        }
    }

    public void ClearLvlSounds()
    {
        t1 = 0;
        clearLvlSounds = true;
    }

    public void StopLvlSounds()
    {
        if (currentLvlSound != null)
        {
            currentLvlSound.source.Stop();
        }
    }

    public void PlayMusic(string musicName)
    {
        try
        {
            currentMusic = Array.Find(sounds, sound => sound.name == musicName);

            currentMusic.source.volume = currentMusic.volume;
            currentMusic.source.Play();
        }
        catch (NullReferenceException)
        {
            Debug.LogError("Sound " + musicName + " not found!");
            return;
        }
    }

    public void ClearMusic()
    {
        t2 = 0;
        clearMusic = true;
    }

    public void StopMusic()
    {
        if (currentMusic != null)
        {
            currentMusic.source.Stop();
        }
    }

    public void Play(string name)
    {
        try
        {
            Array.Find(sounds, sound => sound.name == name).source.Play();
        }
        catch (System.NullReferenceException)
        {
            Debug.LogError("Sound " + name + " not found!");
            return;
        }
    }

    public void Go2LabBasement()
    {
        comeBackFromBasement = false;

        t3 = 0;
        go2Basement = true;
    }

    public void ComeBackFromBasement()
    {
        go2Basement = false;

        t3 = 0;
        comeBackFromBasement = true;
    }
}
