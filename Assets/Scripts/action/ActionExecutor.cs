using System.Collections;
using enums;
using tools.assert;
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

        public void Kick(GameActionManager actionManager, BattleActionContext context)
        {
            // Note: Commands only execute at quiescence (BattleCommandApi.Pump), so a Kick can never
            //       overlap a running batch — an overlap means the serial-resolution rule
            //       (《260717-rule-multiplayer-battle-model》 §3) was broken upstream.
            MyAssert.Assert(!IsRunning, "ActionExecutor kicked while running!");
            _host.StartCoroutine(Run(actionManager, context));
        }

        // Note: context.PlayerState = the player whose command produced this action batch. Actions that
        //       implicitly target "the current player" (draw/energy) act on it. Todo: replace with a
        //       richer context when more per-command state is needed for multiplayer (gap-scan 3.2).
        private IEnumerator Run(GameActionManager actionManager, BattleActionContext context)
        {
            IsRunning = true;
            while (actionManager.ActionQueue.Count > 0)
            {
                GameAction action = actionManager.Pop();
                action.ActionStatus = EnumActionStatus.Executing;
                yield return _host.StartCoroutine(action.Execute(context));
                action.ActionStatus = EnumActionStatus.Finished;
            }
            IsRunning = false;
        }
    }
}
