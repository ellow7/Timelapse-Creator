# Timelapse-Creator
Creates a timelapse .mp4 of .jpg images.
Automatically removes "dark" images.

The timelapse gets created with FFMPEG https://ffmpeg.org<br>
It is wrapped in Accord.NET Framework http://accord-framework.net

For capturing I'm using https://github.com/motioneye-project/motioneyeos on a raspi 4 capturing from a wansview 1080P IP camera.<br>
MotionEyeOS is configured for taking a still image every minute in the file format "%Y-%m-%d/%Y-%m-%d.%H-%M-%S".<br>
I let it run for half a year (~250.000 images) and this software handles it quite well.

Benchmark on my machine:<br>
Preprocess<br>
7200 images (5 days)<br>
1min 20s<br>
<br>
Timelapse of the preprocessed images<br>
2761 images<br>
1min 40s<br>


Example:

![Capture](https://user-images.githubusercontent.com/18436406/210352934-eb90cc82-07ad-4973-bc42-e3aad9c0dbb2.PNG)

![Capture](https://user-images.githubusercontent.com/18436406/210350452-dd825c01-d74b-4649-8638-c8a2449c620b.PNG)
