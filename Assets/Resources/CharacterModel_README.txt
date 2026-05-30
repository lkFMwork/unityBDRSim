DROP-IN CHARACTER MODEL
=======================

The game ships with a procedurally-rigged, animated humanoid — no imports
needed. To use a real imported character instead:

1. Import a rigged HUMANOID character (e.g. Synty / Quaternius + Mixamo
   animations). Set its rig to 'Humanoid' in the model import settings.
2. Give it an Animator Controller with a FLOAT parameter named 'Speed'
   driving an idle<->walk blend tree (0 = idle, 1 = walking).
3. Save/rename that prefab here as:  Assets/Resources/CharacterModel.prefab

Every character in the game (creator, city/office NPCs, mentors, fighters)
will then use it automatically, driven by movement speed. Delete the prefab
to return to the built-in procedural rig.
