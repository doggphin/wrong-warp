// https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Components/InterestManagement/SpatialHashing/Grid3D.cs
//
// Grid3D based on Grid2D
// -> not named 'Grid' because Unity already has a Grid type. causes warnings.
// -> struct to avoid memory indirection. it's accessed a lot.
using System.Collections.Generic;
using UnityEngine;

public struct Grid3D<T> {
    readonly Dictionary<Vector3Int, HashSet<T>> grid;

    readonly Vector3Int[] neighbourOffsets; 

    public Grid3D(int initialCapacity) {
        grid = new Dictionary<Vector3Int, HashSet<T>>(initialCapacity);

        neighbourOffsets = new Vector3Int[9 * 3];
        int i = 0;
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    neighbourOffsets[i] = new Vector3Int(x, y, z);
                    i += 1;
                }
            }
        }
    }


    public void Add(Vector3Int position, T value) {
        if (!grid.TryGetValue(position, out HashSet<T> hashSet)) {
            hashSet = new HashSet<T>(128);
            grid[position] = hashSet;
        }

        hashSet.Add(value);
    }


    void GetAt(Vector3Int position, HashSet<T> result) {
        if (grid.TryGetValue(position, out HashSet<T> hashSet)) {
            foreach (T entry in hashSet)
                result.Add(entry);
        }
    }


    public void GetWithNeighbours(Vector3Int position, HashSet<T> result) {
        // clear result first
        result.Clear();

        // add neighbours
        foreach (Vector3Int offset in neighbourOffsets)
            GetAt(position + offset, result);
    }


    public void ClearNonAlloc() {
        foreach (HashSet<T> hashSet in grid.Values)
            hashSet.Clear();
    }
}