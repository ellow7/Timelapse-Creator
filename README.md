# Timelapse-Creator
Creates a timelapse .mp4 of .jpg images.
Automatically removes "dark" images.

The timelapse gets created with FFMPEG https://ffmpeg.org
It is wrapped in Accord.NET Framework http://accord-framework.net

For capturing I'm using https://github.com/motioneye-project/motioneyeos on a raspi 4 for taking a still image every minute in the file format "%Y-%m-%d/%Y-%m-%d.%H-%M-%S".
I let it run for half a year (~250.000 images). I then decided to only use every 5th image, because it was too detailed for my needs. 
Removing dark images takes 30min and creating the timelapse 5min on my machine.

