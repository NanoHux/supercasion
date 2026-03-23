using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class DeckTests
{
	[Fact]
	public void NewDeck_Has52Cards()
	{
		var deck = new Deck();
		Assert.Equal( 52, deck.Remaining );
	}

	[Fact]
	public void Deal_DecreasesRemaining()
	{
		var deck = new Deck();
		deck.Shuffle();
		deck.Deal();
		Assert.Equal( 51, deck.Remaining );
	}

	[Fact]
	public void Deal_All52_UniqueCards()
	{
		var deck = new Deck();
		deck.Shuffle();
		var dealt = new HashSet<Card>();
		for ( int i = 0; i < 52; i++ )
		{
			var card = deck.Deal();
			Assert.True( dealt.Add( card ), $"Duplicate card: {card}" );
		}
		Assert.Equal( 52, dealt.Count );
	}

	[Fact]
	public void Deal_ThrowsWhenEmpty()
	{
		var deck = new Deck();
		deck.Shuffle();
		for ( int i = 0; i < 52; i++ )
			deck.Deal();
		Assert.Throws<InvalidOperationException>( () => deck.Deal() );
	}

	[Fact]
	public void Shuffle_ProducesDifferentOrder()
	{
		var deck1 = new Deck( new Random( 42 ) );
		deck1.Shuffle();
		var order1 = deck1.Deal( 52 );

		var deck2 = new Deck( new Random( 123 ) );
		deck2.Shuffle();
		var order2 = deck2.Deal( 52 );

		// Extremely unlikely to be the same with different seeds
		bool allSame = true;
		for ( int i = 0; i < 52; i++ )
		{
			if ( order1[i] != order2[i] )
			{
				allSame = false;
				break;
			}
		}
		Assert.False( allSame, "Two shuffles with different seeds produced identical order" );
	}

	[Fact]
	public void Reset_RestoresFullDeck()
	{
		var deck = new Deck();
		deck.Shuffle();
		deck.Deal( 10 );
		Assert.Equal( 42, deck.Remaining );
		deck.Reset();
		Assert.Equal( 52, deck.Remaining );
	}

	[Fact]
	public void DealMultiple_ReturnsCorrectCount()
	{
		var deck = new Deck();
		deck.Shuffle();
		var cards = deck.Deal( 5 );
		Assert.Equal( 5, cards.Count );
		Assert.Equal( 47, deck.Remaining );
	}
}
