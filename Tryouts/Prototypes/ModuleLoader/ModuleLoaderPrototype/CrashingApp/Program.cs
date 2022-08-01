// See https://aka.ms/new-console-template for more information
try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(" Can has exception?");
    Console.ResetColor();
    await Task.Delay(TimeSpan.FromSeconds(5));
    Console.ForegroundColor = ConsoleColor.Yellow;
    throw new Exception("kthxbye");
}
finally
{
    Console.ResetColor();
}