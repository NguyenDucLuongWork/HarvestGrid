using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Simple pulse animation for the suggestion ghost.
/// </summary>
public class GhostPulseAnimation : MonoBehaviour
{
    public Image image;
    public Color baseColor;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.8f;
    public float speed = 3f;

    private void Update()
    {
        if (image == null) return;
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // 0 to 1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        image.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
}