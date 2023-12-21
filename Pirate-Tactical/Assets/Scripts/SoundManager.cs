using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance { get; private set; }

    AudioSource _audioSource;
    public bool playSounds;

    [Header("Ships Sounds")]
    public Sounds attack;
    public Sounds offensiveCapacity;
    public Sounds tileCapacity;
    public Sounds takeDamage;
    public Sounds startMoving;
    public Sounds stopMoving;
    public Sounds buyUpgrade;
    public Sounds mineExploding;
    public Sounds spawnShip;
    public Sounds selectShip;
    public Sounds deselectShip;
    public Sounds shipDestroyed;
    public Sounds shipSink;
    public Sounds changeShipMode;
    public Sounds accost;
    public Sounds fireDamage;

    [Header("Map Sounds")]
    public Sounds zoneShrinking;

    [Header("other Sounds")]
    public Sounds gameStarting;
    public AudioClip music;
    public AudioClip timer;
    public AudioClip oceanAmbient;
    public AudioClip boutonClick;
    public AudioClip goldGain;
    public AudioClip manaGain;

    AudioClip soundToPlayOnClient;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        _audioSource = GetComponent<AudioSource>();
    }

    public void PlaySoundOnClients(int sound)
    {
        if (!playSounds) return;
        PassSoundThroughtServerRpc(sound);
    }

    [ServerRpc(RequireOwnership = false)]
    void PassSoundThroughtServerRpc(int id)
    {
        PlaySoundsClientRpc(id);
    }

    [ClientRpc]
    void PlaySoundsClientRpc(int id)
    {
        Debug.Log("Son serveur");
        _audioSource.PlayOneShot(AudioToPlay(id));
    }

    public void PlaySoundLocally(AudioClip sound)
    {
        if (!playSounds) return;
        Debug.Log("son local");
        _audioSource.PlayOneShot(sound);
    }

    AudioClip AudioToPlay(int clip)
    {
        switch(clip)
        {
            case 0:
                return attack.clip;
            case 1:
                return offensiveCapacity.clip;
            case 2:
                return tileCapacity.clip;
            case 3:
                return takeDamage.clip;
            case 4:
                return startMoving.clip;
            case 5:
                return stopMoving.clip;
            case 6:
                return buyUpgrade.clip;
            case 7:
                return mineExploding.clip;
            case 8:
                return spawnShip.clip;
            case 9:
                return selectShip.clip;
            case 10:
                return deselectShip.clip;
            case 11:
                return shipDestroyed.clip;
            case 12:
                return shipSink.clip;
            case 13:
                return changeShipMode.clip;
            case 14:
                return accost.clip;
            case 15:
                return fireDamage.clip;
            case 16:
                return zoneShrinking.clip;
            case 17:
                return gameStarting.clip;
        }
        return null;
    }
}

[Serializable]
public struct Sounds
{
    public AudioClip clip;
    public int id;

    public Sounds(AudioClip clip, int id)
    {
        this.clip = clip;
        this.id = id;
    }
}

