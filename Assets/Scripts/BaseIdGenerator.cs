using System.Collections.Generic;
using Unity.VisualScripting;

public class BaseIdGenerator {
    private int nextId = -1;

    public int GetNextEntityId<T>(Dictionary<int, T> thingsWithIds) {
        while(thingsWithIds.ContainsKey(++nextId));
        return nextId;
    }
}