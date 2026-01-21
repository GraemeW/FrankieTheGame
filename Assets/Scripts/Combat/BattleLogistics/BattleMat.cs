using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.ZoneManagement;

namespace Frankie.Combat
{
    public class BattleMat : MonoBehaviour
    {
        [Header("Positional Properties")]
        [SerializeField] private int minEnemiesBeforeRowSplit = 5;

        // State
        private readonly List<BattleEntity> activeCharacters = new();
        private readonly List<BattleEntity> activePlayerCharacters = new();
        private readonly List<BattleEntity> activeAssistCharacters = new();
        private readonly List<BattleEntity> activeEnemies = new();
        public int GetCountActivePlayerCharacters() => activePlayerCharacters.Count;
        private readonly Dictionary<BattleRow, int> enemyMap = new() { {BattleRow.Middle, 0}, {BattleRow.Top, 0}, {BattleRow.Bottom, 0} };

        #region Static
        private static readonly HashSet<BattleRow> _defaultBattleRowPriority = new () { BattleRow.Middle, BattleRow.Top };
        private static readonly List<BattleRow> _battleRowSortOrder = new() { BattleRow.Top, BattleRow.Middle, BattleRow.Bottom };
        private const int _maxEnemiesPerRow = 5;
        
        public static int GetBattleRowCount() => _battleRowSortOrder.Count;
        public static BattleRow GetDefaultBattleRow() => _battleRowSortOrder[0];
        public static int GetDefaultBattleColumn() => _maxEnemiesPerRow / 2;
        public static BattleRow GetNextBattleRow(BattleRow battleRow, TargetingNavigationType targetingNavigationType)
        {
            if (battleRow == BattleRow.Any) { return _defaultBattleRowPriority.First(); }
            
            int currentBattleRowIndex = _battleRowSortOrder.IndexOf(battleRow);
            int nextBattleRowIndex;
            switch (targetingNavigationType)
            {
                case TargetingNavigationType.Down:
                    nextBattleRowIndex = (currentBattleRowIndex == _battleRowSortOrder.Count - 1) ? 0 : currentBattleRowIndex + 1;
                    return _battleRowSortOrder[nextBattleRowIndex];
                case TargetingNavigationType.Up:
                    nextBattleRowIndex =  (currentBattleRowIndex == 0) ? _battleRowSortOrder.Count - 1 : currentBattleRowIndex - 1;
                    return _battleRowSortOrder[nextBattleRowIndex];
                case TargetingNavigationType.Hold:
                case TargetingNavigationType.Right:
                case TargetingNavigationType.Left:
                default:
                    return battleRow;
            }
        }
        #endregion
        
        #region GettersSetters
        public IList<BattleEntity> GetActiveCharacters() => activeCharacters.AsReadOnly();
        public IList<BattleEntity> GetActivePlayerCharacters() => activePlayerCharacters.AsReadOnly();
        public IList<BattleEntity> GetActiveAssistCharacters() => activeAssistCharacters.AsReadOnly();
        public IList<BattleEntity> GetActiveEnemies() => activeEnemies.AsReadOnly();
        
        public bool IsEnemyPositionAvailable()
        {
            if (_defaultBattleRowPriority.Any(battleRow => GetEnemyCountInRow(battleRow) < _maxEnemiesPerRow)) { return true; }
            Debug.Log("No remaining positions for enemies to spawn");
            return false;
        }

        public void ClearBattleEntities()
        {
            activeCharacters.Clear();
            activePlayerCharacters.Clear();
            activeAssistCharacters.Clear();
            activeEnemies.Clear();
        }
        
        private int GetEnemyCountInRow(BattleRow battleRow)
        {
            if (battleRow == BattleRow.Any) { return 0; }
            
            // Same as PopCount:  https://learn.microsoft.com/en-us/dotnet/api/system.numerics.bitoperations.popcount?view=net-9.0
            // This implementation used since BitOperations not exposed in Unity's version of C# (non-Core)
            int rowMask = enemyMap[battleRow];
            int rowEnemyCount = 0;
            while (rowMask!=0) { rowMask &= (rowMask-1); rowEnemyCount++; } // n&(n-1) always eliminates the least significant 1

            return rowEnemyCount;
        }
        
        private List<BattleRow> GetOptimalBattleRowPriority(BattleRow desiredBattleRow)
        {
            List<BattleRow> optimalBattleRowPriority = new();
            if (desiredBattleRow != BattleRow.Any)
            {
                optimalBattleRowPriority.Add(desiredBattleRow); 
                _defaultBattleRowPriority.Add(desiredBattleRow); // E.g. Default 2-row @ Mid/Top, new char prefers bott -> thus enables 3-row w/ bott as a default option
            }
            optimalBattleRowPriority.AddRange(_defaultBattleRowPriority.Where(testBattleRow => testBattleRow != desiredBattleRow));

            return optimalBattleRowPriority;
        }
        
        private bool IsEnemyPresent(BattleRow battleRow, int columnIndex)
        {
            int mask = 1 << columnIndex;
            return (enemyMap[battleRow] & mask) != 0;
        }
        #endregion
        
        #region MatSetupMethods
        public void AddCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(true, BattleController.IsBattleAdvantage(true, transitionType));
            character.SubscribeToBattleStateChanges(true);

            var characterBattleEntity = new BattleEntity(character);
            activeCharacters.Add(characterBattleEntity);
            activePlayerCharacters.Add(characterBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(characterBattleEntity, false));
        }

        public void AddAssistCharacterToCombat(CombatParticipant character, TransitionType transitionType)
        {
            character.InitializeCooldown(false, BattleController.IsBattleAdvantage(true, transitionType));
            character.SubscribeToBattleStateChanges(true);

            var assistBattleEntity = new BattleEntity(character, true);
            activeCharacters.Add(assistBattleEntity);
            activeAssistCharacters.Add(assistBattleEntity);

            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(assistBattleEntity, false));
        }
        
        public void AddEnemyToCombat(CombatParticipant enemy, TransitionType transitionType = TransitionType.BattleNeutral, bool addMidCombatForceActive = false)
        {
            enemy.InitializeCooldown(false, BattleController.IsBattleAdvantage(false, transitionType));
            enemy.SubscribeToBattleStateChanges(true);

            BattleRow battleRow = enemy.GetPreferredBattleRow();
            UpdateEnemyPosition(ref battleRow, out int columnIndex);
            if (battleRow == BattleRow.Any) { Debug.Log($"Warning, could not add {enemy.name} to combat"); return; }
            
            Debug.Log($"New enemy added at position row: {battleRow} ; col: {columnIndex}");

            var enemyBattleEntity = new BattleEntity(enemy, enemy.GetBattleEntityType(), battleRow, columnIndex);
            activeEnemies.Add(enemyBattleEntity);
            
            BattleEventBus<BattleEntityAddedEvent>.Raise(new BattleEntityAddedEvent(enemyBattleEntity, true));
        }
        
        public void SetEnemyInMap(BattleRow battleRow, int columnIndex, bool enable)
        {
            int mask = 1 << columnIndex;
            if (enable) { enemyMap[battleRow] |= mask; }
            else { enemyMap[battleRow] &= ~mask; }
        }
        
        private void UpdateEnemyPosition(ref BattleRow battleRow, out int columnIndex)
        {
            List<BattleRow> optimalBattleRowPriority = GetOptimalBattleRowPriority(battleRow);
            optimalBattleRowPriority.RemoveAll(testBattleRow => GetEnemyCountInRow(testBattleRow) >= _maxEnemiesPerRow);

            if (optimalBattleRowPriority.Count == 0) { battleRow = BattleRow.Any; columnIndex = 0; return; } // early exit, no rows available
            if (!optimalBattleRowPriority.Contains(battleRow)) { battleRow = BattleRow.Any; } // desired row not available, swap to any
            
            if (battleRow == BattleRow.Any)
            {
                battleRow = GetEnemyCountInRow(optimalBattleRowPriority[0]) <= minEnemiesBeforeRowSplit ? optimalBattleRowPriority[0] 
                    : optimalBattleRowPriority.OrderBy(GetEnemyCountInRow).ToList().FirstOrDefault();
            }

            // Find column position
            int centerColumn = _maxEnemiesPerRow / 2;
            columnIndex = centerColumn;
            
            if (IsEnemyPresent(battleRow, columnIndex)) // Center already populated, loop through
            {
                for (int i = 1; i < centerColumn + 1; i++)
                {
                    if (!IsEnemyPresent(battleRow, centerColumn - i)) { columnIndex = centerColumn - i; break; } // -1 offset from center
                    if (centerColumn + i >= _maxEnemiesPerRow) { break; } // Handling for even counts
                    if (!IsEnemyPresent(battleRow, centerColumn + i)) { columnIndex = centerColumn + i; break; } // +1 offset from center
                }
            }
            
            SetEnemyInMap(battleRow, columnIndex, true);
        }
        #endregion
    }
}
