namespace PixelBrawl.Abilities
{
    public interface ICooldownAbility
    {
        bool IsActive { get; }
        float CooldownRemaining { get; }
        float CooldownDuration { get; }
    }
}
