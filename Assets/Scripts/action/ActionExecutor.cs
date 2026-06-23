using System.Collections;
using enums;
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

        public void Kick(GameActionManager actionManager)
        {
            if (IsRunning)
            {
                return;
            }

            _host.StartCoroutine(Run(actionManager));
        }

        private IEnumerator Run(GameActionManager actionManager)
        {
            IsRunning = true;
            while (actionManager.ActionQueue.Count > 0)
            {
                GameAction action = actionManager.Pop();
                action.ActionStatus = EnumActionStatus.Executing;
                yield return _host.StartCoroutine(action.Execute());
                action.ActionStatus = EnumActionStatus.Finished;
            }
            IsRunning = false;
        }
    }
}