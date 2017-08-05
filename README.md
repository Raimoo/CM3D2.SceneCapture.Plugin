# CM3D2.SceneCapture.Plugin
This plugin allows you to configure camera effects and lighting, and to save entire presets for both.

## Installation
* Place `CM3D2.SceneCapture.Plugin.dll` into your `UnityInjector` directory.
* Place the contents of the `Config` folder into your `UnityInjector/Config` directory. This is required for the shader injection to work.

## Building
If you use Sybaris and the repository folder is inside the `UnityInjector` folder of the Sybaris install directory, you should be able to build by running `compile.bat`. Otherwise you'll have to edit the path references in `compile.bat` to match your folder structure.

## Usage
These are the default keybindings, but they can be edited in the `UnityInjector/Config/SceneCapture.ini` config file.

| Key | Function                |
|-----|-------------------------|
| Z   | Open Effects Window     |
| X   | Open Environment Window |
| C   | Open Save/Load Window   |

## Bugs
- The `Threshold` parameter of the Bloom effect isn't preserved for some reason. Adjusting it after loading will allow you to set it to the saved value.