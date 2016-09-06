using System;
using System.Linq;
using System.Threading;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;
using static EloBuddy.ObjectManager;
using Player = EloBuddy.Player;

namespace NunuTrollBot
{
    internal class Program
    {
        private static AIHeroClient user;

        private static AIHeroClient myJungler;
        private static AIHeroClient myJunglerAFKBackup;

        private static Spell.Targeted Q;
        private static Spell.Targeted E;

        private static int sequencer;

        private static Spell.Targeted smite;

        private static int tickTock;
        private static int tickTockRequired;
        private static readonly int[] eLevel = {2, 8, 10, 12, 13};

        private static readonly int[] qLevel = {1, 3, 5, 7, 9};
        private static readonly int[] rLevel = {6, 11, 16};
        private static readonly int[] wLevel = {4, 14, 15, 17, 18};

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += LoadingOnOnLoadingComplete;
        }

        private static void LoadingOnOnLoadingComplete(EventArgs args)
        {
            //Check Map
            if (Game.MapId != GameMapId.SummonersRift)
            {
                Chat.Print("NunuTrollBot: Only Summoners Rift is supported!");
            }

            user = Player.Instance;

            //Check Champion
            if (user.ChampionName != "Nunu")
            {
                Chat.Print("NunuTrollBot: " + user.ChampionName + " is not supported! Go for Nunu! Kappa");
                return;
            }

            //Do the spells

            Q = new Spell.Targeted(SpellSlot.Q, 125);
            E = new Spell.Targeted(SpellSlot.E, 550);

            smite = new Spell.Targeted(user.GetSpellSlotFromName("summonersmite"),
                500);

            //Check for smite
            if (smite == null)
            {
                Chat.Print("NunuTrollBot: Sorry, but you have to have smite for this bot to work!");
                return;
            }

            //Get Jungler
            foreach (var ally in EntityManager.Heroes.Allies)
            {
                if (!ally.IsMe && ally.FindSummonerSpellSlotFromName("summonersmite") != SpellSlot.Unknown)
                {
                    myJungler = ally;
                    myJunglerAFKBackup = myJungler;
                }
            }
            if (myJungler == null)
            {
                Chat.Print("NunuTrollBot: No jungler found!");
                return;
            }
            //Starting items

            Shop.BuyItem(ItemId.Warding_Totem_Trinket);
            if (user.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
            {
                user.Spellbook.LevelSpell(SpellSlot.Q);
            }

            sequencer = 1;
            tickTock = 0;
            Game.OnUpdate += Sequence;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
            Game.OnNotify += Game_OnNotify;
            Chat.Print("NunuTrollBot: Following and trolling " + myJungler.ChampionName + "!");
            ////////////////////////////
        }

        private static void Game_OnNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnReconnect && args.NetworkId == myJunglerAFKBackup.NetworkId)
            {
                Chat.Print("NunuTrollBot: Following jungler " + myJunglerAFKBackup.ChampionName + " again!");
                myJungler = myJunglerAFKBackup;
            }
            if (args.NetworkId == myJungler.NetworkId && args.EventId == GameEventId.OnLeave)
            {
                Chat.Print("NunuTrollBot: Jungler left the game! Great success!");
                while (myJungler.NetworkId == myJunglerAFKBackup.NetworkId)
                {
                    Chat.Print("NunuTrollBot: Finding a champion to follow!");
                    myJungler =
                        EntityManager.Heroes.Allies.Where(
                            a => !a.IsMe && a.IsMoving && a.NetworkId != myJunglerAFKBackup.NetworkId)
                            .FirstOrDefault();
                    Thread.Sleep(500);
                }
                Chat.Print("NunuTrollBot: Following " + myJungler.ChampionName + "!");
            }

            if (args.NetworkId == user.NetworkId && args.EventId == GameEventId.OnKill)
            {
                if (args.EventId == GameEventId.OnChampionKill)
                {
                    Core.DelayAction(() => Chat.Say("/masterybadge"), 600);
                }
                Core.DelayAction(() => Player.DoEmote(Emote.Laugh), 600);
            }
        }

        private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (sender.NetworkId == user.NetworkId)
            {
                Core.DelayAction(() => LevelUp(), new Random().Next(500, 1000));
            }
        }

        private static void LevelUp()
        {
            if (user.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
            {
                user.Spellbook.LevelSpell(SpellSlot.Q);
                return;
            }
            if (user.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
            {
                user.Spellbook.LevelSpell(SpellSlot.R);
                return;
            }
            if (user.Spellbook.CanSpellBeUpgraded(SpellSlot.E))
            {
                user.Spellbook.LevelSpell(SpellSlot.E);
                return;
            }
            if (user.Spellbook.CanSpellBeUpgraded(SpellSlot.W))
            {
                user.Spellbook.LevelSpell(SpellSlot.W);
            }
        }

        private static void Obj_AI_Base_OnTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            if (user.IsDead)
            {
                return;
            }
            if (sender.NetworkId == myJungler.NetworkId)
            {
                if (args.RecallName == "Recall")
                {
                    if (!user.IsInFountainRange() && !myJungler.IsInFountainRange())
                    {
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Recall), 200);
                        return;
                    }
                }
                if (user.IsRecalling())
                {
                    Core.DelayAction(() => ChkRecall(), Game.Ping + 200);
                }
            }
        }

        private static void ChkRecall()
        {
            if (!myJungler.IsRecalling() && !myJungler.IsInFountainRange())
            {
                Player.ForceIssueOrder(GameObjectOrder.MoveTo, user.Position, false);
            }
        }

        private static void Sequence(EventArgs args)
        {
            if (user.IsDead)
            {
                return;
            }

            Circle.Draw(Color.AliceBlue, 100, user.Path);

            switch (sequencer)
            {
                case 1:
                    JungleSteal();
                    sequencer = 2;
                    break;
                case 2:
                    KillSteal();
                    sequencer = 3;
                    break;
                case 3:
                    Shoping();
                    sequencer = 4;
                    break;
                case 4:
                    Movement();
                    sequencer = 1;
                    break;
            }
        }

        private static void Movement()
        {
            if (myJungler.IsDead)
            {
                var myBase = Get<Obj_SpawnPoint>().FirstOrDefault(a => a.Team == user.Team);
                Orbwalker.MoveTo(myBase?.Position ?? Vector3.Zero);
                return;
            }

            if (user.Position == myJungler.Position)
            {
                return;
            }

            if (tickTock == tickTockRequired)
            {
                var destination = myJungler.Position;
                Orbwalker.MoveTo(destination);
                tickTock = 1;
                tickTockRequired = new Random().Next(10, 15);
            }
            tickTock++;
        }

        private static void Shoping()
        {
            if (user.IsInShopRange())
            {
                if (!user.HasItem(ItemId.Hunters_Talisman) &&
                    !user.HasItem(ItemId.Trackers_Knife_Enchantment_Cinderhulk) && user.Gold >= 350)
                {
                    Shop.BuyItem(ItemId.Hunters_Talisman);
                }

                if (!user.HasItem(ItemId.Trackers_Knife_Enchantment_Cinderhulk) && user.Gold >= 2275)
                {
                    Shop.BuyItem(ItemId.Trackers_Knife_Enchantment_Cinderhulk);
                }

                if (!user.HasItem(ItemId.Ninja_Tabi) && user.Gold >= 1100)
                {
                    Shop.BuyItem(ItemId.Ninja_Tabi);
                }

                if (!user.HasItem(ItemId.Frozen_Heart) && user.Gold >= 2800)
                {
                    Shop.BuyItem(ItemId.Frozen_Heart);
                }

                if (!user.HasItem(ItemId.Spirit_Visage) && user.Gold >= 2800)
                {
                    Shop.BuyItem(ItemId.Spirit_Visage);
                }

                if (!user.HasItem(ItemId.Dead_Mans_Plate) && user.Gold >= 2900)
                {
                    Shop.BuyItem(ItemId.Dead_Mans_Plate);
                }

                if (!user.HasItem(ItemId.Warmogs_Armor) && user.Gold >= 2850)
                {
                    Shop.BuyItem(ItemId.Warmogs_Armor);
                }
            }
        }

        private static void KillSteal()
        {
            // E Killsteal
            if (E.IsReady())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.Distance(user.Position) <= 550 &&
                        enemy.TotalShieldHealth() <= user.GetSpellDamage(enemy, SpellSlot.E))
                    {
                        Player.CastSpell(SpellSlot.E, enemy);
                        return;
                    }
                }
            }

            // Autoattack creeps
            var killableMinion = EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health)
                .FirstOrDefault(b => b.Distance(user) <= 500);
            if (killableMinion != null && !killableMinion.IsDead && killableMinion.BaseSkinName.Contains("Siege") &&
                killableMinion.Health <= user.GetSpellDamage(killableMinion, SpellSlot.Q))
            {
                Player.CastSpell(SpellSlot.Q, killableMinion);
                return;
            }
            if (killableMinion != null && !killableMinion.IsDead &&
                killableMinion.Health <= user.GetAutoAttackDamage(killableMinion, true))
            {
                Player.IssueOrder(GameObjectOrder.AttackTo, killableMinion);
                return;
            }

            // And jungle too
            var jungleCreep =
                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .OrderBy(a => a.Health)
                    .FirstOrDefault(b => b.Distance(user) <= 500);

            if (jungleCreep != null && !jungleCreep.IsDead &&
                jungleCreep.Health <= user.GetAutoAttackDamage(jungleCreep, true))
            {
                Player.IssueOrder(GameObjectOrder.AttackTo, jungleCreep);
            }
        }

        private static void JungleSteal()
        {
            var KillableJungle =
                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .OrderByDescending(a => a.MaxHealth)
                    .FirstOrDefault(b => b.Distance(user) <= 500);

            if (KillableJungle != null && !KillableJungle.Name.Contains("Mini"))
            {
                if (KillableJungle.Name.Contains("Baron") || KillableJungle.Name.Contains("Dragon") ||
                    KillableJungle.Name.Contains("Red") || KillableJungle.Name.Contains("Blue"))
                {
                    if (Q.IsReady() && smite.IsReady())
                    {
                        if (KillableJungle.Health <=
                            user.GetSpellDamage(KillableJungle, SpellSlot.Q) +
                            user.GetSummonerSpellDamage(KillableJungle, DamageLibrary.SummonerSpells.Smite))
                        {
                            Player.CastSpell(SpellSlot.Q, KillableJungle);
                            Core.DelayAction(() => Player.CastSpell(smite.Slot, KillableJungle), 150);
                        }
                    }
                    else if (Q.IsReady() && !smite.IsReady())
                    {
                        if (KillableJungle.Health <= user.GetSpellDamage(KillableJungle, SpellSlot.Q))
                        {
                            Player.CastSpell(SpellSlot.Q, KillableJungle);
                        }
                    }
                    else if (!Q.IsReady() && smite.IsReady())
                    {
                        if (KillableJungle.Health <=
                            user.GetSummonerSpellDamage(KillableJungle, DamageLibrary.SummonerSpells.Smite))
                        {
                            Player.CastSpell(smite.Slot, KillableJungle);
                        }
                    }
                }
                else
                {
                    if (Q.IsReady())
                    {
                        if (KillableJungle.Health <= user.GetSpellDamage(KillableJungle, SpellSlot.Q))
                        {
                            Player.CastSpell(SpellSlot.Q, KillableJungle);
                        }
                    }
                }
            }
        }
    }
}