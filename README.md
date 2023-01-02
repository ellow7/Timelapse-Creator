# Timelapse-Creator
Creates a timelapse .mp4 of .jpg images.
Automatically removes "dark" images.

The timelapse gets created with FFMPEG https://ffmpeg.org<br>
It is wrapped in Accord.NET Framework http://accord-framework.net

For capturing I'm using https://github.com/motioneye-project/motioneyeos on a raspi 4 capturing from a wansview 1080P IP camera.<br>
MotionEyeOS is configured for taking a still image every minute in the file format "%Y-%m-%d/%Y-%m-%d.%H-%M-%S".<br>
I let it run for half a year (~250.000 images) and this software handles it quite well - although it takes roughly an hour to process.

![Capture](https://user-images.githubusercontent.com/18436406/210234699-53cf1f19-fd08-4a53-816d-028318613e82.PNG)
