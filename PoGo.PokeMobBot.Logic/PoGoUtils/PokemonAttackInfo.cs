#region using directives

using POGOProtos.Enums;
using System.Collections.Generic;

#endregion

namespace PoGo.PokeMobBot.Logic.PoGoUtils
{
    public class PokemonAttackStats
    {
        public PokemonMove Move { get; set; }
        public PokemonType Type { get; set; }

        public int DurationMs { get; set; }

        public int Damage { get; set; }

        public int Energy { get; set; }

        public double Dps { get; set; }

        public PokemonAttackStats(PokemonMove move, PokemonType type, int damage, int durationMs, int energy)
        {
            Move = move;
            Type = type;
            Energy = energy;
            Damage = damage;
            DurationMs = durationMs;
            Dps = Damage*(1000/(float)durationMs);
        }
        
    }


    public static class PokemonMoveStatsDictionary
    {
        public static PokemonAttackStats GetMoveData(PokemonMove move)
        {
            if (BasicMoves.ContainsKey(move))
                return BasicMoves[move];
            return ChargedMoves.ContainsKey(move) ? ChargedMoves[move] : null;
        }


        private static readonly Dictionary<PokemonMove, PokemonAttackStats> BasicMoves = new Dictionary
            <PokemonMove, PokemonAttackStats>
        {
            {PokemonMove.PoundFast, new PokemonAttackStats(PokemonMove.PoundFast, PokemonType.Normal, 7, 540, 7)},
            {PokemonMove.MetalClawFast, new PokemonAttackStats(PokemonMove.MetalClawFast, PokemonType.Steel, 8, 630, 7)},
            {PokemonMove.PsychoCutFast, new PokemonAttackStats(PokemonMove.PsychoCutFast, PokemonType.Psychic, 7, 570, 7)},
            {PokemonMove.WingAttackFast, new PokemonAttackStats(PokemonMove.WingAttackFast, PokemonType.Flying, 9, 750, 7)},
            {PokemonMove.BiteFast, new PokemonAttackStats(PokemonMove.BiteFast, PokemonType.Dark, 6, 500, 7)},
            {PokemonMove.DragonBreathFast, new PokemonAttackStats(PokemonMove.DragonBreathFast, PokemonType.Dragon, 6, 500, 7)},
            {PokemonMove.ScratchFast, new PokemonAttackStats(PokemonMove.ScratchFast, PokemonType.Normal, 6, 500, 7)},
            {PokemonMove.WaterGunFast, new PokemonAttackStats(PokemonMove.WaterGunFast, PokemonType.Water, 6, 500, 7)},
            {(PokemonMove) 240, new PokemonAttackStats(PokemonMove.FireFangFast, PokemonType.Fire, 10, 840, 4)},
            {(PokemonMove) 213, new PokemonAttackStats(PokemonMove.ShadowClawFast, PokemonType.Ghost, 11, 950, 7)},
            {(PokemonMove) 238, new PokemonAttackStats(PokemonMove.FeintAttackFast, PokemonType.Dark, 12, 1040, 7)},
            {(PokemonMove) 224, new PokemonAttackStats(PokemonMove.PoisonJabFast, PokemonType.Poison, 12, 1050, 7)},
            {(PokemonMove) 234, new PokemonAttackStats(PokemonMove.ZenHeadbuttFast, PokemonType.Psychic, 12, 1050, 4)},
            {(PokemonMove) 239, new PokemonAttackStats(PokemonMove.SteelWingFast, PokemonType.Steel, 15, 1330, 4)},
            {(PokemonMove) 201, new PokemonAttackStats(PokemonMove.BugBiteFast, PokemonType.Bug, 5, 450, 7)},
            {(PokemonMove) 218, new PokemonAttackStats(PokemonMove.FrostBreathFast, PokemonType.Ice, 9, 810, 7)},
            {(PokemonMove) 233, new PokemonAttackStats(PokemonMove.MudSlapFast, PokemonType.Ground, 15, 1350, 9)},
            {(PokemonMove) 216, new PokemonAttackStats(PokemonMove.MudShotFast, PokemonType.Ground, 6, 550, 7)},
            {(PokemonMove) 221, new PokemonAttackStats(PokemonMove.TackleFast, PokemonType.Normal, 12, 1100, 7)},
            {(PokemonMove) 237, new PokemonAttackStats(PokemonMove.BubbleFast, PokemonType.Water, 25, 2300, 15)},
            {(PokemonMove) 214, new PokemonAttackStats(PokemonMove.VineWhipFast, PokemonType.Grass, 7, 650, 7)},
            {(PokemonMove) 217, new PokemonAttackStats(PokemonMove.IceShardFast, PokemonType.Ice, 15, 1400, 7)},
            {(PokemonMove) 241, new PokemonAttackStats(PokemonMove.RockSmashFast, PokemonType.Fighting, 15, 1410, 7)},
            {(PokemonMove) 223, new PokemonAttackStats(PokemonMove.CutFast, PokemonType.Normal, 12, 1130, 7)},
            {(PokemonMove) 236, new PokemonAttackStats(PokemonMove.PoisonStingFast, PokemonType.Poison, 6, 575, 4)},
            {(PokemonMove) 215, new PokemonAttackStats(PokemonMove.RazorLeafFast, PokemonType.Grass, 15, 1450, 7)},
            {(PokemonMove) 212, new PokemonAttackStats(PokemonMove.LickFast, PokemonType.Ghost, 5, 500, 7)},
            {(PokemonMove) 206, new PokemonAttackStats(PokemonMove.SparkFast, PokemonType.Electric, 7, 700, 4)},
            {(PokemonMove) 203, new PokemonAttackStats(PokemonMove.SuckerPunchFast, PokemonType.Dark, 7, 700, 4)},
            {(PokemonMove) 235, new PokemonAttackStats(PokemonMove.ConfusionFast, PokemonType.Psychic, 15, 1510, 7)},
            {(PokemonMove) 225, new PokemonAttackStats(PokemonMove.AcidFast, PokemonType.Poison, 10, 1050, 7)},
            {(PokemonMove) 209, new PokemonAttackStats(PokemonMove.EmberFast, PokemonType.Fire, 10, 1050, 7)},
            {(PokemonMove) 227, new PokemonAttackStats(PokemonMove.RockThrowFast, PokemonType.Rock, 12, 1360, 7)},
            {(PokemonMove) 211, new PokemonAttackStats(PokemonMove.PeckFast, PokemonType.Flying, 10, 1150, 10)},
            {(PokemonMove) 207, new PokemonAttackStats(PokemonMove.LowKickFast, PokemonType.Fighting, 5, 600, 7)},
            {(PokemonMove) 205, new PokemonAttackStats(PokemonMove.ThunderShockFast, PokemonType.Electric, 5, 600, 7)},
            {(PokemonMove) 229, new PokemonAttackStats(PokemonMove.BulletPunchFast, PokemonType.Steel, 10, 1200, 7)},
            {(PokemonMove) 219, new PokemonAttackStats(PokemonMove.QuickAttackFast, PokemonType.Normal, 10, 1330, 7)},
            {(PokemonMove) 200, new PokemonAttackStats(PokemonMove.FuryCutterFast, PokemonType.Bug, 3, 400, 12)},
            {(PokemonMove) 208, new PokemonAttackStats(PokemonMove.KarateChopFast, PokemonType.Fighting, 6, 800, 7)},
            {(PokemonMove) 231, new PokemonAttackStats(PokemonMove.SplashFast, PokemonType.Water, 0, 1230, 7)},
        };

        private static readonly Dictionary<PokemonMove, PokemonAttackStats> ChargedMoves = new Dictionary
            <PokemonMove, PokemonAttackStats>
        {
            {(PokemonMove) 32, new PokemonAttackStats(PokemonMove.StoneEdge, PokemonType.Rock, 80, 3100, 100)},
            {(PokemonMove) 28, new PokemonAttackStats(PokemonMove.CrossChop, PokemonType.Fighting, 60, 2000, 100)},
            {(PokemonMove) 83, new PokemonAttackStats(PokemonMove.DragonClaw, PokemonType.Dragon, 35, 1500, 50)},
            {(PokemonMove) 40, new PokemonAttackStats(PokemonMove.Blizzard, PokemonType.Ice, 100, 3900, 100)},
            {(PokemonMove) 131, new PokemonAttackStats(PokemonMove.BodySlam, PokemonType.Normal, 40, 1560, 50)},
            {(PokemonMove) 22, new PokemonAttackStats(PokemonMove.Megahorn, PokemonType.Bug, 80, 3200, 100)},
            {(PokemonMove) 122, new PokemonAttackStats(PokemonMove.Hurricane, PokemonType.Flying, 80, 3200, 100)},
            {(PokemonMove) 116, new PokemonAttackStats(PokemonMove.SolarBeam, PokemonType.Grass, 120, 4900, 100)},
            {(PokemonMove) 103, new PokemonAttackStats(PokemonMove.FireBlast, PokemonType.Fire, 100, 4100, 100)},
            {(PokemonMove) 14, new PokemonAttackStats(PokemonMove.HyperBeam, PokemonType.Normal, 120, 5000, 100)},
            {(PokemonMove) 31, new PokemonAttackStats(PokemonMove.Earthquake, PokemonType.Ground, 100, 4200, 100)},
            {(PokemonMove) 118, new PokemonAttackStats(PokemonMove.PowerWhip, PokemonType.Grass, 70, 2800, 100)},
            {(PokemonMove) 107, new PokemonAttackStats(PokemonMove.HydroPump, PokemonType.Water, 90, 3800, 100)},
            {(PokemonMove) 117, new PokemonAttackStats(PokemonMove.LeafBlade, PokemonType.Grass, 55, 2800, 50)},
            {(PokemonMove) 78, new PokemonAttackStats(PokemonMove.Thunder, PokemonType.Electric, 100, 4300, 100)},
            {(PokemonMove) 123, new PokemonAttackStats(PokemonMove.BrickBreak, PokemonType.Fighting, 30, 1600, 33)},
            {(PokemonMove) 92, new PokemonAttackStats(PokemonMove.GunkShot, PokemonType.Poison, 65, 3000, 100)},
            {(PokemonMove) 90, new PokemonAttackStats(PokemonMove.SludgeBomb, PokemonType.Poison, 55, 2600, 50)},
            {(PokemonMove) 42, new PokemonAttackStats(PokemonMove.HeatWave, PokemonType.Fire, 80, 3800, 100)},
            {(PokemonMove) 87, new PokemonAttackStats(PokemonMove.Moonblast, PokemonType.Fairy, 85, 4100, 100)},
            {(PokemonMove) 91, new PokemonAttackStats(PokemonMove.SludgeWave, PokemonType.Poison, 70, 3400, 100)},
            {(PokemonMove) 79, new PokemonAttackStats(PokemonMove.Thunderbolt, PokemonType.Electric, 55, 2700, 50)},
            {(PokemonMove) 47, new PokemonAttackStats(PokemonMove.PetalBlizzard, PokemonType.Grass, 65, 3200, 50)},
            {(PokemonMove) 89, new PokemonAttackStats(PokemonMove.CrossPoison, PokemonType.Poison, 25, 1500, 25)},
            {(PokemonMove) 108, new PokemonAttackStats(PokemonMove.Psychic, PokemonType.Psychic, 55, 2800, 50)},
            {(PokemonMove) 58, new PokemonAttackStats(PokemonMove.AquaTail, PokemonType.Water, 45, 2350, 50)},
            {(PokemonMove) 24, new PokemonAttackStats(PokemonMove.Flamethrower, PokemonType.Fire, 55, 2900, 50)},
            {(PokemonMove) 88, new PokemonAttackStats(PokemonMove.PlayRough, PokemonType.Fairy, 55, 2900, 50)},
            {(PokemonMove) 82, new PokemonAttackStats(PokemonMove.DragonPulse, PokemonType.Dragon, 65, 3600, 50)},
            {(PokemonMove) 39, new PokemonAttackStats(PokemonMove.IceBeam, PokemonType.Ice, 65, 3650, 50)},
            {(PokemonMove) 49, new PokemonAttackStats(PokemonMove.BugBuzz, PokemonType.Bug, 75, 4250, 50)},
            {(PokemonMove) 46, new PokemonAttackStats(PokemonMove.DrillRun, PokemonType.Ground, 50, 3400, 33)},
            {(PokemonMove) 59, new PokemonAttackStats(PokemonMove.SeedBomb, PokemonType.Grass, 40, 2400, 33)},
            {(PokemonMove) 77, new PokemonAttackStats(PokemonMove.ThunderPunch, PokemonType.Electric, 40, 2400, 33)},
            {(PokemonMove) 100, new PokemonAttackStats(PokemonMove.XScissor, PokemonType.Bug, 35, 2100, 33)},
            {(PokemonMove) 129, new PokemonAttackStats(PokemonMove.HyperFang, PokemonType.Normal, 35, 2100, 33)},
            {(PokemonMove) 64, new PokemonAttackStats(PokemonMove.RockSlide, PokemonType.Rock, 50, 3200, 33)},
            {(PokemonMove) 94, new PokemonAttackStats(PokemonMove.BoneClub, PokemonType.Ground, 25, 1600, 25)},
            {(PokemonMove) 36, new PokemonAttackStats(PokemonMove.FlashCannon, PokemonType.Steel, 60, 3900, 33)},
            {(PokemonMove) 74, new PokemonAttackStats(PokemonMove.IronHead, PokemonType.Steel, 30, 2000, 33)},
            {(PokemonMove) 38, new PokemonAttackStats(PokemonMove.DrillPeck, PokemonType.Flying, 40, 2700, 33)},
            {(PokemonMove) 60, new PokemonAttackStats(PokemonMove.Psyshock, PokemonType.Psychic, 40, 2700, 33)},
            {(PokemonMove) 70, new PokemonAttackStats(PokemonMove.ShadowBall, PokemonType.Ghost, 45, 3080, 33)},
            {(PokemonMove) 99, new PokemonAttackStats(PokemonMove.SignalBeam, PokemonType.Bug, 45, 3100, 33)},
            {(PokemonMove) 115, new PokemonAttackStats(PokemonMove.FirePunch, PokemonType.Fire, 40, 2800, 33)},
            {(PokemonMove) 54, new PokemonAttackStats(PokemonMove.Submission, PokemonType.Fighting, 30, 2100, 33)},
            {(PokemonMove) 102, new PokemonAttackStats(PokemonMove.FlameBurst, PokemonType.Fire, 30, 2100, 25)},
            {(PokemonMove) 127, new PokemonAttackStats(PokemonMove.Stomp, PokemonType.Normal, 30, 2100, 25)},
            {(PokemonMove) 35, new PokemonAttackStats(PokemonMove.Discharge, PokemonType.Electric, 35, 2500, 33)},
            {(PokemonMove) 65, new PokemonAttackStats(PokemonMove.PowerGem, PokemonType.Rock, 40, 2900, 33)},
            {(PokemonMove) 106, new PokemonAttackStats(PokemonMove.Scald, PokemonType.Water, 55, 4000, 33)},
            {(PokemonMove) 109, new PokemonAttackStats(PokemonMove.Psystrike, PokemonType.Psychic, 70, 5100, 100)},
            {(PokemonMove) 56, new PokemonAttackStats(PokemonMove.LowSweep, PokemonType.Fighting, 30, 2250, 25)},
            {(PokemonMove) 51, new PokemonAttackStats(PokemonMove.NightSlash, PokemonType.Dark, 30, 2700, 25)},
            {(PokemonMove) 86, new PokemonAttackStats(PokemonMove.DazzlingGleam, PokemonType.Fairy, 55, 4200, 33)},
            {(PokemonMove) 16, new PokemonAttackStats(PokemonMove.DarkPulse, PokemonType.Dark, 45, 3500, 33)},
            {(PokemonMove) 33, new PokemonAttackStats(PokemonMove.IcePunch, PokemonType.Ice, 45, 3500, 33)},
            {(PokemonMove) 26, new PokemonAttackStats(PokemonMove.Dig, PokemonType.Ground, 70, 5800, 33)},
            {(PokemonMove) 20, new PokemonAttackStats(PokemonMove.ViceGrip, PokemonType.Normal, 25, 2100, 20)},
            {(PokemonMove) 18, new PokemonAttackStats(PokemonMove.Sludge, PokemonType.Poison, 30, 2600, 25)},
            {(PokemonMove) 96, new PokemonAttackStats(PokemonMove.MudBomb, PokemonType.Ground, 30, 2600, 25)},
            {(PokemonMove) 126, new PokemonAttackStats(PokemonMove.HornAttack, PokemonType.Normal, 25, 2200, 25)},
            {(PokemonMove) 121, new PokemonAttackStats(PokemonMove.AirCutter, PokemonType.Flying, 30, 3300, 25)},
            {(PokemonMove) 132, new PokemonAttackStats(PokemonMove.Rest, PokemonType.Normal, 35, 3100, 33)},
            {(PokemonMove) 72, new PokemonAttackStats(PokemonMove.MagnetBomb, PokemonType.Steel, 30, 2800, 25)},
            {(PokemonMove) 57, new PokemonAttackStats(PokemonMove.AquaJet, PokemonType.Water, 25, 2350, 20)},
            {(PokemonMove) 105, new PokemonAttackStats(PokemonMove.WaterPulse, PokemonType.Water, 35, 3300, 25)},
            {(PokemonMove) 30, new PokemonAttackStats(PokemonMove.Psybeam, PokemonType.Psychic, 40, 3800, 25)},
            {(PokemonMove) 63, new PokemonAttackStats(PokemonMove.RockTomb, PokemonType.Rock, 30, 3400, 25)},
            {(PokemonMove) 50, new PokemonAttackStats(PokemonMove.PoisonFang, PokemonType.Poison, 25, 2400, 20)},
            {(PokemonMove) 104, new PokemonAttackStats(PokemonMove.Brine, PokemonType.Water, 25, 2400, 25)},
            {(PokemonMove) 45, new PokemonAttackStats(PokemonMove.AerialAce, PokemonType.Flying, 30, 2900, 25)},
            {(PokemonMove) 53, new PokemonAttackStats(PokemonMove.BubbleBeam, PokemonType.Water, 30, 2900, 25)},
            {(PokemonMove) 95, new PokemonAttackStats(PokemonMove.Bulldoze, PokemonType.Ground, 35, 3400, 25)},
            {(PokemonMove) 125, new PokemonAttackStats(PokemonMove.Swift, PokemonType.Normal, 30, 3000, 25)},
            {(PokemonMove) 62, new PokemonAttackStats(PokemonMove.AncientPower, PokemonType.Rock, 35, 3600, 25)},
            {(PokemonMove) 114, new PokemonAttackStats(PokemonMove.GigaDrain, PokemonType.Grass, 35, 3600, 33)},
            {(PokemonMove) 69, new PokemonAttackStats(PokemonMove.OminousWind, PokemonType.Ghost, 30, 3100, 25)},
            {(PokemonMove) 67, new PokemonAttackStats(PokemonMove.ShadowPunch, PokemonType.Ghost, 20, 2100, 25)},
            {(PokemonMove) 80, new PokemonAttackStats(PokemonMove.Twister, PokemonType.Dragon, 25, 2700, 20)},
            {(PokemonMove) 85, new PokemonAttackStats(PokemonMove.DrainingKiss, PokemonType.Fairy, 25, 2800, 20)},
            {(PokemonMove) 21, new PokemonAttackStats(PokemonMove.FlameWheel, PokemonType.Fire, 40, 4600, 25)},
            {(PokemonMove) 133, new PokemonAttackStats(PokemonMove.Struggle, PokemonType.Normal, 15, 1695, 20)},
            {(PokemonMove) 101, new PokemonAttackStats(PokemonMove.FlameCharge, PokemonType.Fire, 25, 3100, 20)},
            {(PokemonMove) 34, new PokemonAttackStats(PokemonMove.HeartStamp, PokemonType.Psychic, 20, 2550, 25)},
            {(PokemonMove) 75, new PokemonAttackStats(PokemonMove.ParabolicCharge, PokemonType.Electric, 15, 2100, 20)},
            {(PokemonMove) 111, new PokemonAttackStats(PokemonMove.IcyWind, PokemonType.Ice, 25, 3800, 20)},
            {(PokemonMove) 84, new PokemonAttackStats(PokemonMove.DisarmingVoice, PokemonType.Fairy, 25, 3900, 20)},
            {(PokemonMove) 13, new PokemonAttackStats(PokemonMove.Wrap, PokemonType.Normal, 25, 4000, 20)},
            {(PokemonMove) 66, new PokemonAttackStats(PokemonMove.ShadowSneak, PokemonType.Ghost, 15, 3100, 20)},
            {(PokemonMove) 48, new PokemonAttackStats(PokemonMove.MegaDrain, PokemonType.Grass, 15, 3200, 20)}
        };


    }
}