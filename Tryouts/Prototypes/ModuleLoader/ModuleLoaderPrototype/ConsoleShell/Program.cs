using ConsoleShell;
using ModuleLoaderPrototype;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, ComposeUI!");
Console.WriteLine("Choose module loader type 'A' or 'B'");
char type = Console.ReadKey().KeyChar;
IDemo demo = null;
switch (type)
{
    case 'a':
    case 'A':
        demo = new TypeADemo();
        break;
    case 'b':
    case 'B':
        demo = new TypeBDemo();
        break;
    default:
        Console.WriteLine("No such demo");
        break;
}

demo?.RunDemo();

