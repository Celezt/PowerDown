using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public struct AudioPool
{
    private readonly static Dictionary<GameObject, ObjectPool<AudioSource>> _audioSourcePools = new();

    public bool IsEmpty => _prefab == null;
    public GameObject Prefab => _prefab;

    private readonly GameObject _prefab;

    static AudioPool()
    {
        SceneManager.activeSceneChanged += (newScene, oldScene) =>
        {
            foreach (var pools in _audioSourcePools.Values)
                pools.Clear();
        };
    }

    public AudioPool(GameObject audioPrefab)
    {
        _prefab = audioPrefab;

        Assert.IsNotNull(_prefab.GetComponent<AudioSource>());

        if (!_audioSourcePools.ContainsKey(audioPrefab))
        {
            AudioSource CreateAudioSource()
            {
                if (audioPrefab == null)
                    return null;

                GameObject audioSourceObject = UnityEngine.Object.Instantiate(audioPrefab);
                AudioSource audioSource = audioSourceObject.GetComponent<AudioSource>();

                audioSource.Stop();
                audioSource.playOnAwake = false;

                return audioSource;
            }

            void OnGetAudioSource(AudioSource audioSource)
            {
                audioSource.gameObject.SetActive(true);
            }

            void OnReleaseAudioSource(AudioSource audioSource)
            {
                audioSource.Stop();
                audioSource.gameObject.SetActive(false);
            }

            void OnDestroyAudioSource(AudioSource audioSource)
            {
                if (audioSource != null)
                    UnityEngine.Object.Destroy(audioSource.gameObject);
            }

            _audioSourcePools[audioPrefab] = new ObjectPool<AudioSource>(
                createFunc: CreateAudioSource,
                actionOnGet: OnGetAudioSource,
                actionOnRelease: OnReleaseAudioSource,
                actionOnDestroy: OnDestroyAudioSource,
                collectionCheck: true);
        }
    }

    public AudioSource Get()
    {
        if (!_prefab)
        {
            Debug.LogError("Audio Pool does not contain any prefab reference");
            return null;
        }

        var audioSource = _audioSourcePools[_prefab].Get();
        return audioSource;
    }

    public void Release(AudioSource audioSource)
    {
        if (!_prefab)
        {
            Debug.LogError("Audio Pool does not contain any prefab reference");
            return;
        }

        _audioSourcePools[_prefab].Release(audioSource);
    }

    public static void Release(GameObject audioPrefab, AudioSource audioSource)
    {
        _audioSourcePools[audioPrefab].Release(audioSource);
    }

    public static AudioSource Get(GameObject audioPrefab)
    {
        AudioPool audioPool = new(audioPrefab);
        return audioPool.Get();
    }

    public static void Remove(GameObject audioPrefab)
    {
        _audioSourcePools.Remove(audioPrefab);
    }
}
