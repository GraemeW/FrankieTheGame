using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueLinePositionPlanner
    {
        // Line breadth/depth logic:
        //  - unplaced + reachable from Entry:  depth = shallowest speech parent + 1 (Entry = -1, condition nodes don't count) w/ next free breadth
        //  - placed lines sharing a depth.breadth (e.g. pasted):  one keeps it (a synced one if any), the others take the next free breadth

        // Const Tunables
        private const int _entryDepth = -1;

        #region Plan
        public static List<DialogueLinePosition> Plan(DialogueGraph graph, ISet<string> syncedLineIDs)
        {
            // Order matters here ~ duplicates take their breadths before newly reachable lines
            List<SpeechGraphNode> placedLines = GetLines(graph).Where(line => line.IsPlaced()).ToList();
            Dictionary<int, int> nextBreadth = GetNextFreeBreadths(placedLines);

            var positions = new List<DialogueLinePosition>();
            foreach (SpeechGraphNode line in GetDuplicatesToMove(placedLines, syncedLineIDs)) { positions.Add(Take(line, line.GetDepth(), nextBreadth)); }
            foreach ((SpeechGraphNode line, int depth) in GetNewlyReachableLines(graph)) { positions.Add(Take(line, depth, nextBreadth)); }
            return positions;
        }

        public static List<DialogueLinePosition> PlanRekey(DialogueGraph graph)
        {
            // Deliberate re-key:
            //  - A. reachable lines renumbered breadth-first from Entry
            //  - B. unreachable placed lines keep their depth and follow on
            // Returns only lines whose position changes

            List<(SpeechGraphNode line, int depth)> reachableLines = GetFirstReachDepths(graph);
            var reachable = new HashSet<SpeechGraphNode>(reachableLines.Select(reached => reached.line));
            IEnumerable<(SpeechGraphNode line, int depth)> unreachableLines = GetLines(graph).Where(line => line.IsPlaced() && !reachable.Contains(line))
                .OrderBy(line => line.GetDepth()).ThenBy(line => line.GetBreadth()).Select(line => (line, line.GetDepth()));

            var nextBreadth = new Dictionary<int, int>();
            var positions = new List<DialogueLinePosition>();
            foreach ((SpeechGraphNode line, int depth) in reachableLines.Concat(unreachableLines))
            {
                DialogueLinePosition position = Take(line, depth, nextBreadth);
                if (HasMoved(position)) { positions.Add(position); }
            }
            return positions;
        }
        #endregion

        #region Getters
        public static string GetID(INode node) => ((Node)node).ID.ToString();

        private static IEnumerable<SpeechGraphNode> GetLines(DialogueGraph graph) => graph.GetNodes().OfType<SpeechGraphNode>();

        private static bool HasMoved(DialogueLinePosition position) => position.line.GetDepth() != position.depth || position.line.GetBreadth() != position.breadth;

        private static EntryGraphNode GetEntry(DialogueGraph graph)
        {
            // Matches DialogueGraph's entry fix: the first connected entry wins
            return graph.GetNodes().OfType<EntryGraphNode>().FirstOrDefault(node => ((INode)node).IsConnected) ?? graph.GetNodes().OfType<EntryGraphNode>().FirstOrDefault();
        }

        private static Dictionary<int, int> GetNextFreeBreadths(IEnumerable<SpeechGraphNode> placedLines)
        {
            // One past the highest breadth in use
            var nextBreadth = new Dictionary<int, int>();
            foreach (SpeechGraphNode line in placedLines)
            {
                nextBreadth[line.GetDepth()] = Math.Max(nextBreadth.GetValueOrDefault(line.GetDepth()), line.GetBreadth() + 1);
            }
            return nextBreadth;
        }

        private static IEnumerable<SpeechGraphNode> GetDuplicatesToMove(IEnumerable<SpeechGraphNode> placedLines, ISet<string> syncedLineIDs)
        {
            // Keeper is a synced line if any (it owns the table entries), else the first
            foreach (IGrouping<(int, int), SpeechGraphNode> sharedPosition in placedLines.GroupBy(line => (line.GetDepth(), line.GetBreadth())).Where(group => group.Count() > 1))
            {
                SpeechGraphNode keeper = sharedPosition.FirstOrDefault(line => syncedLineIDs.Contains(GetID(line))) ?? sharedPosition.First();
                foreach (SpeechGraphNode line in sharedPosition.Where(line => line != keeper)) { yield return line; }
            }
        }

        private static List<SpeechGraphNode> GetLineTargets(INode source)
        {
            var lines = new List<SpeechGraphNode>();
            AppendLineTargets(source, lines, new HashSet<INode>());
            return lines;
        }
        #endregion

        #region PrivateUtility
        private static DialogueLinePosition Take(SpeechGraphNode line, int depth, Dictionary<int, int> nextBreadth)
        {
            int breadth = nextBreadth.GetValueOrDefault(depth);
            nextBreadth[depth] = breadth + 1;
            return new DialogueLinePosition(line, depth, breadth);
        }

        private static void AppendLineTargets(INode node, List<SpeechGraphNode> lines, HashSet<INode> visitedConditions)
        {
            // Condition nodes are see-through: their targets count as the source's
            var connectedPorts = new List<IPort>();
            foreach (IPort outputPort in node.GetOutputPorts())
            {
                connectedPorts.Clear();
                outputPort.GetConnectedPorts(connectedPorts);
                foreach (INode target in connectedPorts.Select(port => port.GetNode()))
                {
                    switch (target)
                    {
                        case SpeechGraphNode line when !lines.Contains(line):
                            lines.Add(line);
                            break;
                        case ConditionGraphNode condition when visitedConditions.Add(condition):
                            AppendLineTargets(condition, lines, visitedConditions);
                            break;
                    }
                }
            }
        }

        private static List<(SpeechGraphNode line, int depth)> GetFirstReachDepths(DialogueGraph graph)
        {
            // Plain breadth-first walk from Entry (wire order): each line's depth = its first parent's + 1 in order of first reach
            var reachOrder = new List<(SpeechGraphNode line, int depth)>();
            EntryGraphNode entry = GetEntry(graph);
            if (entry == null) { return reachOrder; }

            var depths = new Dictionary<INode, int> { { entry, _entryDepth } };
            var pending = new Queue<INode>();
            pending.Enqueue(entry);
            while (pending.Count > 0)
            {
                INode source = pending.Dequeue();
                foreach (SpeechGraphNode target in GetLineTargets(source).Where(target => !depths.ContainsKey(target)))
                {
                    depths[target] = depths[source] + 1;
                    reachOrder.Add((target, depths[target]));
                    pending.Enqueue(target);
                }
            }
            return reachOrder;
        }

        private static List<(SpeechGraphNode line, int depth)> GetNewlyReachableLines(DialogueGraph graph)
        {
            // Relaxed walk from Entry: a placed line keeps its depth && an unplaced one takes its shallowest parent + 1
            //  - order of first reach (breadth-first, wire order) decides which new line gets the lower breadth
            EntryGraphNode entry = GetEntry(graph);
            if (entry == null) { return new List<(SpeechGraphNode, int)>(); }

            var reachOrder = new List<SpeechGraphNode>();
            var depths = new Dictionary<INode, int> { { entry, _entryDepth } };
            var pending = new Queue<INode>();
            pending.Enqueue(entry);
            while (pending.Count > 0)
            {
                INode source = pending.Dequeue();
                foreach (SpeechGraphNode target in GetLineTargets(source))
                {
                    int depth = target.IsPlaced() ? target.GetDepth() : depths[source] + 1;
                    if (depths.TryGetValue(target, out int knownDepth) && knownDepth <= depth) { continue; }
                    if (!depths.ContainsKey(target)) { reachOrder.Add(target); }
                    depths[target] = depth;
                    pending.Enqueue(target);
                }
            }
            return reachOrder.Where(line => !line.IsPlaced()).Select(line => (line, depths[line])).ToList();
        }
        #endregion
    }
}
