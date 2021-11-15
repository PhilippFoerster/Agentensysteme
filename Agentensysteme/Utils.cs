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
        {
            Random random = new Random();
            var numbers = Enumerable.Range(0, count);
            return numbers.OrderBy(x => random.Next()).ToList();
        }
    }
}