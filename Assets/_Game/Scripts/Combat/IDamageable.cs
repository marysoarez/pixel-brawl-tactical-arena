namespace PixelBrawl.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        bool TakeDamage(float amount);
    }
}
