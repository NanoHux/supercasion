using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TerrysCasino.Core;

/// <summary>
/// Tracks Competition Points (CP) per player. Weekly reset Monday 00:00 UTC.
/// Pure logic — no s&box dependencies.
/// </summary>
public class CompetitionPointSystem
{
	public const int CpPerHand = 100;

	private readonly ConcurrentDictionary<string, int> _points = new();
	private readonly string _persistPath;
	private DateTime _weekStart;

	public CompetitionPointSystem( string persistPath = null )
	{
		_persistPath = persistPath;
		_weekStart = GetCurrentWeekStart();

		if ( _persistPath != null )
			Load();
	}

	/// <summary>
	/// Award CP to a player. Returns new total.
	/// </summary>
	public int AwardPoints( string playerId, int amount = CpPerHand )
	{
		CheckWeekReset();
		return _points.AddOrUpdate( playerId, amount, ( _, old ) => old + amount );
	}

	/// <summary>
	/// Get a player's current CP.
	/// </summary>
	public int GetPoints( string playerId )
	{
		CheckWeekReset();
		return _points.TryGetValue( playerId, out int pts ) ? pts : 0;
	}

	/// <summary>
	/// Get the full leaderboard, sorted descending by CP.
	/// </summary>
	public List<LeaderboardEntry> GetLeaderboard( int top = 10 )
	{
		CheckWeekReset();
		return _points
			.OrderByDescending( kv => kv.Value )
			.Take( top )
			.Select( ( kv, i ) => new LeaderboardEntry
			{
				Rank = i + 1,
				PlayerId = kv.Key,
				Points = kv.Value
			} )
			.ToList();
	}

	/// <summary>
	/// Get a player's rank (1-based). Returns 0 if not on leaderboard.
	/// </summary>
	public int GetRank( string playerId )
	{
		CheckWeekReset();
		var sorted = _points.OrderByDescending( kv => kv.Value ).ToList();
		for ( int i = 0; i < sorted.Count; i++ )
		{
			if ( sorted[i].Key == playerId )
				return i + 1;
		}
		return 0;
	}

	/// <summary>
	/// Total number of players with CP this week.
	/// </summary>
	public int PlayerCount => _points.Count;

	/// <summary>
	/// Time remaining until weekly reset.
	/// </summary>
	public TimeSpan TimeUntilReset => GetNextWeekStart() - DateTime.UtcNow;

	/// <summary>
	/// Force reset (for testing).
	/// </summary>
	public void Reset()
	{
		_points.Clear();
		_weekStart = GetCurrentWeekStart();
	}

	private void CheckWeekReset()
	{
		var currentWeek = GetCurrentWeekStart();
		if ( currentWeek > _weekStart )
		{
			_points.Clear();
			_weekStart = currentWeek;
			Save();
		}
	}

	private static DateTime GetCurrentWeekStart()
	{
		var now = DateTime.UtcNow;
		int daysToMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
		return now.Date.AddDays( -daysToMonday );
	}

	private static DateTime GetNextWeekStart()
	{
		return GetCurrentWeekStart().AddDays( 7 );
	}

	private void Save()
	{
		if ( _persistPath == null ) return;
		try
		{
			var dir = Path.GetDirectoryName( _persistPath );
			if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
				Directory.CreateDirectory( dir );

			var data = new CpData
			{
				WeekStart = _weekStart.ToString( "o" ),
				Points = _points.ToDictionary( kv => kv.Key, kv => kv.Value )
			};
			File.WriteAllText( _persistPath, JsonSerializer.Serialize( data ) );
		}
		catch ( Exception ex )
		{
			Console.Error.WriteLine( $"[CP] Save failed: {ex.Message}" );
		}
	}

	private void Load()
	{
		if ( _persistPath == null || !File.Exists( _persistPath ) ) return;
		try
		{
			string json = File.ReadAllText( _persistPath );
			var data = JsonSerializer.Deserialize<CpData>( json );
			if ( data?.Points == null ) return;

			var savedWeek = DateTime.Parse( data.WeekStart );
			if ( savedWeek >= GetCurrentWeekStart() )
			{
				foreach ( var kv in data.Points )
					_points[kv.Key] = kv.Value;
				_weekStart = savedWeek;
			}
		}
		catch ( Exception ex )
		{
			Console.Error.WriteLine( $"[CP] Load failed: {ex.Message}" );
		}
	}
}

public class CpData
{
	public string WeekStart { get; set; }
	public Dictionary<string, int> Points { get; set; }
}

public class LeaderboardEntry
{
	public int Rank { get; set; }
	public string PlayerId { get; set; }
	public int Points { get; set; }
}
