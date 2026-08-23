# Predicates

Data-driven boolean conditions for gating gameplay logic (dialogue options, quest triggers, interaction availability, etc.) without hardcoding the logic in code.  A `Condition` is authored as data (mostly via the Inspector) and evaluated at runtime against whatever `IPredicateEvaluator`s are available in that context.

## The three pieces

- **`Predicate`** - an abstract `ScriptableObject`. Concrete predicate types are project-specific subclasses authored elsewhere (e.g. a `HasItemPredicate`, `FlagSetPredicate`). This base class is deliberately untyped - it does not carry a generic evaluation method - to avoid generic type parameters leaking into every piece of code that touches a `Predicate` (`Condition`,`IPredicateEvaluator`, the Inspector drawers, etc.). Type-specific dispatch happens inside `IPredicateEvaluator` implementations instead.

- **`IPredicateEvaluator`** - the "answerer." A single method:
  ```csharp
  bool? Evaluate(Predicate predicate);
  ```
  Returns `true`, `false`, or `null`. **`null` means "I don't know how to evaluate this predicate,"** not "false." An evaluator implementation typically checks whether the given `Predicate` is a type it understands and returns `null` for anything else, so several small, focused evaluators can be combined into one list and each only "opts in" on the predicate types it recognizes.

- **`Condition` / `Disjunction` / `PredicateWrapper`** - the structure that combines predicates into a single boolean expression.

## Conjunctive normal form (CNF)

A `Condition` is a boolean expression in CNF: an **AND of ORs**.

```
Condition   = Disjunction₁ AND Disjunction₂ AND ... AND Disjunctionₙ
Disjunction = Literal₁ OR Literal₂ OR ... OR Literalₙ
Literal     = a Predicate, optionally negated  (this is PredicateWrapper)
```

Concretely:
- `Condition` holds a list of `Disjunction`s (the AND).
- `Disjunction` holds a list of `PredicateWrapper`s (the OR).
- `PredicateWrapper` holds one `Predicate` plus a `negate` flag - this is the literal itself, i.e. "this predicate" or "NOT this predicate."

CNF was picked because it's expressive enough for practical gating logic (any number of alternative sub-conditions, all of which must hold) while staying a flat, two-level structure that's easy to author and serialize in the Inspector - no arbitrary nested boolean trees to build a custom drawer for.

### Building one in code

```csharp
var hasKeyOrGuardAsleep = new Condition.Disjunction();
hasKeyOrGuardAsleep.AddPredicateWrapper(new Condition.PredicateWrapper(hasKeyPredicate));
hasKeyOrGuardAsleep.AddPredicateWrapper(new Condition.PredicateWrapper(guardAwakePredicate, negate: true));

var doorNotLocked = new Condition.Disjunction();
doorNotLocked.AddPredicateWrapper(new Condition.PredicateWrapper(doorLockedPredicate, negate: true));

var canEnterRoom = new Condition();
canEnterRoom.AddDisjunction(hasKeyOrGuardAsleep);
canEnterRoom.AddDisjunction(doorNotLocked);

bool canEnter = canEnterRoom.Check(evaluators);
```

This reads as: `(hasKey OR NOT guardAwake) AND (NOT doorLocked)`.

## Evaluating a single literal

`PredicateWrapper.Check(evaluators)`:
- A wrapper with no `Predicate` assigned always passes - useful for an unset/placeholder entry, it just doesn't constrain the condition.
- Otherwise, every evaluator in the list is asked via `Evaluate(predicate)`, and the literal passes only if **none of them contradict it**.

| Evaluator result | `negate = false` | `negate = true` |
|------------------|------------------|-----------------|
| `true`           | Pass             | Fail            |
| `false`          | Fail             | Pass            |
| `null` (abstain) | Pass             | Pass            |

A `null` result never contradicts anything, in either direction - abstaining is always neutral, never a veto.

## Evaluating a literal with multiple evaluators

The list of evaluators is checked against a single predicate together, and the literal passes only if every evaluator's answer is a non-contradiction.  In practice this means: **any one evaluator giving a result equal to `negate` fails the whole literal**, no matter what the others say. Abstentions (`null`) never affect the outcome.

| Evaluator A | Evaluator B | Result (`negate = false`) |
|-------------|-------------|---------------------------|
| `true`      | `true`      | Pass                      |
| `true`      | `null`      | Pass                      |
| `null`      | `null`      | Pass                      |
| `null`      | `false`     | Fail                      |
| `true`      | `false`     | Fail                      |
| `false`     | `false`     | Fail                      |

From the above, it should be clear that an evaluator should never over-reach in its evaluation knowledge, and should only answer the exact/specific question being answered.  

For example, consider answering the question:  "Is the Mage Character alive?," where the Mage's Health component is an IPredicateEvaluator that can respond to this question.  In this example, a Knight Character is also present, also with a Health component, and so its IPredicateEvaluator also provides a response.  The Mage Character is alive, so it responds:  `true`.  The Knight Character is NOT the Mage Character, and knows nothing about the Mage Character, and so it responds:  `null`.

Critically, and as a common point of error, the Knight Character does NOT respond `false` (which may seem intuitive here, but is a common source of error for more complex cases).

## Two things to watch for when calling `Check`

**Pass a materialized collection, not a lazy `IEnumerable`.** 

The same `evaluators` sequence is threaded unchanged through `Condition` → `Disjunction` → `PredicateWrapper`, and `PredicateWrapper.Check` fully re-enumerates it for every single literal. For a `Condition` with several disjunctions/wrappers, that can mean walking `evaluators` many times in one `Check()` call.

**Not every evaluator is guaranteed to run.** 

`.All()`/`.Any()` short-circuit: once a disjunction fails, later disjunctions aren't checked; once a wrapper passes, later wrappers in that disjunction aren't checked; once one evaluator contradicts a literal, later evaluators for that literal aren't asked. `IPredicateEvaluator.Evaluate()` implementations should be pure and side-effect-free.

## Vacuous cases - a deliberate asymmetry to watch for

- An empty `Condition` (no `Disjunction`s) evaluates to **`true`** - AND over nothing is vacuously true.
- An empty `Disjunction` (no `PredicateWrapper`s) evaluates to **`false`** - OR over nothing is vacuously false.

This asymmetry matters if you're building a `Condition` programmatically:  Adding an empty `Disjunction` to a `Condition` and never populating it will silently make the *entire* `Condition` always fail, since AND requires every disjunction (including the empty one) to pass.

## Tests

`Tests/Editor/ConditionTests.cs` exercises all of the above (empty condition/disjunction behavior, negation, abstain-vs-veto, multi-evaluator aggregation, and OR/AND composition) against a `TestPredicate` stand-in and a couple of small test-double evaluators.
