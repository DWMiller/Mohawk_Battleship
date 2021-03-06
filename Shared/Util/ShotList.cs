﻿using MBC.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MBC.Shared
{
    /// <summary>
    /// Contains a collection of <see cref="Shot"/>s and provides a number of functions that allow operation
    /// on the collection.
    /// </summary>
    public class ShotList : ICollection<Shot>
    {
        private List<Shot> allShots;
        private Dictionary<IDNumber, List<Shot>> shotsByReceiver;

        /// <summary>
        /// Contructs a new ShotList copying the contents of an existing ShotList.
        /// </summary>
        /// <param name="copyList">The ShotList to copy.</param>
        public ShotList(ShotList copyList)
        {
            shotsByReceiver = new Dictionary<IDNumber, List<Shot>>();
            allShots = new List<Shot>();

            if (copyList != null)
            {
                foreach (var shot in copyList.allShots)
                {
                    Add(new Shot(shot));
                }
            }
        }

        /// <summary>
        /// Constructs a new empty ShotList object.
        /// </summary>
        public ShotList()
        {
            allShots = new List<Shot>();
            shotsByReceiver = new Dictionary<IDNumber, List<Shot>>();
        }

        /// <summary>
        /// Gets the number of shots in this ShotList.
        /// </summary>
        public int Count
        {
            get
            {
                return allShots.Count;
            }
        }

        /// <summary>
        /// Every ShotList is not read-only, so this returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a Shot at the given index.
        /// </summary>
        public Shot this[int idx]
        {
            get
            {
                return allShots[idx];
            }
        }

        /// <summary>
        /// Searches through a list of shots for a specific shot.
        /// </summary>
        /// <param name="shots"></param>
        /// <param name="specificShot"></param>
        /// <returns></returns>
        public static bool IsShotMade(IList<Shot> shots, Shot specificShot)
        {
            foreach (var shot in shots)
            {
                if (shot == specificShot)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a new ShotList with the Shot objects in list2 removed from list1.
        /// </summary>
        /// <param name="list1">The ShotList to remove Shot objects from.</param>
        /// <param name="list2">The ShotList containing the Shot objects to remove from list1</param>
        /// <returns>A new ShotList object.</returns>
        public static ShotList operator -(ShotList list1, ShotList list2)
        {
            ShotList newList = new ShotList(list1);
            newList.Remove(list2);
            return newList;
        }

        /// <summary>
        /// Creates a new ShotList with Shot objects in both list1 and list2.
        /// </summary>
        /// <returns>A new ShotList object with the Coordinatess of both ShotList objects.</returns>
        public static ShotList operator +(ShotList list1, ShotList list2)
        {
            ShotList newList = new ShotList(list1);
            newList.Add(list2);
            return newList;
        }

        /// <summary>
        /// Adds a Shot to this ShotList.
        /// </summary>
        /// <param name="shot">The Shot to add.</param>
        public void Add(Shot shot)
        {
            allShots.Add(shot);
            if (!shotsByReceiver.ContainsKey(shot.Receiver))
            {
                MakeReceiver(shot.Receiver);
            }
            shotsByReceiver[shot.Receiver].Add(shot);
        }

        /// <summary>
        /// Appends the contents of a ShotList to this ShotList.
        /// </summary>
        /// <param name="shots">The ShotList to add.</param>
        public void Add(ShotList shots)
        {
            foreach (var shot in shots.allShots)
            {
                allShots.Add(shot);
                shotsByReceiver[shot.Receiver].Add(shot);
            }
        }

        /// <summary>
        /// Removes all of the shots from this ShotList.
        /// </summary>
        public void Clear()
        {
            allShots.Clear();
            shotsByReceiver.Clear();
        }

        /// <summary>
        /// Determines whether or not this ShotList contains a Shot.
        /// </summary>
        /// <param name="shot">The Shot to check for.</param>
        /// <returns>true if a Shot with the same field values as the given Shot exists in this ShipList.</returns>
        public bool Contains(Shot shot)
        {
            return shotsByReceiver.ContainsKey(shot.Receiver) && shotsByReceiver[shot.Receiver].Contains(shot);
        }

        /// <summary>
        /// Determines whether or not this ShotList contains a Shot that has been placed at the given Coordinates.
        /// </summary>
        /// <param name="shotCoords">The Coordinates to look for.</param>
        /// <returns>true if at least one Shot in this ShotList has been placed at the given Coordinates.</returns>
        public bool Contains(Coordinates shotCoords)
        {
            foreach (var shots in allShots)
            {
                if (shots.Coordinates == shotCoords)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the contents of this ShotList to a specified array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index of the array to begin copying.</param>
        public void CopyTo(Shot[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array is null.");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex is less than 0.");
            }
            if (Count > array.Length - arrayIndex)
            {
                throw new ArgumentException("The number of elements in the source ShipList is greater than the available space from arrayIndex to the end of the destination array.");
            }
            for (var i = 0; i < Count; i++)
            {
                array[i + arrayIndex] = allShots[i];
            }
        }

        /// <summary>
        /// Gets a ShotListEnumerator for this ShotList.
        /// </summary>
        public IEnumerator<Shot> GetEnumerator()
        {
            return new ShotListEnumerator(this);
        }

        /// <summary>
        /// Gets a ShotListEnumerator for this ShotList.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ShotListEnumerator(this);
        }

        /// <summary>
        /// Removes the shots in this ShotList and adds the shots that were previously not made to
        /// this ShotList up to the provided maximum Coordinates. This method will only invert shots
        /// of known controller ids. They become known once a Shot is added to this ShotList, or if
        /// they are specified with the AddReceiver() method.
        /// </summary>
        /// <param name="maxCoords">The maximum Coordinates to add shots not placed before.</param>
        public void Invert(Coordinates maxCoords)
        {
            //The list of every shot (unorganized) to use for replacement.
            var totalList = new List<Shot>();

            foreach (var receiverPair in shotsByReceiver.ToList())
            {
                var invertedShots = new List<Shot>();

                //Populate the inverted shots list for a certain receiver up to the maxCoords.
                for (var x = 0; x < maxCoords.X; x++)
                {
                    for (var y = 0; y < maxCoords.Y; y++)
                    {
                        var newShot = new Shot(receiverPair.Key);
                        newShot.Coordinates = new Coordinates(x, y);
                        invertedShots.Add(newShot);
                    }
                }
                //Subtract the inverted shots list for a certain receiver to get the inverted list.
                foreach (var receiverShot in receiverPair.Value)
                {
                    invertedShots.Remove(receiverShot);
                }

                //Substitute this ShotList's list for that receiver for that one.
                shotsByReceiver[receiverPair.Key] = invertedShots;
                totalList.AddRange(invertedShots);
            }
            //Replace the entire shot contents of this ShotList with the inverted one.
            allShots = totalList;
        }

        /// <summary>
        /// Creates a new empty internal list for Shot objects for a specific receiver ID.
        /// </summary>
        /// <param name="receiver">The ControllerID of the receiver to create the list for.</param>
        public void MakeReceiver(IDNumber receiver)
        {
            shotsByReceiver[receiver] = new List<Shot>();
        }

        /// <summary>
        /// Creates a new empty internal list for Shot objects for a specific receiver ID.
        /// </summary>
        /// <param name="receiver">The ControllerID of the receiver to create the list for.</param>
        [Obsolete("ControllerID is no longer used. Use IDNumbers instead.")]
        public void MakeReceiver(ControllerID receiver)
        {
            shotsByReceiver[(int)receiver] = new List<Shot>();
        }

        /// <summary>
        /// Creates a new empty internal list for Shot objects for a list of specific receiver IDs.
        /// </summary>
        /// <param name="receivers">A List of ControllerIDs to create lists for.</param>
        public void MakeReceivers(List<IDNumber> receivers)
        {
            foreach (var receiver in receivers)
            {
                MakeReceiver(receiver);
            }
        }

        /// <summary>
        /// Creates a new empty internal list for Shot objects for a list of specific receiver IDs.
        /// </summary>
        /// <param name="receivers">A List of ControllerIDs to create lists for.</param>
        [Obsolete("ControllerID is no longer used. Use a list of IDNumbers instead.")]
        public void MakeReceivers(List<ControllerID> receivers)
        {
            foreach (var receiver in receivers)
            {
                MakeReceiver(receiver);
            }
        }

        /// <summary>
        /// Removes a Shot from this ShotList.
        /// </summary>
        /// <param name="shot">The Shot to remove.</param>
        /// <returns>true if the Shot was in this ShotList and has been removed.</returns>
        public bool Remove(Shot shot)
        {
            bool result = allShots.Remove(shot);
            result &= shotsByReceiver[shot.Receiver].Remove(shot);
            return result;
        }

        /// <summary>
        /// Removes all Shot objects that are in a given ShotList.
        /// </summary>
        /// <param name="shots">The ShotList of Shots to remove.</param>
        public void Remove(ShotList shots)
        {
            foreach (var shot in shots.allShots)
            {
                Remove(shot);
            }
        }

        /// <summary>
        /// Removes all Shot objects that are equivalent.
        /// </summary>
        public void RemoveDuplicates()
        {
            int count = 0;
            foreach (var shot1 in allShots)
            {
                count = 0;
                foreach (var shot2 in allShots)
                {
                    if (shot1 == shot2)
                    {
                        count++;
                    }
                    if (count > 1)
                    {
                        Remove(shot1);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the last Shot made from this ShotList.
        /// </summary>
        public void RemoveLast()
        {
            var lastShot = allShots.Last();
            allShots.RemoveAt(allShots.Count - 1);

            var organizedList = shotsByReceiver[lastShot.Receiver];
            if (organizedList.Last() == lastShot)
            {
                organizedList.RemoveAt(organizedList.Count);
            }
        }

        /// <summary>
        /// Generates a ShotList of Shot objects whose Coordinates are equal to the given Coordinates.
        /// </summary>
        /// <param name="coords">The Coordinates to get a ShotList for.</param>
        /// <returns>A ShotList of Shot objects whose Coordinates are equal to that given.</returns>
        public ShotList ShotsFromCoordinates(Coordinates coords)
        {
            ShotList newList = new ShotList();
            foreach (var shot in allShots)
            {
                if (shot.Coordinates == coords)
                {
                    newList.Add(shot);
                }
            }
            return newList;
        }

        /// <summary>
        /// Generates and returns a ShotList of Shot objects that have been received by a specific ControllerID
        /// </summary>
        public ShotList ShotsToReceiver(IDNumber idx)
        {
            ShotList newList = new ShotList();
            newList.allShots = allShots;
            newList.shotsByReceiver[idx] = shotsByReceiver[idx];
            return newList;
        }

        /// <summary>
        /// The ShotListEnumerator enumerates through a ShotList.
        /// </summary>
        private class ShotListEnumerator : IEnumerator<Shot>
        {
            private ShotList collection;
            private int currentIdx;

            public ShotListEnumerator(ShotList shots)
            {
                collection = shots;
                currentIdx = -1;
            }

            public Shot Current
            {
                get
                {
                    return collection[currentIdx];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IDisposable.Dispose()
            {
            }

            public bool MoveNext()
            {
                return (++currentIdx < collection.Count);
            }

            public void Reset()
            {
                currentIdx = -1;
            }
        }
    }
}