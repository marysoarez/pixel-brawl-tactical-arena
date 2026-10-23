namespace PixelBrawl.Combat
{
    public interface IDamageAbsorber
    {
        int Priority { get; }
        float AbsorbDamage(float incomingDamage);
    }
}
