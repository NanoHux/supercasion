# TODOS

## Infrastructure

### Initialize Git Repository

**What:** Initialize git, create .gitignore for s&box, make initial commit with current codebase.

**Why:** This project has no version control. No git = no rollback, no history, no collaboration. Every line of code written without git is at risk.

**Context:** The project has 4 C# files, 1 Razor file, 2 scenes, and configuration files. All need to be committed. The .gitignore should exclude s&box build outputs, .vs/, and compiled scene files.

**Effort:** S
**Priority:** P0
**Depends on:** None

## Core

### Update PROJECT_PLAN.md for Poker-First Pivot

**What:** Rewrite PROJECT_PLAN.md to reflect the poker-first pivot from the CEO review. Mark original 5-game build order as Phase 2/3 content.

**Why:** Current PROJECT_PLAN.md describes a 5-game casino build order that contradicts the design doc and CEO review decisions. Creates confusion about what to build next.

**Context:** The /office-hours design doc (2026-03-21) and CEO plan review (2026-03-22) both pivot to Texas Hold'em as the lead MVP game with weekly prize competitions. The existing plan's Phase 1 games (Roulette, Blackjack, etc.) become Phase 2 content. See CEO plan at ~/.gstack/projects/supercasino/ceo-plans/2026-03-22-poker-first-mvp.md for the new scope and timeline.

**Effort:** S
**Priority:** P1
**Depends on:** CEO review complete

### Rewrite CreditSystem.cs → CompetitionPointSystem.cs

**What:** Replace the existing daily-login credit system with a Competition Points system (100 CP per hand past flop, weekly reset, leaderboard integration, retry queue for API failures).

**Why:** The existing CreditSystem implements daily login bonus / hourly refresh — a completely different economy model. The poker-first pivot makes it incompatible. CP is the core metric for the weekly prize competition.

**Context:** Current CreditSystem.cs (105 lines) at Code/Core/CreditSystem.cs. New system needs: CP award on hand completion (only if hand reached flop), weekly reset (Monday 00:00 UTC, idempotent), s&box leaderboard API integration with retry queue, "CP pending" HUD state. See CEO plan for full spec.

**Effort:** S
**Priority:** P1
**Depends on:** Week 7 in development timeline

### Delete MyComponent.cs

**What:** Remove the placeholder template file Code/MyComponent.cs (dead code, 10 lines).

**Why:** Dead code creates confusion. This is a template file with no functional purpose.

**Context:** Located at Code/MyComponent.cs. Contains only a template Component with a string property.

**Effort:** S
**Priority:** P3
**Depends on:** None

## Post-MVP

### Cinematic Winner Announcement Replay

**What:** Monday winner highlight reel showing the winning hand replayed on the lobby big screen. Animated replay of the final hand with dramatic camera angles.

**Why:** Creates a "moment" that draws lobby attention and builds social proof. Makes winning feel prestigious. The lobby screen becomes a spectacle.

**Context:** Deferred from CEO review scope. Requires hand replay system (Week 6-7) to be working first. The replay data (state machine snapshots) will already exist — this is purely a cinematic presentation layer.

**Effort:** M
**Priority:** P3
**Depends on:** Hand replay system (Week 6-7)

### Automated Payout System (PayPal API or Stripe)

**What:** Replace manual prize delivery (developer sends gift card/PayPal) with automated payout triggered on weekly leaderboard lock.

**Why:** Manual payout doesn't scale beyond ~20 winners. Automated payout reduces developer overhead and improves winner experience (instant gratification vs. 48-hour wait).

**Context:** MVP uses manual payout: winner DMs developer via Discord, developer sends digital gift card (primary) or PayPal F&F (fallback). Automated payout is the first post-MVP feature, planned for Month 2. Requires: PayPal Payouts API or Stripe Connect, winner identity verification (s&box player ID + Discord), and KYC considerations if prizes exceed $600/year per player.

**Effort:** M
**Priority:** P2
**Depends on:** Successful manual payout flow validation in Week 9

### Create DESIGN.md via /design-consultation

**What:** Run /design-consultation to establish a full DESIGN.md with brand system, typography rationale, color psychology, layout philosophy, and motion spec.

**Why:** The poker-v1.md plan has inline UI design tokens (dark glass + gold casino theme), but a proper DESIGN.md would serve as the single source of truth for visual consistency as the game expands beyond poker into Phase 2 casino games.

**Context:** Current design tokens are defined in poker-v1.md Design Review Decisions section. The gold/black casino aesthetic was established by CreditHud.razor and extended during design review. Best done after MVP launch when the visual language has been validated with real players — don't over-invest in brand system before knowing if the core loop works.

**Effort:** S
**Priority:** P3
**Depends on:** MVP launch, visual language validated with players

## Completed
