using System;
using System.Linq;
using lancedb;

var tableType = typeof(lancedb.Table);
Console.WriteLine($"Methods in lancedb.Table:");
foreach (var method in tableType.GetMethods().OrderBy(m => m.Name))
{
    Console.WriteLine($"- {method.Name}");
}
