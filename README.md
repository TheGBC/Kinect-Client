V1 of Kinect client research
============================
- No GUI
- Simply sends point cloud to server through regular time intervals


V2 of Kinect client research
============================
- Has GUI
- Operates by reading from a local scanned model and localizes you based on 
 where you are relative to the model
 
To Use V2
---------
- Download https://www.dropbox.com/s/x6zarktbi2xfy8x/earthquake.tsv and put it in the KinectV2/KinectV2/bin/x86/Debug
- Download https://www.dropbox.com/s/v5wsfdsw6cw9ur8/out.txt and put it in the KinectV2/KinectV2/bin/x86/Debug
- Download https://www.dropbox.com/s/nlzh8vvxor8m6e8/poseFinder.txt and put it in the KinectV2/KinectV2/bin/x86/Debug
- Download https://www.dropbox.com/s/770pf1qzd5sju8v/globe.fbx and put it in the KinectV2/KinectV2Content
- Run KinectV2 project (Note, Kinect code is commented out, so you can use it without a kinect, to run with kinect, uncomment all "manager" lines in Game1.cs)
- You should see a spinning globe with bars rotating around it. To move in x, y, z, use digit keys 1-6 to pan in +x, -x, +y, -y, +z, and -z directions respectively.
- To change between bars and tile data, press digit 8.
- To toggle between displaying globe and data and not displaying, press digit 9.
- To toggle between visible overlay and invisible overlay or enable occlusion, uncomment line 81 in GlobeModel.cs and press digit 0
- With kinect running, it will try to match to the "portable overlay". The portable overlay is black box with a slanted top kept in the maclab. The starting position is roughly 2 feet away and 1 foot above the "lowest corner" of the overlay.