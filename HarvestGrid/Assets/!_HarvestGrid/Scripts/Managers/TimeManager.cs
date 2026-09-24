using LgTyLib.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : BaseSingleton<TimeManager>
{
    [SerializeField]
    private List<ProgressTimer> timers;

    [SerializeField, Range(0f, 10f)]
    private float timeScale = 1f;

    [SerializeField]
    private bool isPaused;

    private readonly List<ProgressTimer> activeTimers = new List<ProgressTimer>();
    private readonly List<ProgressTimer> pendingAdd = new List<ProgressTimer>();
    private readonly List<ProgressTimer> pendingRemove = new List<ProgressTimer>();

    public float TimeScale => timeScale;
    public bool IsPaused => isPaused;

    private void Start()
    {
        // Register any timers pre-assigned in the inspector
        if (timers != null)
        {
            foreach (var timer in timers)
            {
                RegisterTimer(timer);
            }
        }
    }

    private void Update()
    {
        ApplyPendingChanges();

        if (isPaused) return;

        float deltaTime = Time.deltaTime * timeScale;
        if (deltaTime <= 0f) return;

        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            activeTimers[i]?.Tick(deltaTime);
        }
    }

    private void ApplyPendingChanges()
    {
        if (pendingAdd.Count > 0)
        {
            foreach (var t in pendingAdd)
            {
                if (t != null && !activeTimers.Contains(t))
                    activeTimers.Add(t);
            }
            pendingAdd.Clear();
        }

        if (pendingRemove.Count > 0)
        {
            foreach (var t in pendingRemove)
            {
                activeTimers.Remove(t);
            }
            pendingRemove.Clear();
        }
    }

    public void RegisterTimer(ProgressTimer timer)
    {
        if (timer == null || activeTimers.Contains(timer) || pendingAdd.Contains(timer)) return;
        pendingAdd.Add(timer);
    }

    public void UnregisterTimer(ProgressTimer timer)
    {
        if (timer == null) return;
        pendingRemove.Add(timer);
    }

    public void StartTime()
    {
        isPaused = false;
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void SetSpeed(float speed)
    {
        timeScale = Mathf.Max(0f, speed);
    }
}