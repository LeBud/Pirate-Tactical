using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance { get; private set; }

    AudioSource _audioSource;
    public bool playSounds;

    [Header("Ships Sounds")]
    public AudioClip attack;
    public AudioClip offensiveCapacity;
    public AudioClip tileCapacity;
    public AudioClip takeDamage;
    public AudioClip startMoving;
    public AudioClip stopMoving;
    public AudioClip buyUpgrade;
    public AudioClip mineExploding;
    public AudioClip spawnShip;
    public AudioClip selectShip;
    public AudioClip deselectShip;
    public AudioClip shipDestroyed;
    public AudioClip shipSink;
    public AudioClip changeShipMode;
    public AudioClip accost;
    public AudioClip fireDamage;

    [Header("Map Sounds")]
    public AudioClip zoneShrinking;

    [Header("other Sounds")]
    public AudioClip gameStarting;
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

    public void PlaySoundOnClients(AudioClip sound)
    {
        if (!playSounds) return;
        soundToPlayOnClient = sound;
        PassSoundThroughtServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void PassSoundThroughtServerRpc()
    {
        PlaySoundsClientRpc();
    }

    [ClientRpc]
    void PlaySoundsClientRpc()
    {
        _audioSource.PlayOneShot(soundToPlayOnClient);
    }

    public void PlaySoundLocally(AudioClip sound)
    {
        if (!playSounds) return;
        _audioSource.PlayOneShot(sound);
    }

}
