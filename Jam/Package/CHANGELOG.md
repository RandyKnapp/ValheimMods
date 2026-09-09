**2.0.0**

* Deep North update!
* **Breaking: all config entries are renamed and existing settings reset to their defaults.**
* **Breaking: the recipe format changed** from `Item:Amount,Item:Amount` to
  `Item,Amount,AmountPerLevel|Item,Amount,AmountPerLevel` (e.g. `Raspberry,14,0`).
* The crafting station, minimum station level and craft amount are now configurable per jam - they
  were hardcoded to cauldron / their shipped level / 4.
* Config changes apply live
* Recipes are now re-applied when the ObjectDB is rebuilt, fixing jam recipes reverting to their
  defaults after joining a server whose config differs.
* Removed the long-unused `config/recipes.json`.

**1.1.0**

* Existing release; changelog begins here.
