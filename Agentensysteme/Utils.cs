using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agentensysteme
{
    public static class Utils
    {
        public static async Task<(int count, List<List<int>> matrix)> Import(string path)
        {
            var input = await File.ReadAllLinesAsync(path);
            return (Convert.ToInt32(input[0]), input.Skip(1).Where(x => x.Trim() != "").Select(x => x.Trim().Split(" ").Select(xx => Convert.ToInt32(xx)).ToList()).ToList());
        }

        public static int CalculateCost(List<List<int>> matrix, List<int> order)
        {
            int sum = 0;
            for (int i = 1; i < order.Count; i++)
            {
                int previous = order[i - 1];
                int current = order[i];
                sum += matrix[previous][current];
            }
            return sum;
        }

        public static List<int> GetRandomOrder(int count)
            => Enumerable.Range(0, count).Shuffle().ToList();


        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static (int, int, List<int>) BruteforceBestOrder(List<List<int>> matrix1, List<List<int>> matrix2)
        {
            var orders = GetPermutations(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 10);
            var min = int.MaxValue;
            List<int> minOrder = new List<int>();
            int count = 0;
            long totalCost = 0;

            foreach (var order in orders)
            {
                count++;
                if (count % 100000 == 0)
                    Console.WriteLine($"Permutation {count}");
                var orderAsList = order.ToList();
                var cost1 = Utils.CalculateCost(matrix1, orderAsList);
                var cost2 = Utils.CalculateCost(matrix2, orderAsList);
                var cost = cost1 + cost2;
                totalCost += cost;
                if (cost < min)
                {
                    min = cost;
                    minOrder = orderAsList;
                }
            }
            return (min, (int)(totalCost / count), minOrder);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            Random random = new Random();
            return enumerable.OrderBy(x => random.Next());
        }

        public static T TakeRandom<T>(this IList<T> list)
        {
            return list.Count == 0 ? default : list[new Random().Next(0, list.Count)];
        }
        public static T TakeRandom<T>(this IList<T> list, List<T> except)
        {
            T elem = default;
            for (int i = 0; i < 10; ++i)
            {
                elem = list[new Random().Next(0, list.Count)];
                if (!except.Contains(elem))
                    return elem;
            }
            return elem;
        }

        public static IEnumerable<T> TakePercent<T>(this IEnumerable<T> enumerable, double percent)
        {
            return enumerable.Take((int)(percent * enumerable.Count()) + 1);
        }

        public static bool Get5050()
        {
            return new Random().Next(0, 2) == 0;
        }
    }
}