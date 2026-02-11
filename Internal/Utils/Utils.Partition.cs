using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anatawa12.AvatarOptimizer
{
    partial class Utils
    {
        /// <summary>
        /// Splits given items into groups such that the sum of values in each group does not exceed maxSum.
        /// </summary>
        /// <param name="items">The items to be partitioned, each represented as a tuple of (value, data).</param>
        /// <param name="maxSum">The maximum allowed sum of values in each group.</param>
        /// <typeparam name="T">The type of the data associated with each item.</typeparam>
        /// <returns>A list of groups, where each group is a list of items.</returns>
        /// <exception cref="ArgumentException">If maxSum is less than or equal to 0, or if any item's value exceeds maxSum.</exception>
        public static List<List<(int value, T data)>> Partition<T>(
            IEnumerable<(int value, T data)> items, 
            int maxSum)
        {
            if (maxSum <= 0)
            {
                throw new ArgumentException("maxSum must be greater than 0", nameof(maxSum));
            }

            var itemsList = items.ToList();
        
            // Large first
            var sortedItems = itemsList.OrderByDescending(x => x.value).ToList();

            var groups = new List<List<(int value, T data)>>();
            var groupSums = new List<int>();

            foreach (var item in sortedItems)
            {
                if (item.value > maxSum)
                {
                    throw new ArgumentException(
                        $"Item with value {item.value} exceeds maxSum {maxSum}", 
                        nameof(items));
                }

                for (var i = 0; i < groups.Count; i++)
                {
                    if (groupSums[i] + item.value <= maxSum)
                    {
                        groups[i].Add(item);
                        groupSums[i] += item.value;
                        goto placed;
                    }
                }

                groups.Add(new List<(int value, T data)> { item });
                groupSums.Add(item.value);

                placed: ;
            }

            return groups;
        }
    }
}
