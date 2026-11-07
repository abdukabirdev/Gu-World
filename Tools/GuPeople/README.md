# GU World's people

`make_people.py` makes the game's people in Blender from MakeHuman's base mesh (MPFB 2,
CC0 assets) and writes them to `Assets/GuWorld/Resources/People`:

| Kit | Who |
|---|---|
| `male_teen`, `female_teen` | fifteen: the hero, the students, the children |
| `male_adult`, `female_adult` | the grown-ups and the old |

Each kit is one glTF (`<kit>/<kit>.gltf` + `.bin`) with the textures shared in `textures/`.
All of its meshes are skinned to one skeleton, MakeHuman's `game_engine` rig (53 bones:
`pelvis`, `spine_01..03`, `neck_01`, `head`, `clavicle`, `upperarm`, `lowerarm`, `hand`,
fingers, `thigh`, `calf`, `foot`, `ball`). Everything worn is a separate mesh:

- `Body`, `Eyes`, `Eyebrows`, `Eyelashes`;
- `Hair_Short`, `Hair_Parted`, `Hair_Buns` (a topknot with a pin), `Hair_Bob`, `Hair_Ponytail`, `Hair_Long`;
- the men's `Beard_Stubble`, `Beard_Mustache`, `Beard_Goatee`, `Beard_Full`;
- `Cloth_Under`, `Cloth_Trousers`, `Cloth_Wraps`, `Cloth_Shoes`, `Cloth_Belt`;
- `Cloth_Robe` (the long hanfu) or `Cloth_Tunic` (to the thigh or the knee). Both have a crossed
  collar, left over right, with materials `Robe`, `Inner` and `Trim`;
- `Cloth_Mark`: the Gu Yue's crescent on the back.

The game (`Scripts/Body/HumanBody.cs`) keeps the pieces a look wears and takes off the rest.
It dyes the cloth by material name with the build's dye (`Scripts/Body/Dyes.cs`) and colours
the hair and beards with the hair colour.

## How the clothes are made

The clothes are cut from MakeHuman's own helper geometry, so their bone weights come with them:

- `helper-tights` (a suit over the whole body) gives the trousers, wraps, shoes, the robe's
  top and the sash;
- `helper-skirt` gives the robe's skirt, cut at the hem.

The sleeves are widened from the elbow to the wrist. The skirt is pushed out over the
trousers. The collar's bands are laid on the robe's surface.

The beards are cut from the face of the skin and lifted off it.

## Making them again

1. Install MPFB 2 into Blender, from extensions.blender.org.
2. Unzip MakeHuman's system assets (`makehuman_system_assets_cc0.zip`) into MPFB's user data
   folder, `…/extensions/.user/user_default/mpfb/data`.
3. Unzip MPFB's own zip somewhere with a short path. This is the `<mpfb_data>` argument: on
   Windows, MPFB's longest target names are too long under the Microsoft Store's package folder.
4. Run:

```
blender-launcher.exe --background --python make_people.py -- <out_dir> <log> <mpfb_data>/data [<blend_dir>]
```

To make only some of the kits, set `GU_PEOPLE_ONLY=male_teen,female_teen`.

The `.blend` files go to `<blend_dir>`. We keep them in `GLB/people`, which is not in git.
