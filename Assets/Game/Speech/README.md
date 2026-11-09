# Assets:  Game - Speech

Frankie makes use of a custom dialogue system. Dialogues are authored as graphs (`.dialoguegraph` files) in the sub-folders of this [Dialogue directory](./). Each graph is imported as a runtime `Dialogue` asset w/ one sub-asset per dialogue "line".

## Dialogue Hook-Up

For standard conversation, an AI Conversant (e.g. via the [Conversant Component](./ConversantComponent.prefab) prefab, which is childed to every [NPC](../CharacterObjects/NPCs/)) is given a dialogue graph, whose lines are traversed w/ the [DialogueController](../Controllers/README.md#dialoguecontroller).

As an example, the dialogue created [below](#dialogue-creation--quick-start-guide) can be added to [LucyNPC](../CharacterObjects/PCs/Lucy/LucyNPC.prefab)'s ConversantComponent `Dialogue` parameter by dragging the `.dialoguegraph` file onto it directly:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueHookUp.png" width="800">

In this example, the `New Dialogue Graph` will play whenever the player interacts with the `LucyNPC` character shown above.

## Dialogue Creation:  Quick-Start Guide

### Make the Dialogue Graph

1. Navigate to the relevant sub-directory within this [Speech](./) directory
   * *Note:  sub-directories are typically organized by Zone (scene)*
2. Right click and select `Create` -> `Dialogue` -> `New Dialogue Graph`
3. Name the dialogue appropriately (see [Naming & Localization](#naming--localization)) - N.B. the name becomes the prefix of every line's localization key

<img src="../../../InfoTools/Documentation/Game/Speech/NewDialogueGraphMenu.png" width="400">

### The Graph Editor

Double click the `.dialoguegraph` to open it in the graph editor. A new graph contains a single `Entry` node: the dialogue starts at whatever `Entry` is wired to.

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueGraphNew.png" width="450">

Nodes are added from the node library (right-click the canvas), under the `Dialogue` category:
* `Line` - a line of dialogue (one speaker, one piece of text)
* `Condition` - a gate that flows when its predicates pass (see [Conditional Dialogue](#conditional-dialogue))
* `Entry` - the start of the dialogue (exactly one ~ extras are removed)

Nodes are connected by dragging from a node's `Out` port to another node's `In` port.

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueGraphNodeLibrary.png" width="400">

### Line Configuration

Each `Line` node has the following settings:
* `Speaker Type` (dropdown):
  * `AI Speaker` - for NPCs
  * `Player Speaker` - for the player response *(the party leader's name is shown as the speaker)*
  * `Narrator Direction` - for text without a speaker
* `Character`: for `AI Speaker` lines, optional [CharacterProperties](../OnLoadAssets/CharacterProperties/) of the speaker (if a named character)
  * *the character's display name is shown as the speaker (including any name the player gave them during setup, if applicable)*
* `Speaker`: the fallback speaker name, used when there is no `Character` (e.g. for arbitrary one-off speakers)
* `Text`: what the speaker says

The node's title shows the speaker, its header colour reflects the speaker (player, narrator, and a colour per AI character), and its subtitle shows the line's position (`depth.breadth`, see [Naming & Localization](#naming--localization)).

For example, see below a line spoken by [Lucy](../OnLoadAssets/CharacterProperties/CoreCharacters/Lucy.asset), wired from `Entry`:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueLineConfig.png" width="500">

The graph flags common mistakes with a warning badge on the offending node, e.g.:
* an `Entry` that doesn't lead to any line
* a line not reachable from `Entry`
* a line with no text
* an `AI Speaker` line with neither a `Character` nor a `Speaker`
* a condition with no predicates, or an empty predicate block

#### Building a Conversation

To continue the conversation, add another `Line` and wire the previous line's `Out` port to its `In` port. Keep chaining lines, configuring their speaker/text, and moving them around as-needed for readability:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueConversation.png" width="700">

#### Branched Dialogue and Converging Branches

To branch, wire one line's `Out` port to several lines. The order of the wires is the order of the options.

It is important to note that the game behaviour is different for `AI Speakers` (NPCs) vs. `Player Speakers`:
* `Player Speakers`: if a line leads to several player lines, the player is given an option menu to select their response
* `AI Speakers`: if a line leads to several AI lines, the response is chosen at random from the available ones (use [conditions](#conditional-dialogue) to down-select instead)
* the same applies to `Entry`: if it leads to several lines, one is chosen at random

Branches can re-join by simply wiring several lines into the same line: a line's `In` port accepts any number of wires.

For example, see below branching on Tilly's response and the player's subsequent response:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueBranching.png" width="700">

, with the player's choice shown in game as below:

<img src="../../../InfoTools/Documentation/Game/Speech/DialoguePlayerChoice.png" width="500">

#### Conditional Dialogue

In some cases, access to certain lines needs to be gated by conditions (e.g. story progression, party members present, items in inventory, etc.). This is done with a `Condition` node placed in front of the gated line(s): the flow passes through it only while its condition holds.

A `Condition` node holds one or more `Predicate` blocks, each referencing a [Predicate](../Predicates/) asset (with an optional `Negate`):
* all blocks of one condition must pass (AND), shown as "if all of N" on the node
* for an OR, wire two `Condition` nodes in parallel in front of the same line
* condition nodes don't count as lines: they have no text, and don't affect line positions

Continuing the above example, we can alter the player's response to Lucy's question as a function of a [HasGiantTurkeyLeg](../Predicates/Knapsack/HasGiantTurkeyLeg.asset) predicate (which checks the party knapsacks for the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset)), with one condition using the predicate and another using its negation:
* if the player **does not** have the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset), Frankie proposes to get either burgers or tacos
* if the player **does** have the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset), Frankie instead recommends to nosh that instead

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueConditions.png" width="800">

#### Conditional Openers

Conditions can sit directly after `Entry`, so a dialogue can open differently depending on game state (e.g. a first-visit vs. a repeat-visit greeting). For example, see the [HipsterWorker](./OfficeInterior/OfficeInterior-HipsterWorker.dialoguegraph) dialogue:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueConditionalOpener.png" width="600">

## Dialogue Triggers

A [DialogueTrigger](../../Scripts/Speech/DialogueTrigger.cs) component fires a Unity Event on a dialogue update: when a dialogue starts or completes, or when a specific line is entered or exited (`Dialogue Update Type`). It must sit on the same GameObject as the AI Conversant whose dialogue that it reacts to.

For line-based triggers, set `Trigger Node` to the line: either drag the line's sub-asset in (expand the `.dialoguegraph` in the Project window), or use the `Pick` button, which lists the lines of the current dialogue first (the one the picked line belongs to, else the AI Conversant's on the same object), then every dialogue's lines (by `depth.breadth`, speaker and text).

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueTriggerPick.png" width="400">

Line references stay bound while the graph is edited (lines keep their identity when moved, re-wired, renamed or re-keyed), but are lost if the line itself is deleted.

## Naming & Localization

Line text is stored in the `Speech` localization table, and kept in sync with the graph automatically while it's open:
* every line is keyed `{dialogueName}.{depth}.{breadth}.Speaker` / `.Text`
  * a line gets its position once it's reachable from `Entry` (depth counts lines from `Entry`, starting at 0)
    * N.B. it then keeps this position detail so that translations stay attached
  * hover a line to see its key
* on edits:
  * edits in the graph are written to the English entries straight away (and undo with the graph edit)
  * edits made directly in the table are pulled back into the graph
* deleting a line removes its entries (all languages) ~ undo brings them back
* `Re-key Lines` (graph toolbar) renumbers every line breadth-first from `Entry`
  * e.g. after heavy restructuring - translations move with their lines, but any external references to the old keys break

Since keys start with the dialogue's name:
* **renaming** a `.dialoguegraph` renames its keys too (all languages)
* a rename to a name that another dialogue already uses is refused, as their keys would collide
* **deleting** a `.dialoguegraph` removes all its entries from the table

## Duplicating Dialogue

In some circumstances it can be useful to duplicate an entire dialogue (e.g. for instances where only minor modifications may be needed to a dialogue -- such as with shop attendants). Duplicate the `.dialoguegraph` (cmd + D), rename the copy, then **open it once in the graph editor**: that writes its lines to the table under the copy's own name (until then, its lines show placeholder text in game). Then update the relevant line text as needed.

Line triggers pointing at the original dialogue keep pointing at the original (re-pick them on the copy where needed).
