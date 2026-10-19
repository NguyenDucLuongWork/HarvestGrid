
using System;

[Serializable]
public class PlayTime
{
    public string date;
    public int minutes;
    public float seconds;

    public PlayTime()
    {
    }

    public PlayTime(int minutes, float seconds)
    {
        this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.minutes = minutes;
        this.seconds = seconds;
    }

    public float TotalSeconds => minutes * 60f + seconds;

    public bool IsFasterThan(PlayTime other)
    {
        if (other == null)
            return true;

        return TotalSeconds < other.TotalSeconds;
    }
}
