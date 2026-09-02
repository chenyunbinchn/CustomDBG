using cards.instance;

namespace ui.intents
{
    // This intent means the player requested a play; it does not mean the play succeeded.
    public readonly struct PlayCardIntent
    {
        public readonly CardInstanceId Card;

        public PlayCardIntent(CardInstanceId card)
        {
            Card = card;
        }
    }
}
