public interface IBuff<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public float _duration { get; }
    public void ApplyBuff(TCurrent currentStats, TBase baseStats);
    public void CancelBuff(TCurrent currentStats, TBase baseStats);
    bool IsSameType(IBuff<TCurrent, TBase> other);
    void ResetDuration(float newDuration);
}
