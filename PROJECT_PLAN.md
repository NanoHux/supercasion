# TERRY'S CASINO — Project Plan

## Current Direction: Poker-First MVP

**Updated: 2026-03-23** — Pivoted from 5-game casino to poker-first approach per CEO review and design doc.

### Active Plan

Texas Hold'em with weekly real-money prize competitions ships first. See `docs/designs/poker-v1.md` for the full reviewed and approved design (CEO + Eng + Design CLEARED).

**Phase 1 (Weeks 1-9):** Core Poker MVP
- Hand evaluator, betting state machine, side pot calculator
- Network multiplayer (server-authoritative, s&box [Sync]/[Broadcast])
- Competition Points system (100 CP/hand, weekly prize)
- Poker UI (dark glass + casino gold theme)
- Anti-collusion audit logging
- Lobby with prize screen, spectator mode
- Integration testing + stress testing

**Phase 2 (Weeks 10-12):** Seasonal & Polish
- Seasonal league system (weekly → monthly → seasonal)
- Sound design, animation polish, ambient crowd reactions

**Phase 3 (Future):** Additional Casino Games
- Roulette, Blackjack, Dice, Slots, Baccarat
- These were the original Phase 1 games from v1.0 of this plan

---

## Original Plan (v1.0 — Superseded)

The original 5-game casino plan (Roulette → Blackjack → Dice → Slots → Baccarat over 6 months) has been superseded by the poker-first pivot. The original games are now Phase 3 content, to be built after the poker MVP validates the competition loop with real players.

See git history for the full original plan.
