using cards.instance;

namespace ui.intents
{
    // UI intent requesting a read-only card detail view.
    public readonly struct ViewCardDetailIntent
    {
        public CardInstanceId Card { get; }

        public ViewCardDetailIntent(CardInstanceId card)
        {
            Card = card;
        }
    }
}
