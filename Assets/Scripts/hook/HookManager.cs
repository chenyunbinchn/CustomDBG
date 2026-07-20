using System.Collections.Generic;
using cards.instance;
using gameStates;
using player;

namespace hook
{
    // Note: Gathers EVERY listener on the battlefield, from every kind of host, into one list right
    //       before a Fire. Nothing registers or unregisters: a listener exists exactly as long as the
    //       object holding it, so a dead host simply stops being walked. Order is fixed (players by
    //       index -> their items -> their powers -> their cards, then enemies) so dispatch stays
    //       deterministic for replay and networking.
    public class HookManager
    {
        public List<HookListener> AllHookListeners = new List<HookListener>();

        public void IterateHookListeners(StateManager stateManager)
        {
            AllHookListeners.Clear();

            for (int i = 0; i < stateManager.BattlePlayerStates.Length; i++)
            {
                // Persistent hooks: one list per item the player owns.
                PlayerInfo playerInfo = stateManager.GamePlayerState.PlayerInfos[i];
                for (int itemIndex = 0; itemIndex < playerInfo.Items.Count; itemIndex++)
                {
                    AllHookListeners.AddRange(playerInfo.Items[itemIndex].HookListeners);
                }

                // Battle-scoped powers on the player's battle-side facade.
                AllHookListeners.AddRange(stateManager.BattlePlayerStates[i].HookListeners);

                // Hooks carried by the player's card instances (rebuilt every battle).
                foreach (CardInstance card in stateManager.BattlePlayerStates[i].PileManager.Dictionary.Values)
                {
                    AllHookListeners.AddRange(card.HookListeners);
                }
            }

            for (int i = 0; i < stateManager.BattleState.EnemyList.Count; i++)
            {
                AllHookListeners.AddRange(stateManager.BattleState.EnemyList[i].HookListeners);
            }
        }
    }
}
