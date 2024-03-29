using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class AudioEffect : IEffectAsync, IEffectValid, IEffectTag
{
    string IEffectTag.Tag => Tag;

    [AssetsOnly]
    public GameObject AudioSourcePrefab;
    public string Tag;
    public bool AttachToParent = true;

    public bool CustomDuration;
    [MinValue(0), ShowIf(nameof(CustomDuration))]
    public float Duration = 5;
    [Indent, MinValue(0), ShowIf(nameof(CustomDuration))]
    public float FadeDuration = 0.5f;

    public Playlist Playlist;

    private AudioPool _audioPool;

    public void Initialize(IEffector effector, IEnumerable<IEffectAsync> effects, GameObject sender)
    {
        _audioPool = new AudioPool(AudioSourcePrefab);
    }

    public async UniTask UpdateAsync(IEffector effector, IEnumerable<IEffectAsync> effects, CancellationToken cancellationToken, GameObject sender)
    {
        if (CustomDuration && Duration - FadeDuration <= 0)
            return;

        var audioSource = _audioPool.Get();

        var effectorTransform = effector.GameObject.transform;
        var audioSourceTransform = audioSource.gameObject.transform;

        if (AttachToParent)
        {
            audioSourceTransform.parent = effectorTransform;
            audioSourceTransform.localPosition = Vector3.zero;
        }
        else
            audioSourceTransform.position = effectorTransform.position;

        var clip = audioSource.Play(Playlist);

        try
        {
            float duration = CustomDuration ? Duration - FadeDuration : clip.AudioClip.length;
            await UniTask.WaitForSeconds(duration, cancellationToken: cancellationToken);

            if (CustomDuration && FadeDuration > 0)
                await audioSource.FadeOut(FadeDuration, cancellationToken);
        }
        catch 
        {
            if (!audioSource)
                return;
        }

        audioSource.Stop();

        if (AttachToParent)
            audioSourceTransform.parent = null;

        _audioPool.Release(audioSource);
    }

    public bool IsValid(IEffector effector, IEffect effect, GameObject sender)
    {
        if (effector.Effects.Any(x => x is AudioEffect audioEffect && audioEffect.Tag == Tag))
            return false;

        return true;
    }
}
