# GU hero model

`Assets/GuWorld/Resources/Models/gu-hero.glb` is the hero's own model: the long-haired Gu Yue
youth in a dark hanfu from the concept art. TRELLIS generated it with its texture. It was then
rigged in the Gu Rig editor artifact:

- 209 bones;
- hands with fingers;
- the portrait laid over the face.

The game wears it over the Mannequin's joints (`Scripts/Body/GuHeroBody.cs`), as it wears the
made bodies, so every move the Mannequin makes moves it too: walking, running, crouching,
climbing, sitting, blows and rolls.

## Making it again

Export the rigged GLB from the editor ("Экспорт GLB"), then:

```
python prepare_glb.py gu_hero_rigged.glb ../../Assets/GuWorld/Resources/Models/gu-hero.glb
```

The script:

- names the meshes in English (`GuHero_Face`, `GuHero_Body`, `GuHero_Hands`);
- sets the face portrait to alpha BLEND;
- drops the editor's animation clips, since the game poses the body itself.

The importer's `animationMethod` is 0, so no clips are imported.

## Switching it off

- `GuWorldGame.GuHeroModel` in the inspector, or the `-madebody` command-line flag, puts the
  hero back on the made body.
- A female look always uses the made body.
- The smoke test checks the model first: upright, feet on the ground, left hand on the left.
  It then turns the model off for its checks of the made body's clothes.

## Bones it moves

| Bone | Moved by |
|---|---|
| `hips`, `spine` … `head`, `shoulder`/`upperArm`/`forearm`/`hand`, `thigh`/`shin`/`foot` | the Mannequin's joints |
| finger bones | the Mannequin's grip |
| twist and helper bones (`upperArmTwist`, `elbowHelper`, `forearmTwist1/2`, `hipHelper`, `thighTwist`, `kneeHelper`, `shinTwist`) | a share of their neighbour's turn, as in the editor |

The face, hair, sleeve and skirt bones keep their rest pose.
