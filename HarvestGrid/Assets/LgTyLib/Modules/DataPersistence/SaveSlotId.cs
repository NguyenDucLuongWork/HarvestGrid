using System;

namespace LgTyLib.Modules.DataPersistence
{
    [Serializable]
    public struct SaveSlotId
    {
        public string playthroughId;  // folder name
        public string slotName;       // file name

        public SaveSlotId(string playthroughId, string slotName)
        {
            this.playthroughId = playthroughId;
            this.slotName = slotName;
        }

        public override string ToString() => $"{playthroughId}/{slotName}";
    }
}
