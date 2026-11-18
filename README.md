# LZF
> A simple C# LZF compression implementation which attempts to minimize memory allocations.

While LZF compression might not have the great compression ratio of some algorithms, it is simple and extremely fast. This makes it great for real-time applications where you want some compression but need to prioritize speed and responsiveness, such as in game development.

Some implementations of LZF unfortunately are a bit careless with memory allocations which could potentially cause some issues if you're on a platforms with limited memory and/or where you're trying to manage and minimize garbage collection. This implementation attempts to address this by allowing the re-use of a single buffer for all work and output.

For more algorithm details, refer to [Marc Lehmann's original release](https://oldhome.schmorp.de/marc/liblzf.html).

## Installation (UPM)

This fork is maintained as a UPM package at `com.chasepettit.lzf`. In your Unity project's `Packages/manifest.json`, add:

```json
{
  "dependencies": {
    "com.chasepettit.lzf": "https://github.com/MUDV587/LZF.git#feature-upm"
  }
}
```

Or install it from the Package Manager via "Add package from git URL".

## Usage

This implementation includes several compress/decompress methods which can be called. However, some of these aren't geared towards minimizing memory allocation and are included primarily for legacy support with older versions of this library.

If you're looking to keep memory allocations as limited as possible, you'll want to use these methods:

```csharp
public static int Compress(byte[] inputBytes, ref byte[] outputBuffer)
public static int Decompress(byte[] inputBytes, ref byte[] outputBuffer)
```

* `inputBytes`: Input bytes to compress/decompress.
* `outputBuffer`: A reference to the buffer where the output will be produced. This will be allocated/resized as necessary, so it's acceptable to initially pass in a null reference.
* `Returns`: The size of the output in bytes. This is **VERY IMPORTANT** to note because this will be smaller than outputBuffer.Length in most cases. A small inconvenience but necessary if you want to avoid unnecessary memory allocations.

There are a few ways to tweak the algorithm to prefer speed, compression, or memory use. See the `Tunable Constants` at the top of the source file for details.

Here's a quick example of using this library to compress some generic input data and write it to a file:

```csharp
// Declare input/output buffers.
byte[] inputBuffer = null;
byte[] outputBuffer = null;

// Open output file.
using(FileStream outputFile = File.OpenWrite(outputFilePath))
{
    // GetInput is some method that fills inputBuffer and returns inputBuffer.Length.
    while(GetInput(ref inputBuffer) > 0)
    {
        // Compress input.
        int compressedSize = CLZF2.Compress(inputBuffer, ref outputBuffer);

        // Write compressed data to file.
        // Note the use of the size returned by Compress and not outputBuffer.Length.
        outputFile.Write(outputBuffer, 0, compressedSize);
    }
}
```

## Fork / 来源

This repository is a fork of [Chaser324/LZF](https://github.com/Chaser324/LZF), restructured as a UPM package (`feature-upm` branch). Original upstream history is preserved.

## Testing

Unit tests live in `Tests/Editor/` (NUnit) and can be run via Unity Test Runner (EditMode). The test suite also passes under plain `dotnet test`.

## License
This code is under the BSD 3-Clause license. This essentially means you can freely use it as long as you include the copyright statements as attribution. See the [LICENSE](LICENSE) file for details.
