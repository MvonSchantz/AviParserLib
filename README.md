# AviParserLib
AviParserLib is a C# library for parsing AVI files (including OpenDML files).  
Copyright (c) 2024  Mattias von Schantz

The library can, for example, be used for parsing out file and stream information from AVI files without the need to Windows VfW functions. It does not require any codecs installed on the system.

It can also be used for extracting information about all frames in an AVI file, including if the frame is a keyframe, the byte position in the file where the frame data starts, and the size in bytes of the frame.

## API

The library consists of a number of low level APIs for traversing the RIFF structure of an AVI file.

In addition, two high level APIs are available:

### AviFile

The _AviFile_ class can be used for retrieving all elements of an AVI file. Note: This can be quite slow.

#### Example:

    using (var avi = new AviFile(@"file.avi"))
    {
        var listAtoms = avi.Parse();
    }

### AviFileIndex

The _AviFileIndex_ can be used for retrieving file, header and frame information.

#### Example:

    AviFileIndex.Parse(@"file.avi", out var aviHeader, out var streamHeader, out var indexedChunks);
