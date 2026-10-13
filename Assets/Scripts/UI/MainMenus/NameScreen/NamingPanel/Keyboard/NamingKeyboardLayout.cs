using System;
using UnityEngine;

namespace Frankie.Menu.UI
{
    [Serializable]
    public sealed class NamingKeyboardLayout
    {
        // Logic as below:
        //  - Column counts for the two key chunks (letters | gap | special characters) share one fixed-width grid
        //  - Larger alphabets will grow the letter chunk:
        //      - first by eating into the gap column count
        //      - then by narrowing the special characters column count (which wrap onto more rows)
        //  - Rows are whatever the columns leave - the keyboard grows downwards past its preferred row count as needed
        
        // Const
        public const int defaultLetterColumns = 8;
        public const int defaultSpecialColumns = 5;

        // Tunables
        [SerializeField][Min(1)] private int totalColumns = 16; // Width of the key grid, in keys
        [SerializeField][Min(0)] private int minGapColumns = 1;
        [SerializeField][Min(0)] private int maxExtraLetterColumns = 3;
        [SerializeField][Min(1)] private int minSpecialColumns = 3;

        public void GetColumns(int letterCount, int specialCount, out int letterColumns, out int specialColumns)
        {
            // Fewest rows wins; ties go to the layout closest to the default
            letterColumns = defaultLetterColumns;
            specialColumns = defaultSpecialColumns;
            int fewestRows = GetRows(letterCount, letterColumns, specialCount, specialColumns);

            for (int extraLetterColumns = 1; extraLetterColumns <= maxExtraLetterColumns; extraLetterColumns++)
            {
                int candidateLetterColumns = defaultLetterColumns + extraLetterColumns;
                int candidateSpecialColumns = Mathf.Min(defaultSpecialColumns, totalColumns - minGapColumns - candidateLetterColumns);
                if (candidateSpecialColumns < minSpecialColumns) { break; }

                int rows = GetRows(letterCount, candidateLetterColumns, specialCount, candidateSpecialColumns);
                if (rows >= fewestRows) { continue; }

                fewestRows = rows;
                letterColumns = candidateLetterColumns;
                specialColumns = candidateSpecialColumns;
            }
        }

        private static int GetRows(int letterCount, int letterColumns, int specialCount, int specialColumns)
        {
            return Mathf.Max(Mathf.CeilToInt(letterCount / (float)letterColumns), Mathf.CeilToInt(specialCount / (float)specialColumns));
        }
    }
}
