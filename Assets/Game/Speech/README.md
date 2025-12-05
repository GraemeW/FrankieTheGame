# Assets:  Game - Speech

Frankie makes use of a custom dialogue system, and its dialogue trees are stored as scriptable objects in the sub-folders of this [Dialogue directory](./).

## Dialogue Hook-Up

In the normal flow to display dialogue, an AI Conversant (such as the standard [Conversant Component](./ConversantComponent.prefab) prefab that is childed to every [NPC](../CharacterObjects/NPCs/)) is used to load a [Dialogue](./) scriptable object, which contains nodes that are traversed via the [DialogueController](../Controllers/README.md#dialoguecontroller).

As an example, the dialogue created [below](#dialogue-creation--quick-start-guide), can be added to [LucyNPC](../CharacterObjects/PCs/Lucy/LucyNPC.prefab)'s ConversantComponent `Dialogue` parameter:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueHookUp.png" width="800">

In this example, the `New Dialogue` scriptable object will trigger whenever the player interacts with the `LucyNPC` character shown above.

## Dialogue Creation:  Quick-Start Guide

### Make the Dialogue Tree

1. Navigate to the relevant sub-directory within this [Speech](./) directory
   * *Note:  sub-directories are typically organized by Zone (scene)*
2. Right click and select `Create` -> `Dialogue` -> `New Dialogue`

<img src="../../../InfoTools/Documentation/Game/Speech/NewDialogueMenu.png" width="450">

### Dialogue Editor & Dialogue Node Configuration

Double click on the New Dialogue to open it in the Dialogue Editor.  A root dialogue node is already present, as below:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueRootNode.png" width="500">

Within the Dialogue Editor, set the root node's values:
* `Speaker`: to either
  * the name of any character found in [CharacterProperties](../OnLoadAssets/CharacterProperties/)
    * *if a valid name is provided, the CharacterProperties is auto-set in Inspector, as shown below*
  * or, otherwise, any arbitrary name
* `Default Text to Overwrite`: to the text the speaker will say
* `Ai Speaker` (dropdown): to
  * `Ai Speaker` - for NPCs
  * `Player Speaker` - for the player response
    * *by default, the lead character in the party's name will be used*
  * `Narrator Direction` - for text without a speaker

For example, see below configuration where the Speaker is set to Lucy, with associated inspector properties that automatically reflect the changes made in the editor.  As noted above, since [Lucy](../OnLoadAssets/CharacterProperties/CoreCharacters/Lucy.asset) exists as a [CharacterProperties](../OnLoadAssets/CharacterProperties/) file, this asset is automatically linked.

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueNodeConfig.png" width="600">

#### Building a Simple Conversation with New Dialogue Nodes

To continue the conversation, click on the `+` button on the dialogue node.  This generates a second node which, by default, is configured as a `Player Speaker` response.  Note that dialogue nodes can likewise be simply deleted by pressing the `-` button.

Continuing the above example:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueNodeAdd.png" width="600">

Notice that the colours of the nodes changed to reflect the different speakers.  

By clicking on the `+` symbol on the newly generated node, we can add a third node, and then configure it with a third speaker (e.g. adding Tilly to the conversation):

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueMultiConversant.png" width="700">

, and continue the conversation by adding nodes, configuring their speaker/text, and moving them around as-needed for readability:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueMultiConversant2.png" width="800">

#### Branched Dialogue Nodes

To add branched dialogue options, click the `+` button off of the desired node multiple times.  

For example, see below branching on Tilly's response and the player's subsequent response:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueBranching.png" width="800">

It is important to note that the game behaviour is different for `Ai Speakers` (NPCs) vs. `Player Speakers`.  For `Ai Speakers`, if a [condition](#conditional-dialogue-nodes) is not provided to down-select from the available nodes, the response will be chosen at random from the viable options.  For `PlayerSpeakers`, the player will be given an option menu to select their response.  The latter behaviour is shown below:

<img src="../../../InfoTools/Documentation/Game/Speech/DialoguePlayerChoice.png" width="600">

#### Conditional Dialogue Nodes

In some cases, access to certain Dialogue Nodes needs to be gated by conditions (e.g. story progression, party members present, items in inventory, etc.).  This can be accomplished using [Predicates](../Predicates/), by setting the `Additional Properties - Condition` setting on the Dialogue Node in the Inspector.

Continuing the above example, we can alter the player's response to Lucy's question as a function of a [HasGiantTurkeyLeg](../Predicates/Knapsack/HasGiantTurkeyLeg.asset) predicate (which checks the party knapsacks for the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset)).  By setting the other node as a negation to the above predicate, we've effectively established a split in the dialogue tree as a function of the player's inventory state.  Or, specifically:
* if the player **does not** have the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset), Frankie proposes to get burgers
* if the player **does** have the [GiantTurkeyLeg](../OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset), Frankie instead recommends to nosh that instead

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueNodeConditions.png" width="800">

#### Linking and Unlinking Nodes

Consider the case where a split in the dialogue tree needs to converge to a single node.  For example, what if the player has the option of recommending several food options, but we want them to all elicit the same response (i.e. converge to a single node).  This can be done by:
* selecting `link` on the split node
* selecting `child` on the convergent node

In the example above, Frankie can make a recommendation of burgers, pizza or tacos, and all three should have a chance for Tilly to respond "That sounds great!"  To start, add the new option nodes, and click `link` on the pizza option:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueNodeLinks.png" width="700">

The tacos option should elicit the same responses as the burger option, so we hook that up to both of Tilly's possible responses.  The end result looks as below:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueNodeLinks2.png" width="700">

#### Skipping Root Node

As detailed [above](#dialogue-editor--dialogue-node-configuration), a new dialogue is always instantiated with a root dialogue node.  This is necessary because the [DialogueController](../Controllers/DialogueController.prefab) needs an entry point to begin traversing the dialogue tree.

However, if the first dialogue node in a dialogue tree needs to be split/conditional, this can cause some issues -- how would we know which node to start on?  In order to address this issue, the Dialogue scriptable object has a parameter `Skip Root Node`, which effectively bypasses the first node, but otherwise traverses the dialogue tree as normal.

For example, this parameter is set on the [HipsterWorker](./OfficeInterior/HipsterWorker.asset) dialogue:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueSkipRoot.png" width="300">

, with corresponding dialogue tree as below:

<img src="../../../InfoTools/Documentation/Game/Speech/DialogueSkipRoot2.png" width="700">

, noting that the root node text is irrelevant because it is skipped.
