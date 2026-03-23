namespace TerrysCasino.Games.Poker;

/// <summary>
/// Evaluates the best 5-card poker hand from up to 7 cards (2 hole + 5 community).
/// Pure logic — no s&box dependencies.
/// </summary>
public static class HandEvaluator
{
	/// <summary>
	/// Evaluate the best possible hand from the given cards (typically 7: 2 hole + 5 community).
	/// </summary>
	public static HandResult Evaluate( IReadOnlyList<Card> cards )
	{
		if ( cards.Count < 5 )
			throw new ArgumentException( "Need at least 5 cards to evaluate a hand." );

		HandResult best = default;
		bool first = true;

		// Try all C(n, 5) combinations
		for ( int i = 0; i < cards.Count - 4; i++ )
		for ( int j = i + 1; j < cards.Count - 3; j++ )
		for ( int k = j + 1; k < cards.Count - 2; k++ )
		for ( int l = k + 1; l < cards.Count - 1; l++ )
		for ( int m = l + 1; m < cards.Count; m++ )
		{
			var five = new[] { cards[i], cards[j], cards[k], cards[l], cards[m] };
			var result = EvaluateFive( five );
			if ( first || result > best )
			{
				best = result;
				first = false;
			}
		}

		return best;
	}

	/// <summary>
	/// Compare multiple hand results and return indices of winners (handles ties/splits).
	/// </summary>
	public static List<int> FindWinners( IReadOnlyList<HandResult> hands )
	{
		if ( hands.Count == 0 )
			return new List<int>();

		var winners = new List<int> { 0 };
		var best = hands[0];

		for ( int i = 1; i < hands.Count; i++ )
		{
			var cmp = hands[i].CompareTo( best );
			if ( cmp > 0 )
			{
				winners.Clear();
				winners.Add( i );
				best = hands[i];
			}
			else if ( cmp == 0 )
			{
				winners.Add( i );
			}
		}

		return winners;
	}

	private static HandResult EvaluateFive( Card[] cards )
	{
		Array.Sort( cards, ( a, b ) => b.Rank.CompareTo( a.Rank ) ); // Descending by rank

		bool isFlush = cards[0].Suit == cards[1].Suit
			&& cards[1].Suit == cards[2].Suit
			&& cards[2].Suit == cards[3].Suit
			&& cards[3].Suit == cards[4].Suit;

		bool isStraight = IsStraight( cards, out int straightHigh );

		var groups = cards
			.GroupBy( c => c.Rank )
			.OrderByDescending( g => g.Count() )
			.ThenByDescending( g => (int)g.Key )
			.ToList();

		int groupCount = groups.Count;
		int largestGroup = groups[0].Count();

		// Royal Flush
		if ( isFlush && isStraight && straightHigh == (int)Rank.Ace )
		{
			return new HandResult( HandRank.RoyalFlush, new[] { straightHigh }, cards.ToList() );
		}

		// Straight Flush
		if ( isFlush && isStraight )
		{
			return new HandResult( HandRank.StraightFlush, new[] { straightHigh }, cards.ToList() );
		}

		// Four of a Kind
		if ( largestGroup == 4 )
		{
			int quadRank = (int)groups[0].Key;
			int kicker = (int)groups[1].Key;
			return new HandResult( HandRank.FourOfAKind, new[] { quadRank, kicker }, cards.ToList() );
		}

		// Full House
		if ( largestGroup == 3 && groups[1].Count() == 2 )
		{
			int tripRank = (int)groups[0].Key;
			int pairRank = (int)groups[1].Key;
			return new HandResult( HandRank.FullHouse, new[] { tripRank, pairRank }, cards.ToList() );
		}

		// Flush
		if ( isFlush )
		{
			var kickers = cards.Select( c => (int)c.Rank ).ToArray();
			return new HandResult( HandRank.Flush, kickers, cards.ToList() );
		}

		// Straight
		if ( isStraight )
		{
			return new HandResult( HandRank.Straight, new[] { straightHigh }, cards.ToList() );
		}

		// Three of a Kind
		if ( largestGroup == 3 )
		{
			int tripRank = (int)groups[0].Key;
			var kickers = groups.Skip( 1 ).Select( g => (int)g.Key ).ToArray();
			return new HandResult( HandRank.ThreeOfAKind, new[] { tripRank }.Concat( kickers ).ToArray(), cards.ToList() );
		}

		// Two Pair
		if ( groupCount == 3 && largestGroup == 2 )
		{
			int highPair = Math.Max( (int)groups[0].Key, (int)groups[1].Key );
			int lowPair = Math.Min( (int)groups[0].Key, (int)groups[1].Key );
			int kicker = (int)groups[2].Key;
			return new HandResult( HandRank.TwoPair, new[] { highPair, lowPair, kicker }, cards.ToList() );
		}

		// One Pair
		if ( largestGroup == 2 )
		{
			int pairRank = (int)groups[0].Key;
			var kickers = groups.Skip( 1 ).Select( g => (int)g.Key ).ToArray();
			return new HandResult( HandRank.OnePair, new[] { pairRank }.Concat( kickers ).ToArray(), cards.ToList() );
		}

		// High Card
		{
			var kickers = cards.Select( c => (int)c.Rank ).ToArray();
			return new HandResult( HandRank.HighCard, kickers, cards.ToList() );
		}
	}

	private static bool IsStraight( Card[] sorted, out int highCard )
	{
		// Normal straight check (sorted descending)
		bool normal = true;
		for ( int i = 0; i < 4; i++ )
		{
			if ( (int)sorted[i].Rank - (int)sorted[i + 1].Rank != 1 )
			{
				normal = false;
				break;
			}
		}

		if ( normal )
		{
			highCard = (int)sorted[0].Rank;
			return true;
		}

		// Ace-low straight (A-2-3-4-5): sorted would be A,5,4,3,2
		if ( sorted[0].Rank == Rank.Ace
			&& sorted[1].Rank == Rank.Five
			&& sorted[2].Rank == Rank.Four
			&& sorted[3].Rank == Rank.Three
			&& sorted[4].Rank == Rank.Two )
		{
			highCard = (int)Rank.Five; // 5-high straight
			return true;
		}

		highCard = 0;
		return false;
	}
}
