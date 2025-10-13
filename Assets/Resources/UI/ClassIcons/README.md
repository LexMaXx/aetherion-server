# Class Icons for PlayerHUD

This directory contains class icons displayed in the top-left PlayerHUD.

## Required Files

Place the following icon files (70x70px PNG with transparency):

1. **Warrior.png** - Icon for Warrior class
2. **Mage.png** - Icon for Mage class
3. **Archer.png** - Icon for Archer class
4. **Rogue.png** - Icon for Rogue class
5. **Paladin.png** - Icon for Paladin class

## Specifications

- **Format:** PNG with alpha channel (transparency)
- **Size:** 70x70 pixels (or any square size, will be scaled)
- **Style:** Should match the game's art style
- **Location:** `Assets/Resources/UI/ClassIcons/`

## How PlayerHUD Uses These Icons

The `PlayerHUD.cs` script loads icons dynamically based on the player's selected class:

```csharp
Sprite icon = Resources.Load<Sprite>($"UI/ClassIcons/{className}");
```

Where `className` is one of: Warrior, Mage, Archer, Rogue, Paladin

## Fallback Behavior

If an icon is not found, the PlayerHUD will log a warning and display no icon (empty space).

## Unity Import Settings

After adding PNG files, configure them in Unity Inspector:
1. Select the PNG file
2. Set **Texture Type** to "Sprite (2D and UI)"
3. Set **Sprite Mode** to "Single"
4. Click "Apply"

## Current Status

**PLACEHOLDER ICONS INSTALLED** - The current icons (1.png through 5.png from Assets/UI/Icons) are temporary placeholders. Replace them with proper class-specific icons that match your game's art style.

## Source

You can export icons from the character selection screen or create new ones that match the game's visual style.
