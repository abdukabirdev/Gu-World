# Models: where they come from

| File | What | Source and licence |
|---|---|---|
| `gu-hero.glb` | The hero, full detail (626 000 triangles): high quality | Generated with TRELLIS from the author's concept art, rigged in the Gu Rig editor (209 bones) |
| `gu-hero-game.glb` | The hero, 70 000 triangles: ordinary quality | Made from `gu-hero.glb` by `Tools/V2/models/hero.py` |
| `gu-npc.glb` | People of the valley, 18 000 triangles; dyed by `Resources/Looks/*.jpg` | Made from `gu-hero.glb` by `Tools/V2/models/hero.py` |
| `canid.glb` | Foxes and wolves; dyed by `Resources/Looks/canid_*.png` | "Fox" from Khronos glTF Sample Assets. Model: PixelMannen, CC0 1.0. Rigging and animation: tomkranis, CC BY 4.0. glTF conversion: AsoboStudio and scurest, CC BY 4.0 |

Animations (`Resources/Anim/*.bytes`): the humans' are authored in code (`Tools/V2/models/anim.py`);
the canids' are the Fox's own Survey, Walk and Run, baked by `Tools/V2/models/canid.py`.

The CC BY 4.0 parts must be credited wherever the game is shared (they are in the game's credits
on the title screen).
