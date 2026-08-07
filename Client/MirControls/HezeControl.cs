using System.Drawing;
using C = ClientPackets;

namespace Client.MirControls
{
    /// <summary>Small stateful HUD model for the Heze energy bar and partner information.</summary>
    public sealed class HezeControl
    {
        public string PartnerName { get; private set; } = string.Empty;
        public string SkillName { get; private set; } = string.Empty;
        public int Energy { get; private set; }
        public int Maximum { get; private set; } = 100;
        public byte Level { get; private set; }
        public bool Active { get { return !string.IsNullOrEmpty(PartnerName); } }
        public float EnergyPercent { get { return Maximum == 0 ? 0 : (float)Energy / Maximum; } }
        public void Update(ServerPackets.G_HezeEnergy p) { Energy=p.Energy; Maximum=p.Maximum; PartnerName=p.PartnerName; Level=p.Level; }
        public void Accepted(ServerPackets.G_HezeAccepted p) { PartnerName=p.PartnerName; SkillName=p.SkillName; Level=p.Level; }
        public void Broken() { PartnerName=SkillName=string.Empty; Energy=0; }
        public void Accept(uint requesterID) { Client.MirNetwork.Network.Enqueue(new C.C_HezeAccept { RequesterID = requesterID }); }
        public void Decline(uint requesterID) { Client.MirNetwork.Network.Enqueue(new C.C_HezeDecline { RequesterID = requesterID }); }
        public void Break() { Client.MirNetwork.Network.Enqueue(new C.C_HezeBreak()); }
    }
}
