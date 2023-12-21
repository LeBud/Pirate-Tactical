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
        PassSoundThroughtServerRpc(sound.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    void PassSoundThroughtServerRpc(FixedString128Bytes clip)
    {
        PlaySoundsClientRpc(clip);
    }

    [ClientRpc]
    void PlaySoundsClientRpc(FixedString128Bytes clip)
    {
        _audioSource.PlayOneShot(AudioToPlay(clip.ToString()));
    }

    public void PlaySoundLocally(AudioClip sound)
    {
        if (!playSounds) return;
        _audioSource.PlayOneShot(sound);
    }

    AudioClip AudioToPlay(string clip)
    {
        switch(clip)
        {
            case "attack":
                return attack;
            case "offensiveCapacity":
                return offensiveCapacity;
            case "tileCapacity":
                return tileCapacity;
            case "takeDamage":
                return takeDamage;
            case "startMoving":
                return startMoving;
            case "stopMoving":
                return stopMoving;
            case "buyUpgrade":
                return buyUpgrade;
            case "mineExploding":
                return mineExploding;
            case "spawnShip":
                return spawnShip;
            case "selectShip":
                return selectShip;
            case "deselectShip":
                return deselectShip;
            case "shipDestroyed":
                return shipDestroyed;
            case "shipSink":
                return shipSink;
            case "changeShipMode":
                return changeShipMode;
            case "accost":
                return accost;
            case "fireDamage":
                return fireDamage;
            case "zoneShrinking":
                return zoneShrinking;
            case "gameStarting":
                return gameStarting;
        }
        return null;
    }

}
