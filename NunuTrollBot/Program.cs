using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;

namespace NunuTrollBot
{
    internal class Program
    {
        private static AIHeroClient user;

        private static AIHeroClient myJungler;

        private static Spell.Targeted Q;
        private static Spell.Targeted E;

        private static int curLevel;

        private static int sequencer;

        private static Spell.Targeted smite;

        private static int tickTock;
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
                }
            }
            if (myJungler == null)
            {
                Chat.Print("NunuTrollBot: No jungler found!");
                return;
            }
            
            //Starting items
            
            Shop.BuyItem(ItemId.Warding_Totem_Trinket);

            sequencer = 1;
            tickTock = 0;
            Game.OnUpdate += Sequence;
            Chat.Print("NunuTrollBot: Following and trolling " + myJungler.ChampionName + "!");
            ////////////////////////////
        }

        private static void Sequence(EventArgs args)
        {
            if (user.IsDead)
            {
                return;
            }

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
                    LevelUpAbilities();
                    sequencer = 4;
                    break;
                case 4:
                    Shoping();
                    sequencer = 5;
                    break;
                case 5:
                    Movement();
                    sequencer = 1;
                    break;
            }
        }

        private static void Movement()
        {
            if (myJungler.IsDead || myJungler.IsRecalling())
            {
                if (!user.IsInFountainRange())
                {
                    Core.DelayAction(() => Player.CastSpell(SpellSlot.Recall), 500);
                }
                return;
            }

            if (user.IsRecalling() && !myJungler.IsRecalling())
            {
                Core.DelayAction(() => Player.ForceIssueOrder(GameObjectOrder.MoveTo, user.Position, false), 1100);
            }

            if (user.Position == myJungler.Position)
            {
                return;
            }

            if (tickTock == 10)
            {
                Orbwalker.MoveTo(myJungler.Position);
                tickTock = 1;
            }
            tickTock++;
        }

        private static void Shoping()
        {
            if (user.IsInShopRange())
            {
                if (!user.HasItem(ItemId.Hunters_Talisman) && !user.HasItem(ItemId.Trackers_Knife_Enchantment_Cinderhulk) && user.Gold >= 350)
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

        private static void LevelUpAbilities()
        {
            if (user.Level > curLevel)
            {
                if (qLevel.Contains(user.Level))
                {
                    user.Spellbook.LevelSpell(SpellSlot.Q);
                }
                if (wLevel.Contains(user.Level))
                {
                    user.Spellbook.LevelSpell(SpellSlot.W);
                }
                if (eLevel.Contains(user.Level))
                {
                    user.Spellbook.LevelSpell(SpellSlot.E);
                }
                if (rLevel.Contains(user.Level))
                {
                    user.Spellbook.LevelSpell(SpellSlot.R);
                }
                curLevel++;
            }
        }

        private static void KillSteal()
        {
            // E Killsteal
            if (E.IsReady())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.Distance(user.Position) <= 550 && enemy.Health <= user.GetSpellDamage(enemy,SpellSlot.E))
                    {
                        Player.CastSpell(SpellSlot.E, enemy);
                        return;
                    }
                }
            }

            // Autoattack creeps
            var killableMinion = EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health)
                .FirstOrDefault(b => b.Distance(user) <= user.AttackRange + 100);
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
                            Core.DelayAction(() => Player.CastSpell(smite.Slot), 100);
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