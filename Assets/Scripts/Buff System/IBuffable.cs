public interface IBuffable<TCurrent, TBase> where TCurrent : IStats where TBase : IStats
{
    public void AddBuff(IBuff<TCurrent, TBase> buff);
    public void RemoveBuff(IBuff<TCurrent, TBase> buff);
}
