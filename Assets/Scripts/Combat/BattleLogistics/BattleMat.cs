using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using Frankie.Zones;

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
        private int allowedBattleRowCount = _defaultBattleRowPriority.Count;

        #region Static
        // Defaults
        [NoAutoStaticsCleanup] private static readonly List<BattleRow> _defaultBattleRowPriority = new () { BattleRow.Middle, BattleRow.Top, BattleRow.Bottom };
        [NoAutoStaticsCleanup] private static readonly List<BattleRow> _battleRowSortOrder = new() { BattleRow.Top, BattleRow.Middle, BattleRow.Bottom };
        private const int _maxEnemiesPerRow = 11;
        
        public static int GetBattleRowCount() => _battleRowSortOrder.Count;
        public static BattleRow GetDefaultBattleRow() => _defaultBattleRowPriority[0];
        public static int GetDefaultBattleColumn() => _maxEnemiesPerRow / 2;
        private static int GetMaxBattleRowCount(BattleEntityType battleEntityType)
        {
            return battleEntityType switch
            {
                BattleEntityType.Boss => 1,
                BattleEntityType.Standard => 2,
                _ => _defaultBattleRowPriority.Count
            };
        }
        public static BattleRow GetNextBattleRow(BattleRow battleRow, TargetingNavigationType targetingNavigationType)
        {
            if (battleRow == BattleRow.Any) { return GetDefaultBattleRow(); }
            
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
        
        public bool IsEnemyPositionAvailable(BattleEntityType battleEntityType)
        {
            if (GetAllowedBattleRows(GetRestrictedBattleRowCount(battleEntityType)).Any(battleRow => GetEnemyCountInRow(battleRow) < _maxEnemiesPerRow)) { return true; }
            Debug.Log("No remaining positions for enemies to spawn");
            return false;
        }

        public void ClearBattleEntities()
        {
            activeCharacters.Clear();
            activePlayerCharacters.Clear();
            activeAssistCharacters.Clear();
            activeEnemies.Clear();
            allowedBattleRowCount = _defaultBattleRowPriority.Count;
        }

        private static List<BattleRow> GetAllowedBattleRows(int battleRowCount) => _defaultBattleRowPriority.GetRange(0, battleRowCount);
        private int GetRestrictedBattleRowCount(BattleEntityType battleEntityType) => Mathf.Min(allowedBattleRowCount, GetMaxBattleRowCount(battleEntityType));
        private void RestrictAllowedBattleRows(CombatParticipant enemy) => allowedBattleRowCount = GetRestrictedBattleRowCount(enemy.GetBattleEntityType());
        
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
            List<BattleRow> allowedBattleRows = GetAllowedBattleRows(allowedBattleRowCount);
            List<BattleRow> optimalBattleRowPriority = new();
            if (allowedBattleRows.Contains(desiredBattleRow)) { optimalBattleRowPriority.Add(desiredBattleRow); }
            optimalBattleRowPriority.AddRange(allowedBattleRows.Where(testBattleRow => testBattleRow != desiredBattleRow));

            return optimalBattleRowPriority;
        }
        
        private BattleRow GetOpenBattleRow(List<BattleRow> battleRowPriority)
        {
            int rowSplitThreshold = Mathf.Min(minEnemiesBeforeRowSplit, _maxEnemiesPerRow);
            foreach (BattleRow battleRow in battleRowPriority)
            {
                if (GetEnemyCountInRow(battleRow) < rowSplitThreshold) { return battleRow; }
            }
            
            BattleRow openBattleRow = BattleRow.Any;
            int lowestEnemyCount = _maxEnemiesPerRow;
            foreach (BattleRow battleRow in battleRowPriority)
            {
                int enemyCount = GetEnemyCountInRow(battleRow);
                if (enemyCount < lowestEnemyCount) { openBattleRow = battleRow; lowestEnemyCount = enemyCount; }
            }
            return openBattleRow;
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
        
        public void AddEnemiesToCombat(IList<CombatParticipant> enemies, TransitionType transitionType)
        {
            foreach (CombatParticipant enemy in enemies) { RestrictAllowedBattleRows(enemy); } // First ensure battle row restricted to smallest set based on all enemy types
            foreach (CombatParticipant enemy in enemies) { AddEnemyToCombat(enemy, transitionType); }
        }

        public void AddEnemyToCombat(CombatParticipant enemy, TransitionType transitionType = TransitionType.BattleNeutral)
        {
            enemy.InitializeCooldown(false, BattleController.IsBattleAdvantage(false, transitionType));
            enemy.SubscribeToBattleStateChanges(true);

            RestrictAllowedBattleRows(enemy); // Call within here required for e.g. CallForHelp abilities (mid-combat adds) - adding boss mid-combat would narrow the playfield scope (without removing active enemies)
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
            battleRow = GetOpenBattleRow(GetOptimalBattleRowPriority(battleRow));
            if (battleRow == BattleRow.Any) { columnIndex = -1; return; }
            
            const int centreColumn = _maxEnemiesPerRow / 2;
            columnIndex = centreColumn;
            
            if (IsEnemyPresent(battleRow, columnIndex)) // Centre already populated, loop through
            {
                for (int i = 1; i < centreColumn + 1; i++)
                {
                    if (!IsEnemyPresent(battleRow, centreColumn - i)) { columnIndex = centreColumn - i; break; } // -1 offset from centre
                    if (centreColumn + i >= _maxEnemiesPerRow) { break; } // Handling for even counts
                    if (!IsEnemyPresent(battleRow, centreColumn + i)) { columnIndex = centreColumn + i; break; } // +1 offset from centre
                }
            }
            
            SetEnemyInMap(battleRow, columnIndex, true);
        }
        #endregion
    }
}
