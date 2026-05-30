using card;

namespace gameStates
{
    public class DeckState
    { 
        // Todo: Check if it is good to use arrays like these to split out decks
        // Todo: Is it possible/better to only use 1 card pool...?
        public CardInstance[] DrawDeck;
        public CardInstance[] ExhaustedDeck;
        public CardInstance[] HandDeck;
        public CardInstance[] DiscardDeck;
        
        // Note: All card has innate enchantment will go to InnateDeck, while drawing card, draw from InnateDeck first
        // Check if this makes sense.
        public CardInstance[] InnateDeck; 
    }
}