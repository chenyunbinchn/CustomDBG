using System.Collections;
using enums;
using gameStates.transient;
using UnityEngine;

namespace action
{
    // Todo: Make my own IEnumerator? Instead of Unity?? 
    public class ActionExecutor
    {
        private readonly MonoBehaviour _host;
        public bool IsRunning;

        public ActionExecutor(MonoBehaviour host)
        {
            _host = host;
        }

        public void Kick(GameActionManager actionManager, BattleState battleState, BattlePlayerState playerState)
        {
            if (IsRunning)
            {
                return;
            }

            _host.StartCoroutine(Run(actionManager, battleState, playerState));
        }

        private IEnumerator Run(GameActionManager actionManager, BattleState battleState, BattlePlayerState playerState)
        {
            IsRunning = true;
            while (actionManager.ActionQueue.Count > 0)
            {
                GameAction action = actionManager.Pop();
                action.ActionStatus = EnumActionStatus.Executing;
                yield return _host.StartCoroutine(action.Execute(battleState, playerState));
                action.ActionStatus = EnumActionStatus.Finished;
            }
            IsRunning = false;
        }
    }
}