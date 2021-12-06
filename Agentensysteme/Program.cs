using Agentensysteme;
using System.Diagnostics;

var (count1, matrix1) = await Utils.Import("Files/daten2A.txt");
var (count2, matrix2) = await Utils.Import("Files/daten2B.txt");
var (count3, matrix3) = await Utils.Import("Files/daten3A.txt");
var (count4, matrix4) = await Utils.Import("Files/daten3B.txt");

var agent1 = new Agent(matrix3);
var agent2 = new Agent(matrix4);

List<long> total = new();
int i = 0;
Stopwatch stopWatch = Stopwatch.StartNew();
while(!Console.KeyAvailable)
{
    agent1.Reset();
    agent2.Reset();
    var mediator = new Mediator(new List<Agent> { agent1, agent2 }, agent1.Matrix.Count);
    //Hier kannst du die 2 verschiedenen Ansätze ausprobieren
    var result = mediator.PickBest(200, mediator.BuildProposal); //Neuer Ansatz: Dynamisches zusammenbauen einer Lösung
    //var result = mediator.PickBest(50, () => mediator.RunNegotiation(300, false)); //Alter Ansatz
    //var result = mediator.RunNegotiation(300); //Eine einzelne Verhandlung
    var cost1 = Utils.CalculateCost(agent1.Matrix, result.Order);
    var cost2 = Utils.CalculateCost(agent2.Matrix, result.Order);
    var sum = cost1 + cost2;
    total.Add(sum);
    ++i;
    //Console.WriteLine($"Order: {string.Join(", ", result.Order)}");
    Console.WriteLine($"{cost1} - {cost2} - {sum}");
}
stopWatch.Stop();
Console.WriteLine($"Negotiations: {i}, Time: {stopWatch.Elapsed} - {stopWatch.Elapsed.TotalSeconds / i}");
Console.WriteLine($"Average: {total.Sum() / i}");
Console.WriteLine($"Min: {total.Min()}");
Console.WriteLine($"Max: {total.Max()}");