﻿namespace FF1R
{
  using System;
  using McMaster.Extensions.CommandLineUtils;
  using RomUtilities;

  using FF1Lib;

  /// <summary>
  /// Represents a set of randomization settings to supply
  /// to ROM generation.
  /// </summary>
  struct RandomizerSettings {
    public Blob Seed { get; }
    public Flags Flags { get; }

    public RandomizerSettings(Blob seed, Flags flags)
    {
      Seed = seed;
      Flags = flags;
    }

    public RandomizerSettings(string seed, string flags)
      : this(
        String.IsNullOrEmpty(seed) ? Blob.Random(4) : Blob.FromHex(seed),
        Flags.DecodeFlagsText(flags)
      ) { }

    public static RandomizerSettings FromImportString(string import)
    {
      var seed = import.Substring(0, 8);
      var flags = import.Substring(9);

      return new RandomizerSettings(seed, flags);
    }
  }

  /// <summary>
  /// Represents versioning information for a module or program.
  /// </summary>
  struct VersionInfo {
    public int Major { get; }
    public int Minor { get; }
    public int Revision { get; }
    public string Tag { get; }

    public VersionInfo(int major, int minor = 0, int revision = 0, string tag = null)
    {
      Major = major;
      Minor = minor;
      Revision = revision;
      Tag = tag;
    }

    public override string ToString()
    {
      var semantics = $"{Major}.{Minor}.{Revision}";

      return string.IsNullOrEmpty(Tag)
        ? semantics
        : $"{semantics}-{Tag}";
    }
  }


  [Command(Name = "FF1R", Description = "Final Fantasy (NES) Randomizer")]
	class Program
	{
    readonly VersionInfo version = new VersionInfo(0, 1);

    public static int Main(string[] args)
      => CommandLineApplication.Execute<Program>(args);

    [Argument(0, Description = "Final Fantasy ROM to randomize")]
    [FileExists]
    public string RomPath { get; }

    [Option(Description = "File path for the generated ROM",
        ShortName = "o")]
    public string OutFile { get; }

    [Option(Description = "8 Character Hexadecimal string to use as a seed",
        ShortName = "s")]
    public string Seed { get; }

    [Option("-f|--flags <FLAGS>", Description = "Base64 encoded FFR flag string")]
    public string FlagString { get; }

    // Will take precedence over the above Seed and Flags if present. 
    [Option(Description = "SEED_FLAGS string, as generated by crimbot",
      ShortName = "i")]
    public string Import { get; }

    [Option(Description = "Enable verbose output",
        ShortName = "v")]
    public bool Verbose { get; }

    [Option("--version", Description = "Show version")]
    public bool Version { get; }


    void OnExecute()
    {
      if (Version) {
        Console.WriteLine(version);
        return;
      }

      var settings = String.IsNullOrEmpty(Import)
        ? new RandomizerSettings(Seed, FlagString)
        : RandomizerSettings.FromImportString(Import);


      var outFile = String.IsNullOrEmpty(OutFile)
        ? GenerateDefaultFilename(RomPath, settings)
        : OutFile;

			var rom = new FF1Rom(RomPath);
			rom.Randomize(settings.Seed, settings.Flags);
			rom.Save(outFile);

      if (Verbose) Console.WriteLine(outFile);
    }

    string GenerateDefaultFilename(string rom, RandomizerSettings settings)
    {
			var baseName = rom.Substring(0, rom.LastIndexOf(".", StringComparison.InvariantCulture));
			return $"{baseName}_" +
        $"{settings.Seed.ToHex()}_" +
        $"{Flags.EncodeFlagsText(settings.Flags)}.nes";
    }
  }
}
