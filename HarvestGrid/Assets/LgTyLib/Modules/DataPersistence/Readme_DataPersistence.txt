═══════════════════════════════════════════════
  LgTyLib · DataPersistence Module
═══════════════════════════════════════════════

SETUP (per new project)
───────────────────────
1. Copy the module folder into your project.
2. Edit GameData.Game.cs — remove old fields, add your game's fields.
   Never touch GameData.Core.cs.

   // GameData.Game.cs
   public partial class GameData
   {
       public int level;
       public float score;
       public bool bossDefeated;
       // ...
   }

3. Implement IDataPersistence on any MonoBehaviour that needs save/load:

   public class PlayerStats : MonoBehaviour, IDataPersistence
   {
       public void LoadGame(GameData data) => score = data.score;
       public void SaveGame(ref GameData data) => data.score = score;
   }


QUICK REFERENCE
───────────────
var slot = new SaveSlotId("Playthrough_A", "slot_1");

// New game (resets in-memory state, does not touch disk)
DataPersistenceManager.Instance.NewGame();

// Save / Load
DataPersistenceManager.Instance.SaveGame(slot);   // saves + remembers slot
DataPersistenceManager.Instance.LoadGame(slot);   // loads from disk

// Save to active slot again (slot remembered from last SaveGame/LoadGame)
DataPersistenceManager.Instance.SaveGame();

// Browse saves
string[] playthroughs = DataPersistenceManager.Instance.GetAllPlaythroughs();
string[] slots        = DataPersistenceManager.Instance.GetAllSlots("Playthrough_A");
bool     exists       = DataPersistenceManager.Instance.SaveExists(slot);
SaveSlotId? active    = DataPersistenceManager.Instance.ActiveSlot;

// Delete
DataPersistenceManager.Instance.DeleteSave(slot);
DataPersistenceManager.Instance.DeletePlaythrough("Playthrough_A");


EVENTS
──────
DataPersistenceManager.OnNewGame               += () => { ... };
DataPersistenceManager.OnGameLoaded            += slot => { ... };
DataPersistenceManager.OnGameSaved             += slot => { ... };
DataPersistenceManager.OnSaveDeleted           += slot => { ... };
DataPersistenceManager.OnPlaythroughDeleted    += id   => { ... };


FILE LAYOUT ON DISK
───────────────────
<persistentDataPath>/
  Saves/
    Playthrough_A/
      slot_1.json
      slot_2.json
    Playthrough_B/
      slot_1.json


PORTING CHECKLIST
─────────────────
  [ ] Replace fields in GameData.Game.cs
  [ ] Update IDataPersistence implementors to match new fields
  [ ] Set savesFolderName in the Inspector if needed (default: "Saves")