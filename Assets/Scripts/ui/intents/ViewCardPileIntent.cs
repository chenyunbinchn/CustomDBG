namespace ui.intents
{
    public readonly struct ViewCardPileIntent
    {
        public readonly EnumCardPileKind Pile;

        public ViewCardPileIntent(EnumCardPileKind pile)
        {
            Pile = pile;
        }
    }
}
