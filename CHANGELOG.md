# v1.4.1

- New: `Alignment` config option (`Left`, `Center`, `Right`) shifts the inventory slot row horizontally on screen
- New: `Alignment Offset` config (0–800 UI units, default 350) controls how far left/right shifts go

# v1.4.0

- Reverted: all in-mod network features removed because they were breaking vanilla matchmaking and the server browser (Pick Best Server hanging, lobby creation failures, region selection hanging)
- Mod is back to pure inventory functionality; vanilla Steam Lobby + Photon matchmaking is untouched
- MenuLib dependency removed; no extra UI buttons
- For mod sync with friends, install a dedicated mod-sync mod alongside (or just make sure everyone has the same mods)
- All inventory fixes from v1.1.1 retained (SlotRegistry, GetSpotByIndex prefix, RPC_UpdateItemState prefix)

# v1.3.0

- New main-menu button "Join By Code": enter the host's 6-character code to join directly
- New main-menu button "Modded Servers": browse all REPO lobbies that have mods loaded
- New lobby-menu button "Show Join Code": display the join code your friends should enter
- Server browser shows player count, mod count and a compatibility marker (✓ matches your hash, ! differs)
- All menus are built with MenuLib for consistent visuals (new dependency: nickklmao-MenuLib 2.5.4)

# v1.2.0

- New: decentralized mod-sync layer built on top of Photon and Steam Lobby
- Host now validates every joining client's mod list and version
- Incompatible clients are kicked with a popup listing missing or mismatched mods
- Steam Lobby is enriched with `mod_count`, `mod_hash`, `mod_present`, `protocol`, `join_code`
- Per-lobby short join codes (Crockford-style 6-character Base32)
- Configurable in `BepInEx/config/kazhime.repokastimmod.cfg` under the new `Network` and `Controls` sections
- No external server required; everything piggybacks on Steam Matchmaking and Photon

# v1.1.1

- Major multiplayer fix: extra slots are now re-bound every time the player respawns
- Persistent SlotRegistry survives Inventory recreation between levels and rounds
- Lazy registration via GetSpotByIndex prefix prevents equip RPCs from missing slot 4+
- RPC_UpdateItemState prefix forces slot creation on the equipping client before vanilla code resolves the slot

# v1.1.0

- Fixed extra inventory slots not registering in multiplayer after level load
- Removed duplicate PhotonView from cloned slots to prevent network conflicts
- Ensures inventory list is expanded before equip RPCs resolve slots
- Removed stamina changes
- Host protection now only enforced by the master client

# v1.0.0

- Extra inventory slots (3-10) for current R.E.P.O. versions
- Host protection and multiplayer item tracking
- Auto item swap for occupied slots
- Battery indicator support for extra slots
- Reduced sprint stamina drain (configurable, default 35% less)
- Singleplayer and multiplayer support
