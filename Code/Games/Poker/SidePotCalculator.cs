namespace TerrysCasino.Games.Poker;

/// <summary>
/// A single pot (main or side) with the chips and eligible player indices.
/// </summary>
public class Pot
{
	public int Amount { get; set; }
	public List<int> EligiblePlayers { get; } = new();
}

/// <summary>
/// Calculates main pot and side pots when players go all-in with different chip amounts.
/// Pure logic — no s&box dependencies.
/// </summary>
public static class SidePotCalculator
{
	/// <summary>
	/// Given each player's total contribution to the pot, calculate main and side pots.
	/// Folded players should have their contributions included but NOT be in eligible lists.
	/// </summary>
	/// <param name="contributions">Total chips each player put in this hand (index = player seat).</param>
	/// <param name="foldedPlayers">Set of player indices who folded (not eligible to win).</param>
	/// <returns>List of pots, from main pot to highest side pot.</returns>
	public static List<Pot> Calculate( IReadOnlyList<int> contributions, IReadOnlySet<int> foldedPlayers )
	{
		var pots = new List<Pot>();

		// Get sorted unique contribution levels from non-folded players who contributed > 0
		var allContributions = new int[contributions.Count];
		for ( int i = 0; i < contributions.Count; i++ )
			allContributions[i] = contributions[i];

		// Get distinct non-zero contribution levels, sorted ascending
		var levels = allContributions
			.Where( c => c > 0 )
			.Distinct()
			.OrderBy( c => c )
			.ToList();

		if ( levels.Count == 0 )
			return pots;

		int previousLevel = 0;

		foreach ( int level in levels )
		{
			int increment = level - previousLevel;
			if ( increment <= 0 )
				continue;

			var pot = new Pot();

			for ( int i = 0; i < contributions.Count; i++ )
			{
				if ( allContributions[i] >= increment )
				{
					pot.Amount += increment;
					allContributions[i] -= increment;

					// Only non-folded players who contributed at this level are eligible
					if ( !foldedPlayers.Contains( i ) && contributions[i] >= level )
						pot.EligiblePlayers.Add( i );
				}
				else if ( allContributions[i] > 0 )
				{
					pot.Amount += allContributions[i];
					allContributions[i] = 0;
				}
			}

			if ( pot.Amount > 0 )
				pots.Add( pot );

			previousLevel = level;
		}

		return pots;
	}

	/// <summary>
	/// Simplified overload: all players who contributed are eligible (no one folded).
	/// </summary>
	public static List<Pot> Calculate( IReadOnlyList<int> contributions )
	{
		return Calculate( contributions, new HashSet<int>() );
	}
}
