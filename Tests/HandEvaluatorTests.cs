using TerrysCasino.Games.Poker;
using Xunit;

namespace TerrysCasino.Tests;

public class HandEvaluatorTests
{
	private static Card C( string s ) => Card.Parse( s );
	private static List<Card> Cards( params string[] ss ) => ss.Select( Card.Parse ).ToList();

	// ─── Hand Rank Detection ───

	[Fact]
	public void RoyalFlush()
	{
		var result = HandEvaluator.Evaluate( Cards( "As", "Ks", "Qs", "Js", "Ts", "3d", "7h" ) );
		Assert.Equal( HandRank.RoyalFlush, result.HandRank );
	}

	[Fact]
	public void StraightFlush()
	{
		var result = HandEvaluator.Evaluate( Cards( "9h", "8h", "7h", "6h", "5h", "Kd", "2c" ) );
		Assert.Equal( HandRank.StraightFlush, result.HandRank );
	}

	[Fact]
	public void StraightFlush_AceLow()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "2h", "3h", "4h", "5h", "Kd", "9c" ) );
		Assert.Equal( HandRank.StraightFlush, result.HandRank );
		Assert.Equal( (int)Rank.Five, result.Kickers[0] ); // 5-high
	}

	[Fact]
	public void FourOfAKind()
	{
		var result = HandEvaluator.Evaluate( Cards( "Kh", "Kd", "Kc", "Ks", "7h", "3d", "2c" ) );
		Assert.Equal( HandRank.FourOfAKind, result.HandRank );
		Assert.Equal( (int)Rank.King, result.Kickers[0] );
		Assert.Equal( (int)Rank.Seven, result.Kickers[1] );
	}

	[Fact]
	public void FullHouse()
	{
		var result = HandEvaluator.Evaluate( Cards( "Qh", "Qd", "Qc", "8s", "8h", "3d", "2c" ) );
		Assert.Equal( HandRank.FullHouse, result.HandRank );
		Assert.Equal( (int)Rank.Queen, result.Kickers[0] );
		Assert.Equal( (int)Rank.Eight, result.Kickers[1] );
	}

	[Fact]
	public void FullHouse_TwoTrips()
	{
		// With 7 cards you can have two trips — best full house should be selected
		var result = HandEvaluator.Evaluate( Cards( "Kh", "Kd", "Kc", "Jh", "Jd", "Jc", "2s" ) );
		Assert.Equal( HandRank.FullHouse, result.HandRank );
		Assert.Equal( (int)Rank.King, result.Kickers[0] );
	}

	[Fact]
	public void Flush()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "Jh", "9h", "6h", "3h", "Kd", "Qc" ) );
		Assert.Equal( HandRank.Flush, result.HandRank );
		Assert.Equal( (int)Rank.Ace, result.Kickers[0] );
	}

	[Fact]
	public void Straight()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ts", "9h", "8d", "7c", "6s", "2h", "3d" ) );
		Assert.Equal( HandRank.Straight, result.HandRank );
		Assert.Equal( (int)Rank.Ten, result.Kickers[0] );
	}

	[Fact]
	public void Straight_AceLow()
	{
		var result = HandEvaluator.Evaluate( Cards( "As", "2h", "3d", "4c", "5s", "Kh", "Jd" ) );
		Assert.Equal( HandRank.Straight, result.HandRank );
		Assert.Equal( (int)Rank.Five, result.Kickers[0] );
	}

	[Fact]
	public void Straight_AceHigh()
	{
		var result = HandEvaluator.Evaluate( Cards( "As", "Kh", "Qd", "Jc", "Ts", "3h", "2d" ) );
		Assert.Equal( HandRank.Straight, result.HandRank );
		Assert.Equal( (int)Rank.Ace, result.Kickers[0] );
	}

	[Fact]
	public void ThreeOfAKind()
	{
		var result = HandEvaluator.Evaluate( Cards( "7h", "7d", "7c", "Ks", "9h", "3d", "2c" ) );
		Assert.Equal( HandRank.ThreeOfAKind, result.HandRank );
		Assert.Equal( (int)Rank.Seven, result.Kickers[0] );
	}

	[Fact]
	public void TwoPair()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "Ad", "8c", "8s", "5h", "3d", "2c" ) );
		Assert.Equal( HandRank.TwoPair, result.HandRank );
		Assert.Equal( (int)Rank.Ace, result.Kickers[0] );
		Assert.Equal( (int)Rank.Eight, result.Kickers[1] );
		Assert.Equal( (int)Rank.Five, result.Kickers[2] ); // kicker
	}

	[Fact]
	public void OnePair()
	{
		var result = HandEvaluator.Evaluate( Cards( "Jh", "Jd", "Ac", "9s", "6h", "3d", "2c" ) );
		Assert.Equal( HandRank.OnePair, result.HandRank );
		Assert.Equal( (int)Rank.Jack, result.Kickers[0] );
	}

	[Fact]
	public void HighCard()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "Kd", "9c", "7s", "4h", "3d", "2c" ) );
		Assert.Equal( HandRank.HighCard, result.HandRank );
		Assert.Equal( (int)Rank.Ace, result.Kickers[0] );
	}

	// ─── Kicker Resolution ───

	[Fact]
	public void Kicker_OnePair_HigherKickerWins()
	{
		var hand1 = HandEvaluator.Evaluate( Cards( "Jh", "Jd", "Ac", "9s", "6h", "3d", "2c" ) );
		var hand2 = HandEvaluator.Evaluate( Cards( "Jc", "Js", "Kc", "9h", "6d", "3c", "2h" ) );
		Assert.True( hand1 > hand2 ); // Ace kicker beats King kicker
	}

	[Fact]
	public void Kicker_TwoPair_HigherKickerWins()
	{
		var hand1 = HandEvaluator.Evaluate( Cards( "Ah", "Ad", "8c", "8s", "Kh", "3d", "2c" ) );
		var hand2 = HandEvaluator.Evaluate( Cards( "Ac", "As", "8h", "8d", "Qh", "3c", "2h" ) );
		Assert.True( hand1 > hand2 ); // King kicker beats Queen kicker
	}

	[Fact]
	public void Kicker_HighCard_SecondKickerBreaksTie()
	{
		var hand1 = HandEvaluator.Evaluate( Cards( "Ah", "Kd", "Qc", "9s", "4h", "3d", "2c" ) );
		var hand2 = HandEvaluator.Evaluate( Cards( "Ac", "Ks", "Jh", "9h", "4d", "3c", "2h" ) );
		Assert.True( hand1 > hand2 ); // Q beats J as third kicker
	}

	// ─── Multi-way Comparison / Split Pot Detection ───

	[Fact]
	public void SplitPot_IdenticalHands()
	{
		// Both players have same straight from the board
		var hand1 = HandEvaluator.Evaluate( Cards( "2h", "3d", "Ts", "9h", "8d", "7c", "6s" ) );
		var hand2 = HandEvaluator.Evaluate( Cards( "2c", "3c", "Ts", "9h", "8d", "7c", "6s" ) );
		Assert.Equal( 0, hand1.CompareTo( hand2 ) );
	}

	[Fact]
	public void FindWinners_SingleWinner()
	{
		var hands = new[]
		{
			HandEvaluator.Evaluate( Cards( "Ah", "Kh", "Qh", "Jh", "Th", "2d", "3c" ) ), // Royal flush
			HandEvaluator.Evaluate( Cards( "9s", "8s", "7s", "6s", "5s", "2d", "3c" ) ), // Straight flush
			HandEvaluator.Evaluate( Cards( "Ac", "Ad", "As", "Kc", "Kd", "2h", "3h" ) ), // Full house
		};
		var winners = HandEvaluator.FindWinners( hands );
		Assert.Single( winners );
		Assert.Equal( 0, winners[0] );
	}

	[Fact]
	public void FindWinners_SplitPot()
	{
		// Both have the same straight from the board
		var board = Cards( "Ts", "9h", "8d", "7c", "6s" );
		var h1Cards = new List<Card> { C( "2h" ), C( "3d" ) };
		h1Cards.AddRange( board );
		var h2Cards = new List<Card> { C( "2c" ), C( "3c" ) };
		h2Cards.AddRange( board );

		var hands = new[]
		{
			HandEvaluator.Evaluate( h1Cards ),
			HandEvaluator.Evaluate( h2Cards ),
		};
		var winners = HandEvaluator.FindWinners( hands );
		Assert.Equal( 2, winners.Count );
		Assert.Contains( 0, winners );
		Assert.Contains( 1, winners );
	}

	[Fact]
	public void FindWinners_ThreeWaySplit()
	{
		var board = Cards( "As", "Kh", "Qd", "Jc", "Ts" );
		var hands = new[]
		{
			HandEvaluator.Evaluate( Cards( "2h", "3d", "As", "Kh", "Qd", "Jc", "Ts" ) ),
			HandEvaluator.Evaluate( Cards( "4h", "5d", "As", "Kh", "Qd", "Jc", "Ts" ) ),
			HandEvaluator.Evaluate( Cards( "6h", "7d", "As", "Kh", "Qd", "Jc", "Ts" ) ),
		};
		var winners = HandEvaluator.FindWinners( hands );
		Assert.Equal( 3, winners.Count );
	}

	// ─── Hand Ranking Order ───

	[Fact]
	public void HandRanking_FullOrder()
	{
		var royalFlush = HandEvaluator.Evaluate( Cards( "As", "Ks", "Qs", "Js", "Ts", "3d", "7h" ) );
		var straightFlush = HandEvaluator.Evaluate( Cards( "9h", "8h", "7h", "6h", "5h", "Kd", "2c" ) );
		var fourKind = HandEvaluator.Evaluate( Cards( "Kh", "Kd", "Kc", "Ks", "7h", "3d", "2c" ) );
		var fullHouse = HandEvaluator.Evaluate( Cards( "Qh", "Qd", "Qc", "8s", "8h", "3d", "2c" ) );
		var flush = HandEvaluator.Evaluate( Cards( "Ah", "Jh", "9h", "6h", "3h", "Kd", "Qc" ) );
		var straight = HandEvaluator.Evaluate( Cards( "Ts", "9h", "8d", "7c", "6s", "2h", "3d" ) );
		var threeKind = HandEvaluator.Evaluate( Cards( "7h", "7d", "7c", "Ks", "9h", "3d", "2c" ) );
		var twoPair = HandEvaluator.Evaluate( Cards( "Ah", "Ad", "8c", "8s", "5h", "3d", "2c" ) );
		var onePair = HandEvaluator.Evaluate( Cards( "Jh", "Jd", "Ac", "9s", "6h", "3d", "2c" ) );
		var highCard = HandEvaluator.Evaluate( Cards( "Ah", "Kd", "9c", "7s", "4h", "3d", "2c" ) );

		Assert.True( royalFlush > straightFlush );
		Assert.True( straightFlush > fourKind );
		Assert.True( fourKind > fullHouse );
		Assert.True( fullHouse > flush );
		Assert.True( flush > straight );
		Assert.True( straight > threeKind );
		Assert.True( threeKind > twoPair );
		Assert.True( twoPair > onePair );
		Assert.True( onePair > highCard );
	}

	// ─── Edge Cases ───

	[Fact]
	public void ExactlyFiveCards()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "Kd", "Qc", "Js", "9h" ) );
		Assert.Equal( HandRank.HighCard, result.HandRank );
	}

	[Fact]
	public void ThrowsOnLessThanFiveCards()
	{
		Assert.Throws<ArgumentException>( () => HandEvaluator.Evaluate( Cards( "Ah", "Kd", "Qc", "Js" ) ) );
	}

	[Fact]
	public void BestCards_ReturnsExactlyFive()
	{
		var result = HandEvaluator.Evaluate( Cards( "Ah", "Kd", "Qc", "Js", "9h", "3d", "2c" ) );
		Assert.Equal( 5, result.BestCards.Count );
	}
}
