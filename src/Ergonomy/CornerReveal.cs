namespace Ergonomy;

/// <summary>Testable hover dwell and delayed retreat, independent of desktop coordinates.</summary>
public sealed class CornerReveal
{
    public bool Revealed { get; private set; }
    private double _dwell, _away;
    public bool Update(double seconds, bool inHotspot, bool overPanel, bool pinned, bool quiet)
    {
        seconds = Math.Clamp(seconds,0,.5);
        if(quiet) { Hide();return false; }
        if(pinned) { Revealed=true;_away=0;return true; }
        if(!Revealed)
        {
            _dwell = inHotspot ? _dwell + seconds : 0;
            if(_dwell>=.35) { Revealed=true;_away=0; }
        }
        else
        {
            _away = overPanel || inHotspot ? 0 : _away + seconds;
            if(_away>=1.1) Hide();
        }
        return Revealed;
    }
    public void Reveal() { Revealed=true;_away=0; }
    public void Hide() {Revealed=false;_dwell=0;_away=0;}
}
