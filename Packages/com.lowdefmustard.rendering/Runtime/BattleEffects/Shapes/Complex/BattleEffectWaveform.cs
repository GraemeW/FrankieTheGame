namespace LowDefMustard.Rendering
{
    public enum BattleEffectWaveform
    {
        // Note:  Values are the waveforms of SDFSquiggle (SDFShapes.hlsl)
        Sine = 0,
        Triangle = 1,
        Square = 2,
        Steps = 3, // square wave's flats only
        Trapezoid = 4, // slopes + flat crests
        Pulse = 5 // flat line w/ one trapezoid bump per wavelength (asymmetric trapezoid)
    }
}
