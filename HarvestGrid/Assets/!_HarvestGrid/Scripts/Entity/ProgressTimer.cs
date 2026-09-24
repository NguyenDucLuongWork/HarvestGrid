using System;
using UnityEngine;

[Serializable]
public class ProgressTimer : ICloneable<ProgressTimer>
{
    //Other
    public event Action OnCompleted;
    public event Action<float> OnProgressChanged; //Percent

    //Config
    [SerializeField]
    private float duration;
    [SerializeField]
    private float finishedHoldDuration;
    [SerializeField]
    private bool playOnStart;
    [SerializeField]
    private bool loop;

    //Status (serialized -> safe to persist for save/load)
    [SerializeField]
    private float progress;
    [SerializeField]
    private bool isPlaying;
    [SerializeField]
    private bool isFinishedHolding;

    private float holdTimer;

    public float Duration => duration;
    public float Progress => progress;
    public bool IsPlaying => isPlaying;

    public ProgressTimer(ProgressTimer original)
    {
        this.duration = original.Duration;
        this.finishedHoldDuration = original.finishedHoldDuration;
        this.playOnStart = original.playOnStart;
        this.loop = original.loop;
        this.progress = original.progress;
        this.isPlaying = original.isPlaying;
        this.isFinishedHolding = original.isFinishedHolding;
        this.holdTimer = original.holdTimer;
    }

    public ProgressTimer Clone()
    {
        return new ProgressTimer(this);
    }
    
    public void Play()
    {
        isPlaying = true;
        isFinishedHolding = false;
        holdTimer = 0f;

        TimeManager.Instance?.RegisterTimer(this);
    }

    public void Stop()
    {
        isPlaying = false;
        isFinishedHolding = false;

        TimeManager.Instance?.UnregisterTimer(this);
    }

    public void SetProgess(float progess)
    {
        progress = Mathf.Clamp01(progess);
        OnProgressChanged?.Invoke(progress);
    }

    // Driven by TimeSystem while registered/playing.
    public void Tick(float deltaTime)
    {
        if (!isPlaying) return;

        if (isFinishedHolding)
        {
            holdTimer += deltaTime;
            if (holdTimer >= finishedHoldDuration)
            {
                isFinishedHolding = false;
                holdTimer = 0f;

                if (loop) SetProgess(0f);
                else Stop();
            }
            return;
        }

        float newProgress = progress + deltaTime / duration;

        if (newProgress >= 1f)
        {
            SetProgess(1f);
            OnCompleted?.Invoke();

            if (finishedHoldDuration > 0f)
            {
                isFinishedHolding = true;
                holdTimer = 0f;
            }
            else if (loop)
            {
                SetProgess(0f);
            }
            else
            {
                Stop();
            }
        }
        else
        {
            SetProgess(newProgress);
        }
    }
}