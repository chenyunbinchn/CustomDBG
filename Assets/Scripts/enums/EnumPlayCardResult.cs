namespace enums
{
    public enum EnumPlayCardResult
    {
        InvalidError = 0,
        Ok,
        NotPlayerTurn,
        CardNotFound,
        CardNotInHand,
        NotEnoughEnergy,
        InvalidPlayer,
        InvalidTarget,
        TargetDead,
        BattleEnded,
        BattleFaulted,
        NoPendingChoice,
        InvalidChoice,
    }
}
