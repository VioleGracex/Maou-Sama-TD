# VFX, Animation & Feedback Bible

## 1. Particle Systems (Neon-Gothic Aesthetic)

### A. Combat Particles
- **Physical Hit**: Sharp white directional sparks.
- **Magic Hit**: Circular purple mana ripples expanding from center.
- **Holy Hit**: Vertical gold light beams ("Pillars").
- **Fire/Burn**: Rising orange embers that flicker and fade.
- **Slow/Frost**: Blue crystalline flakes falling slowly.
- **Stun**: Yellow "Static" jagged lines circling the target.

### B. Sovereign Particles (Maou Skills)
- **Abyssal Thunder**: A thick vertical white bolt with blue secondary arcs. Leaves a temporary "Scorched" mark on the ground.
- **Soul Collection**: Wispy blue orbs that float from defeated enemies toward the Authority Bar.

---

## 2. Animation Logic (Tweening & Easing)

### A. Deployment
- **Impact**: When a unit is dropped, the tile "shudders" (TranslateY -5px to 0) and a small shockwave of dust emits.
- **Manifestation**: Unit opacity tweens from 0 to 1 over 0.2s with a slight scale-up bounce.

### B. Floating Text (Physics-Based)
- **Movement**: Text spawns with a random upward velocity (`vx`, `vy`) and is affected by gravity.
- **Scaling**: Critical damage text is 50% larger and uses a "shaking" animation.
- **Fading**: Life cycles should be ~0.8 seconds, fading out exponentially.

---

## 3. UI Animations
- **Bar Depletion**: HP and SP bars should use a "two-layer" approach. The top layer moves instantly; a bottom "ghost" layer (white/red) slowly catches up over 0.5s.
- **Button Feedback**: HoYo-style slanted buttons should have a "shine" sweep on hover and a slight indent (scale 0.95) on click.
