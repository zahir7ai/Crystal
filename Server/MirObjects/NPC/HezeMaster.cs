using Server.MirDatabase;

namespace Server.MirObjects.NPC
{
    /// <summary>NPC service used by a Heze Master script. Learning is free and only needs to occur once.</summary>
    public sealed class HezeMaster
    {
        public bool Learn(PlayerObject player, out string message)
        {
            if (player == null || !Settings.HezeEnabled) { message = "Heze is unavailable."; return false; }
            if (player.Level < Settings.HezeLevelRequirements[0]) { message = "Heze requires level " + Settings.HezeLevelRequirements[0] + "."; return false; }
            if (player.Info.HezeLearned) { message = "You have already learned Heze."; return false; }
            player.Info.HezeLearned = true;
            message = "You have learned the Heze combo discipline.";
            return true;
        }
        public string Information(PlayerObject player)
        {
            return "Heze links compatible classes within " + Settings.HezeRange + " tiles. Both partners attacking one target build energy; at full energy their next shared target releases a combo.";
        }
    }
}
