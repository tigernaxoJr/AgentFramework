namespace MyRAG.Samples.Infrastructure;

/// <summary>
/// 所有範例類別的基礎類別，提供共用的顯示工具方法。
/// </summary>
public abstract class SampleBase
{
    protected static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('═', 60));
        Console.ResetColor();
    }

    protected static void PrintStep(string step)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  ▶ ");
        Console.ResetColor();
        Console.WriteLine(step);
    }

    protected static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  ✔ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    protected static void PrintResult(int index, string content, double? score = null)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  [{index}] ");
        Console.ResetColor();

        if (score.HasValue)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"(score: {score:F4}) ");
            Console.ResetColor();
        }

        // 截斷過長的內容
        var display = content.Length > 120 ? content[..120] + "..." : content;
        Console.WriteLine(display);
    }

    protected static void PrintInfo(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  {label}: ");
        Console.ResetColor();
        Console.WriteLine(value);
    }

    protected static void WaitForKey(string prompt = "按任意鍵繼續...")
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {prompt}");
        Console.ResetColor();
        Console.ReadKey(intercept: true);
    }
}
