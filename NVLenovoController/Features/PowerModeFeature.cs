namespace NVLenovoController.Features
{
    public enum PowerModeState
    {
        Quiet,
        Balance,
        Performance
    }

    public class PowerModeFeature : AbstractWmiFeature<PowerModeState>
    {
        public PowerModeFeature() : base("SmartFanMode", 1)
        {
        }
    }
}