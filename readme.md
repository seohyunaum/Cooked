# Cooked 🍅

A silly little kitchen escape game made for **USYD GameJam 2026**.

The theme was **Flip the Script**, so we took a cooking game and asked:

> what if the food did not want to be food? 😭

You are a tiny tomato with a big dream: **do not get cooked**.

## Story

Welcome to the kitchen, where everything is shiny, busy, and extremely
concerning if you happen to be an ingredient.

You were supposed to be chopped, cooked, plated, and probably called
"rustic" by someone with a tiny spoon.

Unfortunately for the kitchen, you have other plans.

Roll, wobble, jump, panic, dodge the knife, and somehow make your way to the
trash can before dinner happens to you.

It is not glamorous, but it is freedom. ✨

## How It Fits The Theme

Most cooking games let you play as the chef.

We flipped the script and made you the ingredient instead. Now the kitchen is not
a cute workplace. It is a very dramatic obstacle course, and the tomato is the
hero. 🍽️ -> 🍅

Basically: less "yes chef", more "please no chef".

## Current Gameplay

- Roll around as a brave little tomato 🍅
- Try to reach the trash can and escape 🗑️
- Jump with all the confidence a tomato can reasonably have
- Dodge a very rude knife that follows and chops behind you 🔪
- Do not fall off the counter, because that is very bad news
- If you get chopped, there is tomato juice. Very dramatic.
- Press `R` to restart after winning or losing

## Controls

- `WASD` / arrow keys: roll around
- `Space`: jump
- Right mouse button + mouse movement: orbit the camera
- `R`: restart after the run ends

## Team

Made with care, chaos, and probably snacks by:

- Sindy 🌟
- Afia 🌟
- Qiuyue 🌟

## Project

This is a Unity game prototype for **USYD GameJam 2026**.

Built with **Unity 6000.5.0f1** using URP.

Current scene:

- `Assets/Scenes/skeleton.unity`

Current scripts:

- `tomatoRoll.cs` for rolling, camera-relative movement, speed limiting, and
  jumping
- `tomatoGameplay.cs` for win, lose, grace period, restart, knife hits, and
  tomato juice explosion logic
- `KnifeScript.cs` for the chasing chopping knife
- `CameraFollowBehind.cs` / `StableThirdPersonCamera.cs` for third-person camera
  follow behaviour

Asset folders include:

- Kenney furniture and food kit assets
- Egg, potato, trash can, kitchen objects, and cooking props
- Blood / tomato-juice-style decal assets

## Credits

Created for the **USYD GameJam 2026** theme:

> Flip the Script

Asset credits:

- Kenney Food Kit, CC0
- Kenney Furniture Pack, CC0

Thank you for believing in one small tomato's right to simply vibe. 🍅💛
