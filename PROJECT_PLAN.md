
TERRY'S CASINO
s&box Social Casino Platform

Project Master Plan
Strategy | Design | Development | Delivery
Version 1.0  |  March 2026
CONFIDENTIAL


Table of Contents




1. Project Overview

1.1 Project Name
Terry's Casino (working title) - an s&box social casino aggregation platform.
1.2 Vision
Build a visually polished, free-to-play social casino game within s&box, featuring multiple classic casino mini-games aggregated under one elegant lobby. Players use virtual credits only (no real currency). The goal is to become one of the most popular entertainment experiences on the s&box platform, earning revenue through the Play Fund mechanism.
1.3 Core Principles
Zero Real Money: All gameplay uses virtual credits earned for free or through in-game achievements. No purchases, no pay-to-win.
Premium Feel: Casino-grade visual quality with smooth animations, ambient sound, and atmospheric lighting.
Multiplayer Social: Players see each other in the casino lobby, can sit at tables together, chat, and compete on leaderboards.
AI-First Development: 100% developed using AI tools with zero human hires. Claude Code for all programming, Midjourney/Meshy for assets.
1.4 Platform & Technical Stack
Layer	Technology	Notes
Game Engine	s&box (Source 2)	Scene system + C# Components
Programming	C# 11 / .NET 9	s&box native scripting language
UI System	s&box Razor UI	HTML/CSS-like syntax with C# logic (Blazor-based)
Networking	s&box Built-in	Automatic multiplayer sync, no extra setup
Data Persistence	s&box Leaderboard API + WebSocket	Player stats and credit balances
Asset Format	FBX/OBJ models, PNG/JPG textures	Imported via s&box Asset Browser
Audio	WAV/OGG	s&box SoundEvent system


2. AI Development Toolchain

Every aspect of this project is produced by AI. Below is the definitive tool assignment for each production category.
2.1 Tool Assignment Matrix
Category	Tool	Purpose & Deliverables
Game Logic & Code	Claude Code (CLI)	All C# scripts: Components, game rules, probability engines, UI code, networking logic, state management. Run directly in project directory.
Architecture & Planning	Claude (Web/Desktop)	Game design documents, math models, task breakdown, code review, debugging strategy, prompt engineering for other tools.
UI/UX Design	v0 by Vercel	Generate layout prototypes for casino lobby, game tables, HUD panels. Export HTML/CSS as reference for s&box Razor UI implementation.
2D Art Assets	Midjourney v6.1	Card faces, chip textures, table felt patterns, background art, icons, logos, loading screens. Output at 2048x2048 minimum.
3D Models	Meshy AI	Roulette wheel, dice, chip stacks, card decks, slot machines, furniture. Export as FBX with PBR materials for s&box import.
Sound Effects	ElevenLabs SFX	Chip clinking, dice rolling, roulette spinning, card shuffling, win/lose jingles, ambient casino atmosphere, button clicks.
Animation Reference	Kling (Kuaishou)	Generate reference videos for complex animations: roulette ball physics, card dealing motion, chip tossing arcs. Recreate in s&box via code.
Texture Enhancement	Magnific AI	Upscale and enhance Midjourney outputs to 4K for close-up game elements like card faces and table surfaces.
2.2 Tool Workflow Diagram
Claude (Planning)  →  v0 (UI Mockup)  →  Claude Code (Implementation)
Midjourney (2D)  →  Magnific (Upscale)  →  s&box Asset Import
Meshy (3D Models)  →  s&box ModelDoc  →  Scene Assembly
ElevenLabs (Audio)  →  s&box SoundEvent  →  In-Game Integration


3. Game Modules Specification

3.1 Casino Lobby (Hub Scene)
Description: A 3D casino floor where players spawn as Terry avatars. They can walk around, see other players, and approach different game tables/machines to enter mini-games. This is the main scene that loads on game start.
Element	AI Tool	Specification
3D Environment	Meshy + Claude Code	Casino interior: marble floors, golden pillars, ambient lighting. Meshy generates base props, Claude Code assembles the scene with s&box lighting components.
Player Avatar	s&box Default (Terry)	Use s&box built-in Terry model with standard player controller. Add walking animation and name tag above head.
Game Table Markers	Midjourney + Meshy	Glowing interaction zones in front of each game. Player walks near and presses E to enter the mini-game scene.
Lobby UI (HUD)	v0 + Claude Code	Top bar showing: player name, credit balance, level. Bottom bar with: game list quick-select, settings, leaderboard button.
Background Music	ElevenLabs	Looping ambient casino atmosphere: soft jazz, distant chatter, chip sounds.
Leaderboard Display	Claude Code	A large screen in the lobby showing top 10 players by credit balance, updated in real-time via s&box networking.
3.2 Game Module: Roulette
Component	AI Tool	Deliverable & Standard
3D Roulette Wheel	Meshy	Detailed European roulette wheel model with 37 numbered slots (0-36). PBR materials: polished wood rim, brass fittings, red/black felt slots. Export FBX.
Ball Physics	Claude Code	Realistic ball animation using s&box physics or scripted bezier curve path. Ball must visually decelerate and settle into a numbered slot. The actual result is determined by RNG server-side, animation is cosmetic.
Betting Board UI	v0 + Claude Code	Classic roulette betting layout: numbers grid, red/black, odd/even, dozens, columns. Players click to place chip amounts. Responsive layout scales with screen.
Chip Models	Meshy	5 denominations: 10, 50, 100, 500, 1000 credits. Each chip is a 3D cylinder with embossed value text and distinct color.
Win/Loss Animation	Claude Code + ElevenLabs	Win: chips fly toward player with particle sparkle effect + victory sound. Loss: chips fade out with subtle deflation sound.
RNG Engine	Claude Code	Server-authoritative random number generation. Uniform distribution across 0-36. Seed logged for fairness verification. Payout multipliers: Straight (35:1), Split (17:1), Street (11:1), Corner (8:1), Red/Black (1:1).
3.3 Game Module: Blackjack (21 Points)
Component	AI Tool	Deliverable & Standard
Card Deck Models	Midjourney + Meshy	52-card standard deck. Midjourney generates card face textures (2048x2048 each). Meshy creates a 3D card model with front/back materials.
Card Dealing Animation	Claude Code + Kling	Kling generates reference video of smooth card dealing. Claude Code implements scripted animation: cards slide from shoe to player positions with rotation.
Game Table	Meshy	Semi-circular blackjack table with green felt, betting circles for up to 5 seats, dealer position.
Game Logic	Claude Code	Standard blackjack rules: Hit, Stand, Double Down, Split. Dealer stands on soft 17. 6-deck shoe, reshuffled at 75% penetration. Blackjack pays 3:2.
UI Overlay	v0 + Claude Code	Card values displayed, hand total counter, action buttons (Hit/Stand/Double/Split), bet amount selector, insurance prompt.
Multiplayer	Claude Code	Up to 5 players per table. Each sees others' face-up cards. Dealer logic runs server-side. Spectator mode for full tables.
3.4 Game Module: Dice Guess (Guess the Number)
Component	AI Tool	Deliverable & Standard
3D Dice	Meshy	Two photorealistic dice with rounded corners. PBR materials: ivory white with black/red pips.
Dice Roll Physics	Claude Code	Physically simulated dice throw using s&box Rigidbody. Random initial velocity and angular rotation. Result reads from final resting face via raycast.
Betting UI	v0 + Claude Code	Predict: exact sum (2-12), over/under 7, odd/even, doubles. Visual grid showing all bet options with payout multipliers.
Payout Table	Claude Code	Exact number: varies by probability (6x for 7, 36x for 2/12). Over/Under: 1:1. Odd/Even: 1:1. Doubles: 5:1.
Sound Effects	ElevenLabs	Dice shaking in hand, dice hitting table (wood impact), dice bouncing, result reveal fanfare.
3.5 Game Module: Slot Machine
Component	AI Tool	Deliverable & Standard
Machine Model	Meshy	Classic 3-reel slot machine with lever arm. Retro-modern design with chrome trim and LED lights.
Reel Symbols	Midjourney	8 symbols: Cherry, Lemon, Orange, Plum, Bell, Bar, Lucky 7, Diamond. Flat icon style, vibrant colors, 512x512 each.
Spin Animation	Claude Code	Reels spin vertically with blur effect, decelerate one by one left-to-right. Each reel shows 3 visible symbols.
Payline Logic	Claude Code	Single payline (center row). Triple match pays highest. Two matching + wild counts. RTP target: 92-95%.
Jackpot Effect	Claude Code + ElevenLabs	Triple-7: screen flash, coin rain particle effect, siren sound, credit counter rapid-increment animation.
3.6 Game Module: Baccarat
Component	AI Tool	Deliverable & Standard
Table Model	Meshy	Kidney-shaped baccarat table with red felt, gold trim, marked betting areas for Player/Banker/Tie.
Game Logic	Claude Code	Standard punto banco rules. Third card drawing rules. Player pays 1:1, Banker pays 0.95:1 (5% commission), Tie pays 8:1.
Card Squeeze Animation	Claude Code + Kling	Optional dramatic card reveal: card corners lift slowly to reveal pips. Kling provides motion reference.
Scoreboard	Claude Code	Big road, big eye road, small road, cockroach road pattern displays. Classic baccarat trend tracking.
3.7 Future Modules (Phase 2)
The following games are planned for post-launch expansion:
Game	Type	Priority
Texas Hold'em Poker	Multiplayer card game	High
Sic Bo (Big/Small)	Dice game	Medium
Wheel of Fortune	Spin game	Medium
Craps	Dice table game	Low
Mahjong (Simplified)	Tile game	Low


4. Virtual Credit System

4.1 Credit Economy Design
All credits are virtual, free, and have zero real-world monetary value. The economy is designed to keep players engaged without frustration.
Mechanism	Credits Awarded	Frequency
New Player Bonus	10,000	One-time on first join
Daily Login	1,000	Once per 24 hours
Consecutive Login Streak (7 days)	5,000 bonus	Weekly
Hourly Refresh	500	Every 60 minutes of play
Achievement Unlock	500 - 5,000	Per achievement
Leaderboard Weekly Prize	10,000 - 50,000	Top 10 weekly
Bankruptcy Recovery	2,000	When balance hits 0
4.2 Data Persistence
Player credit balances and statistics are stored using s&box's built-in leaderboard/stats system. For more complex data (game history, achievement progress), a lightweight external WebSocket server can be deployed. Claude Code will implement both approaches depending on data complexity.


5. Development Roadmap (6 Months)

5.1 Phase 1: Foundation (Month 1)
Week	Task	Tool	Deliverable
W1	Project setup, s&box Editor config, Git repo, folder structure	Claude Code	Working empty s&box project with correct .sbproj and directory layout
W1	Design all game math models (RNG, payouts, probabilities)	Claude (Web)	Math specification document for every game module
W2	Build basic player controller: walk, camera, interact system	Claude Code	Player can walk around an empty scene, press E on trigger zones
W2	Create casino lobby scene: floor, walls, basic lighting	Claude Code + Meshy	Explorable 3D casino interior with placeholder props
W3	Implement credit system: balance, daily login, HUD display	Claude Code	Working credit counter on screen, persisted across sessions
W3	Design lobby UI: top bar, game selection menu, settings panel	v0 + Claude Code	Functional HUD overlay in s&box Razor UI
W4	Generate casino asset pack: tables, chairs, lamps, decorations	Meshy + Midjourney	20+ 3D props and texture sets imported into s&box
W4	Generate ambient sound pack: casino atmosphere, button clicks	ElevenLabs	10+ sound effect files integrated as s&box SoundEvents
5.2 Phase 2: Core Games (Month 2-3)
Week	Task	Tool	Deliverable
W5-6	Roulette: 3D wheel, betting board, ball animation, RNG logic	Meshy + Claude Code	Fully playable single-player roulette
W6	Roulette: multiplayer sync, spectator view	Claude Code	Multiple players can bet at same table
W7-8	Blackjack: card models, dealing animation, game logic	Midjourney + Meshy + Claude Code	Fully playable blackjack with AI dealer
W8	Blackjack: multiplayer (5 seats + spectators)	Claude Code	Multi-seat live blackjack table
W9-10	Dice Guess: dice physics, betting UI, payout system	Meshy + Claude Code	Playable dice prediction game
W10	Slot Machine: reel animation, symbol art, payline logic	Midjourney + Meshy + Claude Code	Single-player slot machine with jackpot
W11-12	Baccarat: table, card logic, scoreboard roads	Meshy + Claude Code	Fully playable baccarat with trend display
W12	Integration: all games accessible from lobby via interaction	Claude Code	Scene transitions work, credits carry across games
5.3 Phase 3: Polish & Social (Month 4-5)
Week	Task	Tool	Deliverable
W13-14	Visual polish: lighting passes, material upgrades, post-processing	Claude Code + Midjourney	Casino environment reaches release quality
W14-15	Animation polish: card dealing, chip movement, roulette ball	Claude Code + Kling	All animations feel smooth and satisfying
W15-16	Sound design pass: per-game ambient, win/loss stingers, UI feedback	ElevenLabs	Full audio coverage for every interaction
W16-17	Leaderboard system: global rankings, weekly resets, lobby display	Claude Code	Live leaderboard visible in lobby and via menu
W17-18	Achievement system: 20+ achievements with reward credits	Claude Code	Achievement notifications and progress tracking
W18-19	Player profile: stats page, game history, win/loss ratios	v0 + Claude Code	Detailed player statistics accessible via menu
W19-20	Social features: chat system, emotes, player interaction	Claude Code	Text chat in lobby and at game tables
5.4 Phase 4: Testing & Launch (Month 6)
Week	Task	Tool	Deliverable
W21-22	Internal playtesting: all games, edge cases, multiplayer stress	Manual + Claude	Bug list documented and triaged
W22-23	Bug fixing and performance optimization	Claude Code	Stable build with no critical bugs
W23	Final art pass: thumbnail, screenshots, description text	Midjourney + Claude	Marketing-ready assets for sbox.game listing
W24	Publish to sbox.game, announce on Discord, collect feedback	Manual	Game live and playable by public


6. Quality Standards & Acceptance Criteria

6.1 Visual Quality
Target visual quality: comparable to high-end mobile casino games (e.g., Zynga Poker, Jackpot Party). The casino environment should feel atmospheric with warm lighting, reflective surfaces, and volumetric fog. Game tables should look realistic with physically-based materials.
6.2 Performance Targets
Metric	Target	Minimum Acceptable
FPS (Lobby, 20 players)	60 FPS	45 FPS
FPS (Game Table, 5 players)	60 FPS	55 FPS
Scene Load Time	< 3 seconds	< 5 seconds
Network Latency Impact	< 100ms perceived delay	< 200ms
Memory Usage	< 2 GB RAM	< 3 GB RAM
6.3 Game Fairness
All random number generation must be server-authoritative. Client-side code never determines game outcomes. Every game must document its mathematical expected return rate. RNG must use cryptographically secure random sources available in .NET (System.Security.Cryptography.RandomNumberGenerator or s&box equivalent). Over 10,000 simulated rounds, actual distribution must be within 2% of theoretical probability for each outcome.
6.4 Code Standards
All C# code must follow s&box conventions: Components inherit from Sandbox.Component. Use [Property] attributes for inspector-editable fields. Use OnStart(), OnUpdate(), OnDestroy() lifecycle methods. Networking uses [Sync] and [Broadcast] attributes. Code must include XML documentation comments on all public methods. Maximum file length: 300 lines. If a Component exceeds this, split into sub-components.


7. Project File Structure

The following directory structure must be maintained throughout development:
terrys-casino/
├── .sbproj
├── Assets/
│   ├── Scenes/
│   │   ├── lobby.scene
│   │   ├── roulette.scene
│   │   ├── blackjack.scene
│   │   ├── dice.scene
│   │   ├── slots.scene
│   │   └── baccarat.scene
│   ├── Models/
│   │   ├── Casino/          (lobby props)
│   │   ├── Roulette/        (wheel, table)
│   │   ├── Cards/           (deck, shoe)
│   │   ├── Dice/            (dice models)
│   │   ├── Chips/           (chip denominations)
│   │   └── Slots/           (machine, symbols)
│   ├── Materials/
│   ├── Textures/
│   ├── Sounds/
│   ├── UI/
│   └── Prefabs/
├── Code/
│   ├── Core/              (credit system, player data, scene manager)
│   ├── Lobby/             (lobby controller, interaction zones)
│   ├── Games/
│   │   ├── Roulette/
│   │   ├── Blackjack/
│   │   ├── DiceGuess/
│   │   ├── Slots/
│   │   └── Baccarat/
│   ├── UI/                (Razor UI components)
│   └── Shared/            (utilities, constants, enums)
└── Libraries/


8. Claude Code Workflow Guide

Since Claude Code is the primary development tool, this section defines exactly how to use it for each type of task.
8.1 Initial Project Setup Commands
# Step 1: Navigate to your s&box project directory
cd C:\Users\YourName\s&box\projects\terrys-casino

# Step 2: Launch Claude Code
claude

# Step 3: First prompt to Claude Code
> "Read the .sbproj file and understand this s&box project structure.
>  Then create the folder structure defined in the project plan.
>  Create placeholder .cs files for each game module with basic
>  Component scaffolding that compiles in s&box."
8.2 Prompt Templates for Each Task Type
Task Type	Prompt Template
New Game Module	"Create a complete [GameName] Component for s&box. Include: game state machine, [specific rules], betting logic, credit integration using CreditSystem.AddCredits() and CreditSystem.DeductCredits(). Use [Sync] for networked properties. Add XML doc comments."
UI Panel	"Create a Razor UI panel for [PanelName] in s&box. It should display [elements]. Use s&box's built-in Razor UI system (not standard Blazor). Style with inline CSS targeting s&box's UI framework. Bind to [ComponentName] for live data."
Animation Script	"Write a C# animation controller Component for [animation description]. Use Time.Delta for frame-independent motion. Add [Property] fields for speed, duration, easing. Include start/stop methods callable from other Components."
Bug Fix	"I'm getting this error in s&box: [paste error]. The code is in [filename]. Here's what should happen: [expected behavior]. Fix the issue and explain what was wrong."
Integration	"Connect [GameComponent] to the lobby scene. When player approaches [trigger zone] and presses E, load [game scene]. When player presses Escape in game scene, return to lobby with updated credit balance."
8.3 Quality Checkpoints
After Claude Code generates any significant code block, run these checks:
Check	Method	Pass Criteria
Compilation	Press Play in s&box Editor	No errors in console
Visual	Enter Play mode, test interaction	Expected behavior occurs
Network	Host + join from second instance	State syncs correctly between host and client
Edge Cases	Ask Claude Code to write unit test scenarios	All edge cases handled (e.g., bet > balance, disconnect mid-game)
Performance	Check FPS counter in s&box	Maintains 60 FPS target


9. Cost Estimate (AI-Only Development)

With zero human hires, all costs are AI tool subscriptions and infrastructure.
Tool	Plan	Monthly Cost (USD)	6-Month Total (USD)
Claude Pro (Web + Desktop)	Pro Plan	$20	$120
Claude Code	Max Plan (recommended)	$100	$600
Midjourney	Standard Plan	$30	$180
Meshy AI	Pro Plan	$20	$120
v0 by Vercel	Premium Plan	$20	$120
ElevenLabs	Starter Plan	$5	$30
Kling AI	Standard Plan	$8	$48
Magnific AI	Pro Plan	$39	$234
TOTAL		$242/month	$1,452
Total estimated cost for 6 months of AI-only development: approximately $1,500 USD (¥10,500 RMB). This is less than one month's salary of a single junior developer in a third-tier Chinese city.
Note: Claude Max plan ($100/month) is strongly recommended for Claude Code to avoid usage limits during intensive development sprints. You can start with the Pro plan ($20/month) and upgrade when you hit limits.


10. Risks & Mitigation

Risk	Impact	Mitigation
s&box API changes during development	High	Pin to a stable s&box version. Monitor sbox.game/news for breaking changes. Keep code modular so individual Components can be updated independently.
AI-generated 3D models not meeting quality bar	Medium	Use Meshy for base geometry, then manually adjust materials in s&box Material Editor. Supplement with free assets from s&box Asset Browser.
Claude Code unfamiliar with s&box-specific APIs	High	Feed s&box documentation and API reference pages into Claude Code context. Provide working code examples from official s&box repos (GitHub: Facepunch/sbox-public).
Multiplayer desync issues	Medium	Follow s&box networking best practices: all game logic server-authoritative, use [Sync] attributes, avoid client-side randomness.
Facepunch removes or restricts casino-themed content	Low-Medium	Keep game clearly branded as 'for fun only'. No real currency references in any UI text. Maintain compliance with EULA. Have a backup plan to publish as standalone via export feature.
Player economy inflation	Low	Implement credit sinks: cosmetic unlocks, table minimum increases at higher tiers, seasonal resets with legacy rewards.



LET'S BUILD THIS.