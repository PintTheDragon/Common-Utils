using Exiled.API.Extensions;

namespace Common_Utilities.EventHandlers
{
    using System.Collections.Generic;
    using Exiled.API.Features;
    using Exiled.API.Features.Items;
    using Exiled.Events.EventArgs;
    using InventorySystem.Items.Pickups;
    using MEC;
    using UnityEngine;
    
    public class ServerHandlers
    {
        private readonly Plugin plugin;
        public ServerHandlers(Plugin plugin) => this.plugin = plugin;

        public Vector3 EscapeZone = new Vector3();
        
        public void OnRoundStarted()
        {
            if (plugin.Config.AutonukeTime > -1)
                plugin.Coroutines.Add(Timing.RunCoroutine(AutoNuke()));
            
            if (plugin.Config.RagdollCleanupDelay > 0)
                plugin.Coroutines.Add(Timing.RunCoroutine(RagdollCleanup()));
            
            if (plugin.Config.ItemCleanupDelay > 0)
                plugin.Coroutines.Add(Timing.RunCoroutine(ItemCleanup()));
            
            if (plugin.Config.DisarmSwitchTeams)
                plugin.Coroutines.Add(Timing.RunCoroutine(BetterDisarm()));
        }

        private IEnumerator<float> BetterDisarm()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(1.5f);

                foreach (Player player in Player.List)
                {
                    if (!player.IsCuffed || player.Team != Team.CDP || player.Team != Team.MTF || Vector3.Distance(player.Position, EscapeZone) > 20f)
                        continue;

                    switch (player.Role)
                    {
                        case RoleType.FacilityGuard:
                        case RoleType.NtfPrivate:
                        case RoleType.NtfSergeant:
                        case RoleType.NtfCaptain:
                        case RoleType.NtfSpecialist:
                            plugin.Coroutines.Add(Timing.RunCoroutine(DropItems(player, player.Items)));
                            player.Role = RoleType.ChaosConscript;
                            break;
                        case RoleType.ChaosConscript:
                        case RoleType.ChaosMarauder:
                        case RoleType.ChaosRepressor:
                        case RoleType.ChaosRifleman:
                            plugin.Coroutines.Add(Timing.RunCoroutine(DropItems(player, player.Items)));
                            player.Role = RoleType.NtfPrivate;
                            break;
                    }
                }
            }
        }

        public IEnumerator<float> DropItems(Player player, IEnumerable<Item> items)
        {
            yield return Timing.WaitForSeconds(1f);

            foreach (Item item in items)
                item.Spawn(player.Position, default);
        }

        public void OnWaitingForPlayers()
        {
            if (plugin.Config.TimedBroadcastDelay > 0)
                plugin.Coroutines.Add(Timing.RunCoroutine(ServerBroadcast()));
            
            Warhead.IsLocked = false;
        }
        
        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            foreach (CoroutineHandle coroutine in plugin.Coroutines)
                Timing.KillCoroutines(coroutine);
            plugin.Coroutines.Clear();
        }

        private IEnumerator<float> ServerBroadcast()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(plugin.Config.TimedBroadcastDelay);
                
                Map.Broadcast(plugin.Config.TimedBroadcastDuration, plugin.Config.TimedBroadcast);
            }
        }

        private IEnumerator<float> ItemCleanup()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(plugin.Config.ItemCleanupDelay);

                foreach (ItemPickupBase item in Object.FindObjectsOfType<ItemPickupBase>())
                    if (!plugin.Config.ItemCleanupOnlyPocket || item.NetworkInfo.Position.y < -1500f)
                        item.DestroySelf();
            }
        }

        private IEnumerator<float> RagdollCleanup()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(plugin.Config.RagdollCleanupDelay);
                
                foreach (Ragdoll ragdoll in Map.Ragdolls)
                    if (!plugin.Config.RagdollCleanupOnlyPocket || ragdoll.Position.y < -1500f)
                        ragdoll.Delete();
            }
        }

        private IEnumerator<float> AutoNuke()
        {
            yield return Timing.WaitForSeconds(plugin.Config.AutonukeTime);
            
            Warhead.Start();

            if (plugin.Config.AutonukeLock)
                Warhead.IsLocked = true;
        }

        public void OnRestartingRound()
        {
            foreach (CoroutineHandle coroutine in plugin.Coroutines)
                Timing.KillCoroutines(coroutine);
            plugin.Coroutines.Clear();
        }
    }
}