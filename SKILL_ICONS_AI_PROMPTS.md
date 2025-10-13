# 🎨 AI Промпты для генерации иконок скиллов

> **Для использования в:** Midjourney, DALL-E 3, Stable Diffusion
> **Формат:** 256x256px, белая иконка на чёрном фоне
> **Стиль:** RPG game icons, fantasy, simple design

---

## 📝 ПРОМПТ 1 (Скиллы 1-15)

### Для Midjourney / DALL-E 3:

```
Create 15 RPG skill icons in a grid layout, white icons on black background, 256x256px each, simple fantasy game UI style, isometric view:

1. SHIELD BASH (Warrior) - A large metal shield with impact lines radiating outward, dynamic slam motion, medieval knight shield with cross emblem

2. WHIRLWIND (Warrior) - Spinning tornado with sword silhouettes visible inside the vortex, circular motion lines, sharp blade edges cutting through wind

3. BATTLE CRY (Warrior) - Fierce warrior helmet with sound wave circles emanating from mouth area, aggressive skull design, sonic shout effect

4. CHARGE (Warrior) - Running armored warrior in side profile with speed lines, lance pointed forward, dynamic forward momentum

5. EXECUTE (Warrior) - Two crossed swords clashing with impact sparks, dramatic X-shape composition, finisher attack effect

6. DEFENSIVE STANCE (Warrior) - Sturdy round shield with protective aura, defensive posture, glowing barrier effect around edges

7. FIREBALL (Mage) - Flaming sphere with fire trails, orange and red flames, swirling fire patterns, magical projectile

8. ICE LANCE (Mage) - Sharp crystalline ice spear with frozen mist, icicle shards, blue-white frost effect, piercing projectile

9. LIGHTNING BOLT (Mage) - Jagged electric bolt striking downward, bright energy lines, electrical discharge, Zeus-like thunderbolt

10. MANA SHIELD (Mage) - Mystical circular barrier with arcane runes, magical force field, glowing protective dome, ethereal energy

11. TELEPORT (Mage) - Swirling portal with magical particles, dimensional rift, teleportation circles, mystical gateway effect

12. METEOR (Mage) - Flaming rock falling from above with fire trail, asteroid impact, cosmic destruction, apocalyptic spell

13. POWER SHOT (Archer) - Single arrow with glowing energy trail, charged arrow, powerful projectile, concentrated force

14. MULTI SHOT (Archer) - Three arrows fanned out in spread formation, multiple projectiles, barrage attack, triple shot

15. POISON ARROW (Archer) - Arrow with dripping green toxin, venom drop effect, skull symbol on arrowhead, deadly poison

Style: Clean minimalist design, high contrast white on pure black, game UI icons, fantasy RPG aesthetic, recognizable silhouettes, no text or labels
```

---

## 📝 ПРОМПТ 2 (Скиллы 16-30)

### Для Midjourney / DALL-E 3:

```
Create 15 RPG skill icons in a grid layout, white icons on black background, 256x256px each, simple fantasy game UI style, isometric view:

16. BEAR TRAP (Archer) - Medieval steel jaw trap with sharp teeth, spring mechanism visible, hunting equipment, danger symbol

17. EAGLE EYE (Archer) - Stylized eagle head with glowing eye, bird of prey emblem, enhanced vision, targeting scope effect

18. RAIN OF ARROWS (Archer) - Multiple arrows falling from sky in downward pattern, arrow cluster, barrage from above, covering fire

19. BACKSTAB (Rogue) - Dagger stabbing into back silhouette, sneak attack, critical strike from behind, assassination symbol

20. SUMMON SKELETONS (Rogue) - Three skeletal skulls with glowing eyes, necromancy symbol, undead summoning, bone magic circle

21. SMOKE BOMB (Rogue) - Explosive smoke cloud with fuse, stealth escape, ninja smoke effect, concealment device

22. POISON DAGGER (Rogue) - Curved dagger dripping with toxic green liquid, venomous blade, assassin weapon, lethal poison

23. SHADOW STEP (Rogue) - Dark silhouette with motion blur trail, stealth movement, ninja dash, shadow teleport effect

24. CRITICAL STRIKE (Rogue) - Eye with crosshair and bleeding effect, weak point targeting, critical hit symbol, precision attack

25. HOLY LIGHT (Paladin) - Radiant sunburst with healing rays, divine blessing, golden holy magic, sacred restoration

26. BEAR FORM (Paladin) - Fierce bear head roaring, transformation symbol, primal rage, shapeshifting druid magic

27. DIVINE SHIELD (Paladin) - Holy shield with cross emblem and light aura, sacred protection, angelic barrier, righteous defense

28. RESURRECTION (Paladin) - Phoenix rising from flames, revival symbol, angelic wings, life restoration, holy miracle

29. HAMMER OF JUSTICE (Paladin) - Heavy warhammer striking down, righteous judgment, divine punishment, crushing holy weapon

30. BLESSING (Paladin) - Praying hands with holy light radiating, divine buff, sacred enhancement, spiritual empowerment

Style: Clean minimalist design, high contrast white on pure black, game UI icons, fantasy RPG aesthetic, recognizable silhouettes, no text or labels
```

---

## 🎨 Альтернативный стиль (если нужны цветные иконки)

### ПРОМПТ 1 (Цветной вариант):

```
Create 15 RPG skill icons with vibrant colors on dark background, 256x256px each, fantasy game UI style:

[Те же описания 1-15, но добавить:]
- Warrior skills: Red and orange color scheme with metallic silver
- Mage skills: Blue, purple and cyan magical colors with glowing effects
- Archer skills: Green and brown natural tones with golden highlights

Style: Vibrant game UI icons, League of Legends / Dota 2 aesthetic, glowing effects, fantasy RPG, recognizable at small size
```

### ПРОМПТ 2 (Цветной вариант):

```
Create 15 RPG skill icons with vibrant colors on dark background, 256x256px each, fantasy game UI style:

[Те же описания 16-30, но добавить:]
- Archer skills: Green and brown natural tones with golden highlights
- Rogue skills: Dark purple and toxic green with shadow effects
- Paladin skills: Golden yellow and holy white with divine glow

Style: Vibrant game UI icons, League of Legends / Dota 2 aesthetic, glowing effects, fantasy RPG, recognizable at small size
```

---

## 🔧 Настройки для разных AI

### Midjourney
```
Добавь в конец промпта:
--ar 1:1 --style raw --v 6 --s 250
```

### DALL-E 3
```
Используй промпт как есть, но уточни:
"Generate each icon separately as individual images"
```

### Stable Diffusion
```
Negative prompt: text, letters, numbers, watermark, signature, blurry, low quality, jpeg artifacts

Settings:
- Steps: 30
- CFG Scale: 7
- Sampler: DPM++ 2M Karras
- Size: 512x512 (потом resize до 256x256)
```

---

## 📦 Пакетная генерация

### Вариант 1: По одной иконке (лучшее качество)

Создай 30 отдельных промптов для каждого скилла:

```
[Для каждого скилла отдельно]

Example:
"RPG skill icon: Shield Bash - A large metal shield with impact lines radiating outward, dynamic slam motion, medieval knight shield with cross emblem. White icon on pure black background, 256x256px, simple fantasy game UI style, clean minimalist design, high contrast"
```

### Вариант 2: По классам (5 наборов по 6 иконок)

**Warrior (6 иконок):**
```
Create 6 warrior skill icons: Shield Bash (shield with impact), Whirlwind (tornado with swords), Battle Cry (helmet with sound waves), Charge (running warrior), Execute (crossed swords), Defensive Stance (protective shield). White icons on black, 256x256px each, fantasy RPG UI style, clean design
```

**Mage (6 иконок):**
```
Create 6 mage skill icons: Fireball (flaming sphere), Ice Lance (ice spear), Lightning Bolt (electric strike), Mana Shield (magical barrier), Teleport (portal), Meteor (falling asteroid). White icons on black, 256x256px each, fantasy RPG UI style, clean design
```

**Archer (6 иконок):**
```
Create 6 archer skill icons: Power Shot (charged arrow), Multi Shot (three arrows), Poison Arrow (toxic arrow), Bear Trap (steel trap), Eagle Eye (eagle emblem), Rain of Arrows (arrow cluster). White icons on black, 256x256px each, fantasy RPG UI style, clean design
```

**Rogue (6 иконок):**
```
Create 6 rogue skill icons: Backstab (dagger in back), Summon Skeletons (three skulls), Smoke Bomb (smoke cloud), Poison Dagger (dripping blade), Shadow Step (shadow blur), Critical Strike (targeting eye). White icons on black, 256x256px each, fantasy RPG UI style, clean design
```

**Paladin (6 иконок):**
```
Create 6 paladin skill icons: Holy Light (sunburst), Bear Form (bear head), Divine Shield (holy shield), Resurrection (phoenix), Hammer of Justice (warhammer), Blessing (praying hands). White icons on black, 256x256px each, fantasy RPG UI style, clean design
```

---

## 🎯 Референсы стилей (для AI)

Добавь в промпт если нужен конкретный стиль:

### Lineage 2 style:
```
in the style of Lineage 2 skill icons, detailed fantasy art, Korean MMO aesthetic
```

### World of Warcraft style:
```
in the style of World of Warcraft ability icons, Blizzard game art, thick outlines
```

### League of Legends style:
```
in the style of League of Legends ability icons, Riot Games aesthetic, vibrant colors
```

### Diablo style:
```
in the style of Diablo skill icons, dark gothic fantasy, Blizzard art direction
```

---

## 📝 Детализированные промпты для каждого скилла

### WARRIOR

**1. Shield Bash**
```
RPG skill icon: A massive medieval shield seen from front angle, violent impact motion with radiating shock waves, dented metal surface with battle scars, cross emblem in center, dynamic slam effect. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**2. Whirlwind**
```
RPG skill icon: Violent spinning tornado vortex, multiple sword blades visible inside the swirling wind, circular motion blur, sharp cutting edges, debris particles. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**3. Battle Cry**
```
RPG skill icon: Aggressive horned warrior helmet facing forward, powerful sonic waves emanating from mouth opening, intimidating skull-like face design, sound effect circles expanding outward. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**4. Charge**
```
RPG skill icon: Armored warrior in dynamic running pose, side profile with lowered lance, speed lines showing rapid forward movement, cape flowing behind, aggressive attack stance. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**5. Execute**
```
RPG skill icon: Two large broadswords crossing in dramatic X formation, impact sparks at intersection point, finisher attack symbol, deadly strike effect, powerful blade clash. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**6. Defensive Stance**
```
RPG skill icon: Sturdy round shield with reinforced edges, protective magical aura surrounding it, defensive barrier glow, guardian symbol, impenetrable defense. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

### MAGE

**7. Fireball**
```
RPG skill icon: Blazing fire sphere with swirling flame patterns, trailing fire particles, intense heat waves, orange-red magical flames, destructive projectile. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**8. Ice Lance**
```
RPG skill icon: Sharp crystalline ice spear with frost mist trailing, frozen icicle shards, blue-white frozen energy, piercing ice projectile, sub-zero magic. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**9. Lightning Bolt**
```
RPG skill icon: Jagged lightning strike bolt descending from above, electrical discharge branches, Zeus-style thunderbolt, bright energy crackling, chain lightning effect. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**10. Mana Shield**
```
RPG skill icon: Circular magical barrier with glowing arcane runes inscribed, mystical force field dome, protective energy sphere, ethereal magic defense. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**11. Teleport**
```
RPG skill icon: Swirling dimensional portal with magical particles, arcane teleportation circle, reality rift gateway, mystical transportation effect, spatial magic. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**12. Meteor**
```
RPG skill icon: Massive flaming asteroid falling from sky, long fire trail, cosmic rock with impact trajectory, apocalyptic spell symbol, devastating destruction. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

### ARCHER

**13. Power Shot**
```
RPG skill icon: Single arrow with glowing energy aura, charged projectile with power trail, concentrated force arrow, enhanced shot, deadly precision. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**14. Multi Shot**
```
RPG skill icon: Three arrows spread in fan formation, simultaneous barrage attack, multiple projectile symbol, triple shot pattern, covering fire. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**15. Poison Arrow**
```
RPG skill icon: Arrow with dripping toxic green venom, poison drop effect from tip, skull emblem on arrowhead, deadly toxin symbol, lethal shot. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**16. Bear Trap**
```
RPG skill icon: Medieval steel jaw trap with vicious sharp teeth, visible spring mechanism, dangerous hunting equipment, immobilizing device, painful snare. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**17. Eagle Eye**
```
RPG skill icon: Majestic eagle head in profile with glowing targeting eye, bird of prey symbol, enhanced vision effect, precision aim, hunter's sight. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**18. Rain of Arrows**
```
RPG skill icon: Dozens of arrows falling from above in downward rain pattern, arrow cluster bombardment, covering barrage, sky darkening attack. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

### ROGUE

**19. Backstab**
```
RPG skill icon: Curved dagger plunging into human back silhouette, sneak attack from behind, critical assassination strike, deadly stealth kill. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**20. Summon Skeletons**
```
RPG skill icon: Three menacing skeletal skulls with glowing eye sockets, necromancy summoning circle, undead minion magic, bone resurrection symbol. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**21. Smoke Bomb**
```
RPG skill icon: Exploding smoke grenade with billowing cloud, ninja stealth device, concealment bomb with burning fuse, escape mechanism. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**22. Poison Dagger**
```
RPG skill icon: Wicked curved assassin blade dripping with toxic green poison, venomous weapon, lethal toxin drops, deadly rogue strike. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**23. Shadow Step**
```
RPG skill icon: Dark ninja silhouette with motion blur trail, shadow dash effect, stealth teleportation, rapid shadow movement, phantom step. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**24. Critical Strike**
```
RPG skill icon: Eye with crosshair targeting weak point, bleeding critical effect, precision strike symbol, vulnerability exploit, deadly accuracy. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

### PALADIN

**25. Holy Light**
```
RPG skill icon: Divine sunburst radiating golden healing rays, sacred blessing light, holy restoration magic, divine cure symbol, angelic radiance. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**26. Bear Form**
```
RPG skill icon: Ferocious bear head roaring with open jaws, primal transformation symbol, shapeshifting druid magic, wild animal rage. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**27. Divine Shield**
```
RPG skill icon: Holy kite shield with radiant cross emblem, angelic protective aura, sacred barrier magic, righteous defense symbol, invulnerable protection. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**28. Resurrection**
```
RPG skill icon: Phoenix bird rising from flames with spread wings, revival from death symbol, life restoration magic, holy miracle rebirth. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**29. Hammer of Justice**
```
RPG skill icon: Massive warhammer striking downward with divine force, righteous judgment weapon, crushing holy punishment, divine smite attack. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

**30. Blessing**
```
RPG skill icon: Praying hands with holy light radiating upward, divine buff symbol, sacred enhancement aura, spiritual empowerment, holy grace. White icon on pure black background, 256x256px, game UI style, high contrast, recognizable silhouette
```

---

## 💡 Советы по использованию

1. **Для Midjourney:** Используй короткие промпты (вариант по классам)
2. **Для DALL-E 3:** Используй детализированные промпты (по одной иконке)
3. **Для Stable Diffusion:** Используй промпты средней длины с negative prompts

4. **Постобработка:**
   - Убери фон (если остался серый вместо чёрного)
   - Resize до 256x256px точно
   - Конвертируй в PNG с прозрачностью
   - Увеличь контраст если нужно

5. **Если результат не понравился:**
   - Перегенерируй с другим seed
   - Добавь больше деталей в описание
   - Укажи конкретный референс стиль (WoW, LoL, Diablo)

---

**Удачи в создании иконок! 🎨**
