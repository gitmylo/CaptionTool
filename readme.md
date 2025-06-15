# Captiontool
Captiontool is a tool for captioning images and videos.

**Warning** - Captiontool is not always user-friendly. Some features may behave differently from expectations. Error messages may not be very clear and updates may break some saved workflows.

## Features
* Persistent workspaces - Save your captions, configure settings, come back at any time
* Captioning
  * Image and video previewing (video crop preview is estimated)
  * Video range selection for cropping from one timestamp until another
  * Create any number of captions per video, each caption has it's own cropping range
  * LLM captioning - Takes a screenshot of the current preview and sends it to an llm with the specified system prompt from settings. Supports any openai compatible endpoints ([openai](https://openai.com/api/), [gemini](https://ai.google.dev/gemini-api/docs/openai), [openrouter](https://openrouter.ai/), [lm studio](https://lmstudio.ai/), [koboldcpp](https://github.com/LostRuins/koboldcpp), [ollama](https://ollama.com/), [text generation webui](https://github.com/oobabooga/text-generation-webui), etc.)
* Workflows (Advanced users only, experimental)
  * Build your own graph to automate captioning
  * Build a node graph going from the current caption and file name to the new caption after running.
  * Workflows can be used either to edit captions, or to generate captions.
  * Supports reading files, have a .txt file with the desired captions? Just load it!
  * Parallel processing, (Not recommended if locally running a vllm for captioning unless your pc can handle it)
  * Nodes for all sorts of use-cases, more nodes coming in future updates. Some examples below:
    * FFprobe - Allows getting video size, duration, framerate
    * LLM request - Provide a chat, optionally with images, and send it to an LLM
    * Read text - Reads a text file at the specified path
* Exporting
  * Automatically converts each video to the desired framerate and crops to the desired section
  * Parallel processing, user-specified thread count
  * Keep track of all runs and view logs

## Workflows
Workflows allow for automating captioning through the use of graph nodes. Just add nodes and connect the inputs.
**Note**: Workflows are currently very early and were just made to do the bare minimum.
Some features that may come to workflows in the future:
* [ ] Widgets for non-connected string, number or bool inputs
* [ ] Optional inputs
* [ ] Dependent types (e.g. make output a string if the input is connected to a string)

**Example**: Reading a .txt file with the same name and using it as the caption
![image](https://github.com/user-attachments/assets/5dabd199-54c7-470d-9b6b-1761c69045c0)


## Requirements
### FFmpeg
FFmpeg needs to be globally installed on the system*. Running `ffmpeg` in cmd/your terminal should give a message if it is installed.  
FFmpeg is also included as a library with the Godot FFmpegPlayer, however this is only used for the previewing of videos inside of godot. The system's FFmpeg is used when exporting and getting media info.

On linux some distros come with FFmpeg pre-installed. And if not, it should usually be available in your package manager.

On windows,
1. download a [windows release of FFmpeg](https://github.com/BtbN/FFmpeg-Builds/releases)
2. make it accessible directly from captiontool
   1. Recommended - Extract to a folder, and add the `bin` folder to the `PATH` environment variable (there are guides online, just search for "windows add folder to path")
   2. Not recommended, untested - Extract the `bin` folder to the folder with `captiontool.exe` in it. This should make ffmpeg accessible to captiontool.exe as it can be found in the same folder.

## Known issues
* Workflow error messages aren't very descriptive
* Workflow saving shortcut doesn't activate when typing in a node's input widget
* Aspect ratios of gifs don't properly load, leading to gifs using the aspect ratio of the previously loaded video

## Credits
[Godot](https://godotengine.org/)  
[FFmpeg](https://ffmpeg.org/) (LGPL)  
[Godot FFmpegPlayer](https://github.com/KirbyKid256/FFmpegPlayer) (MIT)
