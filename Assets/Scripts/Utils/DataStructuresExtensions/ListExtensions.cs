using System.Collections.Generic;

public static class ListExtensions
{
    private static readonly System.Random _randomNumberGenerator = new();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _randomNumberGenerator.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
