using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Control.Tests.PlayMode
{
    // Covered:
    //  - Mover.MoveToTarget (Walk and Warp styles)
    //  - + integration with a PathFinder + MoveMesh
    // Notes:
    //  - Mover.Awake() calls gameObject.SetActive(false) if movementConfiguration is null
    //      - so we build the GameObject inactive, configure everything via SerializedObject, then activate it
    //  - Nothing calls MoveToTarget() automatically - Mover's own FixedUpdate only tracks timeSinceLastMove
    //      - a real subclass is expected to call it from its own FixedUpdate ~ tests call it explicitly instead
    //  - SetStaticForNoTarget() flips the rigidbody to RigidbodyType2D.Dynamic once a target is set
    //      - so gravityScale is zeroed throughout to avoid gravity drift confounding assertions
    //  - Time.deltaTime is not provably equal to Time.fixedDeltaTime when MoveToTarget() is called manually from a test coroutine rather
    //      - thus, Walk-style assertions are qualitative rather than predicting an exact distance per step
    
    public class MoverMovementTests
    {
        // State
        private readonly List<GameObject> spawnedGameObjects = new();
        private readonly List<MovementConfiguration> spawnedConfigurations = new();
        private TestMover lastCreatedMover;
        private Rigidbody2D lastCreatedRigidBody;

        #region DataStructures
        private class TestMover : Mover
        {
            protected override void SelfInitializeRigidBody()
            {
                rigidBody2D = GetComponent<Rigidbody2D>();
                isRigidBodyInitialized = true;
            }

            public override float GetCurrentSpeed() => movementConfiguration.baseMovementSpeed;
            protected override void UpdateAnimatorParameters(bool useCardinalLookDelay = false) { }

            public bool? CallMoveToTarget() => MoveToTarget();
        }
        #endregion
        
        #region Setup
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject go in spawnedGameObjects.Where(go => go != null))
            {
                Object.Destroy(go);
            }
            foreach (MovementConfiguration config in spawnedConfigurations.Where(config => config != null))
            {
                Object.Destroy(config);
            }
            spawnedGameObjects.Clear();
            spawnedConfigurations.Clear();
            yield return null; // let queued destruction actually take effect before the next test
        }
        #endregion

        #region PrivateMethods
        private MovementConfiguration CreateMovementConfiguration(MovementStyle movementStyle, bool usingPathFinding, float baseMovementSpeed, float warpDelay = 1.25f, float warpPostTargetDelay = 0.25f)
        {
            var config = ScriptableObject.CreateInstance<MovementConfiguration>();
            config.movementStyle = movementStyle;
            config.usingPathFinding = usingPathFinding;
            config.baseMovementSpeed = baseMovementSpeed;
            config.warpDelay = warpDelay;
            config.warpPostTargetDelay = warpPostTargetDelay;
            spawnedConfigurations.Add(config);
            return config;
        }

        // Builds and activates a TestMover + Rigidbody2D w/ config @ starting position
        //  - Note:  Awake/OnEnable/Start have already run by the time this returns
        private IEnumerator CreateTestMover(MovementConfiguration config, Vector3 startPosition, System.Action<PathFinder> beforeActivate = null)
        {
            var go = new GameObject("MoverMovementTests_Target");
            spawnedGameObjects.Add(go);
            go.transform.position = startPosition;
            go.SetActive(false);
            var testMover = go.AddComponent<TestMover>();
            var rigidBody2D = go.AddComponent<Rigidbody2D>();
            rigidBody2D.gravityScale = 0f;

            var serializedMover = new SerializedObject(testMover);
            serializedMover.FindProperty("movementConfiguration").objectReferenceValue = config;
            serializedMover.ApplyModifiedPropertiesWithoutUndo();

            beforeActivate?.Invoke(go.GetComponent<PathFinder>());

            go.SetActive(true);
            yield return null; // let Awake/OnEnable/Start run

            // Coroutine helpers can't "return" a value alongside yielding - stashing for access after the yield
            lastCreatedMover = testMover;
            lastCreatedRigidBody = rigidBody2D;
        }
        
        private MoveMesh CreateMoveMesh(int columns, int rows, float cellSize, bool[] cells)
        {
            var go = new GameObject("MoverMovementTests_MoveMesh");
            spawnedGameObjects.Add(go);
            var moveMesh = go.AddComponent<MoveMesh>();
            moveMesh.gameObject.layer = MoveMesh.GetMoveMeshLayer();
            moveMesh.walkabilityGrid = new WalkabilityGrid
            {
                columns = columns,
                rows = rows,
                cellSize = cellSize,
                originX = 0f,
                originY = 0f,
                cells = new List<bool>(cells),
                traversalCosts = Enumerable.Repeat(1f, columns * rows).ToList()
            };
            return moveMesh;
        }

        private static bool[] AllWalkable(int columns, int rows) => Enumerable.Repeat(true, columns * rows).ToArray();
        #endregion
        
        #region WalkStyleTests
        [UnityTest]
        public IEnumerator MoveToTarget_WalkStyle_MovesTowardTargetOverPhysicsSteps()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: false, baseMovementSpeed: 50f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;
            Rigidbody2D rigidBody2D = lastCreatedRigidBody;

            testMover.SetMoveTarget(new Vector2(10f, 0f));

            float previousX = rigidBody2D.position.x;
            for (int i = 0; i < 5; i++)
            {
                testMover.CallMoveToTarget();
                yield return new WaitForFixedUpdate();
                Assert.That(rigidBody2D.position.x, Is.GreaterThan(previousX), $"Should have moved right on step {i}");
                previousX = rigidBody2D.position.x;
            }
            Assert.That(rigidBody2D.position.y, Is.EqualTo(0f).Within(0.0001f), "Should not have moved vertically toward a same-row target");
        }

        [UnityTest]
        public IEnumerator MoveToTarget_WalkStyle_AlreadyAtTarget_ReturnsFalseAndDoesNotMove()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: false, baseMovementSpeed: 50f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;
            Rigidbody2D rigidBody2D = lastCreatedRigidBody;

            testMover.SetMoveTarget(Vector2.zero); // identical to the starting position

            bool? result = testMover.CallMoveToTarget();
            yield return new WaitForFixedUpdate();

            Assert.That(result, Is.False);
            Assert.That(rigidBody2D.position, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator MoveToTarget_WalkStyle_TargetGameObject_MovesTowardIt()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: false, baseMovementSpeed: 50f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;
            Rigidbody2D rigidBody2D = lastCreatedRigidBody;

            var targetGo = new GameObject("MoverMovementTests_MoveTargetObject");
            spawnedGameObjects.Add(targetGo);
            targetGo.transform.position = new Vector3(10f, 0f, 0f);
            testMover.SetMoveTarget(targetGo);

            float previousX = rigidBody2D.position.x;
            for (int i = 0; i < 5; i++)
            {
                testMover.CallMoveToTarget();
                yield return new WaitForFixedUpdate();
                Assert.That(rigidBody2D.position.x, Is.GreaterThan(previousX));
                previousX = rigidBody2D.position.x;
            }
        }

        [UnityTest]
        public IEnumerator ClearMoveTargets_StopsFurtherMovement()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: false, baseMovementSpeed: 50f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;
            Rigidbody2D rigidBody2D = lastCreatedRigidBody;

            testMover.SetMoveTarget(new Vector2(10f, 0f));
            testMover.CallMoveToTarget();
            yield return new WaitForFixedUpdate();
            Assert.That(rigidBody2D.position.x, Is.GreaterThan(0f), "Sanity check: should have moved before clearing");

            testMover.ClearMoveTargets();
            bool? resultAfterClear = testMover.CallMoveToTarget();
            float positionAfterClear = rigidBody2D.position.x;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(resultAfterClear, Is.Null);
            Assert.That(rigidBody2D.position.x, Is.EqualTo(positionAfterClear).Within(0.0001f), "Should not move any further once targets are cleared");
        }

        [UnityTest]
        public IEnumerator MoveToTarget_NoTargetEverSet_ReturnsNull()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: false, baseMovementSpeed: 50f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;

            bool? result = testMover.CallMoveToTarget();
            Assert.That(result, Is.Null);
        }
        #endregion

        #region WarpStyleTests
        [UnityTest]
        public IEnumerator MoveToTarget_WarpStyle_DelaysThenTeleportsToTarget()
        {
            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Warp, usingPathFinding: false, baseMovementSpeed: 1f, warpDelay: 0.05f, warpPostTargetDelay: 0.05f);
            yield return CreateTestMover(config, Vector3.zero);
            TestMover testMover = lastCreatedMover;

            var target = new Vector2(5f, 0f);
            testMover.SetMoveTarget(target);

            // Allow timeSinceLastMove to exceed warpDelay before attempting warp
            yield return new WaitForSeconds(0.15f);
            Assert.That(testMover.transform.position, Is.Not.EqualTo((Vector3)target), "Sanity check: should not have warped yet");

            bool? result = testMover.CallMoveToTarget();
            Assert.That(result, Is.True, "Should have successfully queued the delayed warp");

            // Allow the warpPostTargetDelay to elapse
            yield return new WaitForSeconds(0.15f);

            Assert.That(testMover.transform.position.x, Is.EqualTo(target.x).Within(0.0001f));
            Assert.That(testMover.transform.position.y, Is.EqualTo(target.y).Within(0.0001f));
        }
        #endregion
        
        #region WalkStyleWithPathfindingTests
        [UnityTest]
        public IEnumerator MoveToTarget_WalkStylePathfinding_RoutesAroundObstacleTowardGap()
        {
            // Column 2 blocked for rows 0-3, leaving row 4 as the only gap
            //  - only checking that it actually moves upward at some point (not the exact path)
            //  - simulating a full A* search isn't safely provable without running it
            
            int columns = 5, rows = 5;
            bool[] cells = AllWalkable(columns, rows);
            for (int row = 0; row < 4; row++) { cells[row * columns + 2] = false; }
            MoveMesh moveMesh = CreateMoveMesh(columns, rows, 1f, cells);

            MovementConfiguration config = CreateMovementConfiguration(MovementStyle.Walk, usingPathFinding: true, baseMovementSpeed: 20f);
            yield return CreateTestMover(config, new Vector3(0.5f, 0.5f, 0f), beforeActivate: pathFinder => pathFinder.cachedMoveMesh = moveMesh);
            TestMover testMover = lastCreatedMover;
            Rigidbody2D rigidBody2D = lastCreatedRigidBody;
            
            var pathFinder = testMover.GetComponent<PathFinder>();
            Assert.That(pathFinder.IsValidPathFinder(), Is.True, "Pathfinding cache should have initialized via Start()");
            
            Vector2 target = moveMesh.CellToWorld(4, 0);
            bool foundDirectly = pathFinder.FindPath(rigidBody2D.position, target);
            Assert.That(foundDirectly, Is.True, "PathFinder itself should find a path from the rigidbody's actual position");
            Assert.That(pathFinder.currentPath.Last(), Is.EqualTo(target), "Direct PathFinder call should reach the exact target cell");

            var targetObject = new GameObject { transform = { position = target } };
            spawnedGameObjects.Add(targetObject);
            testMover.SetMoveTarget(targetObject);
            
            float maxXObserved = rigidBody2D.position.x;
            float maxYObserved = rigidBody2D.position.y;
            for (int i = 0; i < 30; i++)
            {
                testMover.CallMoveToTarget();
                yield return new WaitForFixedUpdate();
                maxXObserved = Mathf.Max(maxXObserved, rigidBody2D.position.x);
                maxYObserved = Mathf.Max(maxYObserved, rigidBody2D.position.y);
            }

            Assert.That(maxXObserved, Is.GreaterThan(0.5f), "Sanity check: mover should have moved in X at all");
            Assert.That(maxYObserved, Is.GreaterThan(1.0f), "Mover should have detoured upward toward the gap rather than walking straight into the wall");
        }
        #endregion
    }
}
