# Assets

## Game Folder Structure

Assets are broken down into 3+1 categories:

* **1:**  [Scripts](./Scripts/):  Game logic
* **2:**  [Scenes](./Scenes/):  World composition
* **3:**  [Game](./Game/):  Game objects (Unity .assets)
* **+1:** [Unity Configurables](./UnityConfigurables/):  Render pipeline configuration, addressables assets data

## Game Objects && Physical Assets

Game objects denoted above are the Unity .asset files, which are prefabs that contain one or more MonoBehaviours.  These assets often make reference to physical artwork, music, etc., which are stored in hidden folders:
* z_MadeAssets -- Assets made by yours truly (or friends)
* z_ImportedAssets -- 3rd party assets that have been licensed

As noted above, these folders are hidden / not pushed to GIT because a) they would take up too much space, and b) to avoid the assets being misused.  If you have a legitimate need for any of the physical assets, please contact me directly.
