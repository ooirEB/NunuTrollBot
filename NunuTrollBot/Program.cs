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
        private static AIHeroClient _user;

        private static AIHeroClient _myJungler;
        private static AIHeroClient _myJunglerAfkBackup;

        private static Spell.Targeted _q;
        private static Spell.Targeted _e;

        private static int _sequencer;
        private static int _idleCounter;
        private static Vector3 _lastPosition;

        private static Spell.Targeted _smite;

        private static int _tickTock;
        private static int _tickTockRequired;
        private static readonly int[] ELevel = {2, 8, 10, 12, 13};

        private static readonly int[] QLevel = {1, 3, 5, 7, 9};
        private static readonly int[] RLevel = {6, 11, 16};
        private static readonly int[] WLevel = {4, 14, 15, 17, 18};

        private static readonly ItemId[] _itemsToBuy =
        {
            ItemId.Trackers_Knife_Enchantment_Cinderhulk,
            ItemId.Boots_of_Swiftness, ItemId.Frozen_Heart, ItemId.Spirit_Visage, ItemId.Dead_Mans_Plate,
            ItemId.Warmogs_Armor
        };

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

            _user = Player.Instance;

            //Check Champion
            if (_user.ChampionName != "Nunu")
            {
                Chat.Print("NunuTrollBot: " + _user.ChampionName + " is not supported! Go for Nunu! Kappa");
                return;
            }

            //Do the spells

            _q = new Spell.Targeted(SpellSlot.Q, 125);
            _e = new Spell.Targeted(SpellSlot.E, 550);

            _smite = new Spell.Targeted(_user.GetSpellSlotFromName("summonersmite"),
                500);

            //Check for smite
            if (_smite.Slot == SpellSlot.Unknown)
            {
                Chat.Print("NunuTrollBot: Sorry, but you have to have smite for this bot to work!");
                return;
            }

            //Get Jungler
            foreach (var ally in EntityManager.Heroes.Allies)
            {
                if (!ally.IsMe && ally.FindSummonerSpellSlotFromName("summonersmite") != SpellSlot.Unknown)
                {
                    _myJungler = ally;
                    _myJunglerAfkBackup = _myJungler;
                }
            }
            if (_myJungler == null)
            {
                Chat.Print("NunuTrollBot: No jungler found!");
                return;
            }
            //Starting items

            Shop.BuyItem(ItemId.Warding_Totem_Trinket);
            if (_user.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
            {
                _user.Spellbook.LevelSpell(SpellSlot.Q);
            }

            _sequencer = 1;
            _tickTock = 0;
            _idleCounter = 1;
            _lastPosition = _user.Position;
            Game.OnUpdate += Sequence;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
            Game.OnNotify += Game_OnNotify;
            Chat.Print("NunuTrollBot: Following and trolling " + _myJungler.ChampionName + "!");
            ////////////////////////////
        }

        private static void Game_OnNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnReconnect && args.NetworkId == _myJunglerAfkBackup.NetworkId)
            {
                Chat.Print("NunuTrollBot: Following jungler " + _myJunglerAfkBackup.ChampionName + " again!");
                _myJungler = _myJunglerAfkBackup;
            }
            if (args.NetworkId == _myJungler.NetworkId && args.EventId == GameEventId.OnLeave)
            {
                Chat.Print("NunuTrollBot: Jungler left the game! Great success!");
                while (_myJungler.NetworkId == _myJunglerAfkBackup.NetworkId)
                {
                    Chat.Print("NunuTrollBot: Finding a champion to follow!");
                    _myJungler =
                        EntityManager.Heroes.Allies
                            .FirstOrDefault(a => !a.IsMe && a.IsMoving && a.NetworkId != _myJunglerAfkBackup.NetworkId);
                    Thread.Sleep(500);
                }
                Chat.Print("NunuTrollBot: Following " + _myJungler.ChampionName + "!");
            }

            if (args.NetworkId == _user.NetworkId && args.EventId == GameEventId.OnKill)
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
            if (sender.NetworkId == _user.NetworkId)
            {
                Core.DelayAction(() => LevelUp(), new Random().Next(500, 1000));
            }
        }

        private static void LevelUp()
        {
            if (_user.Spellbook.CanSpellBeUpgraded(SpellSlot.Q))
            {
                _user.Spellbook.LevelSpell(SpellSlot.Q);
                return;
            }
            if (_user.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
            {
                _user.Spellbook.LevelSpell(SpellSlot.R);
                return;
            }
            if (_user.Spellbook.CanSpellBeUpgraded(SpellSlot.E))
            {
                _user.Spellbook.LevelSpell(SpellSlot.E);
                return;
            }
            if (_user.Spellbook.CanSpellBeUpgraded(SpellSlot.W))
            {
                _user.Spellbook.LevelSpell(SpellSlot.W);
            }
        }

        private static void Obj_AI_Base_OnTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            if (_user.IsDead)
            {
                return;
            }
            if (sender.NetworkId == _myJungler.NetworkId)
            {
                if (args.RecallName == "Recall")
                {
                    if (!_user.IsInFountainRange() && !_myJungler.IsInFountainRange())
                    {
                        Core.DelayAction(() => Player.CastSpell(SpellSlot.Recall), 200);
                        return;
                    }
                }
                if (_user.IsRecalling())
                {
                    Core.DelayAction(() => ChkRecall(), Game.Ping + 200);
                }
            }
        }

        private static void ChkRecall()
        {
            if (!_myJungler.IsRecalling() && !_myJungler.IsInFountainRange())
            {
                Player.ForceIssueOrder(GameObjectOrder.MoveTo, _user.Position, false);
            }
        }

        private static void Sequence(EventArgs args)
        {
            if (_user.IsDead)
            {
                return;
            }

            JungleSteal();

            Circle.Draw(Color.AliceBlue, 100, _user.Path);

            switch (_sequencer)
            {
                case 1:
                    BadManners();
                    _sequencer = 2;
                    break;
                case 2:
                    KillSteal();
                    _sequencer = 3;
                    break;
                case 3:
                    Shoping();
                    _sequencer = 4;
                    break;
                case 4:
                    Movement();
                    _sequencer = 1;
                    break;
            }
        }

        private static void BadManners()
        {
            if (_user.Position == _lastPosition)
            {
                _idleCounter++;
            }
            else
            {
                _lastPosition = _user.Position;
                _idleCounter = 1;
            }
            if (_idleCounter == 150)
            {
                Player.DoEmote(Emote.Dance);
                _idleCounter = 1;
            }
        }

        private static void Movement()
        {
            if (_myJungler.IsDead)
            {
                var myBase = Get<Obj_SpawnPoint>().FirstOrDefault(a => a.Team == _user.Team);
                Orbwalker.MoveTo(myBase?.Position ?? Vector3.Zero);
                return;
            }

            if (_user.Position == _myJungler.Position)
            {
                return;
            }

            if (_tickTock == _tickTockRequired)
            {
                var destination = _myJungler.Position;
                Orbwalker.MoveTo(destination);
                _tickTock = 1;
                _tickTockRequired = new Random().Next(10, 15);
            }
            _tickTock++;
        }

        private static void Shoping()
        {
            if (
                !_user.HasItem(ItemId.Hunters_Talisman, ItemId.Trackers_Knife_Enchantment_Cinderhulk,
                    ItemId.Trackers_Knife))
            {
                Shop.BuyItem(ItemId.Hunters_Talisman);
            }
            foreach (var x in _itemsToBuy)
            {
                if (_user.HasItem(x))
                {
                    continue;
                }
                // Credits to Definitely Not Kappa!
                var item = Item.ItemData.FirstOrDefault(i => i.Key == x);
                var theitem = new Item(item.Key);
                var ia = theitem.GetComponents().Where(a => !a.IsOwned(_user)).Sum(i => i.ItemInfo.Gold.Total);
                var currentprice = theitem.ItemInfo.Gold.Base + ia;
                if ((item.Value != null) && (item.Key != ItemId.Unknown) && item.Value.ValidForPlayer &&
                    item.Value.InStore && item.Value.Gold.Purchasable
                    && item.Value.AvailableForMap && (_user.Gold >= currentprice))
                {
                    Shop.BuyItem(item.Key);
                    return;
                }
                //
                if (_user.InventoryItems.Length < 8)
                {
                    foreach (var component in theitem.GetComponents())
                    {
                        if (!_user.HasItem(component.Id) && _user.Gold >= component.GoldRequired())
                        {
                            Shop.BuyItem(component.Id);
                            return;
                        }
                    }
                }
            }
        }

        private static void KillSteal()
        {
            // E Killsteal
            if (_e.IsReady())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.Distance(_user.Position) <= 550 &&
                        enemy.TotalShieldHealth() <= _user.GetSpellDamage(enemy, SpellSlot.E))
                    {
                        Player.CastSpell(SpellSlot.E, enemy);
                        return;
                    }
                }
            }

            // Autoattack creeps
            var killableMinion = EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health)
                .FirstOrDefault(b => b.Distance(_user) <= 500);
            if (killableMinion != null && !killableMinion.IsDead && killableMinion.BaseSkinName.Contains("Siege") &&
                killableMinion.Health <= _user.GetSpellDamage(killableMinion, SpellSlot.Q))
            {
                Player.CastSpell(SpellSlot.Q, killableMinion);
                return;
            }
            if (killableMinion != null && !killableMinion.IsDead &&
                killableMinion.Health <= _user.GetAutoAttackDamage(killableMinion, true))
            {
                Player.IssueOrder(GameObjectOrder.AttackTo, killableMinion);
                return;
            }

            // And jungle too
            var jungleCreep =
                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .OrderBy(a => a.Health)
                    .FirstOrDefault(b => b.Distance(_user) <= 500);

            if (jungleCreep != null && !jungleCreep.IsDead &&
                jungleCreep.Health <= _user.GetAutoAttackDamage(jungleCreep, true))
            {
                Player.IssueOrder(GameObjectOrder.AttackTo, jungleCreep);
            }
        }

        private static void JungleSteal()
        {
            var killableJungle =
                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .OrderByDescending(a => a.MaxHealth)
                    .FirstOrDefault(b => b.Distance(_user) <= 500);

            if (killableJungle != null && !killableJungle.Name.Contains("Mini"))
            {
                if (killableJungle.Name.Contains("Baron") || killableJungle.Name.Contains("Dragon") ||
                    killableJungle.Name.Contains("Red") || killableJungle.Name.Contains("Blue"))
                {
                    if (_q.IsReady() && _smite.IsReady())
                    {
                        if (killableJungle.Health <=
                            _user.GetSpellDamage(killableJungle, SpellSlot.Q) +
                            _user.GetSummonerSpellDamage(killableJungle, DamageLibrary.SummonerSpells.Smite))
                        {
                            Player.CastSpell(SpellSlot.Q, killableJungle);
                        }
                    }
                    else if (_q.IsReady() && !_smite.IsReady())
                    {
                        if (killableJungle.Health <= _user.GetSpellDamage(killableJungle, SpellSlot.Q))
                        {
                            Player.CastSpell(SpellSlot.Q, killableJungle);
                        }
                    }
                    else if (!_q.IsReady() && _smite.IsReady())
                    {
                        if (killableJungle.Health <=
                            _user.GetSummonerSpellDamage(killableJungle, DamageLibrary.SummonerSpells.Smite))
                        {
                            Player.CastSpell(_smite.Slot, killableJungle);
                        }
                    }
                }
                else
                {
                    if (_q.IsReady())
                    {
                        if (killableJungle.Health <= _user.GetSpellDamage(killableJungle, SpellSlot.Q))
                        {
                            Player.CastSpell(SpellSlot.Q, killableJungle);
                        }
                    }
                }
            }
        }
    }
}