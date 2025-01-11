public interface IBuff<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public float duration { get; }
    public void applyBuff(TCurrent currentStats, TBase baseStats);
    public void cancelBuff(TCurrent currentStats, TBase baseStats);
    bool isSameType(IBuff<TCurrent, TBase> other);
    void resetDuration(float newDuration);
}
