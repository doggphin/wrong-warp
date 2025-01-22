using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Interactions;
using UnityEngine;

namespace Networking.Client {
    public class CTickDifferenceTracker {
        private List<int> receivedTickDifferencesBuffer = new();
        private int totalCompensation = 0;

        /// <summary>
        /// Adds a tick difference to the difference tracking buffer.
        /// </summary>
        /// <param name="desiredDifference"> The difference you want to have </param>
        /// <param name="newDifference"> The difference you got </param>
        public void AddTickDifferenceReading(int newDifference, int desiredDifference = 0) {
            int differenceFromDesired = newDifference - desiredDifference;
            receivedTickDifferencesBuffer.Add(differenceFromDesired);
        }


        public int ReadingsCount => receivedTickDifferencesBuffer.Count;


        /// <returns> The new total compensation </returns>
        public int AddInProgressCompensation(int compensation) {
            totalCompensation += compensation;
            return totalCompensation;
        }


        /// <returns> The total compensation required to get back in sync </returns>
        public float GetRequiredCompensation() {
            return ((float)receivedTickDifferencesBuffer.Sum() / receivedTickDifferencesBuffer.Count) - totalCompensation;
        }


        public void ClearTickDifferencesBuffer() {
            receivedTickDifferencesBuffer.Clear();
        }
    }
}