using System.Drawing;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>Server-authoritative state for a two-player Heze (Combo) bond.</summary>
    public sealed class HezeObject
    {
        private readonly PlayerObject owner;
        private uint lastTarget;
        private long lastHitTime;
        public int Energy { get; private set; }
        public DateTime Expires { get; private set; }
        public int Level { get { return Settings.GetHezeLevel(owner.Level); } }
        public bool Active { get { return owner.HezePartner != null && Envir.Now < Expires; } }

        public HezeObject(PlayerObject owner) { this.owner = owner; }

        public bool CanPair(PlayerObject other)
        {
            return Settings.HezeEnabled && other != null && other != owner && !owner.Dead && !other.Dead &&
                   owner.CurrentMap == other.CurrentMap && owner.HezePartner == null && other.HezePartner == null &&
                   Settings.GetHezeSkill(owner.Class, other.Class) != null &&
                   Functions.MaxDistance(owner.CurrentLocation, other.CurrentLocation) <= Settings.HezeRange;
        }

        public void Start(PlayerObject partner)
        {
            owner.HezePartner = partner;
            Energy = 0;
            Expires = Envir.Now.AddMinutes(Settings.HezeDuration);
            SendEnergy();
        }

        public void Break(string reason)
        {
            PlayerObject partner = owner.HezePartner;
            owner.HezePartner = null;
            Energy = 0;
            if (partner != null && partner.HezePartner == owner)
            {
                partner.HezePartner = null;
                partner.Heze.Energy = 0;
                partner.Enqueue(new S.G_HezeBroken { Reason = reason });
            }
            owner.Enqueue(new S.G_HezeBroken { Reason = reason });
        }

        public void Process()
        {
            if (owner.HezePartner == null) return;
            if (!Active || owner.HezePartner.Dead || owner.CurrentMap != owner.HezePartner.CurrentMap ||
                Functions.MaxDistance(owner.CurrentLocation, owner.HezePartner.CurrentLocation) > Settings.HezeRange)
                Break("The Heze bond has ended.");
        }

        public void RegisterHit(MapObject target, int baseDamage)
        {
            if (!Active || target == null || target.Dead || baseDamage <= 0) return;
            lastTarget = target.ObjectID;
            lastHitTime = Envir.Time;
            if (Energy < Settings.HezeEnergyMax)
            {
                Energy = Math.Min(Settings.HezeEnergyMax, Energy + Settings.HezeEnergyGain);
                owner.HezePartner.Heze.Energy = Energy;
                SendEnergy();
            }
            PlayerObject partner = owner.HezePartner;
            if (Energy < Settings.HezeEnergyMax || partner.Heze.lastTarget != lastTarget || Envir.Time - partner.Heze.lastHitTime > 2000) return;
            Activate(target, baseDamage);
        }

        private void Activate(MapObject target, int baseDamage)
        {
            HezeSkillDefinition skill = Settings.GetHezeSkill(owner.Class, owner.HezePartner.Class);
            if (skill == null) return;
            int level = Math.Min(Level, owner.HezePartner.Heze.Level);
            int damage = (int)(baseDamage * (skill.DamageMultiplier + ((level - 1) * .1F)));
            foreach (MapObject victim in owner.CurrentMap.Objects.ToArray())
            {
                if (victim.Dead || !victim.IsAttackTarget(owner) || Functions.MaxDistance(victim.CurrentLocation, target.CurrentLocation) > skill.Radius) continue;
                victim.Attacked(owner, damage, DefenceType.ACAgility, false);
            }
            Energy = owner.HezePartner.Heze.Energy = 0;
            owner.Enqueue(new S.G_HezeComboActivated { SkillName = skill.Name, Location = target.CurrentLocation, Damage = damage, Animation = skill.Animation });
            owner.HezePartner.Enqueue(new S.G_HezeComboActivated { SkillName = skill.Name, Location = target.CurrentLocation, Damage = damage, Animation = skill.Animation });
            SendEnergy();
        }

        private void SendEnergy()
        {
            string partnerName = owner.HezePartner == null ? string.Empty : owner.HezePartner.Name;
            owner.Enqueue(new S.G_HezeEnergy { Energy = Energy, Maximum = Settings.HezeEnergyMax, PartnerName = partnerName, Level = (byte)Level });
            if (owner.HezePartner != null)
                owner.HezePartner.Enqueue(new S.G_HezeEnergy { Energy = Energy, Maximum = Settings.HezeEnergyMax, PartnerName = owner.Name, Level = (byte)owner.HezePartner.Heze.Level });
        }
    }
}
