using System.Collections.Generic;
using Networking.Server;

public class BaseObservers {
    private HashSet<SPlayer> observers;

    public BaseObservers() {
        observers = new();
    }

    public void AddObserver(SPlayer player) => observers.Add(player);
    public void RemoveObserver(SPlayer player) => observers.Remove(player);
    public IEnumerable<SPlayer> IterateObservers()
    {
        foreach (SPlayer observer in observers)
        {
            yield return observer;
        }
    }
}