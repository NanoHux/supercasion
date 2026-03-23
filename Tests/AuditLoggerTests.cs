using System;
using System.IO;
using TerrysCasino.Core;
using Xunit;

namespace TerrysCasino.Tests;

public class AuditLoggerTests : IDisposable
{
	private readonly string _testDir;
	private readonly AuditLogger _logger;

	public AuditLoggerTests()
	{
		_testDir = Path.Combine( Path.GetTempPath(), $"audit_test_{Guid.NewGuid():N}" );
		_logger = new AuditLogger( _testDir );
	}

	public void Dispose()
	{
		if ( Directory.Exists( _testDir ) )
			Directory.Delete( _testDir, true );
	}

	[Fact]
	public void LogEvent_CreatesDirectory_And_WritesJsonLine()
	{
		Assert.False( Directory.Exists( _testDir ) );

		bool result = _logger.LogEvent( 1, 10, "player1", "Raise", 200 );

		Assert.True( result );
		Assert.True( Directory.Exists( _testDir ) );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		Assert.Single( files );

		string content = File.ReadAllText( files[0] );
		Assert.Contains( "\"HandId\":1", content );
		Assert.Contains( "\"TableId\":10", content );
		Assert.Contains( "\"PlayerId\":\"player1\"", content );
		Assert.Contains( "\"Action\":\"Raise\"", content );
		Assert.Contains( "\"Amount\":200", content );
	}

	[Fact]
	public void LogEvent_MultipleEvents_AppendsToSameFile()
	{
		_logger.LogEvent( 1, 10, "player1", "Raise", 200 );
		_logger.LogEvent( 1, 10, "player2", "Call", 200 );
		_logger.LogEvent( 1, 10, "player1", "Check", 0 );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		Assert.Single( files );

		string[] lines = File.ReadAllLines( files[0] );
		Assert.Equal( 3, lines.Length );
	}

	[Fact]
	public void LogEvent_DailyFileRotation_UsesDateInFilename()
	{
		_logger.LogEvent( 1, 10, "player1", "Fold", 0 );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		Assert.Single( files );

		string expectedDate = DateTime.UtcNow.ToString( "yyyy-MM-dd" );
		Assert.Contains( expectedDate, Path.GetFileName( files[0] ) );
	}

	[Fact]
	public void FlushBuffer_ReturnsZero_WhenNothingBuffered()
	{
		int flushed = _logger.FlushBuffer();
		Assert.Equal( 0, flushed );
	}

	[Fact]
	public void BufferCount_StartsAtZero()
	{
		Assert.Equal( 0, _logger.BufferCount );
	}

	[Fact]
	public void IsHalted_StartsAsFalse()
	{
		Assert.False( _logger.IsHalted );
	}

	[Fact]
	public void LogEvent_IncludesTimestamp()
	{
		_logger.LogEvent( 5, 1, "testplayer", "AllIn", 1000 );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		string content = File.ReadAllText( files[0] );
		Assert.Contains( "\"Timestamp\":", content );
	}

	[Fact]
	public void LogEvent_DifferentActions_AllRecorded()
	{
		string[] actions = { "Fold", "Check", "Call", "Raise", "AllIn" };
		foreach ( var action in actions )
			_logger.LogEvent( 1, 1, "p1", action, 0 );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		string[] lines = File.ReadAllLines( files[0] );
		Assert.Equal( 5, lines.Length );

		foreach ( var action in actions )
			Assert.Contains( lines, l => l.Contains( $"\"Action\":\"{action}\"" ) );
	}

	[Fact]
	public void LogEvent_ReturnsTrue_OnSuccess()
	{
		Assert.True( _logger.LogEvent( 1, 1, "p1", "Fold", 0 ) );
	}

	[Fact]
	public void LogEvent_MultipleTables_RecordTableId()
	{
		_logger.LogEvent( 1, 1, "p1", "Fold", 0 );
		_logger.LogEvent( 2, 2, "p2", "Check", 0 );
		_logger.LogEvent( 3, 3, "p3", "Raise", 500 );

		var files = Directory.GetFiles( _testDir, "audit-*.jsonl" );
		string content = File.ReadAllText( files[0] );
		Assert.Contains( "\"TableId\":1", content );
		Assert.Contains( "\"TableId\":2", content );
		Assert.Contains( "\"TableId\":3", content );
	}
}
