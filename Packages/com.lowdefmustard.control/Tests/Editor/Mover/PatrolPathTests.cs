using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    public class PatrolPathTests
    {
        // State
        private readonly List<GameObject> spawnedGameObjects = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawnedGameObjects.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }

            spawnedGameObjects.Clear();
        }
        #endregion

        #region PrivateMethods
        private GameObject CreateGameObject()
        {
            var go = new GameObject("PatrolPathTests_Target");
            spawnedGameObjects.Add(go);
            return go;
        }

        private PatrolPathWaypoint CreateWaypoint(WaypointType waypointType = WaypointType.Move)
        {
            var waypoint = CreateGameObject().AddComponent<PatrolPathWaypoint>();
            var serializedWaypoint = new SerializedObject(waypoint);
            serializedWaypoint.FindProperty("waypointType").enumValueIndex = (int)waypointType;
            serializedWaypoint.ApplyModifiedPropertiesWithoutUndo();
            return waypoint;
        }

        private PatrolPath CreatePatrolPath(int waypointCount, bool looping = true, bool returnToFirstWaypoint = true)
        {
            var patrolPath = CreateGameObject().AddComponent<PatrolPath>();
            var waypoints = new PatrolPathWaypoint[waypointCount];
            for (int i = 0; i < waypointCount; i++) { waypoints[i] = CreateWaypoint(); }

            var serializedPatrolPath = new SerializedObject(patrolPath);
            SerializedProperty waypointsProperty = serializedPatrolPath.FindProperty("waypoints");
            waypointsProperty.arraySize = waypointCount;
            for (int i = 0; i < waypointCount; i++) { waypointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i]; }
            serializedPatrolPath.FindProperty("looping").boolValue = looping;
            serializedPatrolPath.FindProperty("returnToFirstWaypoint").boolValue = returnToFirstWaypoint;
            serializedPatrolPath.ApplyModifiedPropertiesWithoutUndo();

            return patrolPath;
        }
        #endregion
        
        #region PatrolPathWaypointTests
        [Test]
        public void GetWaypointType_Default_IsMove()
        {
            PatrolPathWaypoint waypoint = CreateGameObject().AddComponent<PatrolPathWaypoint>();
            Assert.That(waypoint.GetWaypointType(), Is.EqualTo(WaypointType.Move));
        }

        [Test]
        public void GetWaypointType_SetToWarp_ReturnsWarp()
        {
            PatrolPathWaypoint waypoint = CreateWaypoint(WaypointType.Warp);
            Assert.That(waypoint.GetWaypointType(), Is.EqualTo(WaypointType.Warp));
        }
        #endregion

        #region GetWaypointsTests
        [Test]
        public void GetWaypoint_NullWaypointsArray_ReturnsNull()
        {
            PatrolPath patrolPath = CreateGameObject().AddComponent<PatrolPath>();
            Assert.That(patrolPath.GetWaypoint(0), Is.Null);
        }

        [Test]
        public void GetWaypoint_EmptyWaypointsArray_ReturnsNull()
        {
            PatrolPath patrolPath = CreatePatrolPath(0);
            Assert.That(patrolPath.GetWaypoint(0), Is.Null);
        }

        [Test]
        public void GetWaypoint_ValidIndex_ReturnsThatWaypoint()
        {
            PatrolPath patrolPath = CreatePatrolPath(3);
            Assert.That(patrolPath.GetWaypoint(1), Is.SameAs(patrolPath.GetWaypoint(1)));
            Assert.That(patrolPath.GetWaypoint(0), Is.Not.SameAs(patrolPath.GetWaypoint(1)));
        }

        [Test]
        public void IsFinalWaypoint_LastIndex_ReturnsTrue()
        {
            PatrolPath patrolPath = CreatePatrolPath(3);
            Assert.That(patrolPath.IsFinalWaypoint(2), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void IsFinalWaypoint_NonLastIndex_ReturnsFalse(int index)
        {
            PatrolPath patrolPath = CreatePatrolPath(3);
            Assert.That(patrolPath.IsFinalWaypoint(index), Is.False);
        }

        [Test]
        public void IsFinalWaypoint_NullWaypoints_ReturnsTrue()
        {
            PatrolPath patrolPath = CreateGameObject().AddComponent<PatrolPath>();
            Assert.That(patrolPath.IsFinalWaypoint(0), Is.True);
        }
        #endregion

        #region GetNextIndexTests
        [Test]
        public void GetNextIndex_NullWaypoints_AlwaysReturnsZero()
        {
            PatrolPath patrolPath = CreateGameObject().AddComponent<PatrolPath>();
            Assert.That(patrolPath.GetNextIndex(0), Is.EqualTo(0));
            Assert.That(patrolPath.GetNextIndex(5), Is.EqualTo(0));
        }

        [Test]
        public void GetNextIndex_EmptyWaypoints_AlwaysReturnsZero()
        {
            PatrolPath patrolPath = CreatePatrolPath(0);
            Assert.That(patrolPath.GetNextIndex(0), Is.EqualTo(0));
        }

        [Test]
        public void GetNextIndex_SingleWaypoint_DefaultSettings_AlwaysStaysAtZero()
        {
            PatrolPath patrolPath = CreatePatrolPath(1);
            Assert.That(patrolPath.GetNextIndex(0), Is.EqualTo(0));
            Assert.That(patrolPath.GetNextIndex(0), Is.EqualTo(0));
        }

        [Test]
        public void GetNextIndex_LoopingReturnToFirst_CyclesForwardForever()
        {
            // looping=true, returnToFirstWaypoint=true (defaults): 0 -> 1 -> 2 -> 0 -> 1 -> 2 -> ...
            PatrolPath patrolPath = CreatePatrolPath(3);
            var sequence = new List<int>();
            int index = 0;
            for (int i = 0; i < 7; i++)
            {
                sequence.Add(index);
                index = patrolPath.GetNextIndex(index);
            }
            Assert.That(sequence, Is.EqualTo(new[] { 0, 1, 2, 0, 1, 2, 0 }));
        }

        [Test]
        public void GetNextIndex_NonLooping_ReturnToFirst_StopsAtFirstWaypointAfterOneLap()
        {
            // looping=false, returnToFirstWaypoint=true: walks forward once, returns to 0, then freezes there
            PatrolPath patrolPath = CreatePatrolPath(3, looping: false, returnToFirstWaypoint: true);
            var sequence = new List<int>();
            int index = 0;
            for (int i = 0; i < 5; i++)
            {
                sequence.Add(index);
                index = patrolPath.GetNextIndex(index);
            }
            Assert.That(sequence, Is.EqualTo(new[] { 0, 1, 2, 0, 0 }));
        }

        [Test]
        public void GetNextIndex_NonLooping_NoReturnToFirst_StopsAtFinalWaypoint()
        {
            // looping=false, returnToFirstWaypoint=false: walks forward once and freezes at the last waypoint
            PatrolPath patrolPath = CreatePatrolPath(3, looping: false, returnToFirstWaypoint: false);
            var sequence = new List<int>();
            int index = 0;
            for (int i = 0; i < 5; i++)
            {
                sequence.Add(index);
                index = patrolPath.GetNextIndex(index);
            }
            Assert.That(sequence, Is.EqualTo(new[] { 0, 1, 2, 2, 2 }));
        }

        [Test]
        public void GetNextIndex_LoopingNoReturnToFirst_PingPongsBackAndForth()
        {
            // looping=true, returnToFirstWaypoint=false: -1/+1 flip takes effect: 0 -> 1 -> 2 -> 1 -> 0 -> 1 -> 2 -> 1 -> 0
            PatrolPath patrolPath = CreatePatrolPath(3, looping: true, returnToFirstWaypoint: false);
            var sequence = new List<int>();
            int index = 0;
            for (int i = 0; i < 9; i++)
            {
                sequence.Add(index);
                index = patrolPath.GetNextIndex(index);
            }
            Assert.That(sequence, Is.EqualTo(new[] { 0, 1, 2, 1, 0, 1, 2, 1, 0 }));
        }

        [Test]
        public void GetNextIndex_CalledFromGizmo_AtNonFinalIndex_AlwaysAddsOneRegardlessOfDirection()
        {
            // Confirms the calledFromGizmo shortcut (waypointIndex + 1)
            PatrolPath patrolPath = CreatePatrolPath(3, looping: true, returnToFirstWaypoint: false);
            Assert.That(patrolPath.GetNextIndex(0, calledFromGizmo: true), Is.EqualTo(1));
            Assert.That(patrolPath.GetNextIndex(1, calledFromGizmo: true), Is.EqualTo(2));
        }

        [Test]
        public void GetNextIndex_CalledFromGizmo_DoesNotMutateRealPatrolState()
        {
            // A gizmo call must NOT set loopedOnce, so the real state machine remains completely undisturbed afterward
            PatrolPath patrolPath = CreatePatrolPath(3, looping: false, returnToFirstWaypoint: true);

            int gizmoResult = patrolPath.GetNextIndex(2, calledFromGizmo: true);
            Assert.That(gizmoResult, Is.EqualTo(0), "gizmo calls still get the same return-to-first value");

            // If the gizmo call above had set loopedOnce, this real call would now be frozen and return 0 unchanged
            int realResult = patrolPath.GetNextIndex(0);
            Assert.That(realResult, Is.EqualTo(1), "real state should be untouched by the earlier gizmo call");
        }
        #endregion
    }
}
