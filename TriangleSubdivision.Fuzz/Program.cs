using System;

namespace Kernel.Fuzz;

internal static class Program
{
    private static void Main(string[] args)
    {
        int iterations = 100_000;
        int maxPoints = 8;
        int seed = 12345;

        if (args.Length > 0 && int.TryParse(args[0], out var iters))
        {
            iterations = iters;
        }

        if (args.Length > 1 && int.TryParse(args[1], out var mp))
        {
            maxPoints = mp;
        }

        if (args.Length > 2 && int.TryParse(args[2], out var s))
        {
            seed = s;
        }

        Console.WriteLine($"TriangleSubdivision fuzz: iterations={iterations}, maxPoints={maxPoints}, seed={seed}");
        TriangleSubdivisionFuzz.Run(iterations, maxPoints, seed);
        Console.WriteLine("Fuzz completed successfully.");
    }
}
