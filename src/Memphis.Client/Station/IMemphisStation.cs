namespace Memphis.Client.Station;

public interface IMemphisStation
{
    public string Name { get; }
    Task DestroyAsync();
}