# Unisky
A [Bluesky](https://bsky.app) client for Windows 10 & Windows 10 Mobile. Built with [FishyFlip](https://drasticactions.github.io/FishyFlip/)! 

## Downloads
<a href="https://apps.microsoft.com/detail/9mxts7g6fchx?mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

See the [latest release](https://github.com/UnicordDev/UniSky/releases)!

Requires Windows 10 build 15063 or later, please make sure your device is up to date!

## News
I post updates semi-frequently on BlueSky itself! Follow me [@wamwoowam.co.uk](https://bsky.app/profile/wamwoowam.co.uk) to stay updated.

## Building
### Prerequisites
- Windows 10 22H1, Windows 11+
- Windows 11 SDK Build 22000
- Visual Studio 2022, with the Universal Windows Platform workload

### Building and Installing
Firstly, as with all GitHub projects, you'll want to clone the repo, but you will also need to pull submodules, to do this, use:

```sh
$ git submodule update --init --recursive
```

From here, building should be as simple as double clicking `UniSky.sln`, ensuring your targets are appropriate to your testing platform (e.g. Debug x64), and hitting F5. 

## Testing
Unisky currently lacks any kind of unit testing. This will likely change as I adopt a more sane workflow, but for now, I suggest going around the app and making sure everything you'd use regularly works, and ensuring all configurations build. A handy way of doing this, is Visual Studio's Batch Build feature, accessible like so:

![batch build](https://i.imgur.com/8bvkRRv.png)

On one specific note, while the project technically targets a minimum of Windows 10 version 1709 (Fall Creators Update), all code should compile and run on version 170**3** (Creators Update) to maintain Windows Phone support. Please pay special attention to the minimum required Windows version when consuming UWP APIs, and be careful when consuming .NET Standard 2.0 APIs, which may require a newer Windows version.

## Contributing
Unisky accepts contributions! Want a feature that doesn't already exist? Feel free to dig right in and give it a shot. Do be mindful of other ongoing projects, make sure someone isn't already building the feature you want, etc. If you don't have the know how yourself, file an issue, someone might pick up on it.
