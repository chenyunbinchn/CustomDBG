using action;
using action.gameEffectActions;
using combat;
using enums;
using gameEffects;
using tools.assert;

namespace systems
{
    // Note: Translate one Effect into a GameAction and enqueue it. Shared by card play and hook
    //       reactions — both call Translate, only the (source, picked) context differs.
    //       Target types resolve to a concrete EntityId here (Self/User -> source, SelectedEnemy -> picked).
    public static class EffectApi
    {
        public static void Translate(Effect effect, ActionEntityId source, ActionEntityId picked, GameActionManager actionManager)
        {
            GameAction action = null;
            switch (effect.EffectType)
            {
                // Note: EnumEffectType.CostEnergy has NO branch — card cost is a card field (plan A),
                //       synthesized into a CostEnergyAction by BattleCommandApi, never authored as an
                //       effect. The enum value is kept (removing it would shift serialized enum ints in
                //       the existing SO .asset). An authored CostEnergy effect hits the default assert.
                case EnumEffectType.GainEnergy:
                    action = new GainEnergyAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                case EnumEffectType.DealDamage:
                    action = new DamageAction(effect.Value, ResolveTargetId(effect.TargetType, source, picked), actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                case EnumEffectType.DrawCards:
                    action = new DrawCardAction(effect.Value, actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                case EnumEffectType.GainBlock:
                    action = new GainBlockAction(effect.Value, ResolveTargetId(effect.TargetType, source, picked), actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                case EnumEffectType.ApplyStatus:
                    action = new ApplyStatusAction(effect.StatusType, effect.Value, ResolveTargetId(effect.TargetType, source, picked), actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                case EnumEffectType.Exhaust:
                    // Todo: resolve exhaust targets — Self => the played card; selected => via HandCardChooseAction.
                    action = new ExhaustCardAction(effect.TargetType, System.Array.Empty<int>(), actionManager.NextId(), EnumActionStatus.WaitingForExecution);
                    break;
                default:
                    MyAssert.Assert(false, $"Unhandled EffectType: {effect.EffectType}");
                    break;
            }
            MyAssert.Assert(action != null, "Effect can't be translated to action, null action detected!!");
            actionManager.Add(action);
        }

        // Note: Single-target resolution only for now. Self/User -> the effect's source; SelectedEnemy -> the picked target.
        //       Todo: AllEnemy / RandomEnemy / SelectedAlly / AllAllies multi-target resolution.
        private static ActionEntityId ResolveTargetId(EnumTargetType type, ActionEntityId source, ActionEntityId picked)
        {
            switch (type)
            {
                case EnumTargetType.Self:
                case EnumTargetType.User:
                    return source;
                case EnumTargetType.SelectedEnemy:
                    return picked;
                default:
                    return picked; // Todo: multi-target
            }
        }
    }
}
