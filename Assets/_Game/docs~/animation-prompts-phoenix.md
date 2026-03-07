# Refined Animation Prompts: Phoenix (Reference Style)

Use these frame-by-frame prompts to generate a consistent animation matching the high-detail, fierce style of the reference image.

## Visual Keywords (Reference Style)
- **Subject**: High-detail celestial fire phoenix, intense focus, sharp beak, fierce eyes.
- **Fire FX**: Liquid-like flowing flames, trailing embers, high-contrast orange to golden yellow.
- **Style**: Professional 2D game asset, clean thick lineart (cel-shaded), vibrant and saturated colors.

---

### Phase 0: Conjure (Start)
*Small flames gather and grow into the phoenix silhouette.*

**Frame 1: The Spark**
> **Prompt**: 2D anime sprite of a small flicker of intense white-hot and orange flame on the ground, starting to swirl rapidly, white background, clean lineart.

**Frame 2: The Fire Column**
> **Prompt**: 2D anime sprite of the small flame growing into a swirling pillar of intense liquid fire, hints of wing shapes starting to form within the flames, vibrant orange and gold, white background.

**Frame 3: The Silhouette**
> **Prompt**: 2D anime sprite of the fire pillar expanding into the clear silhouette of a phoenix, beak and eyes glowing through the flames, wings beginning to unfurl, highly detailed fire textures, white background.

---

### Phase 1: Rise (State: `Rise`)
*The phoenix fully forms and ascends.*

**Frame 4: The Emergence**
> **Prompt**: 2D anime sprite of a fire phoenix bursting upwards from a circle of intense flames on the ground, wings partially folded as it pushes up, fierce expression, trail of heavy orange embers, white background, high-detail fire.

**Frame 2: The Ascent**
> **Prompt**: 2D anime sprite of a fire phoenix rising vertically, wings spread wide and flapping upwards, body trailing liquid flames, glowing amber eyes, majestic and powerful pose, white background, vibrant cel-shading.

**Frame 3: The Stabilize**
> **Prompt**: 2D anime sprite of a fire phoenix hovering mid-air vertically, wings at full extension, intense heat distortion aura around it, feathers made of flowing golden fire, white background, sharp lineart.

---

### Phase 2: Dash (State: `Dash`)
*The phoenix tilts and pierces forward with incredible speed.*

**Frame 4: The Tilt (Transition)**
> **Prompt**: 2D anime sprite of a fire phoenix tilting body horizontally, eyes locked forward, wings starting to sweep back, tail of fire growing longer, high-energy preparation pose, white background.

**Frame 5: The Spear Dash**
> **Prompt**: 2D anime sprite of a fire phoenix dashing horizontally at high speed, body streamlined into a spear shape, wings tight to the body, massive cone of white-hot fire at the beak, long trailing streak of flames, white background.

**Frame 6: The Full Flight**
> **Prompt**: 2D anime sprite of a fire phoenix in continuous horizontal flight, wings swept back like a hawk, plumage trailing back as flowing fire, vibrant orange and gold, dynamic action pose, white background.

---

## Technical Setup in Unity
- **Rise State**: Set animator to loop Frame 2-3 during the 1s delay in `PhoenixProjectile.cs`.
- **Dash State**: Trigger Frame 5-6 when movement begins.
