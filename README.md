NoiseBall5
==========

This is a Unity example project that shows how to use Unity 2019.3 new Mesh API
with C# Job System.

![screenshot](https://i.imgur.com/LVF6qonm.jpg)

The vertices in the Noise Ball object are modified every frame from threaded
jobs that are highly optimized using the Burst compiler. Vertex attributes are
interleaved and stored in a `NativeArray` storage and fed to the graphics APIs
without extra memory copying.

<!--4567890123456789012345678901234567890123456789012345678901234567890123456-->
