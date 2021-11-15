using Agentensysteme;

var (count1, matrix1) = await Utils.Import("Files/daten2A.txt");
var (count2, matrix2) = await Utils.Import("Files/daten2B.txt");
var (count3, matrix3) = await Utils.Import("Files/daten3A.txt");
var (count4, matrix4) = await Utils.Import("Files/daten3B.txt");

//var order1 = Utils.GetRandomOrder(Math.Min(count1, count2));
var order2 = Utils.GetRandomOrder(Math.Min(count3, count4));
var order1 = new List<int> { 0, 1, 2, 3 ,4 ,5 ,6 ,7 ,8 ,9 };

var cost1 = Utils.CalculateCost(matrix1, order1);
var cost2 = Utils.CalculateCost(matrix2, order1);
var cost3 = Utils.CalculateCost(matrix3, order2);
var cost4 = Utils.CalculateCost(matrix4, order2);

Console.WriteLine($"Order: {string.Join(", ", order1)}");
Console.WriteLine($"Cost1: {cost1}");
Console.WriteLine($"Cost2: {cost2}");
Console.WriteLine($"Order: {string.Join(", ", order2)}");
Console.WriteLine($"Cost3: {cost3}");
Console.WriteLine($"Cost4: {cost4}");
