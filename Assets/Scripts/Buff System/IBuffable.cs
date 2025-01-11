public interface IBuffable<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public void addBuff(IBuff<TCurrent, TBase> buff);
    public void removeBuff(IBuff<TCurrent, TBase> buff);
}
